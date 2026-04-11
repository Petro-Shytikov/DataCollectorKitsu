module DataCollectorKitsu.WebService.Telemetry

open System.Diagnostics

[<Literal>]
let ActivitySourceName = "DataCollectorKitsu.WebService"

let activitySource = new ActivitySource(ActivitySourceName)
