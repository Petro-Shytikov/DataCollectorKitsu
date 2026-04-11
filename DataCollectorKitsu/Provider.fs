module DataCollectorKitsu.Provider

open System
open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks

type KitsuLinks =
    { self: string
      related: string option }

type KitsuRelationship =
    { links: KitsuLinks }

type KitsuImageDimensions =
    { width: int option
      height: int option }

type KitsuImageDimensionSet =
    { tiny: KitsuImageDimensions option
      small: KitsuImageDimensions option
      medium: KitsuImageDimensions option
      large: KitsuImageDimensions option }

type KitsuImageMeta =
    { dimensions: KitsuImageDimensionSet }

type KitsuImageSet =
    { tiny: string option
      small: string option
      medium: string option
      large: string option
      original: string option
      meta: KitsuImageMeta option }

type KitsuTitles =
    { en: string option
      en_jp: string option
      ja_jp: string option }

type KitsuAttributes =
    { createdAt: string
      updatedAt: string
      slug: string
      synopsis: string
      coverImageTopOffset: int option
      titles: KitsuTitles
      canonicalTitle: string
      abbreviatedTitles: string list
      averageRating: string option
      ratingFrequencies: Map<string, string>
      userCount: int
      favoritesCount: int
      startDate: string option
      endDate: string option
      popularityRank: int option
      ratingRank: int option
      ageRating: string option
      ageRatingGuide: string option
      subtype: string option
      status: string option
      tba: string option
      posterImage: KitsuImageSet option
      coverImage: KitsuImageSet option
      episodeCount: int option
      episodeLength: int option
      youtubeVideoId: string option
      showType: string option
      nsfw: bool }

type KitsuRelationships =
    { genres: KitsuRelationship option
      categories: KitsuRelationship option
      castings: KitsuRelationship option
      installments: KitsuRelationship option
      mappings: KitsuRelationship option
      reviews: KitsuRelationship option
      mediaRelationships: KitsuRelationship option
      episodes: KitsuRelationship option
      streamingLinks: KitsuRelationship option
      animeProductions: KitsuRelationship option
      animeCharacters: KitsuRelationship option
      animeStaff: KitsuRelationship option }

type KitsuAnimeData =
    { id: string
      ``type``: string
      links: KitsuLinks
      attributes: KitsuAttributes
      relationships: KitsuRelationships }

type KitsuAnimeResponse =
    { data: KitsuAnimeData }

type AnimeStatus =
    | Current
    | Finished
    | Tba
    | Unreleased
    | Upcoming

type AnimeSubtype =
    | ONA
    | OVA
    | TV
    | Movie
    | Music
    | Special

type AnimeSeason =
    | Spring
    | Summer
    | Fall
    | Winter

type AgeRating =
    | G
    | PG
    | R
    | R18

type AnimeFilter =
    { Text: string option
      Categories: string list
      Season: AnimeSeason option
      SeasonYear: int option
      Status: AnimeStatus option
      Subtype: AnimeSubtype option
      AgeRating: AgeRating option }

module AnimeFilter =
    let empty =
        { Text = None
          Categories = []
          Season = None
          SeasonYear = None
          Status = None
          Subtype = None
          AgeRating = None }

type KitsuAnimeListResponse =
    { data: KitsuAnimeData list }

let private serializerOptions =
    JsonSerializerOptions(PropertyNameCaseInsensitive = true)

let private deserializeAnimeResponse (content: string) : Result<KitsuAnimeResponse, string> =
    try
        let model = JsonSerializer.Deserialize<KitsuAnimeResponse>(content, serializerOptions)

        if obj.ReferenceEquals(box model, null) then
            Error "Response body is empty or invalid."
        else
            Ok model
    with ex ->
        Error (sprintf "Failed to deserialize anime response: %s" ex.Message)

let private animeStatusToString =
    function
    | Current -> "current"
    | Finished -> "finished"
    | Tba -> "tba"
    | Unreleased -> "unreleased"
    | Upcoming -> "upcoming"

let private animeSubtypeToString =
    function
    | ONA -> "ONA"
    | OVA -> "OVA"
    | TV -> "TV"
    | Movie -> "movie"
    | Music -> "music"
    | Special -> "special"

let private animeSeasonToString =
    function
    | Spring -> "spring"
    | Summer -> "summer"
    | Fall -> "fall"
    | Winter -> "winter"

let private ageRatingToString =
    function
    | G -> "G"
    | PG -> "PG"
    | R -> "R"
    | R18 -> "R18"

let private buildFilterQueryString (filter: AnimeFilter) : string =
    [ filter.Text |> Option.map (fun t -> sprintf "filter[text]=%s" (Uri.EscapeDataString t))
      (if filter.Categories.IsEmpty then
           None
       else
           filter.Categories
           |> List.map Uri.EscapeDataString
           |> String.concat ","
           |> sprintf "filter[categories]=%s"
           |> Some)
      filter.Season |> Option.map (animeSeasonToString >> sprintf "filter[season]=%s")
      filter.SeasonYear |> Option.map (sprintf "filter[seasonYear]=%d")
      filter.Status |> Option.map (animeStatusToString >> sprintf "filter[status]=%s")
      filter.Subtype |> Option.map (animeSubtypeToString >> sprintf "filter[subtype]=%s")
      filter.AgeRating |> Option.map (ageRatingToString >> sprintf "filter[ageRating]=%s") ]
    |> List.choose id
    |> String.concat "&"
    |> fun s -> if s = "" then "" else "?" + s

let private deserializeAnimeListResponse (content: string) : Result<KitsuAnimeListResponse, string> =
    try
        let model = JsonSerializer.Deserialize<KitsuAnimeListResponse>(content, serializerOptions)

        if obj.ReferenceEquals(box model, null) then
            Error "Response body is empty or invalid."
        else
            Ok model
    with ex ->
        Error (sprintf "Failed to deserialize anime list response: %s" ex.Message)

let getAnimeByFilterAsync
    (cancellationToken: CancellationToken)
    (client: HttpClient)
    (filter: AnimeFilter)
    : Task<Result<KitsuAnimeListResponse, string>> =
    task {
        let route = sprintf "anime%s" (buildFilterQueryString filter)
        let! response = client.GetAsync(route, cancellationToken)
        let! content = response.Content.ReadAsStringAsync cancellationToken

        return
            match response.IsSuccessStatusCode with
            | true -> deserializeAnimeListResponse content
            | false -> Error (sprintf "StatusCode: %d, Content: %s" (int response.StatusCode) content)
    }

let getAnimeByIdAsync
    (cancellationToken: CancellationToken)
    (client: HttpClient)
    (id: int)
    : Task<Result<KitsuAnimeResponse, string>> =
    task {
        let route = sprintf "anime/%d" id
        let! response = client.GetAsync(route, cancellationToken)
        let! content = response.Content.ReadAsStringAsync cancellationToken

        return
            match response.IsSuccessStatusCode with
            | true -> deserializeAnimeResponse content
            | false -> Error (sprintf "StatusCode: %d, Content: %s" (int response.StatusCode) content)
    }
