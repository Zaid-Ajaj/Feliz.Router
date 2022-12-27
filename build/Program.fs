module Program

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq
open Fake.IO
open Fake.Core

let path xs = Path.Combine(Array.ofList xs)

let solutionRoot = Files.findParent __SOURCE_DIRECTORY__ "Feliz.Router.sln";

let router = path [ solutionRoot; "src" ]

let publish projectDir =
    path [ projectDir; "bin" ] |> Shell.deleteDir
    path [ projectDir; "obj" ] |> Shell.deleteDir

    if Shell.Exec(Tools.dotnet, "pack --configuration Release", projectDir) <> 0 then
        failwithf "Packing '%s' failed" projectDir
    else
        let nugetKey =
            match Environment.environVarOrNone "NUGET_KEY" with
            | Some nugetKey -> nugetKey
            | None -> 
                printfn "The Nuget API key was not found in a NUGET_KEY environmental variable"
                printf "Enter NUGET_KEY: "
                Console.ReadLine()

        let nugetPath =
            Directory.GetFiles(path [ projectDir; "bin"; "Release" ])
            |> Seq.head
            |> Path.GetFullPath

        if Shell.Exec(Tools.dotnet, sprintf "nuget push %s -s nuget.org -k %s" nugetPath nugetKey, projectDir) <> 0
        then failwith "Publish failed"

[<EntryPoint>]
let main (args: string[]) = 
    try
        // run tasks
        match args with 
        | [| "publish-nuget" |] -> publish router
        | _ -> printfn "Unknown args: %A" args
        
        // exit succesfully
        0
    with 
    | ex -> 
        // something bad happened
        printfn "Error occured"
        printfn "%A" ex
        1