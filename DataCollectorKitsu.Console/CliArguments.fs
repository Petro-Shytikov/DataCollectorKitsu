module DataCollectorKitsu.Console.CliArguments

open Argu

type Args =
    | Id of id:int
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Id _ -> "The ID to fetch anime data for."