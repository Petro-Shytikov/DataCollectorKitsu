open Argu
open DataCollectorKitsu.Console
open DataCollectorKitsu.Console.Common
open DataCollectorKitsu.Provider
open Serilog
open System
open System.Net.Http
open System.Threading

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CliArguments.Args>(programName = "DataCollectorKitsu.Console")
    let results = parser.Parse(argv)
    let id = results.GetResult(CliArguments.Id, 1)

    let env = getEnvironment ()
    let config = Configuration.loadConfiguration env

    Logger.setupLogger config

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
        | Ok animeResponse ->
            Log.Information("Anime fetched: {AnimeId} - {CanonicalTitle}", animeResponse.data.id, animeResponse.data.attributes.canonicalTitle)
            0
        | Error error ->
            Log.Error("Error: {Error}", error)
            1

    Log.CloseAndFlush()
    exitCode
