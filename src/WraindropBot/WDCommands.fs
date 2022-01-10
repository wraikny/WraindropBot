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

  member val InstantFields: InstantFields = null with get, set

  [<Command("speed"); Description("発話速度を取得します。(50~300)")>]
  member this.Speed(ctx: CommandContext) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let speed = this.InstantFields.GetSpeed(ctx.User.Id)
          let! _ = ctx.RespondAsync($"現在の発話速度は `%d{speed}` です。")
          return Ok()
        }
      )

  [<Command("speed"); Description("発話速度を指定します。(50~300)")>]
  member this.Speed(ctx: CommandContext, [<Description("発話速度")>] speed: int) =
    Utils.handleError
      ctx.RespondAsync
      (fun () ->
        task {
          let speed =
            this.InstantFields.SetSpeed(ctx.User.Id, speed)

          let! _ = ctx.RespondAsync($"発話速度が `%d{speed}` に設定されました。")
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
            let isConnected =
              voiceNext.GetConnection(ctx.Guild) <> null

            let voiceChannel =
              Utils.null' {
                let! m = ctx.Member
                let! vs = m.VoiceState
                let! c = vs.Channel
                return c
              }

            if isConnected then
              return Error "ボイスチャンネルに接続済みです。"

            else if isNull voiceChannel then
              return Error "`join`コマンドはボイスチャンネルに接続した状態で呼び出してください。"

            else
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
