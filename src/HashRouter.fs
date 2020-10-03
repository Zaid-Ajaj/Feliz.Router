namespace Feliz.Router

open Browser.Dom
open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.Router.Shared
open System

module HashRouter =
    [<RequireQualifiedAccess>]
    type HistoryMode = Shared.HistoryMode

    [<RequireQualifiedAccess; System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
    module Internal =
        let private normalizeRoute url =
            match url with
            | String.Prefix "/" (Some path) -> "#" + path
            | String.Prefix "#/" (Some path) -> path
            | String.Prefix "#" (Some path) -> "#/" + path.Substring(1, path.Length - 1)
            | path -> "#/" + path

        let private fullPath () =
            window.location.hash
        
        let urlSegments (path: string) =
            match path with
            | String.Prefix "#" (Some _) ->
                // remove the hash sign
                path.Substring(1, path.Length - 1)
            | String.Suffix "#" (Some _)
            | String.Suffix "#/" (Some _) -> ""
            | _ -> path
            |> tokenizeUrl
            
        let encodeParts xs =
            xs
            |> List.map (fun (part: string) ->
                if part.Contains "?" || part.StartsWith "#" || part.StartsWith "/" then part
                else encodeURIComponent part)
            |> combine
            |> normalizeRoute

        let nav xs (mode: HistoryMode) =
            if mode = HistoryMode.PushState
            then history.pushState ((), "", encodeParts xs)
            else history.replaceState((), "", encodeParts xs)

            let ev = document.createEvent("CustomEvent")

            ev.initEvent (customNavigationEvent, true, true)
            window.dispatchEvent ev |> ignore
        
        let currentUrl () =
            urlSegments <| fullPath ()
        
        let onUrlChange urlChanged =
            fullPath ()
            |> urlSegments
            |> urlChanged

    [<AutoOpen>]
    module ReactExtension =
        type React with
            /// Initializes the router as an element of the page and starts listening to URL changes.
            static member inline router (props: IRouterProperty list) =
                sharedRouter Internal.onUrlChange (unbox<RouterProps> (createObj !!props))

    [<Erase>]
    type router = Shared.router

    [<Erase>]
    type Router =
        /// Parses the current URL of the page and returns the cleaned URL segments. This is default when working with hash URLs. When working with path-based URLs, use Router.currentPath() instead.
        static member inline currentUrl = Internal.currentUrl

        static member inline format([<ParamArray>] xs: string array) =
            Internal.encodeParts (List.ofArray xs)

        static member inline format(segment: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment + encodeQueryString queryString ]

        static member inline format(segment: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; segment2 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; segment2 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: string, segment3:int, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; segment2; unbox<string> segment3 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: string, segment3:int, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; segment2; unbox<string> segment3 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: string, segment3:string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; segment2; segment3 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: string, segment3:string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; segment2; segment3 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; segment2; segment3; segment4 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; segment2; segment3; segment4 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; segment2; segment3; segment4; segment5 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; segment2; segment3; segment4; segment5 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3; segment4 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3; segment4 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; unbox<string> segment3; segment4 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; unbox<string> segment3; segment4 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; unbox<string> segment3; segment4; segment5; segment6 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; unbox<string> segment3; segment4; segment5; segment6 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; unbox<string> segment3; unbox<string> segment4; segment5 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; unbox<string> segment3; unbox<string> segment4; segment5 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3; unbox<string> segment4; segment5 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3; unbox<string> segment4; segment5 + encodeQueryStringInts queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * string) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3; segment4; segment5 + encodeQueryString queryString ]

        static member inline format(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * int) list) : string =
            Internal.encodeParts [ segment1; unbox<string> segment2; segment3; segment4; segment5 + encodeQueryStringInts queryString ]

        static member inline format(fullPath: string) : string =
            Internal.encodeParts [ fullPath ]

        static member inline format(fullPath: string list) : string =
            Internal.encodeParts fullPath

        static member inline format(segment: string, value: int) : string =
            Internal.encodeParts [ segment; string value ]

        static member inline format(segment1: string, value1: int, value2: int) : string =
            Internal.encodeParts [ segment1; string value1; string value2 ]

        static member inline format(segment1: string, segment2: string, value1: int) : string =
            Internal.encodeParts [ segment1; segment2; string value1 ]

        static member inline format(segment1: string, value1: int, segment2: string) : string =
            Internal.encodeParts [ segment1; string value1; segment2 ]

        static member inline format(segment1: string, value1: int, segment2: string, value2: int) : string =
            Internal.encodeParts [ segment1; string value1; segment2; string value2 ]

        static member inline format(segment1: string, value1: int, segment2: string, value2: int, segment3: string) : string =
            Internal.encodeParts [ segment1; string value1; segment2; string value2; segment3 ]

        static member inline format(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string) : string =
            Internal.encodeParts [ segment1; string value1; segment2; string value2; segment3; segment4 ]

        static member inline format(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int) : string =
            Internal.encodeParts [ segment1; string value1; segment2; string value2; segment3; string value3 ]

        static member inline format(segment1: string, value1: int, value2: int, value3: int) : string =
            Internal.encodeParts [ segment1; string value1; string value2; string value3 ]

        static member inline format(segment1: string, value1: int, value2: int, segment2: string) : string =
            Internal.encodeParts [ segment1; string value1; string value2; segment2 ]


        static member inline navigate([<ParamArray>] xs: string array) =
            Internal.nav (List.ofArray xs) HistoryMode.PushState

        static member inline navigate(segment: string, queryString: (string * string) list) =
            Internal.nav [ segment + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment + encodeQueryString queryString ] mode

        static member inline navigate(segment: string, queryString: (string * int) list) =
            Internal.nav [ segment + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: string, queryString: (string * string) list) =
            Internal.nav [ segment1; segment2 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: string, queryString: (string * int) list) =
            Internal.nav [ segment1; segment2 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * string) list) =
            Internal.nav [ segment1; segment2; unbox<string> segment3 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2;string  segment3 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * int) list) =
            Internal.nav [ segment1; segment2; unbox<string> segment3 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; unbox<string> segment3 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * string) list) =
            Internal.nav [ segment1; segment2; segment3 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; segment3 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * int) list) =
            Internal.nav [ segment1; segment2; segment3 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; segment3 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * string) list) =
            Internal.nav [ segment1; segment2; segment3; segment4 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; segment3; segment4 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * int) list) =
            Internal.nav [ segment1; segment2; segment3; segment4 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; segment3; segment4 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * string) list) =
            Internal.nav [ segment1; segment2; segment3; segment4; segment5 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; segment3; segment4; segment5 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * int) list) =
            Internal.nav [ segment1; segment2; segment3; segment4; segment5 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; segment3; segment4; segment5 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4; segment5; segment6 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4; segment5; segment6 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4; segment5; segment6 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; segment4; segment5; segment6 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; unbox<string> segment4; segment5 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; unbox<string> segment4; segment5 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; unbox<string> segment4; segment5 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; unbox<string> segment3; unbox<string> segment4; segment5 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; unbox<string> segment4; segment5 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; unbox<string> segment4; segment5 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; unbox<string> segment4; segment5 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; unbox<string> segment4; segment5 + encodeQueryStringInts queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * string) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4; segment5 + encodeQueryString queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * string) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4; segment5 + encodeQueryString queryString ] mode

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * int) list) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4; segment5 + encodeQueryStringInts queryString ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * int) list, mode: HistoryMode) =
            Internal.nav [ segment1; unbox<string> segment2; segment3; segment4; segment5 + encodeQueryStringInts queryString ] mode

        static member inline navigate(fullPath: string) =
            Internal.nav [ fullPath ] HistoryMode.PushState

        static member inline navigate(fullPath: string, mode: HistoryMode) =
            Internal.nav [ fullPath ] mode

        static member inline navigate(segment: string, value: int) =
            Internal.nav [ segment; string value ] HistoryMode.PushState

        static member inline navigate(segment: string, value: int, mode: HistoryMode) =
            Internal.nav [ segment; string value ] mode

        static member inline navigate(segment1: string, value1: int, value2: int) =
            Internal.nav [ segment1; string value1; string value2 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, value2: int, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; string value2 ] mode

        static member inline navigate(segment1: string, segment2: string, value1: int) =
            Internal.nav [ segment1; segment2; string value1 ] HistoryMode.PushState

        static member inline navigate(segment1: string, segment2: string, value1: int, mode: HistoryMode) =
            Internal.nav [ segment1; segment2; string value1 ] mode

        static member inline navigate(segment1: string, value1: int, segment2: string) =
            Internal.nav [ segment1; string value1; segment2 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, segment2: string, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; segment2 ] mode

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int) =
            Internal.nav [ segment1; string value1; segment2; string value2 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; segment2; string value2 ] mode

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string) =
            Internal.nav [ segment1; string value1; segment2; string value2; segment3 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; segment2; string value2; segment3 ] mode

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string) =
            Internal.nav [ segment1; string value1; segment2; string value2; segment3; segment4 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; segment2; string value2; segment3; segment4 ] mode

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int) =
            Internal.nav [ segment1; string value1; segment2; string value2; segment3; string value3 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; segment2; string value2; segment3; string value3 ] mode

        static member inline navigate(segment1: string, value1: int, value2: int, value3: int) =
            Internal.nav [ segment1; string value1; string value2; string value3 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, value2: int, value3: int, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; string value2; string value3 ] mode

        static member inline navigate(segment1: string, value1: int, value2: int, segment2: string) =
            Internal.nav [ segment1; string value1; string value2; segment2 ] HistoryMode.PushState

        static member inline navigate(segment1: string, value1: int, value2: int, segment2: string, mode: HistoryMode) =
            Internal.nav [ segment1; string value1; string value2; segment2 ] mode

    [<Erase>]
    type Cmd =
        static member inline navigate([<ParamArray>] xs: string array) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(xs) ]

        static member inline navigate(segment: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment, queryString) ]

        static member inline navigate(segment: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment, queryString, mode) ]

        static member inline navigate(segment: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment, queryString) ]

        static member inline navigate(segment: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment, queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, unbox<string> segment3 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2,string  segment3 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, unbox<string> segment3 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:int, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, unbox<string> segment3 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, unbox<string> segment2, segment3 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, unbox<string> segment2, segment3 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, unbox<string> segment2, segment3 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, unbox<string> segment2, segment3 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5, segment6, queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5, segment6, queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5, segment6, queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5, segment6, queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * string) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * string) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * int) list) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString) ]

        static member inline navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString: (string * int) list, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, segment3, segment4, segment5 , queryString, mode) ]

        static member inline navigate(fullPath: string) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(fullPath) ]

        static member inline navigate(fullPath: string, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(fullPath, mode) ]

        static member inline navigate(segment: string, value: int) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment, value) ]

        static member inline navigate(segment: string, value: int, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment, value, mode) ]

        static member inline navigate(segment1: string, value1: int, value2: int) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, value2) ]

        static member inline navigate(segment1: string, value1: int, value2: int, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, value2, mode) ]

        static member inline navigate(segment1: string, segment2: string, value1: int) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, value1) ]

        static member inline navigate(segment1: string, segment2: string, value1: int, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, segment2, value1, mode) ]

        static member inline navigate(segment1: string, value1: int, segment2: string) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, mode) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, mode) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, segment3) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, segment3, mode) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, segment3, segment4) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, segment3, segment4, mode) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, segment3, value3) ]

        static member inline navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, segment2, value2, segment3, value3, mode) ]

        static member inline navigate(segment1: string, value1: int, value2: int, value3: int) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, value2, value3) ]

        static member inline navigate(segment1: string, value1: int, value2: int, value3: int, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, value2, value3, mode) ]

        static member inline navigate(segment1: string, value1: int, value2: int, segment2: string) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, value2, segment2) ]

        static member inline navigate(segment1: string, value1: int, value2: int, segment2: string, mode: HistoryMode) : Cmd<'Msg> =
            [ fun _ -> Router.navigate(segment1, value1, value2, segment2, mode) ]

    module Route =
        let (|Int|_|) (input: string) =
            match Int32.TryParse input with
            | true, value -> Some value
            | _ -> None

        let (|Int64|_|) (input: string) =
            match Int64.TryParse input with
            | true, value -> Some value
            | _ -> None

        let (|Guid|_|) (input: string) =
            match Guid.TryParse input with
            | true, value -> Some value
            | _ -> None

        let (|Number|_|) (input: string) =
            match Double.TryParse input with
            | true, value -> Some value
            | _ -> None

        let (|Decimal|_|) (input: string) =
            match Decimal.TryParse input with
            | true, value -> Some value
            | _ -> None

        let (|Bool|_|) (input: string) =
            match input.ToLower() with
            | ("1"|"true")  -> Some true
            | ("0"|"false") -> Some false
            | "" -> Some true
            | _ -> None

        /// Used to parse the query string parameter of the route.
        ///
        /// For example to match against
        ///
        /// `/users?id={value}`
        ///
        /// You can pattern match:
        ///
        /// `[ "users"; Route.Query [ "id", value ] ] -> value`
        ///
        /// When `{value}` is an integer then you can pattern match:
        ///
        /// `[ "users"; Route.Query [ "id", Route.Int userId ] ] -> userId`
        let (|Query|_|) (input: string) =
            try
                let urlParams = createUrlSearchParams input
                Some [ for entry in urlParams.entries() -> entry.[0], entry.[1] ]
            with | _ -> None