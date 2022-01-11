namespace WraindropBot

open System
open System.Diagnostics
open System.Collections.Concurrent

open DSharpPlus
open DSharpPlus.Entities

[<AllowNullLiteral>]
type DiscordCache(wdConfig: WDConfig) =
  let discordmemberCache =
    ConcurrentDictionary<uint64 * uint64, DiscordMember * DateTime>()

  member this.GetDiscordMemberAsync(guild: DiscordGuild, userId: uint64) =
    task {
      match discordmemberCache.TryGetValue
            <| (guild.Id, userId)
        with
      | true, (dm, time) when
        (DateTime.Now - time).TotalSeconds
        <= wdConfig.requestCacheSeconds
        ->
        return dm
      | _, _ ->
        let! dmember = guild.GetMemberAsync(userId)

        let added = (dmember, DateTime.Now)

        discordmemberCache.AddOrUpdate((guild.Id, userId), added, (fun _k _v -> added))
        |> ignore

        return dmember
    }

  member this.Clean() =
    let current = DateTime.Now

    for x in discordmemberCache do
      let (_, time) = x.Value

      if (current - time).TotalSeconds > wdConfig.requestCacheSeconds then
        discordmemberCache.TryRemove(x) |> ignore
