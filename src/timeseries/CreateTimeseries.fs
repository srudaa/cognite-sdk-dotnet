namespace Fusion

open System.IO
open System.Net.Http
open System.Runtime.CompilerServices
open System.Threading.Tasks
open System.Runtime.InteropServices
open System.Threading

open Thoth.Json.Net

open Fusion
open Fusion.Common
open Fusion.Api
open Fusion.Timeseries

[<RequireQualifiedAccess>]
module CreateTimeseries =
    [<Literal>]
    let Url = "/timeseries"

    type TimeseriesRequest = {
        Items: seq<TimeseriesWriteDto>
    } with
        member this.Encoder =
            Encode.object [
                yield ("items", Seq.map (fun (it: TimeseriesWriteDto) -> it.Encoder) this.Items |> Encode.seq)
            ]

    type TimeseriesResponse = {
        Items: TimeseriesReadDto seq
    } with
        static member Decoder : Decoder<TimeseriesResponse> =
            Decode.object (fun get -> {
                Items = get.Required.Field "items" (Decode.list TimeseriesReadDto.Decoder)
            })

    let createTimeseries (items: seq<TimeseriesWriteDto>) (fetch: HttpHandler<HttpResponseMessage, Stream, 'a>) =
        let request : TimeseriesRequest = { Items = items }
        let decoder = decodeResponse TimeseriesResponse.Decoder id

        POST
        >=> setVersion V10
        >=> setContent (Content.JsonValue request.Encoder)
        >=> setResource Url
        >=> fetch
        >=> decoder

[<AutoOpen>]
module CreateTimeseriesApi =
    /// <summary>
    /// Create one or more new timeseries.
    /// </summary>
    /// <param name="items">The list of timeseries to create.</param>
    /// <param name="next">Async handler to use.</param>
    /// <returns>List of created timeseries.</returns>
    let createTimeseries (items: TimeseriesWriteDto list) (next: NextHandler<CreateTimeseries.TimeseriesResponse,'a>) =
        CreateTimeseries.createTimeseries items fetch next

    /// <summary>
    /// Create one or more new timeseries.
    /// </summary>
    /// <param name="items">The list of timeseries to create.</param>
    /// <returns>List of created timeseries.</returns>
    let createTimeseriesAsync (items: seq<TimeseriesWriteDto>) =
        CreateTimeseries.createTimeseries items fetch Async.single

[<Extension>]
type CreateTimeseriesExtensions =
    /// <summary>
    /// Create one or more new timeseries.
    /// </summary>
    /// <param name="items">The list of timeseries to create.</param>
    /// <returns>List of created timeseries.</returns>
    [<Extension>]
    static member CreateTimeseriesAsync (this: Client, items: seq<TimeseriesWritePoco>, [<Optional>] token: CancellationToken) : Task<TimeseriesReadPoco seq> =
        async {
            let items' = items |> Seq.map TimeseriesWriteDto.FromPoco
            let! ctx = createTimeseriesAsync items' this.Ctx
            match ctx.Result with
            | Ok response ->
                return response.Items |> Seq.map (fun ts -> ts.ToPoco ())
            | Error error ->
                let err = error2Exception error
                return raise err
        } |> fun op -> Async.StartAsTask(op, cancellationToken = token)
