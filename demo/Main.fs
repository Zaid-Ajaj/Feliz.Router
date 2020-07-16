module Main

open Elmish
open Elmish.React

Program.mkProgram AppPath.init AppPath.update AppPath.render
//Program.mkProgram App.init App.update App.render
|> Program.withReactSynchronous "root"
|> Program.withConsoleTrace
|> Program.run