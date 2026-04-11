module DataCollectorKitsu.WebService.Endpoints

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open DataCollectorKitsu.Provider
open DataCollectorKitsu.WebService.CollectionJob
open DataCollectorKitsu.WebService.CollectionQueue

[<CLIMutable>]
type CollectByFilterRequest =
    { Text: string option
      Categories: string list option
      Season: string option
      SeasonYear: int option
      Status: string option
      Subtype: string option
      AgeRating: string option }

[<RequireQualifiedAccess>]
module private Conversion =
    let parseStatus =
        function
        | "current" -> Some Current
        | "finished" -> Some Finished
        | "tba" -> Some Tba
        | "unreleased" -> Some Unreleased
        | "upcoming" -> Some Upcoming
        | _ -> None

    let parseSubtype =
        function
        | "ONA" -> Some ONA
        | "OVA" -> Some OVA
        | "TV" -> Some TV
        | "movie" -> Some Movie
        | "music" -> Some Music
        | "special" -> Some Special
        | _ -> None

    let parseSeason =
        function
        | "spring" -> Some Spring
        | "summer" -> Some Summer
        | "fall" -> Some Fall
        | "winter" -> Some Winter
        | _ -> None

    let parseAgeRating =
        function
        | "G" -> Some G
        | "PG" -> Some PG
        | "R" -> Some R
        | "R18" -> Some R18
        | _ -> None

    let toAnimeFilter (req: CollectByFilterRequest) : AnimeFilter =
        { Text = req.Text
          Categories = req.Categories |> Option.defaultValue []
          Season = req.Season |> Option.bind parseSeason
          SeasonYear = req.SeasonYear
          Status = req.Status |> Option.bind parseStatus
          Subtype = req.Subtype |> Option.bind parseSubtype
          AgeRating = req.AgeRating |> Option.bind parseAgeRating }

let register (app: WebApplication) =
    let group = app.MapGroup("/api/anime")

    group
        .MapPost(
            "/collect/{id:int}",
            Func<int, CollectionQueue, IResult>(fun id queue ->
                if queue.TryEnqueue(CollectionJob.FetchById id) then
                    Results.Accepted(
                        sprintf "/api/anime/collect/%d" id,
                        {| id = id; status = "queued" |})
                else
                    Results.StatusCode 429))
        .WithName("CollectAnimeById")
        .WithSummary("Queue collection of a single anime by its Kitsu ID")
    |> ignore

    group
        .MapPost(
            "/collect/filter",
            Func<CollectByFilterRequest, CollectionQueue, IResult>(fun req queue ->
                let filter = Conversion.toAnimeFilter req

                if queue.TryEnqueue(CollectionJob.FetchByFilter filter) then
                    Results.Accepted(
                        "/api/anime/collect/filter",
                        {| filter = req; status = "queued" |})
                else
                    Results.StatusCode 429))
        .WithName("CollectAnimeByFilter")
        .WithSummary("Queue collection of anime matching the given filter")
    |> ignore
