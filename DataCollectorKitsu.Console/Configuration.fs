module DataCollectorKitsu.Console.Configuration

open Microsoft.Extensions.Configuration

type AppConfiguration = {
    BaseUrl: string
    LogDirectory: string
}

let loadConfiguration (env: string) : AppConfiguration =
    let config =
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{env}.json", optional=true)
            .Build()
    {
        BaseUrl = config.["BaseUrl"]
        LogDirectory = config.["LogDirectory"]
    }