module Tests.Timeseries

open System
open System.IO
open System.Threading.Tasks

open FSharp.Control.Tasks.V2

open Xunit
open Swensen.Unquote

open Cognite.Sdk
open Cognite.Sdk.Timeseries
open Cognite.Sdk.Request


[<Fact>]
let ``Create timeseries is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Timeseries.json" // FIXME:
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! res = Internal.createTimeseries [] fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.POST @>
    test <@ res.Request.Resource = "/timeseries" @>
    test <@ res.Request.Query.IsEmpty @>
}

[<Fact>]
let ``Get timeseries by ids is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Timeseries.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! res = Internal.getTimeseriesByIds [ 0L ] fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.POST @>
    test <@ res.Request.Resource = "/timeseries/byids" @>
    test <@ res.Request.Query.IsEmpty @>
}

[<Fact>]
let ``Delete timeseries is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Timeseries.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! res = Internal.deleteTimeseries "erase" fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.DELETE @>
    test <@ res.Request.Resource = "/timeseries/erase" @>
    test <@ res.Request.Query.IsEmpty @>
}
