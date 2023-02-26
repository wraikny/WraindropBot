namespace WraindropBot

open Microsoft.Extensions.DependencyInjection

open System
open System.Threading.Tasks

open DSharpPlus
open DSharpPlus.Entities
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
          Utils.logfn "Config loaded from '%s'" path

          let dbConnStr =
            Database.createConnectionStr wdConfig.dbPath

          Utils.logfn "Setting up database"

          do!
            Database.setupDatabase dbConnStr
            |> Async.AwaitTask

          // https://github.com/DSharpPlus/DSharpPlus/blob/master/DSharpPlus/DiscordIntents.cs

          let discordConfig =
            DiscordConfiguration(
              Token = wdConfig.token,
              TokenType = TokenType.Bot,
              AutoReconnect = true,
              Intents =
                (DiscordIntents.AllUnprivileged
                 ||| DiscordIntents.MessageContents)
            )

          use client = new DiscordClient(discordConfig)

          Utils.logfn "Building Services"

          let dbHandler = Database.DatabaseHandler(dbConnStr)
          let discordCache = DiscordCache(wdConfig)

          let textConverter =
            TextConverter(wdConfig, discordCache, dbHandler)

          let services =
            ServiceCollection()
              .AddSingleton<WDConfig>(wdConfig)
              .AddSingleton<InstantFields>()
              .AddSingleton<Database.DatabaseHandler>(dbHandler)
              .AddSingleton<DiscordCache>(discordCache)
              .AddSingleton<TextConverter>(textConverter)
              .BuildServiceProvider()

          let commandsConfig =
            CommandsNextConfiguration(
              EnableMentionPrefix = (wdConfig.commandPrefixes |> Array.isEmpty |> not),
              StringPrefixes = wdConfig.commandPrefixes,
              Services = services
            )

          let commands = client.UseCommandsNext(commandsConfig)
          commands.SetHelpFormatter<WDHelpFormatter>()
          commands.RegisterCommands<WDCommands>()

          let voice = client.UseVoiceNext()

          let voiceHandler = new VoiceHandler(wdConfig, services)

          client.add_Ready (fun client _args ->
            task {
              Utils.logfn "Setting Status"

              let activity =
                wdConfig.commandPrefixes
                |> function
                  | [||] -> $"@%s{client.CurrentUser.Username} help"
                  | ps -> $"%s{ps.[0]} help"

              do! client.UpdateStatusAsync(new DiscordActivity(activity))

              Utils.logfn "Clients Ready!"
            }
          )

          client.add_MessageCreated (fun client args ->
            if isNull args.Guild then
              Task.CompletedTask
            else
              let conn = voice.GetConnection(args.Guild)
              voiceHandler.OnReceived(conn, args)
          )

          Utils.logfn "Connectiong"

          do! client.ConnectAsync() |> Async.AwaitTask

          Utils.logfn "Connected"

          while true do
            do! Async.Sleep(wdConfig.requestCacheSeconds)
            discordCache.Clean()

          return 0

      with
      | e ->
        eprintfn "%s" e.Message
        eprintfn "%s" e.StackTrace
        return 1
    }
    |> Async.RunSynchronously
