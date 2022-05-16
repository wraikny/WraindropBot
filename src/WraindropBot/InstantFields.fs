namespace WraindropBot

open System.Collections.Generic

[<Sealed; AllowNullLiteral>]
type InstantFields() =
  let channelDict = Dictionary<uint64, uint64>()

  member _.ConnectedVoiceChannels = channelDict :> IReadOnlyDictionary<_, _>

  member _.GetChannel(guildId) =
    channelDict.TryGetValue(guildId)
    |> function
      | true, channelId -> Some channelId
      | _ -> None

  member _.Joined(guildId, channelId) =
    lock channelDict (fun () -> channelDict.[guildId] <- channelId)

  member _.Leaved(guildId) =
    lock channelDict (fun () -> channelDict.Remove(guildId) |> ignore)
