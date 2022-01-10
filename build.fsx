#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

[<AutoOpen>]
module Utils =
  let dotnet cmd arg =
    let res = DotNet.exec id cmd arg

    if not res.OK then
      failwithf "Failed 'dotnet %s %s'" cmd arg

  let (|LowerCase|_|) (x: string) (s: string) =
    if x.ToLower() = s.ToLower() then
      Some LowerCase
    else
      None

  let getConfiguration =
    function
    | Some (LowerCase ("debug")) -> DotNet.BuildConfiguration.Debug
    | Some (LowerCase ("release")) -> DotNet.BuildConfiguration.Release
    | Some (c) -> failwithf "Invalid configuration '%s'" c
    | _ -> DotNet.BuildConfiguration.Debug

Target.initEnvironment ()

let args = Target.getArguments ()

Target.create "Clean" (fun _ -> !! "src/**/bin" ++ "src/**/obj" |> Shell.cleanDirs)

Target.create
  "Build"
  (fun _ ->
    let configuration =
      args
      |> Option.bind Array.tryHead
      |> getConfiguration

    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build (fun p -> { p with Configuration = configuration }))
  )

let formatTargets =
  !! "src/**/*.fs"
  -- "src/*/obj/**/*.fs"
  -- "src/*/bin/**/*.fs"
  ++ "build.fsx"
  ++ "script/*.fsx"

Target.create
  "Format"
  (fun _ ->
    formatTargets
    |> String.concat " "
    |> dotnet "fantomas"
  )

Target.create
  "Format.Check"
  (fun _ ->
    formatTargets
    |> String.concat " "
    |> sprintf "--check %s"
    |> dotnet "fantomas"
  )

Target.create
  "Publish.Raspbian"
  (fun _ ->
    [ "linux-arm" ]
    |> Seq.iter (fun runtime ->
      "src/WraindropBot/WraindropBot.fsproj"
      |> DotNet.publish (fun p ->
        let runtime = "linux-arm"

        { p with
            Runtime = Some runtime
            Configuration = DotNet.BuildConfiguration.Release
            SelfContained = Some true
            MSBuildParams =
              { p.MSBuildParams with
                  Properties =
                    List.append
                      [ ("DefineConstants", "OS_RASPBIAN")
                        ("DebugType", "none")
                        ("PublishSingleFile", "true")
                        ("PublishTrimmed", "true") ]
                      p.MSBuildParams.Properties }
            OutputPath = $"publish/WraindropBot.%s{runtime}" |> Some }
      )
    )
  )

Target.create "None" ignore

Target.create "All" ignore

"Clean" ==> "Build" ==> "All"

Target.runOrDefaultWithArguments "All"
