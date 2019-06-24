// Learn more about F# at http://fsharp.org

open System
open System.Net.Http
open System.Threading.Tasks

open FsConfig
open FSharp.Control.Tasks.V2

open Cognite.Sdk
open Cognite.Sdk.Assets

type Config = {
    [<CustomName("API_KEY")>]
    ApiKey: string
    [<CustomName("PROJECT")>]
    Project: string
}

let getAssetsExample ctx = task {
    let! rsp = getAssets [ Limit 2 ] Task.FromResult ctx

    match rsp.Result with
    | Ok res -> printfn "%A" res
    | Error err -> printfn "Error: %A" err
}

let updateAssetsExample (ctx : HttpContext) = task {

    let! rsp = updateAssets [(84025677715833721L, [ SetName "string3" ] )] Task.FromResult ctx
    match rsp.Result with
    | Ok res -> printfn "%A" res
    | Error err -> printfn "Error: %A" err
}

let createAssetsExample ctx = task {

    let assets = [{
        Name = "My new asset"
        Description = Some "My description"
        MetaData = Map.empty
        Source = None
        ParentId = None
        ExternalId = None
        ParentExternalId = None
    }]

    let request = fusion {
        let! ga = createAssets assets

        let! gb = concurrent [
            createAssets assets |> retry 500<ms> 5
        ]

        return gb
    }

    let request = concurrent [
        let chunks = Seq.chunkBySize 10 assets
        for chunk in chunks do
            yield createAssets chunk |> retry 500<ms> 5
    ]

    let! rsp = request Task.FromResult ctx
    match rsp.Result with
    | Ok res -> printfn "%A" res
    | Error err -> printfn "Error: %A" err
}

[<EntryPoint>]
let main argv =
    printfn "F# Client"

    let config =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error -> failwith "Failed to read config"

    let ctx =
        defaultContext
        |> setHttpClient (new HttpClient ())
        |> addHeader ("api-key", Uri.EscapeDataString config.ApiKey)
        |> setProject (Uri.EscapeDataString config.Project)

    getAssetsExample(ctx).Wait()

    0 // return an integer exit code



