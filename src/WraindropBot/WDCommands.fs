namespace WraindropBot

open System
open System.Diagnostics
open System.Text
open System.Threading
open System.Threading.Tasks
open System.ComponentModel
open System.Runtime.CompilerServices


open DSharpPlus
open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open DSharpPlus.Entities
open DSharpPlus.EventArgs
open DSharpPlus.VoiceNext
open DSharpPlus.VoiceNext.EventArgs

open WraindropBot

type WDCommands() =
  inherit BaseCommandModule()

  member val WDConfig: WDConfig = Unchecked.defaultof<_> with get, set
  member val InstantFields: InstantFields = null with get, set
  member val DBHandler: Database.DatabaseHandler = null with get, set
  member val DiscordCache: DiscordCache = null with get, set
  member val TextConverter: TextConverter = null with get, set
  member val LanguageTranslator: LanguageTranslator = null with get, set

  member private _.RespondReadAs(ctx: CommandContext, userId, name: string) =
    ctx.RespondAsync($"<@!%d{userId}>は**%s{ctx.Guild.Name}**で`%s{name}`と読み上げられます。")

  [<Command("name-get");
    Description("サーバーで読み上げる名前を取得します。");
    Aliases([| "ng" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.GetName(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()
          let! user = this.TextConverter.GetUserWithValidName(ctx.Guild, ctx.User.Id)
          let! _ = this.RespondReadAs(ctx, ctx.User.Id, user.name)
          return Ok()
        }
      )

  member private this.SetName(ctx: CommandContext, targetId: uint64, name: string) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let maxLen = this.WDConfig.usernameMaxLength

          match name with
          | null ->
            let! result = this.DBHandler.SetUserName(ctx.Guild.Id, targetId, null)

            match result with
            | Ok _ ->
              let! guildMember = this.DiscordCache.GetDiscordMemberAsync(ctx.Guild, targetId)
              let! _ = this.RespondReadAs(ctx, targetId, Utils.getNicknameOrUsername guildMember)
              return Ok()
            | Error _ -> return Error "処理に失敗しました。"
          | name when (1 <= name.Length && name.Length <= maxLen) ->
            let! result = this.DBHandler.SetUserName(ctx.Guild.Id, targetId, name)

            match result with
            | Ok _ ->
              let! _ = this.RespondReadAs(ctx, targetId, name)
              return Ok()
            | Error _ -> return Error "処理に失敗しました。"
          | _ -> return Error $"名前は1文字以上%d{maxLen}文字以下にしてください。"
        }
      )

  [<Command("name-set");
    Description("サーバーで読み上げる名前を設定します。");
    Aliases([| "ns" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.SetName(ctx: CommandContext, [<Description("読み上げる名前")>] name: string) =
    this.SetName(ctx, ctx.User.Id, name)

  [<Command("name-set-user");
    Description("サーバーで指定したユーザを読み上げる名前を設定します。");
    Aliases([| "nsu" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.SetName
    (
      ctx: CommandContext,
      [<Description("対象のユーザ")>] target: DiscordMember,
      [<Description("読み上げる名前")>] name: string
    ) =
    this.SetName(ctx, target.Id, name)

  [<Command("name-delete");
    Description("サーバーで読み上げる名前を消去します。");
    Aliases([| "nd" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.DeleteName(ctx: CommandContext) = this.SetName(ctx, ctx.User.Id, null)

  [<Command("name-delete-user");
    Description("サーバーで指定したユーザを読み上げる名前を消去します。");
    Aliases([| "ndu" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.DeleteName(ctx: CommandContext, [<Description("対象のユーザ")>] target: DiscordMember) =
    this.SetName(ctx, target.Id, null)

  [<Command("speed-get");
    Description("サーバーでの発話速度を取得します。");
    Aliases([| "sg" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.GetSpeed(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let! user = this.DBHandler.GetUser(ctx.Guild.Id, ctx.User.Id)

          match user with
          | Ok user ->
            let speed =
              user
              |> function
                | Some { speakingSpeed = spd } -> spd
                | _ -> this.WDConfig.defaultSpeed

            let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**で<@!%d{ctx.User.Id}>の発話速度は`%d{speed}`です。")
            return Ok()
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("speed-set");
    Description("サーバーでの発話速度を設定します。(50~300)");
    Aliases([| "ss" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.SetSpeed(ctx: CommandContext, [<Description("発話速度")>] speed: int) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let speed = speed |> WDConfig.validateSpeed
          let! res = this.DBHandler.SetUserSpeed(ctx.Guild.Id, ctx.User.Id, speed)

          match res with
          | Ok _ ->
            let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**で<@!%d{ctx.User.Id}>の発話速度は`%d{speed}`に設定されました。")
            return Ok()
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("dict-list");
    Description("読み上げ時に置換されるワードの一覧を取得します。");
    Aliases([| "dl" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.ListWords(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let! words = this.DBHandler.GetWords(ctx.Guild.Id)

          match words with
          | Ok words ->
            let words = words |> Seq.toArray

            if words.Length = 0 then
              let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**の辞書にワードが登録されていません。")
              return Ok()
            else
              let res = StringBuilder()

              res
                .AppendLine($"**%s{ctx.Guild.Name}**の辞書に登録されているワードの一覧です。")
                .AppendLine("```")
              |> ignore

              for w in words do
                res
                  .Append(w.word)
                  .Append(" : ")
                  .Append(w.replaced)
                  .Append("\n")
                |> ignore

              res.AppendLine("```") |> ignore

              let! _ = ctx.RespondAsync(res.ToString())
              return Ok()
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("dict-get");
    Description("読み上げ時に置換されるワードを取得します。");
    Aliases([| "dg" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.GetWord(ctx: CommandContext, [<Description("対象のワード")>] word: string) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let! dbWord = this.DBHandler.GetWord(ctx.Guild.Id, word)

          match dbWord with
          | Ok (Some w) ->
            let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**で`%s{word}`は`%s{w.replaced}`に置換されます。")
            return Ok()
          | Ok (None) -> return Error $"`%s{word}`は**%s{ctx.Guild.Name}**の辞書に登録されていません。"
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("dict-set");
    Description("読み上げ時に置換されるワードを追加・更新します。");
    Aliases([| "ds" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.AddWord
    (
      ctx: CommandContext,
      [<Description("対象のワード")>] word: string,
      [<Description("置換するワード")>] replaced: string
    ) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let! result = this.DBHandler.SetWord(ctx.Guild.Id, word, replaced)

          match result with
          | Ok _ ->
            let! _ = ctx.RespondAsync($"ワードを登録しました。\n**%s{ctx.Guild.Name}**で`%s{word}`は`%s{replaced}`に置換されます。")
            return Ok()
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("dict-delete");
    Description("読み上げ時に置換されるワードを削除します。");
    Aliases([| "dd" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.DeleteWord(ctx: CommandContext, [<Description("対象のワード")>] word: string) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let! deleted = this.DBHandler.DeleteWord(ctx.Guild.Id, word)

          match deleted with
          | Ok true ->
            let! _ = ctx.RespondAsync($"`%s{word}`を**%s{ctx.Guild.Name}**の辞書から削除しました。")
            return Ok()
          | Ok _ -> return Error($"`%s{word}`は**%s{ctx.Guild.Name}**の辞書に登録されていません。")
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("dict-clear");
    Description("読み上げ時に置換されるワードをすべて削除します。");
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.ClearWords(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let! deletedCount = this.DBHandler.DeleteWords(ctx.Guild.Id)

          match deletedCount with
          | Ok deletedCount ->
            let! _ = ctx.RespondAsync($"%d{deletedCount}件のワードを**%s{ctx.Guild.Name}**の辞書から削除しました。")
            return Ok()
          | Error _ -> return Error "処理に失敗しました。"
        }
      )

  [<Command("translate");
    Description("文章を翻訳します。\n言語コード: https://cloud.google.com/translate/docs/languages");
    Aliases([| "t" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.Translate
    (
      ctx: CommandContext,
      [<Description("翻訳先の言語コード")>] target: string,
      [<ParamArray; Description("翻訳する文章")>] text: string []
    ) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let joinedText = text |> String.concat " "

          let! (name, url) =
            task {
              if isNull ctx.Guild then
                return (ctx.User.Username, ctx.User.AvatarUrl)
              else
                let! author = this.TextConverter.GetUserWithValidName(ctx.Guild, ctx.Member.Id)
                return (author.name, ctx.Member.AvatarUrl)
            }

          let! translated = this.LanguageTranslator.Translate(joinedText, "", target)

          match translated with
          | Ok translatedText ->
            let embed =
              DiscordEmbedBuilder(
                Author = DiscordEmbedBuilder.EmbedAuthor(Name = name, IconUrl = url),
                Description = translatedText
              )
                .Build()

            do! ctx.RespondAsync(embed) :> Task
            return Ok()
          | Error errorMessage ->
            Utils.logfn "Failed to run command 'translate' with message '%s'" errorMessage
            return Error "処理に失敗しました。"
        }
      )

  member this.OnUserLeft (channel: DiscordChannel) (conn: VoiceNextConnection) (_args: VoiceUserLeaveEventArgs) =
    let _ =
      Task.Run(fun () ->
        task {
          let voiceChannel = conn.TargetChannel
          let users = voiceChannel.Users |> Seq.toArray
          Utils.logfn "%A" users
          Utils.logfn "%A" (users |> Seq.map (fun u -> u.IsCurrent || u.IsBot))

          if users |> Seq.forall (fun u -> u.IsBot) then
            conn.Disconnect()
            this.InstantFields.Leaved(voiceChannel.GuildId.Value)
            Utils.logfn "Disconnected at '%s'" voiceChannel.Guild.Name

            let! _ = channel.SendMessageAsync($"ボイスチャンネル <#%d{channel.Id}> から切断しました。")
            ()
        }
        :> Task
      )

    Task.CompletedTask

  [<Command("join");
    Description("ボイスチャンネルに参加します。このコマンドを実行したテキストチャンネルに投稿された文章が自動で読み上げられます。");
    Aliases([| "j" |]);
    RequireGuild;
    RequireBotPermissions(Permissions.SendMessages
                          ||| Permissions.UseVoice
                          ||| Permissions.Speak)>]
  member this.Join(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let voiceNext = ctx.Client.GetVoiceNext()

          if isNull voiceNext then
            return Error "ボイス機能が利用できません。"
          else

            let voiceChannel =
              Utils.null' {
                let! m = ctx.Member
                let! vs = m.VoiceState
                let! c = vs.Channel
                return c
              }

            if isNull voiceChannel then
              return Error "`join`コマンドはボイスチャンネルに接続した状態で呼び出してください。"

            else
              let currentConn = voiceNext.GetConnection(ctx.Guild)

              if currentConn <> null then
                currentConn.Disconnect()
                this.InstantFields.Leaved(ctx.Guild.Id)

              Utils.logfn "Connecting to '#%s' at '%s'" voiceChannel.Name ctx.Guild.Name

              let! conn = voiceNext.ConnectAsync(voiceChannel)
              Utils.logfn "Connected to '#%s' at '%s'" voiceChannel.Name ctx.Guild.Name
              this.InstantFields.Joined(ctx.Guild.Id, ctx.Channel.Id)

              conn.add_UserLeft (this.OnUserLeft ctx.Channel)

              let _ = conn.SendSpeakingAsync(false)

              let! _ =
                ctx.RespondAsync(
                  $"ボイスチャンネル <#%d{voiceChannel.Id}> に接続しました。\nテキストチャンネル <#%d{ctx.Channel.Id}> に投稿されたメッセージが読み上げられます。"
                )

              return Ok()
        }
      )

  [<Command("leave"); Description("ボイスチャンネルから切断します。"); Aliases([| "l" |]); RequireGuild>]
  member this.Leave(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let voiceNext = ctx.Client.GetVoiceNext()

          if isNull voiceNext then
            return Error "ボイス機能が利用できません。"
          else
            let conn = voiceNext.GetConnection(ctx.Guild)

            if isNull conn then
              return Error "ボイスチャンネルに接続していません。"
            else
              let channelId = conn.TargetChannel.Id
              conn.Disconnect()
              this.InstantFields.Leaved(ctx.Guild.Id)
              Utils.logfn "Disconnected at '%s'" ctx.Guild.Name


              let! _ = ctx.RespondAsync($"ボイスチャンネル <#%d{channelId}> から切断しました。")
              return Ok()
        }
      )

  [<Command("url"); RequireOwner; RequireDirectMessage>]
  member this.Url(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let clientId = ctx.Client.CurrentUser.Id
          let permissions = 274881072128uL

          let url =
            $"https://discord.com/api/oauth2/authorize?client_id=%d{clientId}&permissions=%d{permissions}&scope=bot"

          do! ctx.RespondAsync(url) :> Task
          return Ok()
        }
      )

  [<Command("status"); RequireOwner; RequireDirectMessage>]
  member this.Status(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          do! ctx.TriggerTypingAsync()

          let guilds =
            ctx.Client.Guilds
            |> Seq.map (fun g ->
              let emoji =
                if this.InstantFields.ConnectedVoiceChannels.ContainsKey(g.Key) then
                  ":sound:"
                else
                  ":mute:"

              $"%s{emoji} %s{g.Value.Name}: `%d{g.Key}`"
            )
            |> Seq.toArray
            |> String.concat "\n"

          let embed =
            DiscordEmbedBuilder()
              .AddField("サーバーリスト", guilds, false)
              .Build()

          do! ctx.RespondAsync(embed) :> Task
          return Ok()
        }
      )

  [<Command("exit-server"); RequireOwner; RequireDirectMessage>]
  member this.ExitServer(ctx: CommandContext, [<Description("サーバーID")>] guildId: uint64) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let guild =
            ctx.Client.Guilds
            |> Seq.tryFind (fun g -> g.Key = guildId)

          match guild with
          | None -> do! ctx.RespondAsync("サーバーが見つかりませんでした。") :> Task
          | Some g ->
            do! g.Value.LeaveAsync()
            do! ctx.RespondAsync($"%s{g.Value.Name}から退出しました。") :> Task

          return Ok()
        }
      )
