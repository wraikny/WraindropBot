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

      let msgBuilder = Text.StringBuilder(msg)

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

      let s =
        $"%s{author.name}, %s{msgBuilder.ToString()}"

      if s.Length > wdConfig.speechMaxStringLength + 1 then
        return s.[0..wdConfig.speechMaxStringLength] + ",省略"
      else
        return s
    }
