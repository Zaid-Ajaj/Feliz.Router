#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System
open System.IO
open Fake
open Fake.Core
open Fake.IO
open Fake.SystemHelper

let libPath = "./src"
let testsPath = "./demo"

let nodeTool = "node"
let npmTool = "npm"

let mutable dotnetCli = "dotnet"

let run fileName args workingDir =
    printfn "CWD: %s" workingDir
    let fileName, args =
        if Environment.isUnix
        then fileName, args else "cmd", ("/C " + fileName + " " + args)

    CreateProcess.fromRawCommandLine fileName args
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.withTimeout TimeSpan.MaxValue
    |> CreateProcess.ensureExitCodeWithMessage (sprintf "'%s> %s %s' task failed" workingDir fileName args)
    |> Proc.run
    |> ignore


let delete file =
    if File.Exists(file)
    then File.Delete file
    else ()

let cleanBundles() =
    Path.Combine("public", "bundle.js")
        |> Path.GetFullPath
        |> delete
    Path.Combine("public", "bundle.js.map")
        |> Path.GetFullPath
        |> delete

let (</>) x y = System.IO.Path.Combine(x, y)

let cleanCacheDirs() =
    [ testsPath </> "bin"
      testsPath </> "obj"
      libPath </> "bin"
      libPath </> "obj" ]
    |> Shell.cleanDirs

Target.create "Clean" <| fun _ ->
    cleanCacheDirs()
    cleanBundles()

Target.create "InstallNpmPackages" (fun _ ->
  printfn "Node version:"
  run nodeTool "--version" __SOURCE_DIRECTORY__
  run "npm" "--version" __SOURCE_DIRECTORY__
  run "npm" "install" __SOURCE_DIRECTORY__
)

Target.create "RestoreFableTestProject" <| fun _ ->
  run dotnetCli "restore" testsPath

Target.create "RunLiveTests" <| fun _ ->
    run npmTool "start" "."

Target.create "Test" <| fun _ -> run npmTool "test" "."

let publish projectPath =
    [ projectPath </> "bin"
      projectPath </> "obj" ] |> Shell.cleanDirs
    run dotnetCli "restore --no-cache" projectPath
    run dotnetCli "pack -c Release" projectPath
    let nugetKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"
    let nupkg =
        Directory.GetFiles(projectPath </> "bin" </> "Release")
        |> Seq.head
        |> Path.GetFullPath

    let pushCmd = sprintf "nuget push %s -s nuget.org -k %s" nupkg nugetKey
    run dotnetCli pushCmd projectPath

Target.create "PublishNuget" (fun _ -> publish libPath)

Target.create "CompileFableTestProject" <| fun _ ->
    run npmTool "run build" "."

Target.create "Build" (fun _ -> ignore())

open Fake.Core.TargetOperators

"Clean"
  ==> "InstallNpmPackages"
  ==> "RunLiveTests"

"Clean"
  ==> "InstallNpmPackages"
  ==> "Test"


"Clean"
  ==> "InstallNpmPackages"
  ==> "CompileFableTestProject"
  ==> "Build"

Target.runOrDefault "Build"
