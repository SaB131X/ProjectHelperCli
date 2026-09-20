module ProjectHelper.Tests.ProjectMakerTests

open Xunit
open ProjectHelper.Core
open ProjectHelper.Core.ArgTypes
open ProjectHelper.Tests.TestHelpers

let private baseOpts : Options = {
    LocalProjectName = "myproj"
    GitProjectName   = "myproj"
    TemplateName     = Some "cs.console"
    ForceTemplates   = false
    NixMode          = false
    GitMode          = "github"
    Public           = false
    Url              = None
}

// Create from scratch (no URL)

[<Fact>]
let ``from scratch: mkdir + git init + gh create + template + commit`` () =
    let actions =
        runDry ["cs.console"] (fun () ->
            ProjectMaker.createProject baseOpts |> ignore)
        |> String.concat "\n"

    Assert.Contains("mkdir",          actions)
    Assert.Contains("git init",       actions)
    Assert.Contains("gh repo create", actions)
    Assert.Contains("dotnet new",     actions)
    Assert.Contains(".gitignore",     actions)
    Assert.Contains("git commit",     actions)

// Clone by URL (no force)

[<Fact>]
let ``from url: clone only, no template, no gh, no commit`` () =
    let opts = { baseOpts with Url = Some "https://x/y.git" }
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject opts |> ignore)
        |> String.concat "\n"

    Assert.Contains("git clone",         actions)
    Assert.DoesNotContain("git init",    actions)
    Assert.DoesNotContain("gh repo create", actions)
    Assert.DoesNotContain("dotnet new",  actions)
    Assert.DoesNotContain("git commit",  actions)

// Clone by URL + force

[<Fact>]
let ``from url + force-templates: clone then apply template, no gh`` () =
    let opts = { baseOpts with
                    Url = Some "https://x/y.git"
                    ForceTemplates = true }
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject opts |> ignore)
        |> String.concat "\n"

    Assert.Contains("git clone",        actions)
    Assert.Contains("dotnet new",       actions)
    Assert.DoesNotContain("git init",   actions)
    Assert.DoesNotContain("gh repo create", actions)

// Nix mode

[<Fact>]
let ``nix mode copies shell.nix and writes .envrc + direnv allow`` () =
    let opts = { baseOpts with NixMode = true }
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject opts |> ignore)
        |> String.concat "\n"

    Assert.Contains("shell.nix", actions)
    Assert.Contains(".envrc",    actions)
    Assert.Contains("direnv",    actions)

[<Fact>]
let ``nix mode off does not touch shell.nix`` () =
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject baseOpts |> ignore)
        |> String.concat "\n"

    Assert.DoesNotContain("shell.nix", actions)
    Assert.DoesNotContain("direnv",    actions)

// Project Visibility(Private/Public)

[<Fact>]
let ``public passes --public to gh`` () =
    let opts = { baseOpts with Public = true }
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject opts |> ignore)
        |> String.concat "\n"

    Assert.Contains("--public", actions)

[<Fact>]
let ``private by default`` () =
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject baseOpts |> ignore)
        |> String.concat "\n"

    Assert.Contains("--private", actions)

// Git modes

[<Fact>]
let ``gitlab mode uses glab`` () =
    let opts = { baseOpts with GitMode = "gitlab" }
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject opts |> ignore)
        |> String.concat "\n"

    Assert.Contains("glab", actions)

[<Fact>]
let ``unknown git mode fails`` () =
    let opts = { baseOpts with GitMode = "bitbucket" }
    Assert.Throws<ProjectMaker.ProjectError>(fun () ->
        ProjectMaker.createProject opts |> ignore) |> ignore

// URL + force + nix + public

[<Fact>]
let ``combo: url + force + nix + public + gitlab`` () =
    let opts = { baseOpts with
                    Url            = Some "https://x/y.git"
                    ForceTemplates = true
                    NixMode        = true
                    Public         = true
                    GitMode        = "gitlab" }
    let actions =
        runDry ["cs.console"] (fun () -> ProjectMaker.createProject opts |> ignore)
        |> String.concat "\n"

    Assert.Contains("git clone",  actions)
    Assert.Contains("dotnet new", actions)
    Assert.Contains("shell.nix",  actions)
    Assert.Contains("direnv",     actions)