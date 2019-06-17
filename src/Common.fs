/// Common types for the SDK.
namespace Cognite.Sdk

open System

open FSharp.Data
open Thoth.Json.Net
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module Common =
    type Numeric =
        | NumString of string
        | NumInteger of int64
        | NumFloat of double

    /// Id or ExternalId
    type Identity =
        | Id of int64
        | ExternalId of string

    /// **Description**
    ///
    /// JSON decode response and map decode error string to exception so we
    /// don't get more response error types.
    ///
    /// **Parameters**
    ///   * `decoder` - parameter of type `'a`
    ///   * `result` - parameter of type `Result<'b,'c>`
    ///
    /// **Output Type**
    ///   * `Result<'d,'c>`
    ///
    /// **Exceptions**
    ///
    let decodeResponse<'a, 'b, 'c> (decoder : Decoder<'a>) (resultMapper : 'a -> 'b) (next: NextHandler<'b,'c>) (context: HttpContext) =
        let result =
            context.Result
            |> Result.map (fun response ->
                match response.Body with
                | Text text ->
                    //printfn "Got: %A" text
                    text
                | Binary _ ->
                    failwith "binary format not supported"
            )
            |> Result.bind (fun res ->
                let ret = Decode.fromString decoder res
                match ret with
                | Error error -> DecodeError error |> Error
                | Ok value -> Ok value
            )
            |> Result.map resultMapper

        next { Request = context.Request; Result = result}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Encode =
    let inline stringify encoder = Encode.toString 0 encoder

    /// Encode int64 to Json number (not to string as Thoth.Json.Net)
    let inline int53 (value : int64) : JsonValue = JValue(value) :> JsonValue

    /// Encode int64 list to Json number array.
    let inline int53List (values: int64 list) = Encode.list (List.map int53 values) |> stringify

    /// Encode int64 seq to Json number array.
    let inline int53seq (items: int64 seq) = Seq.map int53 items |> Encode.seq

    let inline uri (value: Uri) : JsonValue =
        Encode.string (value.ToString ())

    let inline metaData (values: Map<string, string>) =  values |> Map.map (fun _ value -> Encode.string value) |> Encode.dict

    let inline propertyBag (values: Map<string, string>) = Encode.dict (Map.map (fun key value -> Encode.string value) values)

    let optionalProperty<'a> (name: string) (encoder: 'a -> JsonValue) (value : 'a option) =
        [
            if value.IsSome then
                yield name, encoder value.Value
        ]
