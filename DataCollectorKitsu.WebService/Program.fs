module DataCollectorKitsu.WebService.Program

open System
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open OpenTelemetry.Metrics
open OpenTelemetry.Resources
open OpenTelemetry.Trace
open Serilog
open DataCollectorKitsu.WebService.Configuration
open DataCollectorKitsu.WebService.CollectionQueue
open DataCollectorKitsu.WebService.KafkaProducer
open DataCollectorKitsu.WebService.CollectionWorker
open DataCollectorKitsu.WebService.PollingWorker

[<EntryPoint>]
let main argv =
    let builder = WebApplication.CreateBuilder(argv)

    // Structured logging
    builder.Host.UseSerilog(fun ctx services loggerConfig ->
        loggerConfig
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
        |> ignore)
    |> ignore

    // Configuration sections
    builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection("Kafka")) |> ignore
    builder.Services.Configure<PollingConfig>(builder.Configuration.GetSection("Polling")) |> ignore
    builder.Services.Configure<ApiConfig>(builder.Configuration.GetSection("Api")) |> ignore

    // Named HttpClient for Kitsu — reused by CollectionWorker
    builder.Services.AddHttpClient(
        "Kitsu",
        Action<IServiceProvider, HttpClient>(fun sp client ->
            let cfg = sp.GetRequiredService<IOptions<ApiConfig>>().Value
            client.BaseAddress <- Uri(cfg.KitsuBaseUrl)))
    |> ignore

    // Singleton queue — shared between HTTP endpoints and CollectionWorker
    builder.Services.AddSingleton<CollectionQueue>() |> ignore

    // Kafka producer
    builder.Services.AddSingleton<KafkaProducer>() |> ignore

    // Background workers
    builder.Services.AddHostedService<CollectionWorker>() |> ignore
    builder.Services.AddHostedService<PollingWorker>() |> ignore

    // OpenAPI spec (served at /openapi/v1.json)
    builder.Services.AddOpenApi() |> ignore

    // OpenTelemetry
    let serviceName =
        builder.Configuration["Api:ServiceName"]
        |> Option.ofObj
        |> Option.defaultValue "DataCollectorKitsu.WebService"

    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(fun r -> r.AddService(serviceName) |> ignore)
        .WithTracing(fun t ->
            t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(Telemetry.ActivitySourceName)
                .AddConsoleExporter()
            |> ignore)
        .WithMetrics(fun m ->
            m
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter()
            |> ignore)
    |> ignore

    let app = builder.Build()

    // OpenAPI endpoint available in all environments
    app.MapOpenApi() |> ignore

    Endpoints.register app

    app.Run()
    0
