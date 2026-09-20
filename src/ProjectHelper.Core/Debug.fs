module ProjectHelper.Core.Debug

open System.Collections.Generic

let mutable DryRun = false

let private actions = ResizeArray<string>()

let RecordedActions () : string list = List.ofSeq actions

let Reset () = actions.Clear()

let log (msg: string) =
    actions.Add msg
    if DryRun then printfn "[dry-run] %s" msg
    else printfn "%s" msg

let run (description: string) (action: unit -> unit) =
    if DryRun then log (sprintf "would: %s" description)
    else action ()

let runWith (description: string) (defaultValue: 'a) (action: unit -> 'a) : 'a =
    if DryRun then
        log (sprintf "would: %s" description)
        defaultValue
    else
        action ()