namespace WraindropBot

open Microsoft.Extensions.DependencyInjection

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
          printfn "Set config path to args[0]."
          return 1

        | Some path ->
          let! wdConfig = WDConfig.asyncLoad path

          let discordConfig =
            DiscordConfiguration(Token = wdConfig.token, TokenType = TokenType.Bot, AutoReconnect = true)

          use client = new DiscordClient(discordConfig)

          let services =
            ServiceCollection()
              .AddSingleton<InstantFields>()
              .BuildServiceProvider()

          let commandsConfig =
            CommandsNextConfiguration(
              EnableMentionPrefix = true,
              StringPrefixes = [ wdConfig.commandPrefix ],
              Services = services
            )

          let commands = client.UseCommandsNext(commandsConfig)
          commands.SetHelpFormatter<WDHelpFormatter>()
          commands.RegisterCommands<WDCommands>()

          let voice = client.UseVoiceNext()

          let voiceHandler = new VoiceHandler(wdConfig, services)

          client.add_MessageCreated (fun client args ->
            let conn = voice.GetConnection(args.Guild)
            voiceHandler.OnReceived(conn, args)
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
