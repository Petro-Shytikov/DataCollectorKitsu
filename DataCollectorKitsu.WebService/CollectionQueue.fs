module DataCollectorKitsu.WebService.CollectionQueue

open System.Threading.Channels
open DataCollectorKitsu.WebService.CollectionJob

type CollectionQueue(capacity: int) =
    let channel =
        Channel.CreateBounded<CollectionJob>(
            BoundedChannelOptions(capacity, FullMode = BoundedChannelFullMode.DropOldest))

    new() = CollectionQueue(1000)

    member _.TryEnqueue(job: CollectionJob) : bool =
        channel.Writer.TryWrite(job)

    member _.Reader : ChannelReader<CollectionJob> =
        channel.Reader
