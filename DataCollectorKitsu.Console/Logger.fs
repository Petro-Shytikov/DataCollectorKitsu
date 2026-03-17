module DataCollectorKitsu.Console.Logger

open Serilog
open Serilog.Events
open System.IO
open Configuration

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