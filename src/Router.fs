namespace Feliz.Router

open System
open Browser.Dom
open Fable.React
open Elmish
open Fable.Core
open Fable.Core.JsInterop

type IUrlSearchParamters =
    abstract entries : unit -> seq<string array>

[<RequireQualifiedAccess>]
module internal Router =
    let customNavigationEvent = "CUSTOM_NAVIGATION_EVENT"
    let hashPrefix = sprintf "#/%s"
    let combine xs = String.concat "/" xs
    [<Emit("encodeURIComponent($0)")>]
    let encodeURIComponent (value: string) : string = jsNative
    [<Emit("decodeURIComponent($0)")>]
    let decodeURIComponent (value: string) : string = jsNative
    let encodeQueryString queryStringPairs =
        queryStringPairs
        |> List.map (fun (key, value) -> encodeURIComponent key, encodeURIComponent value)
        |> List.map (fun (key, value) -> String.concat "=" [ key; value ])
        |> String.concat "&"
        |> function
            | "" -> ""
            | pairs -> sprintf "?%s" pairs

    let encodeQueryStringInts queryStringIntPairs =
        queryStringIntPairs
        |> List.map (fun (key, value: int) -> encodeURIComponent key, (string value))
        |> List.map (fun (key, value) -> String.concat "=" [ key; value ])
        |> String.concat "&"
        |> function
            | "" -> ""
            | pairs -> sprintf "?%s" pairs

    let encodeParts xs =
        xs
        |> List.map (fun (part: string) ->
            if part.Contains "?" || part.StartsWith "#" || part.StartsWith "/"
            then part
            else encodeURIComponent part)
        |> combine
        |> function
            | path when path.StartsWith "/" -> sprintf "#%s" path
            | path when path.StartsWith "#/" -> path
            | path when path.StartsWith "#" -> "#/" + path.Substring(1, path.Length - 1)
            | path -> sprintf "#/%s" path

    let nav xs : Elmish.Cmd<_> =
        Cmd.ofSub (fun _ ->
            history.pushState ((), "", encodeParts xs)
            let ev = document.createEvent("CustomEvent")
            ev.initEvent (customNavigationEvent, true, true)
            window.dispatchEvent ev |> ignore)

    /// Parses the URL into multiple path segments
    let urlSegments (urlHash: string) =
        if urlHash.StartsWith "#"
        // remove the hash sign
        then urlHash.Substring(1, urlHash.Length - 1)
        // return as is
        else urlHash
        |> fun hash -> hash.Split '/' // split the url segments
        |> List.ofArray
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> List.collect (fun segment ->
            if segment = "?"
            then [ ]
            elif segment.StartsWith "?"
            then [ segment ]
            else
            match segment.Split [| '?' |] with
            | [| value |] -> [value]
            | [| value; "" |] -> [value]
            | [| value; query |] -> [ value; "?" + query ]
            | _ -> [])
        |> List.map decodeURIComponent

    [<Emit("new URLSearchParams($0)")>]
    let createUrlSearchParams (queryString: string) : IUrlSearchParamters = jsNative

type RouterProperties = {
    urlChanged: string list -> unit
    application: ReactElement
}

type RouterComponent(props: RouterProperties)  =
    inherit Fable.React.PureStatelessComponent<RouterProperties>(props)

    override this.render() =
        this.props.application

    override this.componentDidMount() =
        let onChange (ev: _) =
            window.location.hash
            |> Router.urlSegments
            |> this.props.urlChanged

        // listen to manual hash changes or page refresh
        window.addEventListener("hashchange", unbox onChange)
        // listen to custom navigation events published by `Router.navigate()`
        window.addEventListener(Router.customNavigationEvent, unbox onChange)
        // trigger event here
        onChange()

    override this.componentWillUnmount() =
        // clean up when the router isn't in view anymore
        window.removeEventListener("hashchange", unbox null)
        window.removeEventListener(Router.customNavigationEvent, unbox null)

/// Defines a property for the `router` element
type IRouterProperty = interface end

type Router =
    /// An event that is triggered when the URL in the address bar changes, whether by hand or programmatically using `Router.navigate(...)`.
    /// The event arguments are the parts of the URL, segmented into strings:
    ///
    /// `segment "#/" => [ ]`
    ///
    /// `segment "#/home" => [ "home" ]`
    ///
    /// `segment "#/home/settings" => [ "home"; "settings" ]`
    ///
    /// `segment "#/users/1" => [ "users"; "1" ]`
    ///
    /// `segment "#/users/1/details" => [ "users"; "1"; "details" ]`
    ///
    /// with query string parameters
    ///
    /// `segment "#/users?id=1" => [ "users"; "?id=1" ]`
    ///
    /// `segment "#/home/users?id=1" => [ "home"; "users"; "?id=1" ]`
    ///
    /// `segment "#/users?id=1&format=json" => [ "users"; "?id=1&format=json" ]`
    ///
    /// escaped query string parameters are decoded when the url is segmented
    ///
    /// `segment @"#/search?q=whats%20up" => [ "search"; "?q=whats up" ]`
    static member onUrlChanged (eventHandler: string list -> unit) : IRouterProperty = unbox ("onUrlChanged", eventHandler)

    /// The element that is rendered inside where the `router` is placed. Usually this contains the root application but it could also be part of another root element.
    ///
    /// It will keep listening for URL changes as long as the `router` is rendered on screen somewhere.
    static member application (app: ReactElement) : IRouterProperty = unbox ("application", app)

    /// Initializes the router as an element of the page to starts listening to URL changes.
    static member router (properties: IRouterProperty list) : ReactElement =
        let defaultProperties : RouterProperties =
            { urlChanged = fun _ -> ignore()
              application = nothing }

        let modifiedProperties =
            (defaultProperties, properties)
            ||> List.fold (fun state prop ->
                let (key, value) = unbox<string * obj> prop
                match key with
                | "onUrlChanged" -> { state with urlChanged = unbox value  }
                | "application"  -> { state with application = unbox value }
                | _ -> state)

        ofType<RouterComponent, _, _>(modifiedProperties) [| |]

    static member navigate([<ParamArray>] xs: string array) =
        Router.nav (List.ofArray xs)
    static member navigate(segment: string, queryString) =
        Router.nav [ segment + Router.encodeQueryString queryString ]
    static member navigate(segment: string, queryString) =
        Router.nav [ segment + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: string, queryString) =
        Router.nav [ segment1; segment2 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: string, queryString) =
        Router.nav [ segment1; segment2 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: string, segment3:string, queryString) =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: string, segment3:string, queryString) =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, queryString) =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, queryString) =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString) =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString) =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString) =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString) =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString) =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString) =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString) =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString) =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString) =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString) =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString) =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString) =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString) =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString) =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString) =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ]
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString) =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ]
    static member navigate(fullPath: string) : Cmd<_> =
        Router.nav [ fullPath ]
    static member navigate(segment: string, value: int) : Cmd<_> =
        Router.nav [segment; string value ]
    static member navigate(segment1: string, value1: int, value2: int) : Cmd<_> =
        Router.nav [segment1; string value1; string value2 ]
    static member navigate(segment1: string, value1: int, segment2: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2 ]
    static member navigate(segment1: string, value1: int, segment2: string, value2: int) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2 ]
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3 ]
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; segment4 ]
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; string value3 ]
    static member navigate(segment1: string, value1: int, value2: int, value3: int) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; string value3 ]
    static member navigate(segment1: string, value1: int, value2: int, segment2: string) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; segment2 ]

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
            let urlParams = Router.createUrlSearchParams input
            Some [ for entry in urlParams.entries() -> entry.[0], entry.[1] ]
        with | _ -> None