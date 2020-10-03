[<RequireQualifiedAccess>]
module PathRouterTests

open Feliz.Router.Shared
open Feliz.Router.PathRouter
open Fable.Mocha

type test =
    static member pass() = Expect.isTrue true "It must be true"
    static member fail() = Expect.isTrue false "It must be false"
    static member failwith x = failwith x
    static member passWith x = Expect.isTrue true

let itCompiles() = Router.navigate "users"

let testCases =
    [
        testCase "PathRouter Route.Int works" <| fun _ ->
            match [ "5"; "-1";  "" ] with
            | [ Route.Int 5; Route.Int -1; "" ] -> test.pass()
            | [ Route.Int 5; Route.Int -1; Route.Int value ] -> test.failwith (string value)
            | _ -> test.fail()
            
        testCase "PathRouter Router.urlSegments works" <| fun _ ->
            [
                "/ace/", "/ace/", [ ]
                "/ace/", "/ace/?", [ ]
                "/ace/", "/ace?", [ ]
                "/ace/", "/ace/home", [ "home" ]
                "/ace/", "/ace/home/", [ "home" ]
                "/ace/", "/ace/home/users", [ "home"; "users" ]
                "/ace/", "/ace/home/users/", [ "home"; "users" ]
                "/ace/", "/ace/home/users/settings", [ "home"; "users"; "settings" ]
                "/ace/", "/ace/home/users/1", [ "home"; "users"; "1" ]
                "/ace/", "/ace/users?id=1", [ "users"; "?id=1" ]
                "/ace/", @"/ace/search?q=whats%20up", [ "search"; @"?q=whats%20up" ]
                "/ace/", "/ace/?token=jwt", [ "?token=jwt" ]
                "/ace/", "/ace?token=jwt", [ "?token=jwt" ]
                "/ace/", "/ace/?pretty", [ "?pretty" ]
                "/ace/", "/ace/some/path#", ["some"; "path"]
                "/ace", "/ace/some/path#", ["some"; "path"]
                "/", "/ace/some/path", ["ace"; "some"; "path"]
                "", "/ace/some/path", ["ace"; "some"; "path"]
                "/", "/ace/Feliz.MaterialUI#", ["ace";"Feliz.MaterialUI"]
                "/ace/base/", "/ace/base/some/path", ["some";"path"]
                "/ace/base/", "/ace/base/Feliz.MaterialUI", ["Feliz.MaterialUI"]
            ]
            |> List.iter (fun (basePath, input, output) -> 
                Expect.equal (Internal.urlSegments basePath input) output (sprintf "Input of %s should output %A" input output))

        testCase "PathRouter Router.urlSegments decodes URL segments" <| fun _ ->
            let pathInput = "/Hello%20World"
            let baseInput = "/ace/Hello%20World"
            let expected  = [ "Hello World" ]
            Expect.equal expected (Internal.urlSegments "/" pathInput)  "They are equal"
            Expect.equal expected (Internal.urlSegments "/ace/" baseInput)  "They are equal"

        testCase "PathRouter Route.Query works" <| fun _ ->
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

        testCase "PathRouter Boolean query string paramters" <| fun _ ->
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

        testCase "PathRouter Double and decimal Route" <| fun _ ->
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

        testCase "PathRouter encode segments works" <| fun _ ->
            [
                [  ], "/"
                [ "users" ], "/users"
                [ "/home" ], "/home"
                [ "/hello?value=1" ], "/hello?value=1"
                [ "/hello/home" ], "/hello/home"
                [ "about" ], "/about"
                [ "users"; "home" ], "/users/home"
                [ "/one"; "two" ], "/one/two"
                [ "users"; "1" ], "/users/1"
                [ "users" + encodeQueryString [ "id", "1" ] ], "/users?id=1"
                [ "search" + encodeQueryString [ "q", "whats up" ] ], @"/search?q=whats%20up"
                [ "products" + encodeQueryStringInts [ "id", 1 ] ], "/products?id=1"
                [ "users" + encodeQueryString [ ] ], "/users"
            ]
            |> List.iter (fun (input, output) -> Expect.equal (Internal.encodeParts "/" input) output "They are equal")
            
            [
                [  ], "/ace"
                [ "users" ], "/ace/users"
                [ "/home" ], "/home"
                [ "/hello?value=1" ], "/hello?value=1"
                [ "/hello/home" ], "/hello/home"
                [ "about" ], "/ace/about"
                [ "users"; "home" ], "/ace/users/home"
                [ "/one"; "two" ], "/one/two"
                [ "users"; "1" ], "/ace/users/1"
                [ "users" + encodeQueryString [ "id", "1" ] ], "/ace/users?id=1"
                [ "search" + encodeQueryString [ "q", "whats up" ] ], @"/ace/search?q=whats%20up"
                [ "products" + encodeQueryStringInts [ "id", 1 ] ], "/ace/products?id=1"
                [ "users" + encodeQueryString [ ] ], "/ace/users"
            ]
            |> List.iter (fun (input, output) -> Expect.equal (Internal.encodeParts "/ace/" input) output "They are equal")
    ]