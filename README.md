# Feliz.Router [![Nuget](https://img.shields.io/nuget/v/Feliz.Router.svg?maxAge=0&colorB=brightgreen)](https://www.nuget.org/packages/Feliz.Router) [![Build status](https://ci.appveyor.com/api/projects/status/qwjte2b9vn43j9ff?svg=true)](https://ci.appveyor.com/project/Zaid-Ajaj/feliz-router)

An Elmish router that is focused, powerful yet extremely easy to use.

Here is a full example

```fs
open Feliz
open Feliz.Router

type State = { CurrentUrl : string list }
type Msg = UrlChanged of string list

let init() = { CurrentUrl = [ ] }

let update (UrlChanged segments) state =
    { state with CurrentUrl = segments }

let render state dispatch =
    let currentPage =
        match state.CurrentUrl with
        | [ ] -> Html.h1 "Home"
        | [ "users" ] -> Html.h1 "Users page"
        | [ "users"; Route.Int userId ] -> Html.h1 (sprintf "User ID %d" userId)
        | _ -> Html.h1 "Not found"

    Router.router [
        Router.onUrlChanged (UrlChanged >> dispatch)
        Router.application currentPage
    ]

Program.mkSimple init update render
|> Program.withReactSynchronous "root"
|> Program.run
```

### Installation

```
dotnet add package Feliz.Router
```

The package includes a single element called `router` that is to be included at the very top level of your `render` or `view` function
```fs
let render state dispatch =

    let currentPage = Html.h1 "App"

    Router.router [
        Router.onUrlChanged (UrlChanged >> dispatch)
        Router.application currentPage
    ]
```
Where it has two primary properties
 - `Router.onUrlChanged : string list -> unit` gets triggered when the url changes where it gives you the *url segments* to work with.
 - `Router.application: ReactElement` the element to be rendered as the single child of the `router` component, usually here is where your root application `render` function goes.
 - `Router.application: ReactElement list` overload to be rendered as the children of the `router`

```fs
let render state dispatch =

    let currentPage = Html.h1 "App"

    Router.router [
        Router.onUrlChanged (UrlChanged >> dispatch)
        Router.application [
            Html.div [
                Html.h1 "Using the router"
                currentPage
            ]
        ]
    ]
```
### `Router.onUrlChanged` is everything

Routing in most applications revolves around having your application react to url changes, causing the current page to change and data to reload. Here is where `Router.onUrlChanged` comes into play where it triggers when the url changes giving you the *cleaned url segments* as a list of strings. These are sample urls their corresposing url segments that get triggered as input of of `onUrlChanged`:
```fs
segment "#/" => [ ]
segment "#/home" => [ "home" ]
segment "#/home/settings" => [ "home"; "settings" ]
segment "#/users/1" => [ "users"; "1" ]
segment "#/users/1/details" => [ "users"; "1"; "details" ]

// with query string parameters
segment "#/users?id=1" => [ "users"; "?id=1" ]
segment "#/home/users?id=1" => [ "home"; "users"; "?id=1" ]
segment "#/users?id=1&format=json" => [ "users"; "?id=1&format=json" ]
// escaped query string parameters are decoded when the url is segmented
segment @"#/search?q=whats%20up" => [ "search"; "?q=whats%20up" ]
```

### Parsing URL segments into `Page` definitions

Instead of using overly complicated parser combinators to parse a simple structure such as URL segments, the `Route` module includes a handful of convenient active patterns to use against these segments:
```fs
type Page =
    | Home
    | Users
    | User of id:int
    | NotFound

type State = { CurrentPage : Page }

type Msg = PageChanged of Page

let update (PageChanged nextPage) state =
    { state with CurrentPage = nextPage }

// string list -> Page
let parseUrl = function
    // matches #/ or #
    | [ ] ->  Page.Home
    // matches #/users or #/users/ or #users
    | [ "users" ] -> Page.Users
    // matches #/users/{userId}
    | [ "users"; Route.Int userId ] -> Page.User userId
    // matches #/users?id={userId} where userId is an integer
    | [ "users"; Route.Query [ "id", Route.Int userId ] ] -> Page.User userId
    // matches everything else
    | _ -> NotFound

let render state dispatch =
    let currentPage =
        match state.CurrentPage with
        | Home -> Html.h1 "Home"
        | Users -> Html.h1 "Users page"
        | User userId -> Html.h1 (sprintf "User ID %d" userId)
        | NotFound -> Html.h1 "Not Found"

    Router.router [
        Router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
        Router.application currentPage
    ]
```
Of course, you can define your own patterns to match against the route segments, just remember that you are working against simple string.

### Using Path routes without hash sign
The router by default prepends all generated routes with a hash sign (`#`), to omit the hash sign and use plain old paths, just use `Router.pathMode`
```fs
Router.router [
    Router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
    Router.pathMode
    Router.application currentPage
]
```


### Programmatic Navigation

Aside from listening to manual changes made to the URL by hand, the `Router.router` element is able to listen to changes made programmatically from your code with `Router.navigate(...)`. This function is implemented as a *command* and can be used inside your `update` function.

The function `Router.navigate` has the general syntax:
```
Router.navigate(segment1, segment2, ..., segmentN, [query string parameters], [historyMode])
```
Examples of the generated paths:
```fs
Router.navigate("users") => "#/users"
Router.navigate("users", "about") => "#/users/about"
Router.navigate("users", 1) => "#/users/1"
Router.navigate("users", 1, "details") => "#/users/1/details"
```
Examples of generated paths with query string parameters
```fs
Router.navigate("users", [ "id", 1 ]) => "#/user?id=1"
Router.navigate("users", [ "name", "john"; "married", "false" ]) => "#/users?name=john&married=false"
// paramters are encoded automatically
Router.navigate("search", [ "q", "whats up" ]) => @"#/search?q=whats%20up"
// Pushing a new history entry is the default bevahiour
Router.navigate("users", HistoryMode.PushState)
// to replace current history entry, use HistoryMode.ReplaceState
Router.navigate("users", HistoryMode.ReplaceState)
```

### Generating links

In addition to `Router.navigate(...)` you can also use the `Router.format(...)` if you only need to generate the string that can be used to set the `href` property of a link.

The function `Router.format` has a similar general syntax as `Router.navigate`:
```
Router.format(segment1, segment2, ..., segmentN, [query string parameters])
```
Examples of the generated paths:
```fs
Router.format("users") => "#/users"
Router.format("users", "about") => "#/users/about"
Router.format("users", 1) => "#/users/1"
Router.format("users", 1, "details") => "#/users/1/details"
```
Examples of generated paths with query string parameters
```fs
Router.format("users", [ "id", 1 ]) => "#/user?id=1"
Router.format("users", [ "name", "john"; "married", "false" ]) => "#/users?name=john&married=false"
// paramters are encoded automatically
Router.format("search", [ "q", "whats up" ]) => @"#/search?q=whats%20up"
```

Example of usage:
```fs
Html.a [
    prop.href (Router.format("users", ["id", 10]))
    prop.text "Single User link"
]
```

### Demo application

Here is a full example in an Elmish program.

```fs
type State = { CurrentUrl : string list }

type Msg =
    | UrlChanged of string list
    | NavigateToUsers
    | NavigateToUser of int

let init() = { CurrentUrl = [ ] }, Cmd.none

let update msg state =
    match msg with
    | UrlChanged segments -> { state with CurrentUrl = segments }, Cmd.none
    // notice here the use of the command Router.navigate
    | NavigateToUsers -> state, Router.navigate("users")
    // Router.navigate with query string parameters
    | NavigateToUser userId -> state, Router.navigate("users", [ "id", userId ])

let render state dispatch =

    let currentPage =
        match state.CurrentUrl with
        | [ ] ->
            Html.div [
                Html.h1 "Home"
                Html.button [
                    prop.text "Navigate to users"
                    prop.onClick (fun _ -> dispatch NavigateToUsers)
                ]
                Html.a [
                    prop.href (Router.format("users"))
                    prop.text "Users link"
                ]
            ]
        | [ "users" ] ->
            Html.div [
                Html.h1 "Users page"
                Html.button [
                    prop.text "Navigate to User(10)"
                    prop.onClick (fun _ -> dispatch (NavigateToUser 10))
                ]
                Html.a [
                    prop.href (Router.format("users", ["id", 10]))
                    prop.text "Single User link"
                ]
            ]

        | [ "users"; Route.Query [ "id", Route.Int userId ] ] ->
            Html.h1 (sprintf "Showing user %d" userId)

        | _ -> Html.h1 "Not found"

    Router.router [
        Router.onUrlChanged (UrlChanged >> dispatch)
        Router.application currentPage
    ]
```
