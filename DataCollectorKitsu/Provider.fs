module DataCollectorKitsu.Provider

open System.Net.Http
open System.Threading
open System.Threading.Tasks

let getAnimeByIdAsync (cancellationToken: CancellationToken) (client: HttpClient) (id: int) : Task<Result<string, string>> =
    task {
        let route = sprintf "anime/%d" id
        let! response = client.GetAsync(route, cancellationToken)
        let! content = response.Content.ReadAsStringAsync cancellationToken
        return match response.IsSuccessStatusCode with
                | true -> Ok content
                | false -> Error (sprintf "StatusCode: %d, Content: %s" (int response.StatusCode) content)
    }
