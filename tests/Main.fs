module Main

open Fable.Mocha

let routerTests =
    testList "Router tests" [
        yield! UtilsTests.testCases
        yield! HashRoutingTests.testCases
        yield! PathRouterTests.testCases
        yield! LegacyTests.testCases
    ]

[<EntryPoint>]
let main _ = Mocha.runTests routerTests