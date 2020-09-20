module DotnetApp.Validator

let validate (condition : bool, errorResponse : string) =
    if (not condition) then
        Error errorResponse
    else
        Ok ()

// Use this to wrap things like Int32.TryParse so that you can get the ref value safely
let tryValidate (value : 'T, func : 'T -> bool * 'U, errorResponse : string) =
    match func(value) with
    | true, x ->
        Ok x
    | _ ->
        Error errorResponse

type ValidatorBuilder() =
    member this.Bind (m : Result<'T, string>, f : ('T -> Result<'U, string>)) : Result<'U, string> =
        match m with
        | Ok s -> 
            try
                f s
            with
            | exc -> Error exc.Message
        | Error x -> Error x
    
    member this.Return(x : 'T) = Ok x

let Validator = ValidatorBuilder()