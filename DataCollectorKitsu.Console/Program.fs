module DataCollectorKitsu.Console

open System
open System.Net.Http
open System.Threading
open Microsoft.Extensions.Configuration

open Provider

type AppConfig = {
    BaseUrl: string
}

let getAssemblyVersion () =
    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()

let getEnvironment () =
    let envVar = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    if isNull envVar then "Debug" else envVar

let loadConfig (env: string) : AppConfig =
    let config =
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{env}.json", optional=true)
            .Build()
    {
        BaseUrl = config.["BaseUrl"]
    }

[<EntryPoint>]
let main _argv =
    let id =
        _argv
        |> Array.tryHead
        |> Option.bind (fun s -> match Int32.TryParse(s) with | true, i -> Some i | false, _ -> None)
        |> Option.defaultValue 1

    printfn "Starting DataCollectorKitsu.Console with id: %d" id

    let version = getAssemblyVersion ()
    printfn "Assembly Version: %s" version

    let env = getEnvironment ()
    printfn "Environment: %s" env

    let config = loadConfig env
    let baseUrl = config.BaseUrl
    let baseAddress = new Uri(baseUrl)
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
