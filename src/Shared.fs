module Feliz.Router.Shared

open Browser.Types
open Fable.Core
open Feliz
open Feliz.UseListener
open System

[<RequireQualifiedAccess>]
module String =
    let (|Prefix|) (prefix: string) (str: string) =
        if str.StartsWith prefix then Some str
        else None

    let (|Suffix|) (suffix: string) (str: string) =
        if str.EndsWith suffix then Some str
        else None

    let inline split (sep: char) (str: string) =
        str.Split(sep)

[<Emit("encodeURIComponent($0)")>]
let encodeURIComponent (value: string) : string = jsNative

[<Emit("decodeURIComponent($0)")>]
let decodeURIComponent (value: string) : string = jsNative

type IUrlSearchParameters =
    abstract entries : unit -> seq<string array>

[<Emit("new URLSearchParams($0)")>]
let createUrlSearchParams (queryString: string) : IUrlSearchParameters = jsNative

[<Emit("window.navigator.userAgent")>]
let navigatorUserAgent : string = jsNative

let encodeQueryString queryStringPairs =
    queryStringPairs
    |> List.map (fun (key, value) ->
        String.concat "=" [ encodeURIComponent key; encodeURIComponent value ])
    |> String.concat "&"
    |> function
        | "" -> ""
        | pairs -> "?" + pairs

let encodeQueryStringInts queryStringIntPairs =
    queryStringIntPairs
    |> List.map (fun (key, value: int) ->
        String.concat "=" [ encodeURIComponent key; unbox<string> value ])
    |> String.concat "&"
    |> function
        | "" -> ""
        | pairs -> "?" + pairs

let tokenizeUrl (path: string) =
    path
    |> String.split '/'
    |> List.ofArray
    |> List.collect (fun segment ->
        if String.IsNullOrWhiteSpace segment then []
        else
            let segment = segment.TrimEnd '#'

            match segment with
            | "?" -> []
            | String.Prefix "?" (Some _) -> [ segment ]
            | _ ->
                match segment.Split [| '?' |] with
                | [| value |] -> [ decodeURIComponent value ]
                | [| value; "" |] -> [ decodeURIComponent value ]
                | [| value; query |] -> [ decodeURIComponent value; "?" + query ]
                | _ -> [])

let inline combine xs = String.concat "/" xs

/// Determines whether the router will push a new entry to the history of modify the current one.
[<RequireQualifiedAccess>]
type HistoryMode =
    /// A new history will be added to the entries such that if the user clicks the back button,
    /// the previous page will be shown, this is the default behavior of the router.
    | PushState = 1
    /// Only modifies the current history entry and does not add a new one to the history stack. Clicking the back button will *not* have the effect of retuning to the previous page.
    | ReplaceState = 2

let [<Literal>] customNavigationEvent = "CUSTOM_NAVIGATION_EVENT"

/// Defines a property for the `router` element
type IRouterProperty = interface end

[<Erase>]
type router =
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
    static member inline onUrlChanged (eventHandler: string list -> unit) : IRouterProperty = unbox ("onUrlChanged", eventHandler)

    /// The element that is rendered inside where the `router` is placed. Usually this contains the root application but it could also be part of another root element.
    ///
    /// It will keep listening for URL changes as long as the `router` is rendered on screen somewhere.
    static member inline children (element: ReactElement) : IRouterProperty = unbox ("children", element)

    /// The content that is rendered inside where the `router` is placed. Usually this contains the root application but it could also be part of another root element.
    ///
    /// It will keep listening for URL changes as long as the `router` is rendered on screen somewhere.
    static member inline children (elements: ReactElement list) : IRouterProperty = unbox ("children", React.fragment elements)

type RouterProps =
    abstract onUrlChanged: (string list -> unit) option
    abstract children: ReactElement option

let sharedRouter onUrlChange = React.memo(fun (input: RouterProps) ->
    let onChange = React.useCallbackRef(fun (_: Event) ->
        let urlChanged = Option.defaultValue ignore input.onUrlChanged

        onUrlChange urlChanged)

    if navigatorUserAgent.Contains "Trident" || navigatorUserAgent.Contains "MSIE"
    then React.useWindowListener.onHashChange(onChange)
    else React.useWindowListener.onPopState(onChange)

    React.useWindowListener.on(customNavigationEvent, onChange)

    match input.children with
    | Some elem -> elem
    | None -> Html.none)