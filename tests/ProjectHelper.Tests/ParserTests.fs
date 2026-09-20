module ProjectHelper.Tests.ParserTests

open System
open Xunit
open ProjectHelper.Core.ClaParser

// Required Args

[<Fact>]
let ``no args fails`` () =
    Assert.Throws<ArgError>(fun () -> parse [||] |> ignore) |> ignore

[<Fact>]
let ``only local name (no url, no template) fails`` () =
    Assert.Throws<ArgError>(fun () -> parse [| "myproj" |] |> ignore) |> ignore

// Clone by URL

[<Fact>]
let ``url without template is allowed`` () =
    let o = parse [| "myproj"; "-u"; "https://github.com/foo/bar.git" |]
    Assert.Equal("myproj", o.LocalProjectName)
    Assert.Equal("myproj", o.GitProjectName)
    Assert.Equal(Some "https://github.com/foo/bar.git", o.Url)
    Assert.Equal(None, o.TemplateName)
    Assert.False(o.ForceTemplates)

[<Fact>]
let ``second positional sets GitProjectName`` () =
    let o = parse [| "local"; "remote"; "-u"; "u" |]
    Assert.Equal("local",  o.LocalProjectName)
    Assert.Equal("remote", o.GitProjectName)

// Create from scratch (no URL)

[<Fact>]
let ``three positionals set local/git/template`` () =
    let o = parse [| "local"; "remote"; "cs.console" |]
    Assert.Equal("local", o.LocalProjectName)
    Assert.Equal("remote", o.GitProjectName)
    Assert.Equal(Some "cs.console", o.TemplateName)

// flags

[<Fact>]
let ``-n enables nix`` () =
    Assert.True((parse [| "p"; "-u"; "u"; "-n" |]).NixMode)

[<Fact>]
let ``-p enables public`` () =
    Assert.True((parse [| "p"; "-u"; "u"; "-p" |]).Public)

[<Fact>]
let ``default git mode is github`` () =
    Assert.Equal("github", (parse [| "p"; "-u"; "u" |]).GitMode)

[<Fact>]
let ``-g sets git mode`` () =
    Assert.Equal("gitlab", (parse [| "p"; "-u"; "u"; "-g"; "gitlab" |]).GitMode)

// force

[<Fact>]
let ``-f without template fails`` () =
    Assert.Throws<ArgError>(fun () ->
        parse [| "p"; "-u"; "u"; "-f" |] |> ignore) |> ignore

[<Fact>]
let ``-f with template and url ok`` () =
    let o = parse [| "local"; "remote"; "cs.console"; "-u"; "u"; "-f" |]
    Assert.True(o.ForceTemplates)
    Assert.Equal(Some "cs.console", o.TemplateName)

// shit

[<Fact>]
let ``unknown flag fails`` () =
    Assert.Throws<ArgError>(fun () -> parse [| "p"; "--nope" |] |> ignore) |> ignore

[<Fact>]
let ``missing value for -u fails`` () =
    Assert.Throws<ArgError>(fun () -> parse [| "p"; "-u" |] |> ignore) |> ignore

[<Fact>]
let ``too many positionals fails`` () =
    Assert.Throws<ArgError>(fun () ->
        parse [| "a"; "b"; "c"; "d" |] |> ignore) |> ignore

// Home projects ("~/{projectName}" => "https://github.com/{UserName}/{projectName}.git")

[<Fact>]
let ``tilde in url expands to github.com/USER/repo`` () =
    let old = Environment.GetEnvironmentVariable "USER"
    try
        Environment.SetEnvironmentVariable("USER", "alice")
        let o = parse [| "p"; "-u"; "~/myrepo" |]
        Assert.Equal(Some "https://github.com/alice/myrepo.git", o.Url)
    finally
        Environment.SetEnvironmentVariable("USER", old)