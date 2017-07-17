# Golden Layout extension for WebSharper

This is the documentation for the WebSharper binding of the
[Golden Layout](https://golden-layout.com/) JavaScript library. The docs of the original
library can be found [here](http://golden-layout.com/docs/).

This documentation will go over the usage of the WebSharper extension and the differences
between the original library and the extension's API.

## Usage

### ContentItem config

You can create 5 different types of _ContentItems_: `component`, `react-component`,
`column`, `row` and `stack`. The config object would have different special fields added
depending on the item's type. As this would not work very well in a statically typed
environment, the extension takes a slightly different approach:

1. You have the `Item` struct with all common properties
2. You have a different config struct for each type with their special properties
3. You combine them with a factory method to get the full _ContentItem_ config 


The result will be of a type called `GeneralItemConfig`.

>**Note:** the `GeneralItemConfig` is
>not intended for you to create the config with. It is used when the original library would
>return or pass a `ContentItem` config as a parameter. _This means that in these cases you
>have to look out for the value of the config object's_ `Type` _property when deciding if
>you can ask for type-specific fields!_

The factory methods are all static methods of the `ItemFactory` type.

Method name|Signature
---:|---
`CreateComponent`|`(Component * Item) -> GeneralItemConfig`|
`CreateReactComponent`|`(ReactComponent * Item) -> GeneralItemConfig`|
`CreateStack`|`(Stack * Item) -> GeneralItemConfig`|
`CreateColumn`|`Item -> GeneralItemConfig`|
`CreateRow`|`Item -> GeneralItemConfig`|

The `row` and `column` types don't require any special config fields. The other config objects listed here:

__**Component**__

Field|Type|Required
---|:---:|:---:
`ComponentName`|`string`|**required**
`ComponentName`|`obj`|-

__**ReactComponent**__

Field|Type|Required
---|:---:|:---:
`Component`|`string`|**required**
`Props`|`obj`|-

__**Stack**__

Field|Type|Required
---|:---:|:---:
`ActiveItemIndex`|`int`|-

#### Example

```fsharp
    type State = {Text: string}

    let helloComponent =
        ItemFactory.CreateComponent(
            Component(
                componentName = "hello-component",
                ComponentState = {Text = "Hello, "}
            ),
            Item(
                Title = "Hello",
                IsClosable = false
            )
        )
        
    let worldComponent = 
        ItemFactory.CreateComponent(
            Component(
                componentName = "world-component",
                ComponentState = {Text = "World!"}
            ),
            Item(
                Title = "World!",
                IsClosable = false
            )
        )

    let mainContentItem =
        ItemFactory.CreateRow(
            Item(
                Content = [|
                    helloComponent
                    worldComponent
                |],
                IsClosable = false
            )
        )
```

The rest of the API should be familiar from the [original documentation](http://golden-layout.com/docs/).

### Layout config

The config for layouts is much more analog with the original specification.

__**Layout**__

Field|Type|Required
---|---|:---:
`Settings`|`LayoutSettings`|-
`Dimensions`|`LayoutDimensions`|-
`Labels`|`LayoutLabels`|-
`Content`|`GeneralItemConfig []`|-

The usage of these fields can be found
[here](http://golden-layout.com/docs/Config.html) in the original docs.

### Instantiation

Now that we know how to describe our layout and content item configs, we can finally
instantiate the main layout manager class which is called `GoldenLayout`. You can
either pass the constructor a `Layout` config, or a `Layout` config and a `JQuery`
or `Dom.Element` as the container to create the layout in. Otherwise, the DOM of the
layout will be appended to the `<body>` of your HTML file. Let's look at an example
using the configs defined before:

```fsharp
let layoutManager =
    GoldenLayout(
        Layout(
            Content = [|
                mainContentItem
            |]
        )
    )
```

### Registering components

When we have our layout manager ready, we can start to _register our components_.
This means that we will bind a function to every _component name_, which function
when given the _container_ and the _state_ of the component, will produce the
content of the component into the _container_.

```fsharp
layoutManager.RegisterComponent(
    "hello-component", 
    fun (container, s) ->
        let state = s :?> State
        (h3 [text state.Text]).Html
        |> container.GetElement().Html
        |> fun jq -> jq.Ignore
        
layoutManager.RegisterComponent(
    "world-component", 
    fun (container, s) ->
        let state = s :?> State
        (h2 [text state.Text]).Html
        |> container.GetElement().Html
        |> fun jq -> jq.Ignore
)
```

### Initialising

We've created our layout manager and registered our components, but the layout won't
work just yet. We have to initialise it manually.

```fsharp
layoutManager.Init()
```
The layouts should appear now as described in their configs.

## Events

The `GoldenLayout`, `ContentItem`, `Container` and `BrowserWindow` classes all
inherit the functionality of the `EventEmitter` class with all of them having
their respective events, too. To make things easier and faster, these classes'
respective events have been collected into enums called `{ClassName}Event`.
All methods of the `EventEmitter` class which take an `"event"` as parameter
have been overloaded for these enums. You can still choose to pass the event
name as a `string`, but using these enums will make your code safer.

Here's an example:

```fsharp
let layoutManager = 

layoutManager.On(
    LayoutEvent.Initialised, //an event from the LayoutEvent enum
    fun (params: obj []) -> //the callback function
        Console.Log("The Layout has been initialised.")
        Console.Log(params)
    )
```

## Examples

For fully working examples, visit these links:
* [Programatically created tabs](http://try.websharper.com/snippet/adam.abonyi-toth/0000EM)
* [Dynamically adding tabs](http://try.websharper.com/snippet/adam.abonyi-toth/0000EN)
* [Reactive Markdown editor with "Preview" tab](http://try.websharper.com/snippet/adam.abonyi-toth/0000EO)
