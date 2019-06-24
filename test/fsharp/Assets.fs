module Tests.Assets

open System.IO
open System.Net.Http
open System.Threading.Tasks

open FSharp.Control.Tasks.V2

open Xunit
open Swensen.Unquote
open Newtonsoft.Json

open Cognite.Sdk
open Cognite.Sdk.Assets


[<Fact>]
let ``Get asset is Ok``() = task {
    // Arrange
    let json = File.ReadAllText "Asset.json"
    let fetch = Fetch.fromJson json
    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! response = Internal.getAsset 42L fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk response.Result @>
    test <@ response.Request.Method = RequestMethod.GET @>
    test <@ response.Request.Resource = "/assets/42" @>
    test <@ response.Request.Query.IsEmpty @>
}

[<Fact>]
let ``Get invalid asset is Error`` () = task {
    // Arrenge
    let json = File.ReadAllText "InvalidAsset.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")


    // Act
    let! response = Internal.getAsset 42L fetch Task.FromResult ctx

    // Assert
    test <@ Result.isError response.Result @>
}

[<Fact>]
let ``Get asset with extra fields is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "AssetExtra.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! response = Internal.getAsset 42L fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk response.Result @>
}

[<Fact>]
let ``Get asset with missing optional fields is Ok`` () = task {
    // Arrenge

    let json = File.ReadAllText "AssetOptional.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! response = Internal.getAsset 42L fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk response.Result @>
}

[<Fact>]
let ``Get assets is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Assets.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")
    let args = [
            Name "string"
            Source "source"
            Root false
            ParentIds [42L; 43L]
            Limit 10
            Cursor "mycursor"
        ]

    // Act
    let! res =
        (fetch, Task.FromResult, ctx) |||> Internal.getAssets args

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.GET @>
    test <@ res.Request.Resource = "/assets" @>

    test <@ res.Request.Query = [
        ("name", "string")
        ("source", "source")
        ("root", "false")
        ("parentIds", "[42,43]")
        ("limit", "10")
        ("cursor", "mycursor")
    ] @>
}

[<Fact>]
let ``Create assets empty is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Assets.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! res = Internal.createAssets [] fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.POST @>
    test <@ res.Request.Resource = "/assets" @>
    test <@ res.Request.Query.IsEmpty @>
}

[<Fact>]
let ``Create single asset is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText("Assets.json")
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    let asset: AssetCreateDto = {
        Name = "myAsset"
        Description = Some "Description"
        MetaData = Map.empty
        Source = None
        ParentId = Some 42L
        ExternalId = None
        ParentExternalId = None
    }

    // Act
    let! res = Internal.createAssets [ asset ] fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.POST @>
    test <@ res.Request.Resource = "/assets" @>
    test <@ res.Request.Query.IsEmpty @>

    match res.Result with
    | Ok assets ->
        test <@ Seq.length assets = 1 @>
    | Error error ->
        raise (Error.error2Exception error)
}

[<Fact>]
let ``Update single asset with no updates is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Assets.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! res = Internal.updateAssets [ 42L, [] ] fetch Task.FromResult ctx

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.POST @>
    test <@ res.Request.Resource = "/assets/update" @>
    test <@ res.Request.Query.IsEmpty @>
}

[<Fact>]
let ``Update single asset with is Ok`` () = task {
    // Arrenge
    let json = File.ReadAllText "Assets.json"
    let fetch = Fetch.fromJson json

    let ctx =
        defaultContext
        |> addHeader ("api-key", "test-key")

    // Act
    let! res = (fetch, Task.FromResult, ctx) |||> Internal.updateAssets  [ (42L, [
        SetName "New name"
        SetDescription (Some "New description")
        SetSource None
    ])]

    // Assert
    test <@ Result.isOk res.Result @>
    test <@ res.Request.Method = RequestMethod.POST @>
    test <@ res.Request.Resource = "/assets/update" @>
    test <@ res.Request.Query.IsEmpty @>
}