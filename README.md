# Feliz.Router [![Nuget](https://img.shields.io/nuget/v/Feliz.Router.svg?maxAge=0&colorB=brightgreen)](https://www.nuget.org/packages/Feliz.Router) [![Build status](https://ci.appveyor.com/api/projects/status/qwjte2b9vn43j9ff?svg=true)](https://ci.appveyor.com/project/Zaid-Ajaj/feliz-router)

An React/Elmish router that is focused, powerful yet extremely easy to use.

Here is a full example in [Feliz](https://github.com/Zaid-Ajaj/Feliz)

```fs
module App

open Feliz
open Feliz.Router

[<ReactComponent>]
let Router() =
    let (currentUrl, updateUrl) = React.useState(Router.currentUrl())
    React.router [
        router.onUrlChanged updateUrl
        router.children [
            match currentUrl with
            | [ ] -> Html.h1 "Index"
            | [ "users" ] -> Html.h1 "Users page"
            | [ "users"; Route.Int userId ] -> Html.h1 (sprintf "User ID %d" userId)
            | otherwise -> Html.h1 "Not found"
        ]
    ]

open Browser.Dom

ReactDOM.render(Router(), document.getElementById "root")
```

Full Elmish example
```fs
module App

open Feliz
open Feliz.Router

type State = { CurrentUrl : string list }
type Msg = UrlChanged of string list

let init() = { CurrentUrl = Router.currentUrl() }
let update (UrlChanged segments) state = { state with CurrentUrl = segments }

let render state dispatch =
    React.router [
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [
            match state.CurrentUrl with
            | [ ] -> Html.h1 "Home"
            | [ "users" ] -> Html.h1 "Users page"
            | [ "users"; Route.Int userId ] -> Html.h1 (sprintf "User ID %d" userId)
            | _ -> Html.h1 "Not found"
        ]
    ]

Program.mkSimple init update render
|> Program.withReactSynchronous "root"
|> Program.run
```

### Installation

```
dotnet add package Feliz.Router
```

The package includes a single React component called `router` that you can use at the very top level of your application
```fs
React.router [
    router.onUrlChanged (UrlChanged >> dispatch)
    router.children [
        Html.h1 "App"
    ]
]
```
Where it has two primary properties
 - `router.onUrlChanged : string list -> unit` gets triggered when the url changes where it gives you the *url segments* to work with.
 - `router.children: ReactElement` the element to be rendered as the single child of the `router` component, usually here is where your root application `render` function goes.
 - `router.children: ReactElement list` overload to be rendered as the children of the `router`

```fs
let currentPage = Html.h1 "App"

React.router [
    router.onUrlChanged (UrlChanged >> dispatch)
    router.children [
        Html.div [
            Html.h1 "Using the router"
            currentPage
        ]
    ]
]
```
### `router.onUrlChanged` is everything

Routing in most applications revolves around having your application react to url changes, causing the current page to change and data to reload. Here is where `router.onUrlChanged` comes into play where it triggers when the url changes giving you the *cleaned url segments* as a list of strings. These are sample urls their corresposing url segments that get triggered as input of of `onUrlChanged`:
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
```

### Parsing URL segments into `Page` definitions

Instead of using overly complicated parser combinators to parse a simple structure such as URL segments, the `Route` module includes a handful of convenient active patterns to use against these segments:
```fs
type Page =
    | Home
    | Users
    | User of id:int
    | NotFound

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

[<ReactComponent>]
static member Router() =
    let (pageUrl, updateUrl) = React.useState(parseUrl(Router.currentUrl()))
    let currentPage =
        match pageUrl with
        | Home -> Html.h1 "Home"
        | Users -> Html.h1 "Users page"
        | User userId -> Html.h1 (sprintf "User ID %d" userId)
        | NotFound -> Html.h1 "Not Found"

    React.router [
        router.onUrlChanged (parseUrl >> updateUrl)
        router.children currentPage
    ]
```
Of course, you can define your own patterns to match against the route segments, just remember that you are working against simple string.

### Programmatic Navigation

Aside from listening to manual changes made to the URL by hand, the `React.router` element is able to listen to changes made programmatically from your code with `Router.navigate(...)`.
To use this function is as a *command* inside your `update` function the same functionality is exposed as `Cmd.navigate`.

The function `Router.navigate` (and `Cmd.navigate`) has the general syntax:
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

### Using Path routes without hash sign
The router by default prepends all generated routes with a hash sign (`#`), to omit the hash sign and use plain old paths, use `router.pathMode`
```fs
React.router [
    router.pathMode
    router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
    router.children currentPage
]
```
Then refactor the application to use path-based functions rather than the default functions which are hash-based:
| Hash-based            | Path-based              |
| --------------------- | ----------------------- |
| `Router.currentUrl()` | `Router.currentPath()`  |
| `Router.format()`     | `Router.formatPath()`   |
| `Router.navigate()`   | `Router.navigatePath()` |
| `Cmd.navigate()`      | `Cmd.navigatePath()`    |

Using (anchor) `Html.a` tags using path mode can be problematic because they cause a full-refresh if they are not prefixed with the hash sign.
Still, you can use them with path mode routing by overriding the default behavior using the `prop.onClick` event handler to dispatch a
message which executes a `Cmd.navigatePath` command. It goes like this:
```fs
type Msg =
 | NavigateTo of string

let update msg state =
  match msg with
  | NavigateTo href -> state, Cmd.navigatePath(href)

let goToUrl (dispatch: Msg -> unit) (href: string) (e: MouseEvent) =
    // disable full page refresh
    e.preventDefault()
    // dispatch msg
    dispatch (NavigateTo href)

let render state dispatch =
  let href = Router.format("some-sub-path")
  Html.a [
    prop.text "Click me"
    prop.href href
    prop.onClick (goToUrl dispatch href)
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

let init() = { CurrentUrl = Router.currentUrl() }, Cmd.none

let update msg state =
    match msg with
    | UrlChanged segments -> { state with CurrentUrl = segments }, Cmd.none
    // notice here the use of the command Cmd.navigate
    | NavigateToUsers -> state, Cmd.navigate("users")
    // Router.navigate with query string parameters
    | NavigateToUser userId -> state, Cmd.navigate("users", [ "id", userId ])

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

        | _ ->
            Html.h1 "Not found"

    React.router [
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children currentPage
    ]
```
### Migrating from 2.x to 3.x

The 3.x release refactored the API to be more in line with how Feliz libraries are built as well as using latest features from React like functional components to implement the router itself. This release also made it easy to work with the router from a React-only applications and not just from Elmish based apps.

To migrate your router to latest version, here are the required changes:
  - `Router.router` becomes `React.router`
  - `Router.onUrlChanged` becomes `router.onUrlChanged`
  - `Router.application` becomes `router.children`
  - `Router.navigate` becomes `Cmd.navigate` for the Elmish variant and `Router.navigate() : unit` for the React variant

The rest of the implementation and API is kept as is. If you have any questions or run into problems, please let us know!