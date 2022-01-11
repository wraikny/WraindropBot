namespace WraindropBot

open System.IO

open FSharp.Json

type WDConfig =
  { [<JsonField("token")>]
    token: string
    [<JsonField("commandPrefixes")>]
    commandPrefixes: string []
    [<JsonField("ignorePrefixes")>]
    ignorePrefixes: string []
    [<JsonField("dbPath")>]
    dbPath: string
    [<JsonField("usernameMaxLength")>]
    usernameMaxLength: int
    [<JsonField("dictionaryReplacementRepeatedCount")>]
    dictionaryReplacementRepeatedCount: int
    [<JsonField("dictionaryMaxLength")>]
    dictionaryMaxLength: int
    [<JsonField("requestCacheSeconds")>]
    requestCacheSeconds: int
    [<JsonField("aquesTalkPath")>]
    aquesTalkPath: string
    [<JsonField("volume")>]
    volume: int
    [<JsonField("defaultSpeed")>]
    defaultSpeed: int }


[<RequireQualifiedAccess>]
module WDConfig =
  let validateSpeed = max 50 >> min 300

  let asyncLoad (path: string) =
    async {
      let! text = File.ReadAllTextAsync(path) |> Async.AwaitTask
      let config = Json.deserialize<WDConfig> (text)

      let config =
        { config with
            defaultSpeed = config.defaultSpeed |> validateSpeed }

      return config
    }
