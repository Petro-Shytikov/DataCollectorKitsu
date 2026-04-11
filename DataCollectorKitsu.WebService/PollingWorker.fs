module DataCollectorKitsu.WebService.PollingWorker

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open DataCollectorKitsu.Provider
open DataCollectorKitsu.WebService.Configuration
open DataCollectorKitsu.WebService.CollectionJob
open DataCollectorKitsu.WebService.CollectionQueue

type PollingWorker
    (queue: CollectionQueue,
     options: IOptions<PollingConfig>,
     logger: ILogger<PollingWorker>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) : Task =
        task {
            let config = options.Value
            let interval = TimeSpan.FromSeconds(float config.IntervalSeconds)
            logger.LogInformation("PollingWorker started, interval: {Interval}", interval)

            use timer = new PeriodicTimer(interval)

            try
                while! timer.WaitForNextTickAsync(ct) do
                    logger.LogInformation("Polling tick — enqueuing collection job")

                    let filter =
                        if String.IsNullOrWhiteSpace config.TextFilter then
                            { AnimeFilter.empty with Status = Some Current }
                        else
                            { AnimeFilter.empty with Text = Some config.TextFilter }

                    let enqueued = queue.TryEnqueue(CollectionJob.FetchByFilter filter)

                    if enqueued then
                        logger.LogInformation("Polling job enqueued")
                    else
                        logger.LogWarning("Polling job dropped — queue full")
            with
            | :? OperationCanceledException ->
                logger.LogInformation("PollingWorker stopping")
        } :> Task
