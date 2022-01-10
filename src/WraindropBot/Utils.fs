module WraindropBot.Utils

open System
open System.Threading.Tasks

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
        let! _ = respondAsync ($"エラー: %s{msg}")
        ()
    with
    | e ->
      let! _ = respondAsync ("botプログラム内でエラーが発生しました。")
      eprintfn "%s" e.Message
      eprintfn "%s" e.StackTrace
      ()
  }
  :> Task
