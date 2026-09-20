module ProjectHelper.Core.ClaParser

open System
open ProjectHelper.Core.ArgTypes

exception ArgError of string

let private fail msg = raise (ArgError msg)

type private Acc = {
    Positional       : string list
    ForceTemplates   : bool
    NixMode          : bool
    GitMode          : string
    Public           : bool
    Url              : string option
}

let private emptyAcc = {
    Positional = []
    ForceTemplates = false
    NixMode = false
    GitMode = "github"
    Public = false
    Url = None
}

let private expandTilde (s: string) =
    if s.StartsWith "~/" then
        let user =
            match Environment.GetEnvironmentVariable "USER" with
            | null | "" -> Environment.GetEnvironmentVariable "USERNAME"
            | u -> u
        let user = if isNull user || user = "" then "user" else user
        let repo = s.Substring 2
        Some (sprintf "https://github.com/%s/%s.git" user repo)
    else None

let private needsValue (flag: string) (rest: string list) =
    match rest with
    | v :: _ -> v
    | [] -> fail (sprintf "Flag %s require value" flag)

let rec private parseLoop (acc: Acc) (args: string list) : Acc =
    match args with
    // Bool Flags
    | ("-f" | "--force-templates") :: rest ->
        parseLoop { acc with ForceTemplates = true } rest
    | ("-n" | "--nix") :: rest ->
        parseLoop { acc with NixMode = true } rest
    | ("-p" | "--public") :: rest ->
        parseLoop { acc with Public = true } rest

    // Value Flags
    | ("-g" | "--git") :: rest ->
        let v = needsValue "-g/--git" rest
        parseLoop { acc with GitMode = v } (List.tail rest)
    | ("-u" | "--url") :: rest ->
        let v = needsValue "-u/--url" rest
        let normalized = defaultArg (expandTilde v) v
        parseLoop { acc with Url = Some normalized } (List.tail rest)

    // Garbage 
    | flag :: _ when flag.StartsWith "-" && flag <> "-" ->
        fail (sprintf "Unknown flags: %s" flag)

    // Pos
    | arg :: rest ->
        parseLoop { acc with Positional = arg :: acc.Positional } rest

    | [] -> acc


let parse (args: string[]) : Options =
    let args = if isNull args then [||] else args
    let acc = parseLoop emptyAcc (List.ofArray args)
    let positional = List.rev acc.Positional

    // Posisitional Args: [local] <req> [git] <opt> [template] <opt>
    let local, gitOpt, templateFromPos =
        match positional with
        | []        -> fail "Required local project name (first argument)"
        | [l]       -> l, None, None
        | [l; g]    -> l, Some g, None
        | [l; g; t] -> l, Some g, Some t
        | _         -> fail "Too many pos args you vibe shitted yourself"

    let template = templateFromPos

    match acc.Url, template with
    | None, None ->
        // No URL - Need Template To Create New Project
        fail "no repo url found, template required"
    | _ -> ()

    if acc.ForceTemplates && template.IsNone then
        fail "flag -f/--force-templates can't be used without a template"

    { LocalProjectName     = local
      GitProjectName       = defaultArg gitOpt local
      TemplateName         = template
      ForceTemplates       = acc.ForceTemplates
      NixMode              = acc.NixMode
      GitMode              = acc.GitMode
      Public               = acc.Public
      Url                  = acc.Url             }