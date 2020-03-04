module Tests.Integration.Login

open FSharp.Control.Tasks.V2.ContextInsensitive
open Swensen.Unquote
open Xunit

open Common
open System.Net.Http
open CogniteSdk
open System

[<Fact>]
let ``Login status is Ok`` () = task {
    // Arrange

    // Act
    let! status = readClient.Login.StatusAsync ()

    // Assert
    test <@ status.Project = "publicdata" @>
}

[<Fact>]
let ``Login status not authenticated gives not loggedIn`` () = task {
    // Arrange

    // Act
    let! status = noAuthClient.Login.StatusAsync ()

    // Assert
    test <@ not status.LoggedIn @>
}