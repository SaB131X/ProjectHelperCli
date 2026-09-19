// For more information see https://aka.ms/fsharp-console-apps
open System
open System.CommandLine
open Option

[<EntryPoint>]
let Main(args : string array) =
    let localProjectName : string = args[0]
    let gitProjectName : string = args[1]
    let verboseOption = Option<string>("--verbose", [| "-v" |], Description = "Enable verbose logging")
    printfn "Hello from F#"
    0
