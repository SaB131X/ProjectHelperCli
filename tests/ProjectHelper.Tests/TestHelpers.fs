module ProjectHelper.Tests.TestHelpers

open System
open System.IO
open ProjectHelper.Core

// if i ever need it
let makeTempDir () =
    let p = Path.Combine(Path.GetTempPath(), "ph-test-" + Guid.NewGuid().ToString "N")
    Directory.CreateDirectory p |> ignore
    p

/// Get corresponding templates:
/// $HOME/Templates/Code/<name>.gitignore
/// $HOME/Templates/Code/Shells/shell.nix
let setupTemplateFixtures (home: string) (names: string list) =
    let root   = Path.Combine(home, "Templates", "Code")
    let shells = Path.Combine(root, "Shells")
    Directory.CreateDirectory root   |> ignore
    Directory.CreateDirectory shells |> ignore
    for n in names do
        File.WriteAllText(Path.Combine(root, n + ".gitignore"), "bin/\nobj/\n")
    File.WriteAllText(
        Path.Combine(shells, "shell.nix"),
        "{ pkgs ? import <nixpkgs> {} }: pkgs.mkShell {}\n")

/// Temporary spoof HOME and USERPROFILE on action
let withHome (home: string) (action: unit -> 'a) : 'a =
    let oldHome    = Environment.GetEnvironmentVariable "HOME"
    let oldProfile = Environment.GetEnvironmentVariable "USERPROFILE"
    try
        Environment.SetEnvironmentVariable("HOME", home)
        Environment.SetEnvironmentVariable("USERPROFILE", home)
        action ()
    finally
        Environment.SetEnvironmentVariable("HOME", oldHome)
        Environment.SetEnvironmentVariable("USERPROFILE", oldProfile)

/// Enable DryRun, recover logs, do action, get record.
let withDryRun (action: unit -> unit) : string list =
    let old = Debug.DryRun
    try
        Debug.DryRun <- true
        Debug.Reset ()
        action ()
        Debug.RecordedActions ()
    finally
        Debug.DryRun <- old
        Debug.Reset ()

/// setup temp HOME + fixtures + dry-run env
/// drops temp shit after
let runDry (templateNames: string list) (action: unit -> unit) : string list =
    let home = makeTempDir ()
    try
        setupTemplateFixtures home templateNames
        withHome home (fun () -> withDryRun action)
    finally
        try Directory.Delete(home, true) with _ -> ()