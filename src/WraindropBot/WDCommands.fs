namespace WraindropBot

open System.Diagnostics
open System.Text
open System.Threading
open System.Threading.Tasks

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

  member private _.RespondReadAs(ctx: CommandContext, userId, name: string) =
    ctx.RespondAsync($"<@!%d{userId}>は**%s{ctx.Guild.Name}**で`%s{name}`と読み上げられます。")

  [<Command("name-get");
    Description("サーバーで読み上げる名前を取得します。");
    Aliases([| "ng" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.GetName(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
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
          let maxLen = this.WDConfig.usernameMaxLength

          match name with
          | null ->
            do! this.DBHandler.SetUserName(ctx.Guild.Id, targetId, null)
            let! guildMember = this.DiscordCache.GetDiscordMemberAsync(ctx.Guild, targetId)
            let! _ = this.RespondReadAs(ctx, targetId, Utils.getNicknameOrUsername guildMember)
            return Ok()
          | name when (1 <= name.Length && name.Length <= maxLen) ->
            do! this.DBHandler.SetUserName(ctx.Guild.Id, targetId, name)
            let! _ = this.RespondReadAs(ctx, targetId, name)
            return Ok()
          | _ -> return Error $"名前は1文字以上%d{maxLen}文字以下にしてください。"
        }
      )

  [<Command("name-set");
    Description("サーバーで読み上げる名前を設定します。");
    Aliases([| "ns" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.SetName(ctx: CommandContext, [<Description("読み上げる名前")>] name: string) =
    this.SetName(ctx, ctx.User.Id, name)

  [<Command("name-set-user");
    Description("サーバーで指定したユーザを読み上げる名前を設定します。");
    Aliases([| "nsu" |]);
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
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.DeleteName(ctx: CommandContext) = this.SetName(ctx, ctx.User.Id, null)

  [<Command("name-delete-user");
    Description("サーバーで指定したユーザを読み上げる名前を消去します。");
    Aliases([| "ndu" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.DeleteName(ctx: CommandContext, [<Description("対象のユーザ")>] target: DiscordMember) =
    this.SetName(ctx, target.Id, null)

  [<Command("speed-get");
    Description("サーバーでの発話速度を取得します。");
    Aliases([| "sg" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.GetSpeed(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! user = this.DBHandler.GetUser(ctx.Guild.Id, ctx.User.Id)

          let speed =
            user
            |> function
              | Some { speakingSpeed = spd } -> spd
              | _ -> this.WDConfig.defaultSpeed

          let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**で<@!%d{ctx.User.Id}>の発話速度は`%d{speed}`です。")
          return Ok()
        }
      )

  [<Command("speed-set");
    Description("サーバーでの発話速度を設定します。(50~300)");
    Aliases([| "ss" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.SetSpeed(ctx: CommandContext, [<Description("発話速度")>] speed: int) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! speed = this.DBHandler.SetUserSpeed(ctx.Guild.Id, ctx.User.Id, speed)

          let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**で<@!%d{ctx.User.Id}>の発話速度は`%d{speed}`に設定されました。")
          return Ok()
        }
      )

  [<Command("dict-list");
    Description("読み上げ時に置換されるワードの一覧を取得します。");
    Aliases([| "dl" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.ListWords(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! words = this.DBHandler.GetWords(ctx.Guild.Id)
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
        }
      )

  [<Command("dict-get");
    Description("読み上げ時に置換されるワードを取得します。");
    Aliases([| "dg" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.GetWord(ctx: CommandContext, [<Description("対象のワード")>] word: string) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! dbWord = this.DBHandler.GetWord(ctx.Guild.Id, word)

          match dbWord with
          | Some w ->
            let! _ = ctx.RespondAsync($"**%s{ctx.Guild.Name}**で`%s{word}`は`%s{w.replaced}`に置換されます。")
            return Ok()
          | None -> return Error $"`%s{word}`は**%s{ctx.Guild.Name}**の辞書に登録されていません。"
        }
      )

  [<Command("dict-set");
    Description("読み上げ時に置換されるワードを追加・更新します。");
    Aliases([| "ds" |]);
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
          do! this.DBHandler.SetWord(ctx.Guild.Id, word, replaced)
          let! _ = ctx.RespondAsync($"ワードを登録しました。\n**%s{ctx.Guild.Name}**で`%s{word}`は`%s{replaced}`に置換されます。")
          return Ok()
        }
      )

  [<Command("dict-delete");
    Description("読み上げ時に置換されるワードを削除します。");
    Aliases([| "dd" |]);
    RequireBotPermissions(Permissions.SendMessages)>]
  member this.DeleteWord(ctx: CommandContext, [<Description("対象のワード")>] word: string) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! deleted = this.DBHandler.DeleteWord(ctx.Guild.Id, word)

          if deleted then
            let! _ = ctx.RespondAsync($"`%s{word}`を**%s{ctx.Guild.Name}**の辞書から削除しました。")
            return Ok()
          else
            return Error($"`%s{word}`は**%s{ctx.Guild.Name}**の辞書に登録されていません。")
        }
      )

  [<Command("dict-clear"); Description("読み上げ時に置換されるワードをすべて削除します。"); RequireBotPermissions(Permissions.SendMessages)>]
  member this.ClearWords(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! deletedCount = this.DBHandler.DeleteWords(ctx.Guild.Id)
          let! _ = ctx.RespondAsync($"%d{deletedCount}件のワードを**%s{ctx.Guild.Name}**の辞書から削除しました。")
          return Ok()
        }
      )

  member this.OnUserLeft (channel: DiscordChannel) (conn: VoiceNextConnection) (args: VoiceUserLeaveEventArgs) =
    let _ = Task.Run(fun () ->
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
    RequireBotPermissions(Permissions.SendMessages
                          ||| Permissions.UseVoice
                          ||| Permissions.Speak)>]
  member this.Join(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
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

  [<Command("leave"); Description("ボイスチャンネルから切断します。"); Aliases([| "l" |])>]
  member this.Leave(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
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
