namespace Oryx.Cognite

open System.Net.Http

open Oryx
open Oryx.Cognite
open Oryx.SystemTextJson.ResponseReader

open CogniteSdk.Login

/// Various event HTTP handlers.

[<RequireQualifiedAccess>]
module Login =
    let get<'a, 'b> (url: string) : HttpHandler<HttpResponseMessage, 'a, 'b> =
        GET
        >=> withVersion V10
        >=> withUrl url
        >=> withLogMessage "Login:get"
        >=> fetch
        >=> log
        >=> withError decodeError
        >=> json jsonOptions

    /// Returns the authentication information about the asking entity.
    let status () : HttpHandler<HttpResponseMessage, LoginStatus, 'a> =
        withLogMessage "Login:status"
        >=> req {
            let! data = get<LoginDataRead, 'a> "/login/status"
            return data.Data
        }  

