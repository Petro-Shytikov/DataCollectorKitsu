namespace DataCollectorKitsu.Console

open Argu

[<RequireQualifiedAccess>]
module internal CliArguments =
    type Args =
        | Id of id:int
    with
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Id _ -> "The ID to fetch anime data for."
