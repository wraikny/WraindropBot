namespace WraindropBot

open System
open System.Threading.Tasks

module Utils =
  type NullBuilder() =
    member _.Return(x) = x
    member _.Bind(x, f) = if isNull x then null else f x

  let null' = NullBuilder()

  let logfn msg =
    let current =
      DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")

    Printf.kprintf (printfn "[%s]%s" current) msg

  let handleError (respondAsync) (t: unit -> Task<Result<unit, string>>) =
    task {
      try
        match! t () with
        | Ok () -> ()
        | Error msg ->
          let! _ = respondAsync (msg)
          ()
      with
      | e ->
        let! _ = respondAsync ("botプログラム内でエラーが発生しました。")
        eprintfn "%s" e.Message
        eprintfn "%s" e.StackTrace
        ()
    }
    :> Task

  let (|ValidStr|_|) =
    function
    | null
    | "" -> None
    | s -> Some s

  open DSharpPlus.Entities

  let getNicknameOrUsername (discordMember: DiscordMember) =
    (discordMember.Nickname, discordMember.Username)
    |> function
      | (ValidStr s, _)
      | (_, s) -> s


[<AutoOpen>]
module Extension =
  open System.Text
  open System.Text.RegularExpressions

  type StringBuilder with
    member this.Replace(regex: Regex, replace: string) =
      let original = this.ToString()
      let matches = regex.Matches(original)

      if matches.Count <> 0 then

        let sb = new StringBuilder()

        // position in original string
        let mutable pos = 0

        for m in matches do
          // Append the portion of the original we skipped
          sb
            .Append(original.Substring(pos, m.Index - pos))
            // Replace string
            .Append(
              regex.Replace(m.Value, replace)
            )
          |> ignore

          pos <- m.Index + m.Value.Length

        if pos < original.Length - 1 then
          sb.Append(original.Substring(pos, original.Length - pos - 1))
          |> ignore

        this.Clear().Append(sb) |> ignore

      this
