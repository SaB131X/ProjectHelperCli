module ProjectHelper.Core.TemplateSystem

open System
open System.IO
open System.Diagnostics
open ProjectHelper.Core.Debug

exception TemplateError of string

let private fail msg = raise (TemplateError msg)

let private homeDir () =
    match Environment.GetEnvironmentVariable "HOME" with
    | null | "" -> Environment.GetEnvironmentVariable "USERPROFILE"
    | h -> h

let private templatesRoot () = Path.Combine(homeDir (), "Templates", "Code")
let private shellsDir     () = Path.Combine(templatesRoot (), "Shells")

let ensureStructure () =
    let root = templatesRoot ()
    Directory.CreateDirectory(templatesRoot ())    |> ignore
    Directory.CreateDirectory(shellsDir ())        |> ignore
    root

let private dotnetTemplateMap =
    dict [
        "cs.avalonia.app",  "avalonia.app"
        "cs.avalonia.mvvm", "avalonia.mvvm"
        "cs.console",       "console"
    ]

let private runProcess (fileName: string) (arguments: string) (workingDir: string) : int =
    let psi = ProcessStartInfo()
    psi.FileName <- fileName
    psi.Arguments <- arguments
    psi.WorkingDirectory <- workingDir
    psi.UseShellExecute <- false
    use proc = Process.Start psi
    proc.WaitForExit()
    proc.ExitCode

let private copyGitignore (templateName: string) (targetDir: string) =
    let src = Path.Combine(templatesRoot (), templateName + ".gitignore")
    if not (File.Exists src) then
        fail (sprintf "gitignore not found: %s" src)
    File.Copy(src, Path.Combine(targetDir, ".gitignore"), true)

let private copyShellNix (targetDir: string) =
    let src = Path.Combine(shellsDir (), "shell.nix")
    if not (File.Exists src) then
        fail (sprintf "shell.nix not found: %s" src)
    File.Copy(src, Path.Combine(targetDir, "shell.nix"), true)

let applyTemplate (templateName: string) (nixMode: bool) (targetDir: string) =
    ensureStructure() |> ignore

    Debug.run (sprintf "check target dir exists: %s" targetDir) (fun () ->
        if not (Directory.Exists targetDir) then
            fail (sprintf "Target directory does not exist: %s" targetDir))

    let dotnetTemplate =
        match dotnetTemplateMap.TryGetValue templateName with
        | true, t -> t
        | _ -> fail (sprintf "Unknown template: %s" templateName)

    let dirName = Path.GetFileName(Path.GetFullPath targetDir)
    let args = sprintf "new %s -n %s -o . --force" dotnetTemplate dirName

    Debug.run (sprintf "dotnet %s (in %s)" args targetDir) (fun () ->
        let code = runProcess "dotnet" args targetDir
        if code <> 0 then
            fail (sprintf "dotnet new failed with exit code %d" code))

    Debug.run (sprintf "copy .gitignore from %s" templateName) (fun () ->
        copyGitignore templateName targetDir)

    if nixMode then
        Debug.run "copy shell.nix" (fun () -> copyShellNix targetDir)