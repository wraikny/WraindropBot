namespace WraindropBot

open System.Collections.Generic

[<Sealed; AllowNullLiteral>]
type InstantFields() =
  let channelDict = Dictionary<uint64, uint64>()

  member _.GetChannel(guildId) =
    channelDict.TryGetValue(guildId)
    |> function
    | true, channelId -> Some channelId
    | _ -> None

  member _.Joined(guildId, channelId) = channelDict.[guildId] <- channelId
  member _.Leaved(guildId) = channelDict.Remove(guildId) |> ignore
