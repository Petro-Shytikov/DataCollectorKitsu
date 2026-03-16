open Argu
open DataCollectorKitsu.Console.Configuration
open DataCollectorKitsu.Provider
open Serilog
open Serilog.Events
open System
open System.IO
open System.Net.Http
open System.Threading

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

let setupLogger (config: AppConfiguration) =
    let allLogsPath = Path.Combine(config.LogDirectory, "all-.txt")
    let infoLogsPath = Path.Combine(config.LogDirectory, "info-.txt")
    let errorLogsPath = Path.Combine(config.LogDirectory, "errors-.txt")

    let infoLogger = 
        LoggerConfiguration()
            .Filter.ByIncludingOnly(fun e -> e.Level <= LogEventLevel.Information)
            .WriteTo.File(infoLogsPath, rollingInterval = RollingInterval.Day)
            .CreateLogger()

    let errorLogger = 
        LoggerConfiguration()
            .Filter.ByIncludingOnly(fun e -> e.Level >= LogEventLevel.Error)
            .WriteTo.File(errorLogsPath, rollingInterval = RollingInterval.Day)
            .CreateLogger()

    Log.Logger <- 
        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(allLogsPath, rollingInterval = RollingInterval.Day)
            .WriteTo.Logger(infoLogger)
            .WriteTo.Logger(errorLogger)
            .CreateLogger()

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Args>(programName = "DataCollectorKitsu.Console")
    let results = parser.Parse(argv)
    let id = results.GetResult(Id, 1)

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
