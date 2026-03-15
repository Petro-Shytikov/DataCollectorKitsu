module DataCollectorKitsu.Console

open System
open System.Net.Http
open System.Threading

open Provider

[<EntryPoint>]
let main _argv =
    let id =
        _argv
        |> Array.tryHead
        |> Option.bind (fun s -> match Int32.TryParse(s) with | true, i -> Some i | false, _ -> None)
        |> Option.defaultValue 1

    printfn "Starting DataCollectorKitsu.Console with id: %d" id

    let baseAddress = new Uri("https://kitsu.io/api/edge/")
    use client = new HttpClient(BaseAddress = baseAddress)
    let result =
        getAnimeByIdAsync (new CancellationToken()) client id
            |> Async.AwaitTask
            |> Async.RunSynchronously

    match result with
    | Ok content ->
        printfn "Content: %s" content
        0
    | Error error ->
        printfn "Error: %s" error
        1
