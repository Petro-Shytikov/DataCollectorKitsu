module DataCollectorKitsu.WebService.KafkaProducer

open System
open System.Diagnostics
open System.Text.Json
open System.Threading.Tasks
open Confluent.Kafka
open DataCollectorKitsu.Provider
open DataCollectorKitsu.WebService.Telemetry
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open DataCollectorKitsu.WebService.Configuration

type KafkaProducer(options: IOptions<KafkaConfig>, logger: ILogger<KafkaProducer>) =
    let config = options.Value

    let producer =
        ProducerBuilder<string, string>(
            ProducerConfig(BootstrapServers = config.BootstrapServers))
            .Build()

    member _.PublishAsync(data: KitsuAnimeData) : Task<unit> =
        task {
            let activity = activitySource.StartActivity("kafka.produce")

            try
                if not (isNull activity) then
                    activity.SetTag("kafka.topic", config.Topic) |> ignore
                    activity.SetTag("anime.id", data.id) |> ignore

                let json = JsonSerializer.Serialize(data)
                let message = Message<string, string>(Key = data.id, Value = json)

                try
                    let! result = producer.ProduceAsync(config.Topic, message)

                    logger.LogInformation(
                        "Published anime {AnimeId} to {Topic} [{Offset}]",
                        data.id,
                        config.Topic,
                        result.Offset)
                with ex ->
                    if not (isNull activity) then
                        activity.SetStatus(ActivityStatusCode.Error, ex.Message) |> ignore

                    logger.LogError(ex, "Failed to publish anime {AnimeId} to Kafka", data.id)
            finally
                if not (isNull activity) then
                    activity.Dispose()
        }

    interface IDisposable with
        member _.Dispose() =
            producer.Flush(TimeSpan.FromSeconds 5.0) |> ignore
            producer.Dispose()
