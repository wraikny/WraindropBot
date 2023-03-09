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

      let! dbUsers =
        args.MentionedUsers
        |> Seq.map (fun u -> this.GetUserWithValidName(args.Guild, u.Id))
        |> Seq.toArray
        |> Task.WhenAll

      let! words = dbHandler.GetWords(args.Guild.Id)

      let convertedText =
        let msgBuilder = StringBuilder()

        msgBuilder.Append(msg) |> ignore

        msgBuilder.Replace(Regex("https?://[\w/:%#\$&\?\(\)~\.=\+\-]+"), "")
        |> ignore

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

        for user in dbUsers do
          msgBuilder.Replace($"<@!%d{user.userId}>", $"@%s{user.name}")
          |> ignore

        for _ = 1 to wdConfig.dictionaryReplacementRepeatedCount do
          for (o, n) in wdConfig.primitiveDictionary do
            msgBuilder.Replace(o, n) |> ignore

        msgBuilder.ToString()

      let languageDetector =
        this.ServiceProvider.GetService<LanguageDetector>()

      let languageTranslator =
        this.ServiceProvider.GetService<LanguageTranslator>()

      let! textToSpeak =
        task {
          if
            convertedText.Trim() = ""
            || languageDetector.DetectIsJapanese(convertedText)
          then
            return convertedText
          else
            do! args.Channel.TriggerTypingAsync()
            let! translationResult = languageTranslator.TranslateToJapanese(convertedText)

            match translationResult with
            | Error errorMsg ->
              Utils.logfn "Failed to translate '%s' because %s" convertedText errorMsg
              // do! args.Message.RespondAsync("翻訳に失敗しました。") :> Task
              return convertedText
            | Ok translatedText ->
              let embed =
                DiscordEmbedBuilder(
                  Author = DiscordEmbedBuilder.EmbedAuthor(IconUrl = args.Author.AvatarUrl, Name = author.name),
                  Description = translatedText
                )
                  .Build()

              do! args.Message.RespondAsync(embed) :> Task

              return translatedText
        }

      let textBuilder = StringBuilder()

      textBuilder.Append(author.name).Append(", ")
      |> ignore

      if textToSpeak.Length > wdConfig.speechMaxStringLength + 1 then
        textBuilder
          .Append(textToSpeak.Substring(0, wdConfig.speechMaxStringLength))
          .Append(", 省略")
        |> ignore
      else
        textBuilder.Append(textToSpeak) |> ignore

      textBuilder
        .Replace("\r\n", ". ")
        .Replace("\n", ". ")
        .Replace("\r", ". ")
      |> ignore

      return textBuilder.ToString()
    }
