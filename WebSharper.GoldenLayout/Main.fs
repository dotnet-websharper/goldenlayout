namespace WebSharper.GoldenLayout.Extension

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    // Classes

    let GoldenLayoutClass = Class "GoldenLayout"
    let ContentItemClass = Class "ContentItem"
    let ContainerClass = Class "Container"
    let BrowserWindowClass = Class "BrowserWindow"
    let HeaderClass = Class "Header"
    let TabClass = Class "Tab"
    let EventEmitterClass = Class "EventEmitter"

    // Events

    let LayoutEvents =
        Pattern.EnumStrings "LayoutEvents"
            [
                "initialised"
                "stateChanged"
                "windowOpened"
                "windowClosed"
                "selectionChanged"
                "itemDestroyed"
                "itemCreated"
                "componentCreated"
                "rowCreated"
                "columnCreated"
                "stackCreated"
                "tabCreated"
            ]

    let ItemEvents =
        Pattern.EnumStrings "ItemEvent" [
            "stateChanged"
            "titleChanged"
            "activeContentItemChanged"
            "itemDestroyed"
            "itemCreated"
            "componentCreated"
            "rowCreated"
            "columnCreated"
            "stackCreated"
        ]


    let EventType = LayoutEvents + ItemEvents + T<string>


    // Item config

    let ComponentConfig =
        Pattern.Config "Component" {
            Required = [ "componentName", T<string> ]
            Optional = [ "componentState", T<obj> ]
        }

    let ReactComponentConfig =
        Pattern.Config "ReactComponent" {
            Required = [ "component", T<string> ]
            Optional = [ "props", T<obj> ]
        }

    let StackConfig = 
        Pattern.Config "Stack" {
            Required = []
            Optional = [ "activeItemIndex", T<int> ]
        }

    let GeneralItemConfig =
        Pattern.Config "GeneralItemConfig" {
            Required = []
            Optional =
                [
                    "content", Type.ArrayOf TSelf
                    "width", T<int>
                    "height", T<int>
                    "id", T<string> + Type.ArrayOf T<string>
                    "isClosable", T<bool>
                    "title", T<string>
                ]
        }

    let ComponentString = "component"
    let ReactComponentString = "react-component"
    let RowString = "row"
    let ColumnString = "column"
    let StackString = "stack"

    let ItemType =
        Pattern.EnumStrings "ItemType" [
            ComponentString
            ReactComponentString
            RowString
            ColumnString
            StackString
        ]

    let ItemConfig =
        Pattern.Config "ItemConfig" {
            Required =
                [
                    "type", ItemType.Type
                ]
            Optional = 
                [
                    "content", Type.ArrayOf TSelf
                    "width", T<int>
                    "height", T<int>
                    "id", T<string> + Type.ArrayOf T<string>
                    "isClosable", T<bool>
                    "title", T<string>
                    //type = "component"
                    "componentName", T<string>
                    "componentState", T<obj>
                    //type = "react-component"
                    "component", T<string>
                    "props", T<obj>
                    //type = "stack"
                    "activeItemIndex", T<int>
                ]
        }

    let ItemFactory =
        Class "ItemFactory"
        |+> Static [
            "createComponent" => ComponentConfig.Type?special ^-> GeneralItemConfig.Type?general ^-> ItemConfig
                |> WithInline ("Object.assign({type: " + ComponentString + "}, $special, $general);")

            "createReactComponent" => ReactComponentConfig.Type?special ^-> GeneralItemConfig.Type?general ^-> ItemConfig
                |> WithInline ("Object.assign({type: " + ReactComponentString + "}, $special, $general);")

            "createStack" => StackConfig.Type?special ^-> GeneralItemConfig.Type?general ^-> ItemConfig
                |> WithInline ("Object.assign({type: " + StackString + "}, $special, $general);")

            "createRow" => GeneralItemConfig.Type?general ^-> ItemConfig
                |> WithInline ("Object.assign({type: " + RowString + "}, $special, $general);")

            "createColumn" => GeneralItemConfig.Type?general ^-> ItemConfig
                |> WithInline ("Object.assign({type: " + ColumnString + "}, $special, $general);")
        ]

    // Layout config 

    let LayoutSettings =
        Pattern.Config "LayoutSettings" {
            Optional =
                [
                    "hasHeaders", T<bool>
                    "constrainDragToContainer", T<bool>
                    "reorderEnabled", T<bool>
                    "selectionEnabled", T<bool>
                    "popoutWholeStack", T<bool>
                    "blockedPopoutsThrowError", T<bool>
                    "closePopoutsOnUnload", T<bool>
                    "showPopoutIcon", T<bool>
                    "showMaximiseIcon", T<bool>
                    "showCloseIcon", T<bool>
                ]
            Required = []
        }

    let LayoutDimensions =
        Pattern.Config "LayoutDimensions" {
            Optional =
                [
                    "borderWidth", T<int>
                    "minItemHeight", T<int>
                    "minItemWidth", T<int>
                    "headerHeight", T<int>
                    "dragProxyWidth", T<int>
                    "dragProxyHeight", T<int>
                ]
            Required = []
        }

    let LayoutLabels =
        Pattern.Config "LayoutLabels" {
            Optional =
                [
                    "close", T<string>
                    "maximise", T<string>
                    "minimise", T<string>
                    "popout", T<string>
                ]
            Required = []
        }

    let LayoutConfig =
        Pattern.Config "Layout" {
            Required =
                [
                    
                ]
            Optional = 
                [
                    "settings", LayoutSettings.Type
                    "dimensions", LayoutDimensions.Type
                    "labels", LayoutLabels.Type
                    "content", Type.ArrayOf ItemConfig.Type
                ]
        }

    // GoldenLayout

    let Dimensions =
        Pattern.Config "Dimensions" {
            Required =
                [
                    "width", T<int>
                    "height", T<int>
                    "left", T<int>
                    "top", T<int>
                ]
            Optional = []
        }

    let GoldenLayout =
        GoldenLayoutClass
        |+> Static [
            Constructor (LayoutConfig.Type?configuration * !?(T<JQuery.JQuery> + T<Dom.Element>)?container)
            "minifyConfig" => LayoutConfig.Type?config ^-> T<obj>
            "unminifyConfig" => T<obj>?minifiedConfig ^-> LayoutConfig.Type
        ]
        |+> Instance [
            "root" =? T<obj>
            "container" =? T<JQuery.JQuery>
            "isInitialised" =? T<bool>
            "config" =? LayoutConfig.Type
            "selectedItem" =? T<obj>
            "width" =? T<int>
            "height" =? T<int>
            "openPopouts" =? Type.ArrayOf BrowserWindowClass.Type
            "isSubWindow" =? T<bool>
            "eventHub" =? EventEmitterClass.Type
            ("registerComponent"
                => T<string>?name
                ^-> (
                    (T<Dom.Element>?container ^-> T<obj>?state ^-> T<obj>) + 
                    (T<Dom.Element>?container ^-> T<obj>?state ^-> T<unit>)
                )?component
                ^-> T<unit>) //TODO: must test thoroughly
            "init" => T<unit> ^-> T<unit>
            "toConfig" => T<unit> ^-> LayoutConfig.Type
            "getComponent" => T<string>?name ^-> T<JavaScript.Function> //TODO: test
            "updateSize" => !? T<int>?width ^-> !? T<int>?height ^-> T<unit>
            "destroy" => T<unit> ^-> T<unit>
            "createContentItem" => ItemConfig.Type?itemConfiguration ^-> !? ContentItemClass.Type?parent ^-> T<unit> // Test
            "createPopout"
                => (LayoutConfig.Type + ContentItemClass)?configOrContentItem
                ^-> Dimensions.Type?dimensions
                ^-> !? T<string>?parentId
                ^-> !? T<int>?indexInParent
                ^-> T<unit>
            "createDragSource" => (T<Dom.Element> + T<JQuery.JQuery>)?element ^-> ItemConfig.Type?itemConfiguration ^-> T<unit>
            "selectItem" => ContentItemClass.Type?contentItem ^-> T<unit>
        ]

    // ContentItem

    let ContentItemType =
        Pattern.EnumStrings "ContentItemType" [
            "row"
            "column"
            "stack"
            "component"
            "root"
        ]

    let ContentItem =
        ContentItemClass
        |+> Instance [
            "config" =? ItemConfig.Type
            "type" =? ContentItemType.Type
            "contentItems" =? Type.ArrayOf TSelf
            "parent" =? TSelf
            "id" =? T<string> + Type.ArrayOf T<obj>
            "isInitialised" =? T<bool>
            "isMaximised" =? T<bool>
            "isRoot" =? T<bool>
            "isRow" =? T<bool>
            "isColumn" =? T<bool>
            "isStack" =? T<bool>
            "isComponent" =? T<bool>
            "layoutManager" =? GoldenLayout.Type //TODO: test
            "element" =? T<Dom.Element>
            "childElementContainer" =? T<Dom.Element>
            
            "addChild" => (TSelf + ItemConfig.Type)?itemOrItemConfig ^-> !? T<int>?index ^-> T<unit>
            "removeChild" => TSelf?contentItem ^-> !? T<bool>?keepChild ^-> T<unit>
            "replaceChild" => TSelf?oldChild ^-> (TSelf + ItemConfig.Type)?newChild ^-> T<unit>
            "setSize" => T<unit> ^-> T<unit>
            "setTitle" => T<string>?title ^-> T<unit>
            "callDownwards"
                => T<string>?functionName
                ^-> !? T<obj []>?functionArguments
                ^-> !? T<bool>?bottomUp
                ^-> !? T<bool>? skipSelf
                ^-> T<unit>
            "emitBubblingEvent" => EventType?name ^-> T<string>
            "remove" => T<unit> ^-> T<unit>
            "popout" => T<unit> ^-> T<unit>
            "toggleMaximise" => T<unit> ^-> T<unit>
            "select" => T<unit> ^-> T<unit>
            "deselect" => T<unit> ^-> T<unit>
            "hasId" => T<string>?id ^-> T<string>
            "setActiveContentItem" => TSelf?contentItem ^-> T<unit>
            "getActiveContentItem" => T<unit> ^-> TSelf
            "addId" => T<string>?id ^-> T<unit>
            "removeId" => T<string>?id ^-> T<unit>
            "getItemsByFilter" => (TSelf ^-> T<bool>)?filterFunction ^-> Type.ArrayOf TSelf
            "getItemsById" => T<string>?id ^-> Type.ArrayOf TSelf
            "getItemsByType" => ContentItemType.Type ^-> Type.ArrayOf TSelf
            "getComponentsByName" => T<string>?name ^-> Type.ArrayOf TSelf
        ]


    let Assembly =
        Assembly [
            Namespace "WebSharper.GoldenLayout.Resources" [
            ]
            Namespace "WebSharper.GoldenLayout" [
            ]
        ]


[<Sealed>]
type Extension() =
    interface IExtension with
        member x.Assembly = Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
