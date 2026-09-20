module ProjectHelper.Core.GhClient

open System.Diagnostics
open ProjectHelper.Core.ArgTypes
open ProjectHelper.Core.Debug

exception GhError of string

let private fail msg = raise (GhError msg)

let private runCapture (fileName: string) (arguments: string) (workingDir: string) : int * string =
    let psi = ProcessStartInfo()
    psi.FileName <- fileName
    psi.Arguments <- arguments
    psi.WorkingDirectory <- workingDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true

    use proc = Process.Start psi
    let out = proc.StandardOutput.ReadToEnd()
    let err = proc.StandardError.ReadToEnd()
    proc.WaitForExit()
    proc.ExitCode, (out + err)

let createGithubRepo (opts: Options) (workingDir: string) : string =
    let visibility = if opts.Public then "--public" else "--private"
    let name = opts.GitProjectName

    let args =
        sprintf "repo create %s %s --source=. --remote=origin" name visibility

    if Debug.DryRun then
        Debug.log (sprintf "would: gh %s" args)
        ""
    else
        let code, output = runCapture "gh" args workingDir
        if code <> 0 then
            fail (sprintf "gh repo create failed (%d):\n%s" code output)
        output.Trim()

let createGitlabRepo (opts: Options) (workingDir: string) : string =
    let visibility = if opts.Public then "--public" else "--private"
    let name = opts.GitProjectName
    let args =
        sprintf "repo create %s %s --source=. --remote=origin" name visibility

    if Debug.DryRun then
        Debug.log (sprintf "would: glab %s" args)
        ""
    else
        let code, output = runCapture "glab" args workingDir
        if code <> 0 then
            fail (sprintf "glab repo create failed (%d):\n%s" code output)
        output.Trim()