namespace WraindropBot

open System.IO

open FSharp.Json

type SubCommandConfig =
  { [<JsonField("description")>]
    description: string }

type WDConfig =
  { [<JsonField("token")>]
    token: string
    [<JsonField("commandPrefixes")>]
    commandPrefixes: string[]
    [<JsonField("ignorePrefixes")>]
    ignorePrefixes: string[]
    [<JsonField("aquesTalkPath")>]
    aquesTalkPath: string
    [<JsonField("volume")>]
    volume: int }


[<RequireQualifiedAccess>]
module WDConfig =
  let asyncLoad (path: string) =
    async {
      let! text = File.ReadAllTextAsync(path) |> Async.AwaitTask
      let config = Json.deserialize<WDConfig> (text)
      return config
    }
