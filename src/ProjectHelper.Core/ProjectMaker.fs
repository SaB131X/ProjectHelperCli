module ProjectHelper.Core.ProjectMaker

open System
open System.IO
open System.Diagnostics
open ProjectHelper.Core.ArgTypes
open ProjectHelper.Core.Debug
open ProjectHelper.Core.TemplateSystem
open ProjectHelper.Core.GhClient

exception ProjectError of string

let private fail msg = raise (ProjectError msg)

let private runProcess (fileName: string) (arguments: string) (workingDir: string) : int =
    let psi = ProcessStartInfo()
    psi.FileName <- fileName
    psi.Arguments <- arguments
    psi.WorkingDirectory <- workingDir
    psi.UseShellExecute <- false
    use proc = Process.Start psi
    proc.WaitForExit()
    proc.ExitCode

let private isGitRepo (dir: string) =
    Directory.Exists(Path.Combine(dir, ".git"))

let private ensureProjectDir (name: string) =
    let full = Path.GetFullPath name
    Debug.run (sprintf "mkdir -p %s" full) (fun () ->
        Directory.CreateDirectory full |> ignore)
    full

let private cloneRepo (url: string) (targetDir: string) =
    let parent  = Path.GetDirectoryName(Path.GetFullPath targetDir)
    let dirName = Path.GetFileName(Path.GetFullPath targetDir)
    Debug.run (sprintf "git clone %s %s (in %s)" url dirName parent) (fun () ->
        let code = runProcess "git" (sprintf "clone \"%s\" \"%s\"" url dirName) parent
        if code <> 0 then fail (sprintf "git clone failed (%d)" code))

let private initRepo (dir: string) =
    Debug.run "git init" (fun () ->
        let code = runProcess "git" "init" dir
        if code <> 0 then fail (sprintf "git init failed (%d)" code))

let private firstCommit (dir: string) =
    Debug.run "git add -A && git commit -m \"Initial commit\"" (fun () ->
        runProcess "git" "add -A" dir |> ignore
        let code = runProcess "git" "commit -m \"Initial commit\"" dir
        if code <> 0 then fail (sprintf "git commit failed (%d)" code))

let private setupNixEnv (dir: string) =
    Debug.run "write .envrc (use nix) && direnv allow" (fun () ->
        File.WriteAllText(Path.Combine(dir, ".envrc"), "use nix\n")
        let code = runProcess "direnv" "allow" dir
        if code <> 0 then
            eprintfn "Warning: 'direnv allow' failed (%d)" code)

let createProject (opts: Options) =
    TemplateSystem.ensureStructure() |> ignore

    let targetDir = ensureProjectDir opts.LocalProjectName

    let isNewRepo =
        match opts.Url with
        | Some url ->
            if Directory.Exists targetDir then
                let entries = Directory.GetFileSystemEntries targetDir
                if entries.Length > 0 && not (isGitRepo targetDir) then
                    fail (sprintf "Directory '%s' is not empty and not a git repo" targetDir)
            cloneRepo url targetDir
            false
        | None ->
            initRepo targetDir
            let created =
                match opts.GitMode with
                | "github" -> GhClient.createGithubRepo opts targetDir
                | "gitlab" -> GhClient.createGitlabRepo opts targetDir
                | other    -> fail (sprintf "Unknown git mode for remote create: %s" other)
            if not Debug.DryRun && String.IsNullOrWhiteSpace created then
                fail "Remote repo creation returned no URL"
            true

    let shouldApplyTemplate =
        match opts.Url, opts.TemplateName, opts.ForceTemplates with
        | None,    Some _, _     -> true   // new proj: template required
        | Some _,  Some _, true  -> true   // to clone + --force-templates
        | _                      -> false  // bad request

    if shouldApplyTemplate then
        let t = opts.TemplateName.Value
        TemplateSystem.applyTemplate t opts.NixMode targetDir

    if opts.NixMode then
        setupNixEnv targetDir

    if isNewRepo then
        firstCommit targetDir

    targetDir