namespace WraindropBot

open System.Reflection
open System.IO

open NTextCat

[<AllowNullLiteral>]
type LanguageDetector() =
  let factory = RankedLanguageIdentifierFactory()

  let path = 
    Path.Combine(
      Assembly.GetExecutingAssembly().Location,
      "..",
      "LanguageModels/Core14.profile.xml"
    )

  do
    if File.Exists(path) |> not then
      failwithf "language model file is not found at '%s'" path

  let identifier = factory.Load(path)

  member _.Detect(text: string): string option =
    try
      identifier.Identify(text)
      |> Seq.tryHead
      |> Option.map (fun (lang, _) -> lang.Iso639_3)
    with e ->
      None

  member this.DetectIsJapanese(text: string): bool =
    this.Detect(text) |> Option.is ((=) "jpn")
