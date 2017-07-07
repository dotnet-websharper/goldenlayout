namespace WebSharper.GoldenLayout.Extension

open WebSharper
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

    let RowConfig =
        Pattern.Config "Row" {
            Required = []
            Optional = []
        }

    let ColumnConfig =
        Pattern.Config "Column" {
            Required = []
            Optional = []
        }

    let ItemConfig =
        Pattern.Config "ItemConfig" {
            Required =
                [
                    "type",
                        ComponentConfig.Type +
                        ReactComponentConfig.Type +
                        StackConfig.Type +
                        RowConfig.Type +
                        ColumnConfig.Type
                ]
            Optional = 
                [
                    "content", Type.ArrayOf TSelf
                    "width", T<int>
                    "height", T<int>
                    "id", T<string> + Type.ArrayOf T<string>
                    "isClosavle", T<bool>
                    "title", T<string>
                ]
        }

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
