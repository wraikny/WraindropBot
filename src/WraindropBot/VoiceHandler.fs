namespace WraindropBot

open Microsoft.Extensions.DependencyInjection


open System
open System.Diagnostics
open System.Threading.Tasks
open System.IO
open System.Collections.Concurrent
open System.Runtime.InteropServices
open System.Text.RegularExpressions

open DSharpPlus.VoiceNext
open DSharpPlus.EventArgs
open DSharpPlus.Entities

type VoiceHandler(wdConfig: WDConfig, services: ServiceProvider) =
  let instantFields = services.GetService<InstantFields>()

  let textConverter = services.GetService<TextConverter>()

  let speakingMessageIdDict = ConcurrentDictionary<uint64, uint64>()

  member private _.TextToBytesForWindows(text: string) : Task<byte []> =
    task {
      use synth = new Speech.Synthesis.SpeechSynthesizer()
      use stream = new MemoryStream()
      synth.SetOutputToWaveStream(stream)
      synth.Speak(text)
      return stream.ToArray()
    }

  member private this.TextToBytesForAquesTalk(user: Database.User, text: string) : Task<byte []> =
    task {
      let voiceKind = "f1"

      use aquesTalk =
        Process.Start(
          ProcessStartInfo(
            FileName = wdConfig.aquesTalkPath,
            Arguments = $"-f - -v %s{voiceKind} -g %d{wdConfig.volume} -s %d{user.speakingSpeed}",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
          )
        )

      use output = new MemoryStream()

      let reader =
        task {
          do! aquesTalk.StandardOutput.BaseStream.CopyToAsync(output)
          aquesTalk.StandardOutput.Close()
        }

      let writer =
        task {
          let input = Text.Encoding.UTF8.GetBytes(text)
          do! aquesTalk.StandardInput.BaseStream.WriteAsync(input)
          aquesTalk.StandardInput.Close()
        }

      let! _ = Task.WhenAll(writer, reader)
      return output.ToArray()
    }

  member private this.TextToBytes(_user: Database.User, text: string) : Task<byte []> =
#if OS_RASPBIAN
    this.TextToBytesForAquesTalk(_user, text)
#else

    let os = Environment.OSVersion

    match os.Platform with
    | PlatformID.Win32S
    | PlatformID.Win32Windows
    | PlatformID.Win32NT
    | PlatformID.WinCE -> this.TextToBytesForWindows(text)
    | _ -> raise <| NotImplementedException()
#endif

  member private this.TextToVoice(user: Database.User, text: string, outStream: VoiceTransmitSink) =
    task {
      let! bytes = this.TextToBytes(user, text)

      use ffmpeg =
        Process.Start(
          ProcessStartInfo(
            FileName = "ffmpeg",
            Arguments = "-i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
          )
        )

      let reader =
        task {
          do! ffmpeg.StandardOutput.BaseStream.CopyToAsync(outStream)
          ffmpeg.StandardOutput.Close()
        }

      let writer =
        task {
          do! ffmpeg.StandardInput.BaseStream.WriteAsync(bytes, 0, bytes.Length)
          ffmpeg.StandardInput.Close()
        }

      let! _ = Task.WhenAll(writer, reader)
      ()
    }

  member private this.Speak(user: Database.User, conn: VoiceNextConnection, args: MessageCreateEventArgs, msg: string) =
    Utils.handleError
      args.Message.RespondAsync
      (fun () ->
        task {
          Utils.logfn "Speak '%s'" msg

          try
            while speakingMessageIdDict.GetOrAdd(args.Guild.Id, args.Message.Id) <> args.Message.Id do
              do! Task.Yield()

            let txStream = conn.GetTransmitSink()
            do! this.TextToVoice(user, msg, txStream)
            do! conn.SendSpeakingAsync(true)
            do! txStream.FlushAsync()
            do! conn.WaitForPlaybackFinishAsync()
            do! conn.SendSpeakingAsync(false)

            speakingMessageIdDict.TryRemove(args.Guild.Id)
            |> ignore

            return Ok()
          with
          | e ->
            do! conn.SendSpeakingAsync(false)
            raise e
            return Ok()
        }
      )

  member this.OnReceived(conn: VoiceNextConnection, args: MessageCreateEventArgs) =
    let messageChannelId = args.Channel.Id
    let registeredChannelId = instantFields.GetChannel(args.Guild.Id)

    let _ =
      task {

        if isNull conn |> not
           && (Some messageChannelId = registeredChannelId) then
          let! user = textConverter.GetUserWithValidName(args.Guild, args.Author.Id)
          let! msg = textConverter.ConvertTextForSpeeching(user, args)
          match msg with
          | None -> ()
          | Some msg ->
            Task.Run(fun () ->
              this.Speak(user, conn, args, msg)
            )
            |> ignore
      }

    Task.CompletedTask
