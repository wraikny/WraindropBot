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

  [<Command("join"); Description("ボイスチャンネルに参加します。")>]
  member _.Join(ctx: CommandContext) =
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

              let _ = conn.SendSpeakingAsync(false)
              let! _ = ctx.RespondAsync($"ボイスチャンネル %s{voiceChannel.Name} に接続しました。")

              return Ok()
        }
      )

  [<Command("leave"); Description("ボイスチャンネルから切断します。")>]
  member _.Leave(ctx: CommandContext) =
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
              conn.Disconnect()
              Utils.logfn "Disconnected at '%s'" ctx.Guild.Name

              let! _ = ctx.RespondAsync("ボイスチャンネルから切断しました。")
              return Ok()
        }
      )
