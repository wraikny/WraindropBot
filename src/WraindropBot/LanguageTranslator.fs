namespace WraindropBot

open System
open System.Threading.Tasks
open System.Collections.Generic
open System.Net.Http

open FSharp.Json

type TranslationResponse = { code: int; text: string }

[<AllowNullLiteral>]
type LanguageTranslator(wdConfig: WDConfig) =
  let urlIsValid =
    let url = wdConfig.translationUrl
    url <> null && url.Trim() <> ""

  let client = lazy (new HttpClient())

  member _.Translate(text: string, source: string, target: string) : Task<Result<string, string>> =
    task {
      if not urlIsValid then
        return Error "translationUrl is not valid."
      elif String.IsNullOrEmpty(text) then
        return Error "invalid input text"
      else
        let param = Dictionary<string, string>()
        param.Add("text", text)
        param.Add("source", source)
        param.Add("target", target)

        try
          let! paramStr =
            (new FormUrlEncodedContent(param))
              .ReadAsStringAsync()

          let! result = client.Value.GetAsync(sprintf "%s?%s" wdConfig.translationUrl paramStr)

          let! resString =
            result.Content.ReadAsStringAsync()
            |> Async.AwaitTask

          let resJson =
            Json.deserialize<TranslationResponse> (resString)

          if result.IsSuccessStatusCode then
            if resJson.code = 200 then
              return Ok resJson.text
            else
              return Error resJson.text
          else
            return Error(result.ReasonPhrase)
        with
        | e -> return Error e.Message
    }

  member this.TranslateToJapanese(text) = this.Translate(text, "", "ja")
