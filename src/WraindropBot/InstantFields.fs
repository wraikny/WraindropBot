namespace WraindropBot

open System.Collections.Generic

[<Sealed; AllowNullLiteral>]
type InstantFields() =
  let channelDict = Dictionary<uint64, uint64>()

  let speedDict = Dictionary<uint64, int>()

  member _.GetChannel(guildId) =
    channelDict.TryGetValue(guildId)
    |> function
      | true, channelId -> Some channelId
      | _ -> None

  member _.Joined(guildId, channelId) =
    lock channelDict (fun () -> channelDict.[guildId] <- channelId)

  member _.Leaved(guildId) =
    lock channelDict (fun () -> channelDict.Remove(guildId) |> ignore)

  member _.GetSpeed(userId) =
    lock
      speedDict
      (fun () ->
        speedDict.TryGetValue(userId)
        |> function
          | true, speed -> speed
          | _ -> 100
      )

  member _.SetSpeed(userId, speed) =
    lock
      speedDict
      (fun () ->
        let speed = speed |> max 50 |> min 300

        if speed = 100 then
          speedDict.Remove(userId) |> ignore
          100
        else
          speedDict.[userId] <- speed
          speed
      )
