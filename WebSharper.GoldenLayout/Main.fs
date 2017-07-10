namespace WebSharper.GoldenLayout.Extension

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    //Item config

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
        Class "Item"
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

    //Layout config 

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
    let BrowserWindowClass = Class "BrowserWindow"
    let EventEmitterClass = Class "EventEmitter"
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
    let EventType = LayoutEvents + T<string>

    let GoldenLayout =
        Class "GoldenLayout"
        |+> Static [
            Constructor (LayoutConfig?configuration * !?(T<JQuery.JQuery> + T<Dom.Element>)?container)
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
            "toConfig" => T<unit> ^-> T<JavaScript.Object> //TODO: ask about this
            "getComponent" => T<string>?name ^-> T<obj> //TODO: test
            "updateSize" => !? T<int>?width ^-> !? T<int>?height ^-> T<unit>
            "destroy" => T<unit> ^-> T<unit>
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
