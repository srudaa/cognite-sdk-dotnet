namespace Cognite.Sdk.Assets

open System.Net
open System.Net.Http
open System.Threading.Tasks

open FSharp.Control.Tasks.V2

open Cognite.Sdk
open Cognite.Sdk.Assets
open Cognite.Sdk.Common


[<RequireQualifiedAccess>]
module Internal =

    let getAssets (args: GetParams seq) (fetch: HttpHandler<HttpResponseMessage,string, 'a>) =
        let decoder = decodeResponse AssetResponse.Decoder id
        let query = args |> Seq.map renderParams |> List.ofSeq

        GET
        >=> setVersion V10
        >=> addQuery query
        >=> setResource Url
        >=> fetch
        >=> decoder

    let getAssetsResult (args: GetParams seq) (fetch: HttpHandler<HttpResponseMessage, string, AssetResponse>) (ctx: HttpContext) =
        getAssets args fetch Task.FromResult ctx
        |> Task.map (fun decoded -> decoded.Result)

    let getAssetsByIds (ids: Identity seq) (fetch: HttpHandler<HttpResponseMessage, string, AssetResponse>) =
        let decoder = decodeResponse AssetResponse.Decoder id
        let body = "" // FIXME:
        let url = Url + "byids"

        POST
        >=> setVersion V10
        >=> setBody body
        >=> setResource Url
        >=> fetch
        >=> decoder

    let getAssetsByIdsResult (ids: Identity seq)  (fetch: HttpHandler<HttpResponseMessage, string, AssetResponse>) (ctx: HttpContext) =
        getAssetsByIds ids fetch Task.FromResult ctx
        |> Task.map (fun decoded -> decoded.Result)

    let getAsset (assetId: int64) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let decoder = decodeResponse AssetReadDto.Decoder id
        let url = Url + sprintf "/%d" assetId

        GET
        >=> setVersion V10
        >=> setResource url
        >=> fetch
        >=> decoder

    let getAssetResult (assetId: int64) (fetch: HttpHandler<HttpResponseMessage, string, AssetReadDto>) (ctx: HttpContext) =
        getAsset assetId fetch Task.FromResult ctx
        |> Task.map (fun decoded -> decoded.Result)

    let createAssets (assets: AssetCreateDto seq) (fetch: HttpHandler<HttpResponseMessage,string,'a>)  =
        let decoder = decodeResponse AssetResponse.Decoder (fun res -> res.Items)
        let request : AssetsCreateRequest = { Items = assets }
        let body = Encode.stringify  request.Encoder

        POST
        >=> setVersion V10
        >=> setBody body
        >=> setResource Url
        >=> fetch
        >=> decoder

    let createAssetsResult (assets: AssetCreateDto seq) (fetch: HttpHandler<HttpResponseMessage, string, seq<AssetReadDto>>) (ctx: HttpContext) =
        createAssets assets fetch Task.FromResult ctx
        |> Task.map (fun context -> context.Result)

    let deleteAssets (assets: Identity seq) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let request : AssetsDeleteRequest = { Items = assets }
        let body = Encode.stringify  request.Encoder

        POST
        >=> setVersion V10
        >=> setBody body
        >=> setResource Url
        >=> fetch

    let deleteAssetsResult<'a> (assets: Identity seq) (fetch: HttpHandler<HttpResponseMessage, string, string>) (ctx: HttpContext) =
        deleteAssets assets fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

    let updateAssets (args: (int64*UpdateParams list) list) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let request : AssetsUpdateRequest = {
            Items = [
                for (assetId, args) in args do
                    yield { Id = assetId; Params = args }
            ]
        }

        let body = Encode.stringify request.Encoder
        let url = Url + sprintf "/update"

        POST
        >=> setVersion V10
        >=> setBody body
        >=> setResource url
        >=> fetch

    let updateAssetsResult (args: (int64*UpdateParams list) list) (fetch: HttpHandler<HttpResponseMessage, string, string>) (ctx: HttpContext) =
        updateAssets args fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

[<AutoOpen>]
module Methods =

    /// **Description**
    ///
    /// Retrieve a list of all assets in the given project. The list is sorted alphabetically by name. This operation
    /// supports pagination.
    ///
    /// You can retrieve a subset of assets by supplying additional fields; Only assets satisfying all criteria will be
    /// returned. Names and descriptions are fuzzy searched using edit distance. The fuzziness parameter controls the
    /// maximum edit distance when considering matches for the name and description fields.
    ///
    /// **Parameters**
    ///   * `args` - list of parameters for getting assets.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<Response,exn>>`
    ///
    let getAssets (args: seq<GetParams>) (next: NextHandler<AssetResponse,'a>) (ctx: HttpContext) : Task<Context<'a>> =
        Internal.getAssets args fetch next ctx

    //let searchAssets (args: )

    /// **Description**
    ///
    /// Retrieves information about an asset in a certain project given an asset id.
    ///
    /// **Parameters**
    ///   * `assetId` - The id of the attet to retrieve.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<Response,exn>>`
    ///
    let getAsset (assetId: int64) (next: NextHandler<AssetReadDto,'a>) (ctx: HttpContext) : Task<Context<'a>> =
        Internal.getAsset assetId fetch next ctx

    /// **Description**
    ///
    /// Creates new assets in the given project
    ///
    /// **Parameters**
    ///   * `assets` - The list of assets to create.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<Response,exn>>`
    ///
    let createAssets (assets: AssetCreateDto seq) (next: NextHandler<seq<AssetReadDto>,'a>) (ctx: HttpContext) =
        Internal.createAssets assets fetch next ctx

    /// **Description**
    ///
    /// Deletes multiple assets in the same project, along with all their descendants in the asset hierarchy.
    ///
    /// **Parameters**
    ///   * `assets` - The list of asset ids to delete.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<HttpResponse,ResponseError>>`
    ///
    let deleteAssets (assets: Identity seq) (next: NextHandler<string,'a>) (ctx: HttpContext) =
        Internal.deleteAssets assets fetch next ctx

    /// **Description**
    ///
    /// Updates multiple assets within the same project. Updating assets does not replace the existing asset hierarchy.
    /// This operation supports partial updates, meaning that fields omitted from the requests are not changed.
    ///
    /// **Parameters**
    ///   * `args` - parameter of type `(int64 * UpdateArgs seq) seq`
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<string,exn>>`
    ///
    let updateAssets (args: (int64*(UpdateParams list)) list) (next: NextHandler<string,'a>) (ctx: HttpContext) =
        Internal.updateAssets args fetch next ctx
