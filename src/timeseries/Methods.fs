namespace Cognite.Sdk.Timeseries

open System.Net.Http
open System.Threading.Tasks
open FSharp.Control.Tasks.V2

open Cognite.Sdk
open Cognite.Sdk.Common


[<RequireQualifiedAccess>]
module Internal =
    let getTimeseries (query: QueryParam seq) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let decoder = decodeResponse<TimeseriesResponse, TimeseriesResponse, 'a> TimeseriesResponse.Decoder id
        let url = Url
        let query = query |> Seq.map renderParams |> List.ofSeq

        GET
        >=> setVersion V10
        >=> setResource url
        >=> addQuery query
        >=> fetch
        >=> decoder

    let getTimeseriesResult (query: QueryParam seq) (fetch: HttpHandler<HttpResponseMessage, string, TimeseriesResponse>) (ctx: HttpContext) =
        let request = getTimeseries query fetch Task.FromResult

        request ctx
        |> Task.map (fun ctx -> ctx.Result)

    let insertData (items: seq<DataPointsCreateDto>) (fetch: HttpHandler<HttpResponseMessage, string, string>) =
        let request : PointRequest = { Items = items }
        let body = Encode.stringify request.Encoder
        let url = Url + "/data"

        POST
        >=> setVersion V10
        >=> setBody body
        >=> setResource url
        >=> fetch

    let insertDataResult (items: seq<DataPointsCreateDto>) (fetch: HttpHandler<HttpResponseMessage, string, string>) (ctx: HttpContext) =
        let request = insertData items fetch Task.FromResult
        request ctx
        |> Task.map (fun ctx -> ctx.Result)

    let createTimeseries (items: seq<TimeseriesCreateDto>) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let request : TimeseriesRequest = { Items = items }
        let decoder = decodeResponse TimeseriesResponse.Decoder id
        let body = Encode.stringify request.Encoder

        POST
        >=> setVersion V10
        >=> setBody body
        >=> setResource Url
        >=> fetch
        >=> decoder

    let createTimeseriesResult (items: seq<TimeseriesCreateDto>) (fetch: HttpHandler<HttpResponseMessage, string, TimeseriesResponse>) (ctx: HttpContext) =
        createTimeseries items fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

    let getTimeseriesByIds (ids: seq<int64>) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let decoder = decodeResponse TimeseriesResponse.Decoder (fun res -> res.Items)
        let url = Url + sprintf "/byids"

        let request : TimeseriesReadRequest = {
            Items = [
                for id in ids do
                    yield { Id = id }
            ]
        }
        let body = Encode.stringify request.Encoder

        POST
        >=> setVersion V10
        >=> setResource url
        >=> setBody body
        >=> fetch
        >=> decoder

    let getTimeseriesByIdsResult (ids: seq<int64>) (fetch: HttpHandler<HttpResponseMessage, string, TimeseriesReadDto seq>) (ctx: HttpContext) =
        getTimeseriesByIds ids fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

    let deleteTimeseries (name: string) (fetch: HttpHandler<HttpResponseMessage, string, string>) =
        let url = Url + sprintf "/%s" name

        DELETE
        >=> setVersion V05
        >=> setResource url
        >=> fetch

    let deleteTimeseriesResult (name: string) (fetch: HttpHandler<HttpResponseMessage, string, string>) (ctx: HttpContext) =
        deleteTimeseries name fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

    let getTimeseriesData (defaultArgs: QueryDataParam seq) (args: (int64*(QueryDataParam seq)) seq) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let url = Url + "/data/list"
        let decoder = decodeResponse PointResponse.Decoder (fun res -> res.Items)
        let request = renderDataQuery defaultArgs args
        let body = Encode.stringify request

        POST
        >=> setVersion V10
        >=> setResource url
        >=> setBody body
        >=> fetch
        >=> decoder

    let getTimeseriesDataResult (defaultQueryParams: QueryDataParam seq) (queryParams: (int64*(QueryDataParam seq)) seq) (fetch: HttpHandler<HttpResponseMessage, string, seq<PointResponseDataPoints>>) (ctx: HttpContext) =
        getTimeseriesData defaultQueryParams queryParams fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

    let getTimeseriesLatestData (queryParams: LatestDataRequest seq) (fetch: HttpHandler<HttpResponseMessage, string, 'a>) =
        let url = Url + "/data/latest"
        let decoder = decodeResponse PointResponse.Decoder (fun res -> res.Items)
        let request = { Items = queryParams }
        let body = request.Encoder |> Encode.stringify

        POST
        >=> setVersion V10
        >=> setResource url
        >=> setBody body
        >=> fetch
        >=> decoder

    let getTimeseriesLatestDataResult (queryParams: LatestDataRequest seq) (fetch: HttpHandler<HttpResponseMessage, string, seq<PointResponseDataPoints>>) (ctx: HttpContext) =
        getTimeseriesLatestData queryParams fetch Task.FromResult ctx
        |> Task.map (fun ctx -> ctx.Result)

[<AutoOpen>]
module Methods =

    /// **Description**
    ///
    /// Retrieves a list of all time series in a project, sorted by name
    /// alphabetically. Parameters can be used to select a subset of time
    /// series. This operation supports pagination.
    ///
    /// https://doc.cognitedata.com/api/v1/#operation/getTimeSeries
    ///
    /// **Parameters** * `query` - parameter of type `seq<QueryParams>` * `ctx`
    /// - parameter of type `HttpContext`
    ///
    /// **Output Type** * `HttpHandler<FSharp.Data.HttpResponse,TimeseriesResponse>`
    ///
    let getTimeseries (query: QueryParam seq) (next: NextHandler<TimeseriesResponse,'a>) (ctx: HttpContext) =
        Internal.getTimeseries query fetch next ctx

    /// **Description**
    ///
    /// Inserts a list of data points to a time series. If a data point is
    /// posted to a timestamp that is already in the series, the existing
    /// data point for that timestamp will be overwritten.
    ///
    /// **Parameters**
    ///   * `name` - The name of the timeseries to insert data points into.
    ///   * `items` - The list of data points to insert.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<HttpResponse,ResponseError>>`
    ///
    let insertDataByName (items: DataPointsCreateDto list) (next: NextHandler<string,string>) (ctx: HttpContext) =
        Internal.insertData items fetch next ctx

    /// **Description**
    ///
    /// Create new timeseries
    ///
    /// **Parameters**
    ///   * `items` - The list of timeseries to create.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<HttpResponse,ResponseError>>`
    ///
    let createTimeseries (items: TimeseriesCreateDto list) (next: NextHandler<TimeseriesResponse,'a>) (ctx: HttpContext) =
        Internal.createTimeseries items fetch next ctx

    /// **Description**
    ///
    /// Get timeseries with given id. Retrieves the details of an existing time
    /// series given a project id and the unique time series identifier
    /// generated when the time series was created.
    ///
    /// **Parameters**
    ///   * `id` - The id of the timeseries to get.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<TimeseriesReadDto list,exn>>`
    ///
    let getTimeseriesByIds (ids: seq<int64>) (next: NextHandler<TimeseriesReadDto seq,'a>) (ctx: HttpContext) =
        Internal.getTimeseriesByIds ids fetch next ctx

    /// **Description**
    ///
    /// Retrieves a list of data points from multiple time series in the same project
    ///
    /// **Parameters**
    ///   * `name` - parameter of type `string`
    ///   * `query` - parameter of type `QueryParams seq`
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<HttpResponse,ResponseError>>`
    ///
    let getTimeseriesData (defaultArgs: QueryDataParam seq) (args: (int64*(QueryDataParam seq)) seq) (next: NextHandler<PointResponseDataPoints seq,'a>) (ctx: HttpContext) =
        Internal.getTimeseriesData defaultArgs args fetch next ctx


    /// **Description**
    ///
    /// Retrieves the single latest data point in a time series.
    ///
    /// **Parameters**
    ///   * `queryParams` - parameter of type `seq<QueryLatestParam>`
    ///   * `ctx` - parameter of type `HttpContext`
    ///
    /// **Output Type**
    ///   * `Async<Context<seq<PointResponseDataPoints>>>`
    ///
    let getTimeseriesLatestData (queryParams: LatestDataRequest seq) (next: NextHandler<PointResponseDataPoints seq,'a>) (ctx: HttpContext) =
        Internal.getTimeseriesLatestData queryParams fetch next ctx

    /// **Description**
    ///
    /// Deletes a time series object given the name of the time series.
    ///
    /// **Parameters**
    ///   * `name` - The name of the timeseries to delete.
    ///   * `ctx` - The request HTTP context to use.
    ///
    /// **Output Type**
    ///   * `Async<Result<HttpResponse,ResponseError>>`
    ///
    let deleteTimeseries (name: string) (next: NextHandler<string,string>) (ctx: HttpContext) =
        Internal.deleteTimeseries name fetch next ctx