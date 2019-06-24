namespace Cognite.Sdk

open System.Threading.Tasks
open FSharp.Control.Tasks.V2

/// Async extensions
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Task =

    /// Transform the asynchronous value.
    let map f (asyncX : Task<'a>) = task {
        let! x = asyncX
        return f x
    }

    /// Create async value from synchronous value.
    let single x = async {
        return x
    }