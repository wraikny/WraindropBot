namespace WraindropBot

open Microsoft.Extensions.DependencyInjection


open System
open System.Diagnostics
open System.Threading.Tasks
open System.IO

open DSharpPlus.VoiceNext
open DSharpPlus.EventArgs


type VoiceHandler(wdConfig: WDConfig, services: ServiceProvider) =
  let instantFields = services.GetService<InstantFields>()

#if OS_WINDOWS
  member private _.TextToBytes(text: string): Task<byte []> =
    task {
      use synth = new Speech.Synthesis.SpeechSynthesizer()
      use stream = new MemoryStream()
      synth.SetOutputToWaveStream(stream)
      synth.Speak(text)
      return stream.ToArray()
    }
#else
#if OS_RASPBIAN
  member private _.TextToBytes(text: string): Task<byte []> =
    task {
      let voiceKind = "f1"
      let speed = 100

      use aquesTalk =
        Process.Start(
          ProcessStartInfo(
            FileName = wdConfig.aquesTalkPath,
            Arguments = $"-f - -v %s{voiceKind} -g %d{wdConfig.volume} -s %d{speed}",
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
#else
  member private _.TextToBytes (wdConfig: WDConfig) (text: string) : Task<byte []> =
    raise <| NotImplementedException()
#endif
#endif

  member private this.TextToVoice (text: string, outStream: VoiceTransmitSink) =
    task {
      let! bytes = this.TextToBytes(text)

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

  member private this.ConvertMessage (args: MessageCreateEventArgs) =
    let msg = args.Message.Content
    let author = args.Author

    if args.Author.IsCurrent
      || args.Author.IsBot
      || msg.StartsWith(wdConfig.commandPrefix)
      || (args.MentionedUsers.Count <> 0
          && args.MentionedUsers
              |> Seq.forall (fun x -> x.IsBot)) then
      None
    else
      let name = author.Username

      let msgBuilder = Text.StringBuilder(msg)

      for role in args.MentionedRoles do
        msgBuilder.Replace($"<@&%d{role.Id}>", role.Name)
        |> ignore

      for ch in args.MentionedChannels do
        msgBuilder.Replace($"<#%d{ch.Id}>", $"#%s{ch.Name}")
        |> ignore

      for user in args.MentionedUsers do
        msgBuilder.Replace($"<@!%d{user.Id}>", $"@%s{user.Username}")
        |> ignore

      Some $"%s{name}, %s{msgBuilder.ToString()}"

  member private this.Speak(msg: string, conn: VoiceNextConnection, args: MessageCreateEventArgs) =
    Utils.handleError
      args.Message.RespondAsync
      (fun () ->
        task {
          while conn.IsPlaying do
            do! conn.WaitForPlaybackFinishAsync()

          Utils.logfn "Speak '%s'" msg

          try
            let txStream = conn.GetTransmitSink()
            do! this.TextToVoice (msg, txStream)
            do! conn.SendSpeakingAsync(true)
            do! txStream.FlushAsync()
            do! conn.WaitForPlaybackFinishAsync()
            do! conn.SendSpeakingAsync(false)
            return Ok()
          with
          | e ->
            do! conn.SendSpeakingAsync(false)
            raise e
            return Ok()
        }
      )

  member this.OnReceived (conn: VoiceNextConnection, args: MessageCreateEventArgs) =
    let messageChannelId = args.Channel.Id
    let registeredChannelId = instantFields.GetChannel(args.Guild.Id)

    if isNull conn |> not && (Some messageChannelId = registeredChannelId) then
      this.ConvertMessage(args)
      |> Option.iter (fun msg ->
        Task.Run(fun () -> this.Speak (msg, conn, args))
        |> ignore
      )

    Task.CompletedTask
