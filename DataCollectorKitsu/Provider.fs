module DataCollectorKitsu.Provider

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
