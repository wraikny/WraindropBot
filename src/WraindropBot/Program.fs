﻿namespace WraindropBot

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
          Utils.logfn "Config loaded from '%s'" path

          let dbConnStr =
            Database.createConnectionStr wdConfig.dbPath

          Utils.logfn "Setting up database"

          do!
            Database.setupDatabase dbConnStr
            |> Async.AwaitTask

          let discordConfig =
            DiscordConfiguration(Token = wdConfig.token, TokenType = TokenType.Bot, AutoReconnect = true)

          use client = new DiscordClient(discordConfig)

          Utils.logfn "Building Services"

          let discordCache = DiscordCache(wdConfig)

          let services =
            ServiceCollection()
              .AddSingleton<WDConfig>(wdConfig)
              .AddSingleton<InstantFields>()
              .AddSingleton<Database.DatabaseHandler>(Database.DatabaseHandler(dbConnStr))
              .AddSingleton<DiscordCache>(discordCache)
              .BuildServiceProvider()

          let commandsConfig =
            CommandsNextConfiguration(
              EnableMentionPrefix = true,
              StringPrefixes = wdConfig.commandPrefixes,
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

          Utils.logfn "Connectiong"

          do! client.ConnectAsync() |> Async.AwaitTask

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
