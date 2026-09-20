module ProjectHelper.Tests.TemplateSystemTests

open System.IO
open Xunit
open ProjectHelper.Core
open ProjectHelper.Tests.TestHelpers

[<Fact>]
let ``ensureStructure creates Templates/Code and Shells`` () =
    let home = makeTempDir ()
    try
        withHome home (fun () ->
            let root = TemplateSystem.ensureStructure ()
            Assert.True(Directory.Exists root)
            Assert.True(Directory.Exists(Path.Combine(root, "Shells"))))
    finally
        try Directory.Delete(home, true) with _ -> ()

[<Fact>]
let ``ensureStructure is idempotent`` () =
    let home = makeTempDir ()
    try
        withHome home (fun () ->
            let r1 = TemplateSystem.ensureStructure ()
            let r2 = TemplateSystem.ensureStructure ()
            Assert.Equal(r1, r2))
    finally
        try Directory.Delete(home, true) with _ -> ()

[<Fact>]
let ``applyTemplate fails for unknown template`` () =
    let home = makeTempDir ()
    try
        setupTemplateFixtures home []
        withHome home (fun () ->
            let target = makeTempDir ()
            Assert.Throws<TemplateSystem.TemplateError>(fun () ->
                TemplateSystem.applyTemplate "cs.unknown" false target) |> ignore)
    finally
        try Directory.Delete(home, true) with _ -> ()