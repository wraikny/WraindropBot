namespace WraindropBot

open System
open System.Threading.Tasks

open DSharpPlus.Entities

module Utils =
  type NullBuilder() =
    member _.Return(x) = x
    member _.Bind(x, f) = if isNull x then null else f x

  let null' = NullBuilder()

  let logfn msg =
    let current =
      DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")

    Printf.kprintf (printfn "[%s]%s" current) msg

  let errorColor = DiscordColor(189uy, 40uy, 40uy)

  let handleError (respondAsync) (t: unit -> Task<Result<unit, string>>) =
    task {
      try
        match! t () with
        | Ok () -> ()
        | Error msg ->
          do!
            DiscordEmbedBuilder(Description = msg, Color = errorColor)
              .Build()
            |> respondAsync
            :> Task

          ()
      with
      | e ->
        do!
          DiscordEmbedBuilder(Description = "botプログラム内でエラーが発生しました。", Color = errorColor)
            .Build()
          |> respondAsync
          :> Task

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

  module Language =
    let codes =
      [ "アフリカーンス語", [ "af" ]
        "アルバニア語", [ "sq" ]
        "アムハラ語", [ "am" ]
        "アラビア文字", [ "ar" ]
        "アルメニア語", [ "hy" ]
        "アッサム語", [ "as" ]
        "アイマラ語", [ "ay" ]
        "アゼルバイジャン語", [ "az" ]
        "バンバラ語", [ "bm" ]
        "バスク語", [ "eu" ]
        "ベラルーシ語", [ "be" ]
        "ベンガル文字", [ "bn" ]
        "ボージュプリー語", [ "bho" ]
        "ボスニア語", [ "bs" ]
        "ブルガリア語", [ "bg" ]
        "カタロニア語", [ "ca" ]
        "セブ語", [ "ceb" ]
        "中国語（簡体）", [ "zh-CN"; "zh" ]
        "中国語（繁体）", [ "zh-TW" ]
        "コルシカ語", [ "co" ]
        "クロアチア語", [ "hr" ]
        "チェコ語", [ "cs" ]
        "デンマーク語", [ "da" ]
        "ディベヒ語", [ "dv" ]
        "ドグリ語", [ "doi" ]
        "オランダ語", [ "nl" ]
        "英語", [ "en" ]
        "エスペラント語", [ "eo" ]
        "エストニア語", [ "et" ]
        "エウェ語", [ "ee" ]
        "フィリピン語（タガログ語）", [ "fil" ]
        "フィンランド語", [ "fi" ]
        "フランス語", [ "fr" ]
        "フリジア語", [ "fy" ]
        "ガリシア語", [ "gl" ]
        "グルジア語", [ "ka" ]
        "ドイツ語", [ "de" ]
        "ギリシャ文字", [ "el" ]
        "グアラニ語", [ "gn" ]
        "グジャラート文字", [ "gu" ]
        "クレオール語（ハイチ）", [ "ht" ]
        "ハウサ語", [ "ha" ]
        "ハワイ語", [ "haw" ]
        "ヘブライ語", [ "he"; "iw" ]
        "ヒンディー語", [ "hi" ]
        "モン語", [ "hmn" ]
        "ハンガリー語", [ "hu" ]
        "アイスランド語", [ "is" ]
        "イボ語", [ "ig" ]
        "イロカノ語", [ "ilo" ]
        "インドネシア語", [ "id" ]
        "アイルランド語", [ "ga" ]
        "イタリア語", [ "it" ]
        "日本語", [ "ja" ]
        "ジャワ語", [ "jv"; "jw" ]
        "カンナダ文字", [ "kn" ]
        "カザフ語", [ "kk" ]
        "クメール語", [ "km" ]
        "キニヤルワンダ語", [ "rw" ]
        "コンカニ語", [ "gom" ]
        "韓国語", [ "ko" ]
        "クリオ語", [ "kri" ]
        "クルド語", [ "ku" ]
        "クルド語（ソラニ語）", [ "ckb" ]
        "キルギス語", [ "ky" ]
        "ラオ語", [ "lo" ]
        "ラテン語", [ "la" ]
        "ラトビア語", [ "lv" ]
        "リンガラ語", [ "ln" ]
        "リトアニア語", [ "lt" ]
        "Luganda", [ "lg" ]
        "ルクセンブルク語", [ "lb" ]
        "マケドニア語", [ "mk" ]
        "マイティリー語", [ "mai" ]
        "マラガシ語", [ "mg" ]
        "マレー語", [ "ms" ]
        "マラヤーラム文字", [ "ml" ]
        "マルタ語", [ "mt" ]
        "マオリ語", [ "mi" ]
        "マラーティー語", [ "mr" ]
        "メイテイ語（マニプリ語）", [ "mni-Mtei" ]
        "ミゾ語", [ "lus" ]
        "モンゴル文字", [ "mn" ]
        "ミャンマー語（ビルマ語）", [ "my" ]
        "ネパール語", [ "ne" ]
        "ノルウェー語", [ "no" ]
        "ニャンジャ語（チェワ語）", [ "ny" ]
        "オリヤ語", [ "or" ]
        "オロモ語", [ "om" ]
        "パシュト語", [ "ps" ]
        "ペルシャ語", [ "fa" ]
        "ポーランド語", [ "pl" ]
        "ポルトガル語（ポルトガル、ブラジル）", [ "pt" ]
        "パンジャブ語", [ "pa" ]
        "ケチュア語", [ "qu" ]
        "ルーマニア語", [ "ro" ]
        "ロシア語", [ "ru" ]
        "サモア語", [ "sm" ]
        "サンスクリット語", [ "sa" ]
        "スコットランド ゲール語", [ "gd" ]
        "セペディ語", [ "nso" ]
        "セルビア語", [ "sr" ]
        "セソト語", [ "st" ]
        "ショナ語", [ "sn" ]
        "シンド語", [ "sd" ]
        "シンハラ語", [ "si" ]
        "スロバキア語", [ "sk" ]
        "スロベニア語", [ "sl" ]
        "ソマリ語", [ "so" ]
        "スペイン語", [ "es" ]
        "スンダ語", [ "su" ]
        "スワヒリ語", [ "sw" ]
        "スウェーデン語", [ "sv" ]
        "タガログ語（フィリピン語）", [ "tl" ]
        "タジク語", [ "tg" ]
        "タミル語", [ "ta" ]
        "タタール語", [ "tt" ]
        "テルグ語", [ "te" ]
        "タイ語", [ "th" ]
        "ティグリニャ語", [ "ti" ]
        "ツォンガ語", [ "ts" ]
        "トルコ語", [ "tr" ]
        "トルクメン語", [ "tk" ]
        "トウィ語（アカン語）", [ "ak" ]
        "ウクライナ語", [ "uk" ]
        "ウルドゥー語", [ "ur" ]
        "ウイグル語", [ "ug" ]
        "ウズベク語", [ "uz" ]
        "ベトナム語", [ "vi" ]
        "ウェールズ語", [ "cy" ]
        "コーサ語", [ "xh" ]
        "イディッシュ語", [ "yi" ]
        "ヨルバ語", [ "yo" ]
        "ズールー語", [ "zu" ] ]

    let tryFindCode (code: string) : string option =
      codes
      |> List.tryFind (snd >> List.contains code)
      |> Option.map fst

[<AutoOpen>]
module Extension =
  open System.Text
  open System.Text.RegularExpressions

  module Option =
    let is f =
      function
      | Some x when f x -> true
      | _ -> false

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
