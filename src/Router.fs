namespace Feliz.Router

open System
open Browser.Dom
open Fable.React
open Elmish
open Fable.Core

type IUrlSearchParamters =
    abstract entries : unit -> seq<string array>

/// Determines whether the router will push a new entry to the history of modify the current one.
[<RequireQualifiedAccess>]
type HistoryMode =
    /// A new history will be added to the entries such that if the user clicks the back button,
    /// the previous page will be shown, this is the default bahavior of the router.
    | PushState = 1
    /// Only modifies the current history entry and does not add a new one to the history stack. Clicking the back button will *not* have the effect of retuning to the previous page.
    | ReplaceState = 2
    
/// Determines whether the router will use path or hash based routes
[<RequireQualifiedAccess>]
type RouteType =
    | Hash = 1
    | Path = 2

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

    let encodeParts xs routeType =
        let normalizeRoute : (string -> string) = 
            if routeType = RouteType.Hash then
                function
                | path when path.StartsWith "/" -> sprintf "#%s" path
                | path when path.StartsWith "#/" -> path
                | path when path.StartsWith "#" -> "#/" + path.Substring(1, path.Length - 1)
                | path -> sprintf "#/%s" path
            else 
                (fun path -> if path.StartsWith "/" then path else sprintf "/%s" path)

        xs
        |> List.map (fun (part: string) ->
            if part.Contains "?" || part.StartsWith "#" || part.StartsWith "/"
            then part
            else encodeURIComponent part)
        |> combine
        |> normalizeRoute

    let nav xs (mode: HistoryMode) (routeType: RouteType) : Elmish.Cmd<_> =
        Cmd.ofSub (fun _ ->
            if mode = HistoryMode.PushState
            then history.pushState ((), "", encodeParts xs routeType)
            else history.replaceState((), "", encodeParts xs routeType)
            let ev = document.createEvent("CustomEvent")
            ev.initEvent (customNavigationEvent, true, true)
            window.dispatchEvent ev |> ignore
        )

    /// Parses the URL into multiple path segments
    let urlSegments (path: string) =
        if path.StartsWith "#"
        // remove the hash sign
        then path.Substring(1, path.Length - 1)
        // return as is
        else path
        |> fun p -> p.Split '/' // split the url segments
        |> List.ofArray
        |> List.filter (String.IsNullOrWhiteSpace >> not)
        |> List.collect (fun segment ->            
            if segment = "?"
            then [ ]
            elif segment.StartsWith "?"
            then [ segment ]
            else
            match segment.Split [| '?' |] with
            | [| value |] -> [decodeURIComponent value]
            | [| value; "" |] -> [decodeURIComponent value]
            | [| value; query |] -> [ decodeURIComponent value; "?" + query ]
            | _ -> [])

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
            match window.location.hash with
            | "" -> window.location.pathname + window.location.search
            | _ -> window.location.hash
            |> Router.urlSegments
            |> this.props.urlChanged

        // listen to path changes 
        window.addEventListener("popstate", unbox onChange)
        // listen to manual hash changes or page refresh
        window.addEventListener("hashchange", unbox onChange)
        // listen to custom navigation events published by `Router.navigate()`
        window.addEventListener(Router.customNavigationEvent, unbox onChange)
        // trigger event here
        onChange()

    override this.componentWillUnmount() =
        // clean up when the router isn't in view anymore
        window.removeEventListener("popstate", unbox null)
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
        Router.nav (List.ofArray xs) HistoryMode.PushState RouteType.Hash
    static member navigate(segment: string, queryString) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment: string, queryString) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:int, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; string segment3 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:int, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2;string  segment3 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:int, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; string segment3 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:int, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; string segment3 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Hash
    static member navigate(fullPath: string) : Cmd<_> =
        Router.nav [ fullPath ] HistoryMode.PushState RouteType.Hash
    static member navigate(fullPath: string, mode) : Cmd<_> =
        Router.nav [ fullPath ] mode RouteType.Hash
    static member navigate(segment: string, value: int) : Cmd<_> =
        Router.nav [segment; string value ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment: string, value: int, mode) : Cmd<_> =
        Router.nav [segment; string value ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, value2: int) : Cmd<_> =
        Router.nav [segment1; string value1; string value2 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, value2: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; string value2 ] mode RouteType.Hash
    static member navigate(segment1: string, segment2: string, value1: int) : Cmd<_> =
        Router.nav [segment1; segment2; string value1 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, segment2: string, value1: int, mode) : Cmd<_> =
        Router.nav [segment1; segment2; string value1 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; segment4 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; segment4 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; string value3 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; string value3 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, value2: int, value3: int) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; string value3 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, value2: int, value3: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; string value3 ] mode RouteType.Hash
    static member navigate(segment1: string, value1: int, value2: int, segment2: string) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; segment2 ] HistoryMode.PushState RouteType.Hash
    static member navigate(segment1: string, value1: int, value2: int, segment2: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; segment2 ] mode RouteType.Hash
        
    static member navigateP([<ParamArray>] xs: string array) =
        Router.nav (List.ofArray xs) HistoryMode.PushState RouteType.Path
    static member navigateP(segment: string, queryString) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment: string, queryString) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:int, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; string segment3 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:int, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2;string  segment3 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:int, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; string segment3 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:int, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; string segment3 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: string, segment5: string, segment6, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; segment4; segment5; segment6 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:int, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; string segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: int, segment5: string, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; string segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryString queryString ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: int, segment3:string, segment4: string, segment5, queryString, mode) : Cmd<_> =
        Router.nav [ segment1; string segment2; segment3; segment4; segment5 + Router.encodeQueryStringInts queryString ] mode RouteType.Path
    static member navigateP(fullPath: string) : Cmd<_> =
        Router.nav [ fullPath ] HistoryMode.PushState RouteType.Path
    static member navigateP(fullPath: string, mode) : Cmd<_> =
        Router.nav [ fullPath ] mode RouteType.Path
    static member navigateP(segment: string, value: int) : Cmd<_> =
        Router.nav [segment; string value ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment: string, value: int, mode) : Cmd<_> =
        Router.nav [segment; string value ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, value2: int) : Cmd<_> =
        Router.nav [segment1; string value1; string value2 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, value2: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; string value2 ] mode RouteType.Path
    static member navigateP(segment1: string, segment2: string, value1: int) : Cmd<_> =
        Router.nav [segment1; segment2; string value1 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, segment2: string, value1: int, mode) : Cmd<_> =
        Router.nav [segment1; segment2; string value1 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, segment3: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, segment3: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; segment4 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, segment3: string, segment4: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; segment4 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; string value3 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, segment2: string, value2: int, segment3: string, value3: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; segment2; string value2; segment3; string value3 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, value2: int, value3: int) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; string value3 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, value2: int, value3: int, mode) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; string value3 ] mode RouteType.Path
    static member navigateP(segment1: string, value1: int, value2: int, segment2: string) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; segment2 ] HistoryMode.PushState RouteType.Path
    static member navigateP(segment1: string, value1: int, value2: int, segment2: string, mode) : Cmd<_> =
        Router.nav [segment1; string value1; string value2; segment2 ] mode RouteType.Path

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