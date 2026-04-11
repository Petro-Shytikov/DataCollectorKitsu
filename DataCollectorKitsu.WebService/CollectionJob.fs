module DataCollectorKitsu.WebService.CollectionJob

open DataCollectorKitsu.Provider

type CollectionJob =
    | FetchById of id: int
    | FetchByFilter of filter: AnimeFilter
