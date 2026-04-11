module DataCollectorKitsu.WebService.CollectionWorker

open System
open System.Diagnostics
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open DataCollectorKitsu.Provider
open DataCollectorKitsu.WebService.Telemetry
open DataCollectorKitsu.WebService.CollectionJob
open DataCollectorKitsu.WebService.CollectionQueue
open DataCollectorKitsu.WebService.KafkaProducer

type CollectionWorker
    (queue: CollectionQueue,
     httpClientFactory: IHttpClientFactory,
     kafka: KafkaProducer,
     logger: ILogger<CollectionWorker>) =
    inherit BackgroundService()

    let processJob (client: HttpClient) (ct: CancellationToken) (job: CollectionJob) : Task<unit> =
        task {
            let activity = activitySource.StartActivity("collection.process_job")

            try
                match job with
                | CollectionJob.FetchById id ->
                    if not (isNull activity) then
                        activity.SetTag("job.type", "fetch_by_id") |> ignore
                        activity.SetTag("anime.id", id) |> ignore

                    logger.LogInformation("Processing FetchById for id {AnimeId}", id)
                    let! result = getAnimeByIdAsync ct client id

                    match result with
                    | Ok response -> do! kafka.PublishAsync(response.data)
                    | Error err ->
                        if not (isNull activity) then
                            activity.SetStatus(ActivityStatusCode.Error, err) |> ignore

                        logger.LogError("Provider error for id {AnimeId}: {Error}", id, err)

                | CollectionJob.FetchByFilter filter ->
                    if not (isNull activity) then
                        activity.SetTag("job.type", "fetch_by_filter") |> ignore

                    logger.LogInformation("Processing FetchByFilter job")
                    let! result = getAnimeByFilterAsync ct client filter

                    match result with
                    | Ok listResponse ->
                        for anime in listResponse.data do
                            do! kafka.PublishAsync(anime)
                    | Error err ->
                        if not (isNull activity) then
                            activity.SetStatus(ActivityStatusCode.Error, err) |> ignore

                        logger.LogError("Provider error for filter: {Error}", err)
            finally
                if not (isNull activity) then
                    activity.Dispose()
        }

    override _.ExecuteAsync(ct: CancellationToken) : Task =
        task {
            logger.LogInformation("CollectionWorker started")
            let client = httpClientFactory.CreateClient("Kitsu")

            try
                while not ct.IsCancellationRequested do
                    try
                        let! job = queue.Reader.ReadAsync(ct)
                        do! processJob client ct job
                    with
                    | :? OperationCanceledException -> ()
                    | ex -> logger.LogError(ex, "Unhandled error processing collection job")
            with
            | :? OperationCanceledException ->
                logger.LogInformation("CollectionWorker stopping")
        } :> Task
