module ProjectHelperCli.Program

open System
open ProjectHelper.Core
open ProjectHelper.Core.ArgTypes
open ProjectHelper.Core.ClaParser
open ProjectHelper.Core.ProjectMaker
open ProjectHelper.Core.Debug

let knownTemplates =
    set [ "cs.avalonia.app"; "cs.avalonia.mvvm"; "cs.console" ]

[<EntryPoint>]
let main(args : string array) =
    Debug.DryRun <- true
    let args = if isNull args then [||] else args
    try
        let opts = parse args
        let target = ProjectMaker.createProject opts
        Debug.log (sprintf "Project ready at: %s" target)
        0
    with
    | ArgError msg                      -> eprintfn "Arg error: %s" msg; 2
    | TemplateSystem.TemplateError msg  -> eprintfn "Template error: %s" msg; 1
    | GhClient.GhError msg              -> eprintfn "gh error: %s" msg; 1
    | ProjectMaker.ProjectError msg     -> eprintfn "Project error: %s" msg; 1
    | ex                                -> eprintfn "Unexpected: %s" ex.Message; 1
    
