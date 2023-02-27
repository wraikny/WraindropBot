namespace WraindropBot

open Microsoft.Extensions.DependencyInjection

open System
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks

open DSharpPlus
open DSharpPlus.EventArgs
open DSharpPlus.Entities

[<AllowNullLiteral>]
type TextConverter(wdConfig: WDConfig, discordCache: DiscordCache, dbHandler: Database.DatabaseHandler) =

  member val ServiceProvider: ServiceProvider = null with get, set

  member _.GetUserWithValidName(guild: DiscordGuild, userId) =
    task {
      let guildId = guild.Id

      match! dbHandler.GetUser(guildId, userId) with
      | Ok (Some ({ name = Utils.ValidStr _ } as u)) -> return u
      | Ok (Some u) ->
        let! dmember = discordCache.GetDiscordMemberAsync(guild, userId)

        return
          { u with
              name = Utils.getNicknameOrUsername dmember }
      | _ ->
        let! dmember = discordCache.GetDiscordMemberAsync(guild, userId)
        return Database.User.init guildId userId (Utils.getNicknameOrUsername dmember) wdConfig.defaultSpeed
    }

  member this.ConvertTextForSpeeching(author: Database.User, args: MessageCreateEventArgs) =
    task {
      let msg = args.Message.Content

      let dbUsers =
        args.MentionedUsers
        |> Seq.map (fun u -> this.GetUserWithValidName(args.Guild, u.Id))
        |> Seq.toArray

      let msgBuilder = StringBuilder()

      msgBuilder
        .Append(msg)
      |> ignore

      msgBuilder.Replace(Regex("https?://[\w/:%#\$&\?\(\)~\.=\+\-]+"), "URL")
      |> ignore

      let! words = dbHandler.GetWords(args.Guild.Id)

      match words with
      | Error _e -> ()
      | Ok words ->
        for _ = 1 to wdConfig.dictionaryReplacementRepeatedCount do
          for w in words do
            msgBuilder.Replace(w.word, w.replaced) |> ignore

      for role in args.MentionedRoles do
        msgBuilder.Replace($"<@&%d{role.Id}>", role.Name)
        |> ignore

      for ch in args.MentionedChannels do
        msgBuilder.Replace($"<#%d{ch.Id}>", $"#%s{ch.Name}")
        |> ignore

      let! users = Task.WhenAll(dbUsers)

      for user in users do
        msgBuilder.Replace($"<@!%d{user.userId}>", $"@%s{user.name}")
        |> ignore

      for ignoredString in wdConfig.ignoredStrings do
        msgBuilder.Replace(ignoredString, "") |> ignore

      let convertedText = msgBuilder.ToString()

      let mutable textToSpeak = convertedText

      let languageDetector = this.ServiceProvider.GetService<LanguageDetector>()
      let languageTranslator = this.ServiceProvider.GetService<LanguageTranslator>()

      if not <| languageDetector.DetectIsJapanese(convertedText) then
        let! translationResult = languageTranslator.TranslateToJapanese(convertedText)
        match translationResult with
        | Error errorMsg -> Utils.logfn "Failed to translate '%s' because %s" convertedText errorMsg
        | Ok translatedText -> textToSpeak <- translatedText

      let omittedText =
        if textToSpeak.Length > wdConfig.speechMaxStringLength + 1 then
          textToSpeak.Substring(0, wdConfig.speechMaxStringLength) + ", 省略"
        else
          textToSpeak

      return sprintf "%s, %s" author.name omittedText
    }
