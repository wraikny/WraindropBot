namespace WraindropBot

open System
open System.Threading.Tasks

open DSharpPlus
open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open DSharpPlus.Entities
open DSharpPlus.EventArgs
open DSharpPlus.VoiceNext

open WraindropBot

type WDCommands() =
  inherit BaseCommandModule()

  member val WDConfig: WDConfig = Unchecked.defaultof<_> with get, set
  member val InstantFields: InstantFields = null with get, set
  member val DBHandler: Database.DatabaseHandler = null with get, set
  member val DiscordCache: DiscordCache = null with get, set

  member private _.RespondReadAs(ctx: CommandContext, userId, name: string) =
    ctx.RespondAsync($"<@!%d{userId}> は %s{ctx.Guild.Name} で %s{name} と読み上げられます。")

  [<Command("name"); Description("サーバー毎の読み上げ時の名前を取得します。")>]
  member this.Name(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! user = this.DBHandler.GetUser(ctx.Guild.Id, ctx.User.Id)

          match user with
          | Some { name = Utils.ValidStr name } ->
            let! _ = this.RespondReadAs(ctx, ctx.User.Id, name)
            return Ok()
          | _ ->
            let! guildMember = this.DiscordCache.GetDiscordMemberAsync(ctx.Guild, ctx.User.Id)
            let! _ = this.RespondReadAs(ctx, ctx.User.Id, Utils.getNicknameOrUsername guildMember)
            return Ok()
        }
      )

  member private this.SetName(ctx: CommandContext, targetId: uint64, name: string) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let maxLen = this.WDConfig.usernameMaxLength

          if name.Length < 1 || maxLen < name.Length then
            return Error $"名前は1文字以上%d{maxLen}文字以下にしてください。"
          else
            do! this.DBHandler.SetUserName(ctx.Guild.Id, targetId, name)

            let! _ = this.RespondReadAs(ctx, targetId, name)
            return Ok()
        }
      )

  [<Command("name"); Description("サーバー毎に読み上げ時の名前を設定します。")>]
  member this.Name(ctx: CommandContext, [<Description("読み上げ時の名前")>] name: string) = this.SetName(ctx, ctx.User.Id, name)

  [<Command("name"); Description("サーバー毎に読み上げ時の名前を設定します。")>]
  member this.Name
    (
      ctx: CommandContext,
      [<Description("対象のユーザ")>] target: DiscordMember,
      [<Description("読み上げ時の名前")>] name: string
    ) =
    this.SetName(ctx, target.Id, name)

  [<Command("speed"); Description("発話速度を取得します。")>]
  member this.Speed(ctx: CommandContext) =
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

          let! _ = ctx.RespondAsync($"%s{ctx.Guild.Name} での <@!%d{ctx.User.Id}> の現在の発話速度は `%d{speed}` です。")
          return Ok()
        }
      )

  [<Command("speed"); Description("発話速度を指定します。(50~300)")>]
  member this.Speed(ctx: CommandContext, [<Description("発話速度")>] speed: int) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let! speed = this.DBHandler.SetUserSpeed(ctx.Guild.Id, ctx.User.Id, speed)

          let! _ = ctx.RespondAsync($"%s{ctx.Guild.Name} での <@!%d{ctx.User.Id}> の発話速度が `%d{speed}` に設定されました。")
          return Ok()
        }
      )

  [<Command("join"); Description("ボイスチャンネルに参加します。")>]
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

              let _ = conn.SendSpeakingAsync(false)

              let! _ =
                ctx.RespondAsync(
                  $"ボイスチャンネル <#%d{voiceChannel.Id}> に接続しました。\nテキストチャンネル <#%d{ctx.Channel.Id}> に投稿されたメッセージが読み上げられます。"
                )

              return Ok()
        }
      )

  [<Command("leave"); Description("ボイスチャンネルから切断します。")>]
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
