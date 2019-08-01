namespace Fusion

open System
open System.Collections.Generic
open System.Net.Http
open System.IO
open System.Threading.Tasks
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading

open Thoth.Json.Net

open Fusion
open Fusion.Api
open Fusion.Common
open Fusion.Timeseries

[<RequireQualifiedAccess>]
module UpdateTimeseries =
    [<Literal>]
    let Url = "/timeseries/update"
    
    type MetaDataChange = {
        Add : Map<string, string> option
        Remove : string seq
    }

    type MetaDataUpdate =
        | Set of Map<string, string>
        | Change of MetaDataChange
    
    type SecurityCategoriesChange = {
        Add : int64 seq
        Remove : int64 seq
    }

    type SecurityCategoriesUpdate =
        | Set of int64 seq
        | Change of SecurityCategoriesChange

    type Option =
        private
        | CaseExternalId of string option
        | CaseName of string option
        | CaseMetaData of MetaDataUpdate option
        | CaseUnit of string option
        | CaseAssetId of int64 option
        | CaseDescription of string option
        | CaseSecurityCategories of SecurityCategoriesUpdate option
        /// Set the externalId of the timeseries
        static member SetExternalId externalId =
            CaseExternalId externalId
        /// Clear the externalId of the timeseries
        static member ClearExternalId =
            CaseExternalId None
        /// Set the name of the timeseries
        static member SetName name =
            CaseName name
        /// Clear the name of the timeseries
        static member ClearName =
            CaseName None
        /// Set the metadata of the timeseries. This removes any old metadata
        static member SetMetaData (md : IDictionary<string, string>) =
            md |> Seq.map (|KeyValue|) |> Map.ofSeq |> MetaDataUpdate.Set |> Some |> CaseMetaData
        /// Set the metadata of the timeseries. This removes any old metadata
        static member SetMetaData (md : Map<string, string>) =
            md |> MetaDataUpdate.Set |> Some |> CaseMetaData
        /// Clear the metadata of the timeseries
        static member ClearMetaData () =
            CaseMetaData None
        /// Change the metadata of the timeseries, adding any new metadata given in `add` and removing keys given in `remove`
        static member ChangeMetaData (add: IDictionary<string, string>, remove: string seq) : Option =
            ({
                Add =
                    if isNull add then
                        None
                    else
                        add |> Seq.map (|KeyValue|) |> Map.ofSeq |> Some
                Remove = if isNull remove then Seq.empty else remove
            } : MetaDataChange) |> MetaDataUpdate.Change |> Some |> CaseMetaData
        /// Change the metadata of the timeseries, adding any new metadata given in `add` and removing keys given in `remove`
        static member ChangeMetaData (add: Map<string, string> option, remove: string seq) : Option =
            ({
                Add = add
                Remove = remove
            } : MetaDataChange) |> MetaDataUpdate.Change |> Some |> CaseMetaData
        /// Set the unit of the data in the timeseries
        static member SetUnit unit =
            CaseUnit unit
        /// Clear the unit of the data in the timeseries
        static member ClearUnit =
            CaseUnit None
        /// Set the asset id of the timeseries
        static member SetAssetId assetId =
            CaseAssetId assetId
        /// Clear the asset id of the timeseries
        static member ClearAssetId () =
            CaseAssetId None
        /// Set the description of the timeseries
        static member SetDescription description =
            CaseDescription description
        /// Clear the description of the timeseries
        static member ClearDescription =
            CaseDescription None
        /// Set the security categories of the timeseries. This removes any old security categories
        static member SetSecurityCategories (ct : int64 seq) =
            ct |> Set |> Some |> CaseSecurityCategories
        /// Clear the security categories of the timeseries
        static member ClearSecurityCategories () =
            CaseSecurityCategories None
        /// Change the security categories of the timeseries, adding any in `add` and removing any in `remove`
        static member ChangeSecurityCategories (add: int64 seq) (remove: int64 seq) : Option =
            {
                Add = if isNull add then Seq.empty else add
                Remove = if isNull remove then Seq.empty else remove
            } |> Change |> Some |> CaseSecurityCategories

    let renderUpdateField (arg: Option) =
        match arg with
        | CaseExternalId id ->
            "externalId", Encode.object [
                match id with
                | Some value -> yield "set", Encode.string value
                | None -> yield "setNull", Encode.bool true
            ]
        | CaseName name ->
            "name", Encode.object [
                match name with
                | Some value -> yield "set", Encode.string value
                | None -> yield "setNull", Encode.bool true
            ]
        | CaseMetaData optMeta ->
            "metadata", Encode.object [
                match optMeta with
                | Some meta ->
                    match meta with
                    | MetaDataUpdate.Set data ->
                        yield "set", Encode.propertyBag data
                    | MetaDataUpdate.Change data ->
                        if data.Add.IsSome then yield "add", Encode.propertyBag data.Add.Value
                        yield "remove", Encode.seq (Seq.map Encode.string data.Remove)
                | None -> ()
            ]
        | CaseUnit unt ->
            "unit", Encode.object [
                match unt with
                | Some value -> yield "set", Encode.string value
                | None -> yield "setNull", Encode.bool true
            ]
        | CaseAssetId id ->
            "assetId", Encode. object [
                match id with
                | Some value -> yield "set", Encode.int64 value
                | None -> yield "setNull", Encode.bool true
            ]
        | CaseDescription desc ->
            "description", Encode.object [
                match desc with
                | Some value -> yield "set", Encode.string value
                | None -> yield "setNull", Encode.bool true
            ]
        | CaseSecurityCategories cat ->
            "securityCategories", Encode.object [
                match cat with
                | Some ct ->
                    match ct with
                    | Set data ->
                        yield "set", Encode.int53seq data
                    | Change data ->
                        yield "add", Encode.int53seq data.Add
                        yield "remove", Encode.int53seq data.Remove
                | None -> ()
            ]
    type TimeseriesUpdateRequest = {
        Id: Identity
        Params: Option seq
    } with
        member this.Encoder =
            Encode.object [
                yield
                    match this.Id with
                    | Identity.CaseId id -> "id", Encode.int53 id
                    | Identity.CaseExternalId id -> "externalId", Encode.string id
                yield "update", Encode.object [
                    yield! this.Params |> Seq.map(renderUpdateField)
                ]
            ]

    type TimeseriesUpdateRequests = {
        Items: TimeseriesUpdateRequest seq
    } with
        member this.Encoder =
            Encode.object [
                "items", this.Items |> Seq.map(fun item -> item.Encoder) |> Encode.seq
            ]
    type TimeseriesResponse = {
        Items: TimeseriesReadDto seq
    } with
        static member Decoder : Decoder<TimeseriesResponse> =
            Decode.object (fun get -> {
                Items = get.Required.Field "items" (Decode.list TimeseriesReadDto.Decoder)
            })
    
    let updateTimeseries (args: (Identity * Option list) list) (fetch: HttpHandler<HttpResponseMessage, Stream, 'a>) =
        let decoder = decodeResponse TimeseriesResponse.Decoder (fun res -> res.Items)
        let request : TimeseriesUpdateRequests = {
           Items = [
               yield! args |> Seq.map(fun (assetId, args) -> { Id = assetId; Params = args })
           ]
        }

        POST
        >=> setVersion V10
        >=> setContent (Content.JsonValue request.Encoder)
        >=> setResource Url
        >=> fetch
        >=> decoder

[<AutoOpen>]
module UpdateTimeseriesApi =
    /// <summary>
    /// Updates multiple timeseries within the same project.
    /// This operation supports partial updates, meaning that fields omitted from the requests are not changed
    /// <param name="timeseries">List of tuples of timeseries id to update and updates to perform on that timeseries.</param>
    /// <param name="next">Async handler to use.</param>
    /// <returns>List of updated timeseries.</returns>
    let updateTimeseries (timeseries: (Identity * (UpdateTimeseries.Option list)) list) (next: NextHandler<TimeseriesReadDto seq, 'a>) : HttpContext -> Async<Context<'a>> =
        UpdateTimeseries.updateTimeseries timeseries fetch next
    
    /// <summary>
    /// Updates multiple timeseries within the same project.
    /// This operation supports partial updates, meaning that fields omitted from the requests are not changed
    /// <param name="timeseries">List of tuples of timeseries id to update and updates to perform on that timeseries.</param>
    /// <returns>List of updated timeseries.</returns>
    let updateTimeseriesAsync (timeseries: (Identity * (UpdateTimeseries.Option list)) list) : HttpContext -> Async<Context<TimeseriesReadDto seq>> =
        UpdateTimeseries.updateTimeseries timeseries fetch Async.single

[<Extension>]
type UpdateTimeseriesExtensions =
    /// <summary>
    /// Updates multiple timeseries within the same project.
    /// This operation supports partial updates, meaning that fields omitted from the requests are not changed
    /// <param name="timeseries">List of tuples of timeseries id to update and updates to perform on that timeseries.</param>
    /// <returns>List of updated timeseries.</returns>
    [<Extension>]
    static member UpdateTimeseriesAsync (this: Client, timeseries: ValueTuple<Identity, UpdateTimeseries.Option seq> seq, [<Optional>] token: CancellationToken) : Task<TimeseriesReadPoco seq> =
        async {
            let timeseries' = timeseries |> Seq.map (fun struct (id, options) -> (id, options |> List.ofSeq)) |> List.ofSeq
            let! ctx = updateTimeseriesAsync timeseries' this.Ctx
            match ctx.Result with
            | Ok response ->
                return response |> Seq.map (fun timeseries -> timeseries.ToPoco ())
            | Error error ->
                let err = error2Exception error
                return raise err
        } |> fun op -> Async.StartAsTask(op, cancellationToken = token)
