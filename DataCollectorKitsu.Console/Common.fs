namespace DataCollectorKitsu.Console

open System

module internal Common =
    let getAssemblyVersion () =
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()

    let getEnvironment () =
        let envVar = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        if isNull envVar then "Debug" else envVar
