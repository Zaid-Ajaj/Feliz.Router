[<RequireQualifiedAccess>]
module UtilsTests

open Feliz.Router.Shared
open Fable.Mocha

let testCases = [
    testCase "encodeQueryString for a single argument" <| fun _ ->
        let input = [ "id", "1" ]
        let expected = "?id=1"
        let actual = encodeQueryString input
        Expect.equal actual expected "They are equal"

    testCase "encodeQueryString for an empty map" <| fun _ ->
        let input = [  ]
        let expected = ""
        let actual = encodeQueryString input
        Expect.equal actual expected "They are equal"

    testCase "encodeQueryString for multiple arguments" <| fun _ ->
        let input = [ "id", "1"; "limit", "5" ]
        let expected = "?id=1&limit=5"
        let actual = encodeQueryString input
        Expect.equal actual expected "They are equal"
]