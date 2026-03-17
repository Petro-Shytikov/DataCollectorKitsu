open Argu
open DataCollectorKitsu.Console.Common
open DataCollectorKitsu.Console.Configuration
open DataCollectorKitsu.Console.Logger
open DataCollectorKitsu.Provider
open Serilog
open System
open System.Net.Http
open System.Threading

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<DataCollectorKitsu.Console.CliArguments.Args>(programName = "DataCollectorKitsu.Console")
    let results = parser.Parse(argv)
    let id = results.GetResult(DataCollectorKitsu.Console.CliArguments.Id, 1)

    let env = getEnvironment ()
    let config = loadConfiguration env

    setupLogger config

    let version = getAssemblyVersion ()
    Log.Information("Assembly Version: {Version}", version)
    Log.Information("Environment: {Environment}", env)
    Log.Information("Starting DataCollectorKitsu.Console with id: {Id}", id)

    let baseUrl = config.BaseUrl
    let baseAddress = new Uri(baseUrl)
    use client = new HttpClient(BaseAddress = baseAddress)
    let result =
        getAnimeByIdAsync (new CancellationToken()) client id
            |> Async.AwaitTask
            |> Async.RunSynchronously

    let exitCode = 
        match result with
        | Ok content ->
            Log.Information("Content: {Content}", content)
            0
        | Error error ->
            Log.Error("Error: {Error}", error)
            1

    Log.CloseAndFlush()
    exitCode
