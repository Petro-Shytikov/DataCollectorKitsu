module DataCollectorKitsu.WebService.Configuration

[<CLIMutable>]
type KafkaConfig =
    { BootstrapServers: string
      Topic: string }

[<CLIMutable>]
type PollingConfig =
    { IntervalSeconds: int
      TextFilter: string }

[<CLIMutable>]
type ApiConfig =
    { KitsuBaseUrl: string
      ServiceName: string }
