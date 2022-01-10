namespace WraindropBot

open System
open System.Threading.Tasks

open DSharpPlus
open DSharpPlus.VoiceNext
open DSharpPlus.CommandsNext

module Program =
  [<EntryPoint>]
  let main (args) =
    async {
      try
        match Array.tryHead args with
        | None ->
          printfn "Set config path as an args[0]."
          return 1

        | Some path ->
          let! wdConfig = WDConfig.asyncLoad path

          let discordConfig =
            DiscordConfiguration(Token = wdConfig.token, TokenType = TokenType.Bot, AutoReconnect = true)

          use client = new DiscordClient(discordConfig)

          let commandsConfig =
            CommandsNextConfiguration(EnableMentionPrefix = true, StringPrefixes = [ wdConfig.command ])

          let commands = client.UseCommandsNext(commandsConfig)
          commands.RegisterCommands<WDCommands>()

          let voice = client.UseVoiceNext()

          client.add_MessageCreated (fun client args ->
            Voice.convertMessage wdConfig args
            |> Option.iter (fun msg ->
              Task.Run(fun () -> Voice.speak voice args msg)
              |> ignore
            )

            Task.CompletedTask
          )

          do! client.ConnectAsync() |> Async.AwaitTask

          do! Async.Sleep(-1)

          return 0

      with
      | e ->
        eprintfn "%s" e.Message
        eprintfn "%s" e.StackTrace
        return 1
    }
    |> Async.RunSynchronously
