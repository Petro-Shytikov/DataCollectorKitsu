module DataCollectorKitsu.Console

open Argu
open Microsoft.Extensions.Configuration
open System
open System.Net.Http
open System.Threading

open Provider

type AppConfig = {
    BaseUrl: string
}

type Args =
    | Id of id:int
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Id _ -> "The ID to fetch anime data for."

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
let main argv =
    let parser = ArgumentParser.Create<Args>(programName = "DataCollectorKitsu.Console")
    let results = parser.Parse(argv)
    let id = results.GetResult(Id, 1)

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
