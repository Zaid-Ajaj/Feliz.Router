[<RequireQualifiedAccess>]
module HashRoutingTests

open Feliz.Router.Shared
open Feliz.Router.HashRouter
open Fable.Mocha

type test =
    static member pass() = Expect.isTrue true "It must be true"
    static member fail() = Expect.isTrue false "It must be false"
    static member failwith x = failwith x
    static member passWith x = Expect.isTrue true

let itCompiles() = Router.navigate "users"

let testCases =
    [
        testCase "HashRouter Route.Int works" <| fun _ ->
            match [ "5"; "-1";  "" ] with
            | [ Route.Int 5; Route.Int -1; "" ] -> test.pass()
            | [ Route.Int 5; Route.Int -1; Route.Int value ] -> test.failwith (string value)
            | _ -> test.fail()

        testCase "HashRouter Router.urlSegments works" <| fun _ ->
            [
                "", [ ]
                "#" , [ ]
                "#/", [ ]
                "#/?", [ ]
                "#?", [ ]
                "#home", [ "home" ]
                "#home/", [ "home" ]
                "#/home", [ "home" ]
                "#/home/", [ "home" ]
                "#/home/users", [ "home"; "users" ]
                "#/home/users/", [ "home"; "users" ]
                "#/home/users/settings", [ "home"; "users"; "settings" ]
                "#/home/users/1", [ "home"; "users"; "1" ]
                "#/users?id=1", [ "users"; "?id=1" ]
                @"#/search?q=whats%20up", [ "search"; @"?q=whats%20up" ]
                "#/?token=jwt", [ "?token=jwt" ]
                "#?token=jwt", [ "?token=jwt" ]
                "#?pretty", [ "?pretty" ]
                "/", [ ]
                "/?", [ ]
                "?", [ ]
                "home", [ "home" ]
                "home/", [ "home" ]
                "/home", [ "home" ]
                "/home/", [ "home" ]
                "/home/users", [ "home"; "users" ]
                "/home/users/", [ "home"; "users" ]
                "/home/users/settings", [ "home"; "users"; "settings" ]
                "/home/users/1", [ "home"; "users"; "1" ]
                "/users?id=1", [ "users"; "?id=1" ]
                @"/search?q=whats%20up", [ "search"; @"?q=whats%20up" ]
                "/?token=jwt", [ "?token=jwt" ]
                "?token=jwt", [ "?token=jwt" ]
                "?pretty", [ "?pretty" ]
            ]
            |> List.iter (fun (input, output) -> 
                Expect.equal (Internal.urlSegments input) output (sprintf "Input of %s should output %A" input output))

        testCase "HashRouter RouteMode affects how the URL segments are cleaned up" <| fun _ ->
            "/some/path#"
            |> Internal.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            "/Feliz.MaterialUI/#"
            |> Internal.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            "/Feliz.MaterialUI#"
            |> Internal.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            "/some/path#/"
            |> Internal.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

        testCase "HashRouter Router.urlSegments decodes URL segments" <| fun _ ->
            let hashInput = "#/Hello%20World"
            let expected  = [ "Hello World" ]
            Expect.equal expected (Internal.urlSegments hashInput)  "They are equal"

        testCase "HashRouter Route.Query works" <| fun _ ->
            match [ "users"; "?id=1" ] with
            | [ "users"; Route.Query [ "id", "1" ] ] -> test.pass()
            | otherwise -> test.fail()

            match [ "users"; "?id=1" ] with
            | [ "users"; Route.Query [ "id", Route.Int userId ] ] -> Expect.equal userId 1 "They are equal"
            | otherwise -> test.fail()

            match [ "users"; "?id=1&limit=5" ] with
            | [ "users"; Route.Query [ "id", Route.Int userId; "limit", Route.Int limit ] ] ->
                Expect.equal userId 1 "They are equal"
                Expect.equal limit 5 "They are equal"
            | otherwise ->
                test.fail()

        testCase "HashRouter Boolean query string paramters" <| fun _ ->
            match "?value=true" with
            | Route.Query [ "value", Route.Bool true ] -> test.pass()
            | otherwise -> test.fail()

            match "?value=1" with
            | Route.Query [ "value", Route.Bool true ] -> test.pass()
            | otherwise -> test.fail()

            match "?value=0" with
            | Route.Query [ "value", Route.Bool false ] -> test.pass()
            | otherwise -> test.fail()

            match "?value=false" with
            | Route.Query [ "value", Route.Bool false ] -> test.pass()
            | otherwise -> test.fail()

            match "?users=all&pretty" with
            | Route.Query [ "users", "all"; "pretty", "" ]  -> test.pass()
            | otherwise -> test.fail()

        testCase "HashRouter Double and decimal Route" <| fun _ ->
            match "?lng=12.12411241&lat=2.3451241" with
            | Route.Query [ "lng", Route.Decimal 12.12411241M; "lat", Route.Decimal 2.3451241M ] ->
                test.pass()
            | otherwise ->
                test.fail()

            match "?lng=12.12&lat=2.345" with
            | Route.Query [ "lng", Route.Number 12.12; "lat", Route.Number 2.345 ] ->
                test.pass()
            | otherwise ->
                test.fail()

        testCase "HashRouter encode hash segments works" <| fun _ ->
        [
            [  ], "#/"
            [ "users" ], "#/users"
            [ "#/home" ], "#/home"
            [ "#/hello?value=1" ], "#/hello?value=1"
            [ "#/hello/home" ], "#/hello/home"
            [ "#about" ], "#/about"
            [ "users"; "home" ], "#/users/home"
            [ "#one"; "two" ], "#/one/two"
            [ "users"; "1" ], "#/users/1"
            [ "users" + encodeQueryString [ "id", "1" ] ], "#/users?id=1"
            [ "search" + encodeQueryString [ "q", "whats up" ] ], @"#/search?q=whats%20up"
            [ "products" + encodeQueryStringInts [ "id", 1 ] ], "#/products?id=1"
            [ "users" + encodeQueryString [ ] ], "#/users"
        ]
        |> List.iter (fun (input, output) -> Expect.equal (Internal.encodeParts input) output "They are equal")
    ]
