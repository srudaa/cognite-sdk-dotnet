namespace Cognite.Sdk

open System.Collections.Generic
open System.Net.Http
open System.Runtime.CompilerServices
open System.Threading.Tasks

open FSharp.Control.Tasks.V2
open Thoth.Json.Net

open Cognite.Sdk
open Cognite.Sdk.Common
open Cognite.Sdk.Api

[<RequireQualifiedAccess>]
module GetTimeseriesAggregateData =
    [<Literal>]
    let Url = "/timeseries/data/list"

    type AggregateDataPointReadDto = {
        Timestamp: int64
        Average: float option
        Max: float option
        Min: float option
        Count: int option
        Sum: float option
        Interpolation: float option
        StepInterpolation: float option
        ContinousVariance: float option
        DiscreteVariance: float option
        TotalVariation: float option } with

        member this.Poco () = {|
            Timestamp = this.Timestamp
            Average = if this.Average.IsSome then this.Average.Value else Unchecked.defaultof<float>
            Max = if this.Max.IsSome then this.Max.Value else Unchecked.defaultof<float>
            Min = if this.Min.IsSome then this.Min.Value else Unchecked.defaultof<float>
            Count = if this.Count.IsSome then this.Count.Value else Unchecked.defaultof<int>
            Sum = if this.Sum.IsSome then this.Sum.Value else Unchecked.defaultof<float>
            Interpolation = if this.Interpolation.IsSome then this.Interpolation.Value else Unchecked.defaultof<float>
            StepInterpolation = if this.StepInterpolation.IsSome then this.StepInterpolation.Value else Unchecked.defaultof<float>
            ContinousVariance = if this.ContinousVariance.IsSome then this.ContinousVariance.Value else Unchecked.defaultof<float>
            DiscreteVariance = if this.DiscreteVariance.IsSome then this.DiscreteVariance.Value else Unchecked.defaultof<float>
            TotalVariation = if this.TotalVariation.IsSome then this.TotalVariation.Value else Unchecked.defaultof<float>
        |}


    type PointResponseAggregateDataPoints = {
        Id: int64
        ExternalId: string option
        DataPoints: AggregateDataPointReadDto seq
    }

    type AggregatePointResponse = {
        Items: PointResponseAggregateDataPoints seq
    }

    type Aggregate =
        | ContinuousVariance
        | StepInterpolation
        | DiscreteVariance
        | TotalVariation
        | Interpolation
        | Average
        | Count
        | Max
        | Min
        | Sum

        override this.ToString () =
            match this with
            | StepInterpolation -> "step"
            | ContinuousVariance -> "cv"
            | DiscreteVariance -> "dv"
            | Interpolation -> "int"
            | TotalVariation -> "tv"
            | Count -> "count"
            | Average -> "avg"
            | Max -> "max"
            | Min -> "min"
            | Sum -> "sum"

    type Granularity =
        private
        | CaseDay of int
        | CaseHour of int
        | CaseMinute of int
        | CaseSecond of int

        static member Day day = CaseDay day
        static member Hour hour = CaseHour hour
        static member Minute minute = CaseMinute minute
        static member Second second = CaseSecond second

    /// Query parameters
    type Option =
        private
        | CaseStart of string
        | CaseEnd of string
        | CaseAggregates of Aggregate seq
        | CaseGranularity of Granularity
        | CaseLimit of int32
        | CaseIncludeOutsidePoints of bool

        static member Start start = CaseStart start
        static member End end' = CaseEnd end'
        static member Aggregates ags = CaseAggregates ags
        static member Limit limit = CaseLimit limit
        static member IncludeOutsidePoints iop = CaseIncludeOutsidePoints iop

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
        getTimeseriesData defaultQueryParams queryParams fetch Async.single ctx
        |> Async.map (fun ctx -> ctx.Result)


[<AutoOpen>]
module GetTimeseriesAggregateDataApi =

    //// **Description**
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

[<Extension>]
type GetTimeseriesAggregateDataExtensions =
    /// <summary>
    /// Retrieves a list of assets matching the given criteria. This operation does not support pagination.
    /// </summary>
    ///
    ///   * `limit` - Limits the maximum number of results to be returned by single request. In case there are more
    ///   results to the request 'nextCursor' attribute will be provided as part of response. Request may contain less
    ///   results than request limit.
    ///   * `options` - Search options.
    ///   * `filters` - Search filters.
    ///
    /// <returns>Assets.</returns>
    [<Extension>]
    static member SearchAssetsAsync (this: Client, limit : int, options: SearchAssets.Option seq, filters: SearchAssets.Filter seq) : Task<GetAssets.Assets> =
        task {
            let! ctx = searchAssetsAsync limit options filters this.Ctx
            match ctx.Result with
            | Ok assets ->
                return assets
            | Error error ->
                return raise (Error.error2Exception error)
        }
