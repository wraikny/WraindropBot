module WraindropBot.Voice

open System.Diagnostics
open System.Threading.Tasks
open System.IO
open System.Speech.Synthesis

open DSharpPlus.VoiceNext

let textToVoice (text: string) (outStream: VoiceTransmitSink) =
  task {
    use synth = new SpeechSynthesizer()
    use stream = new MemoryStream()
    synth.SetOutputToWaveStream(stream)
    synth.Speak(text)
    let bytes = stream.ToArray()

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

    let writer =
      task {
        do! ffmpeg.StandardInput.BaseStream.WriteAsync(bytes, 0, bytes.Length)
        ffmpeg.StandardInput.Close()
      }

    let reader =
      task { do! ffmpeg.StandardOutput.BaseStream.CopyToAsync(outStream) }

    let! _ = Task.WhenAll(writer, reader)
    ()
  }

open DSharpPlus
open DSharpPlus.EventArgs


let convertMessage (wdConfig: WDConfig) (args: MessageCreateEventArgs) =
  let msg = args.Message.Content
  let author = args.Author

  if
    args.Author.IsCurrent || args.Author.IsBot
    || msg.StartsWith(wdConfig.command)
  then
    None
  else
    let name = author.Username
    Some $"%s{name}, %s{msg}"


let speak (voice: VoiceNextExtension) (args: MessageCreateEventArgs) (msg: string) =
  Utils.handleError
    args.Message.RespondAsync
    (fun () ->
      task {
        let conn = voice.GetConnection(args.Guild)

        if isNull conn then
          return Ok()

        else
          while conn.IsPlaying do
            do! conn.WaitForPlaybackFinishAsync()

          try
            let txStream = conn.GetTransmitSink()
            let msg = $"%s{args.Author.Username} %s{msg}"
            do! textToVoice msg txStream
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
