namespace WraindropBot

open System.Reflection
open System.IO

open NTextCat

[<AllowNullLiteral>]
type LanguageDetector() =
  let factory = RankedLanguageIdentifierFactory()

  let path =
    let path = "LanguageModels/Core14.profile.xml"

    if File.Exists(path) then
      path
    else
      Path.Combine(Assembly.GetExecutingAssembly().Location, "..", path)

  do
    if File.Exists(path) |> not then
      failwithf "language model file is not found at '%s'" path

  let identifier = factory.Load(path)

  member _.Detect(text: string) : string option =
    try
      identifier.Identify(text)
      |> Seq.tryHead
      |> Option.map (fun (lang, _) -> lang.Iso639_3)
    with
    | e -> None

  member this.DetectIsJapanese(text: string) : bool =
    this.Detect(text) |> Option.is ((=) "jpn")
