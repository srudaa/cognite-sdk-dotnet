namespace Cognite.Sdk

//open FSharp.Data
open System.Net
open System.Net.Http

open FSharp.Control.Tasks.V2


type RequestBuilder () =
    member this.Zero () : HttpHandler<HttpResponseMessage, HttpResponseMessage, _> = fun next _ -> next Request.defaultContext
    member this.Return (res: 'a) : HttpHandler<HttpResponseMessage, 'a, _> = fun next _ ->  next { Request = Request.defaultRequest; Result = Ok res }

    member this.Return (req: HttpRequest) : HttpHandler<HttpResponseMessage, HttpResponseMessage, _> = fun next ctx -> next { Request = req; Result = Request.defaultResult }

    member this.Delay fn = fn ()

    member this.Bind(source: HttpHandler<'a, 'b, 'd>, fn: 'b -> HttpHandler<'a, 'c, 'd>) :  HttpHandler<'a, 'c, 'd> =

        fun (next : NextHandler<'c, 'd>) (ctx : Context<'a>) ->
            let next' (cb : Context<'b>)  = task {
                match cb.Result with
                | Ok b ->
                    let innerHandler = fn b
                    return! innerHandler next ctx
                | Error error ->
                    return! next { Request = cb.Request; Result = Error error }
            }
            // Run source
            source next' ctx

[<AutoOpen>]
module Builder =
    /// Request builder for an async context of request/result
    let fusion = RequestBuilder ()
