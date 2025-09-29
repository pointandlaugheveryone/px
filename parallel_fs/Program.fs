
let check (result: Result<'T, exn>) =
    match result with
    | Ok value -> value
    | Error e -> raise e


