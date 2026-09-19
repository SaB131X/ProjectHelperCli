// For more information see https://aka.ms/fsharp-console-apps
module ProjectHelperCli

open System

[<EntryPoint>]
let Main(args : string array) =
    let verbose = args |> Array.exists (fun arg -> arg = "--verbose" || arg = "-v")
    let positionalArgs = args |> Array.filter (fun arg -> arg <> "--verbose" && arg <> "-v")

    if positionalArgs.Length < 2 then
        eprintfn "Usage: ProjectHelperCli <local-project-name> <git-project-name> [--verbose|-v]"
        1
    else
        let localProjectName = positionalArgs[0]
        let gitProjectName = positionalArgs[1]

        if verbose then
            printfn "Creating local project '%s' from git project '%s'" localProjectName gitProjectName
        else
            printfn "Local project: %s; git project: %s" localProjectName gitProjectName

        0
