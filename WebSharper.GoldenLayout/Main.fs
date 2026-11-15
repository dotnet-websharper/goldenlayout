// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2026 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}
namespace WebSharper.GoldenLayout

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

/// Binding for Golden Layout 2.x (https://golden-layout.com/), imported from the
/// `golden-layout` npm package.
module Definition =

    // ------------------------------------------------------------------
    // Class shells (so members can be attached after the config types)
    // ------------------------------------------------------------------

    let EventEmitterClass = Class "EventEmitter"
    let ContentItemClass = AbstractClass "ContentItem" |> Import "ContentItem" "golden-layout"
    let ComponentParentableItemClass = AbstractClass "ComponentParentableItem"
    let ComponentItemClass = Class "ComponentItem"
    let RowOrColumnClass = Class "RowOrColumn"
    let StackClass = Class "Stack"
    let HeaderClass = Class "Header"
    let TabClass = Class "Tab"
    let ComponentContainerClass = Class "ComponentContainer"
    let BrowserPopoutClass = Class "BrowserPopout"
    let LayoutManagerClass = AbstractClass "LayoutManager" |> Import "LayoutManager" "golden-layout"
    let VirtualLayoutClass = Class "VirtualLayout"
    let GoldenLayoutClass = Class "GoldenLayout" |> Import "GoldenLayout" "golden-layout"
    let DragSourceClass = Class "DragSource"
    let EventHubClass = Class "EventHub"
    let ExternalErrorClass = AbstractClass "ExternalError"
    let ApiErrorClass = Class "ApiError"
    let BindErrorClass = Class "BindError"
    let ConfigurationErrorClass = Class "ConfigurationError"
    let PopoutBlockedErrorClass = Class "PopoutBlockedError"
    /// Core does not model `PointerEvent`; Golden Layout's drag events pass one.
    let PointerEventClass = Class "PointerEvent" |=> Inherits T<Dom.MouseEvent>
    let BubblingEventClass = Class "BubblingEvent"
    let ClickBubblingEventClass = Class "ClickBubblingEvent"
    let TouchStartBubblingEventClass = Class "TouchStartBubblingEvent"

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// A JSON value: `string | number | boolean | null | object | JsonValue[]`.
    /// Golden Layout serialises component state as JSON, so this is the one
    /// genuinely untyped position of the API (upstream declares `JsonValue` the
    /// same way).
    let JsonValue = T<obj>

    /// Namespace helpers for `JsonValue`.
    let JsonValueHelpers =
        Class "JsonValue"
        |+> Static [
            "isJson" => JsonValue?value ^-> T<bool>
            "isJsonObject" => JsonValue?value ^-> T<bool>
        ]
        |> Import "JsonValue" "golden-layout"

    let private quote (text: string) =
        "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""

    let private param (name: string) (t: Type.IType) : Type.Parameter =
        { Name = Some name; Optional = false; Type = t.Type }

    let private parameters (ps: Type.Parameter list) : Type.Parameters =
        { Type.Parameters.Empty with Arguments = ps }

    /// Builds a function type from named arguments; zero arguments map to
    /// `unit -> 'T` so WIG emits a JS function of arity 0.
    let private fnOf (args: (string * Type.IType) list) (ret: Type.IType) =
        match args with
        | [] -> T<unit> ^-> ret
        | _ -> (parameters [ for (n, t) in args -> param n t ]) ^-> ret

    /// Builds parameters (first the event name, then the event payload) for `emit`.
    let private emitArgs (nameType: Type.IType) (args: (string * Type.IType) list) : Type.Parameters =
        parameters [ yield param "eventName" nameType; for (n, t) in args -> param n t ]

    // ------------------------------------------------------------------
    // Enums
    // ------------------------------------------------------------------

    let ItemType =
        Pattern.EnumStrings "ItemType" [ "ground"; "row"; "column"; "stack"; "component" ]

    let Side =
        Pattern.EnumStrings "Side" [ "top"; "left"; "right"; "bottom" ]

    let LogicalZIndex =
        Pattern.EnumStrings "LogicalZIndex" [ "base"; "drag"; "stackMaximised" ]

    let ResponsiveMode =
        Pattern.EnumStrings "ResponsiveMode" [ "none"; "always"; "onload" ]

    let SizeUnit =
        Pattern.EnumInlines "SizeUnit" [
            "Pixel", "\"px\""
            "Percent", "\"%\""
            "Fractional", "\"fr\""
            "Em", "\"em\""
        ]

    let SizeUnitEnum =
        Pattern.EnumInlines "SizeUnitEnum" [
            "Pixel", "\"px\""
            "Percent", "\"%\""
            "Fractional", "\"fr\""
            "Em", "\"em\""
        ]
        |+> Static [
            "tryParse" => T<string>?value ^-> !? TSelf
            "format" => TSelf?value ^-> TSelf
        ]

    let LocationSelectorTypeId =
        Pattern.EnumInlines "LocationSelectorTypeId" [
            "FocusedItem", "0"
            "FocusedStack", "1"
            "FirstStack", "2"
            "FirstRowOrColumn", "3"
            "FirstRow", "4"
            "FirstColumn", "5"
            "Empty", "6"
            "Root", "7"
        ]

    let StyleConstants =
        Class "StyleConstants"
        |+> Static [
            Getter "DefaultComponentBaseZIndex" T<string> |> WithGetterInline "\"auto\""
            Getter "DefaultComponentDragZIndex" T<string> |> WithGetterInline "\"32\""
            Getter "DefaultComponentStackMaximisedZIndex" T<string> |> WithGetterInline "\"41\""
        ]

    let I18nStrings =
        Class "I18nStrings"
        |+> Static [
            Getter "idCount" T<int>
            Getter "strings" (!| T<string>) |> Import "i18nStrings" "golden-layout"
            "checkInitialise" => T<unit> ^-> T<unit>
        ]
        |> Import "I18nStrings" "golden-layout"

    // ------------------------------------------------------------------
    // Item configs
    // ------------------------------------------------------------------

    let ItemConfig =
        Pattern.Config "ItemConfig" {
            Required = []
            Optional =
                [
                    "type", ItemType.Type
                    "content", Type.ArrayOf TSelf
                    "width", T<float>
                    "minWidth", T<float>
                    "height", T<float>
                    "minHeight", T<float>
                    "size", T<string>
                    "minSize", T<string>
                    "id", T<string>
                    "isClosable", T<bool>
                    "title", T<string>
                ]
        }
        |+> Static [
            "isGround" => TSelf?config ^-> T<bool>
            "isRow" => TSelf?config ^-> T<bool>
            "isColumn" => TSelf?config ^-> T<bool>
            "isStack" => TSelf?config ^-> T<bool>
            "isComponent" => TSelf?config ^-> T<bool>
        ]
        |> Import "ItemConfig" "golden-layout"

    /// `HeaderedItemConfig.Header`.
    let ItemHeaderConfig =
        Pattern.Config "ItemHeaderConfig" {
            Required = []
            Optional =
                [
                    "show", T<bool> + Side.Type
                    "popout", T<bool> + T<string>
                    "dock", T<bool> + T<string>
                    "maximise", T<bool> + T<string>
                    "close", T<string>
                    "minimise", T<string>
                    "tabDropdown", T<bool> + T<string>
                ]
        }

    let HeaderedItemConfig =
        Pattern.Config "HeaderedItemConfig" {
            Required = []
            Optional =
                [
                    "hasHeaders", T<bool>
                    "header", ItemHeaderConfig.Type
                    "maximised", T<bool>
                ]
        }
        |=> Inherits ItemConfig.Type

    let ComponentItemConfig =
        Pattern.Config "ComponentItemConfig" {
            Required = []
            Optional =
                [
                    "componentName", T<string>
                    "componentType", T<string>
                    "componentState", JsonValue
                    "reorderEnabled", T<bool>
                ]
        }
        |=> Inherits HeaderedItemConfig.Type
        |+> Static [
            "componentTypeToTitle" => T<string>?componentType ^-> T<string>
        ]
        |> Import "ComponentItemConfig" "golden-layout"

    let StackItemConfig =
        Pattern.Config "StackItemConfig" {
            Required = []
            Optional =
                [
                    "content", Type.ArrayOf ComponentItemConfig.Type
                    "activeItemIndex", T<int>
                ]
        }
        |=> Inherits HeaderedItemConfig.Type

    let RowOrColumnItemConfig =
        Pattern.Config "RowOrColumnItemConfig" {
            Required = []
            Optional =
                [
                    "content", Type.ArrayOf ItemConfig.Type
                ]
        }
        |=> Inherits ItemConfig.Type
        |+> Static [
            "isChildItemConfig" => TSelf?itemConfig ^-> T<bool>
        ]
        |> Import "RowOrColumnItemConfig" "golden-layout"

    // ------------------------------------------------------------------
    // Layout configs
    // ------------------------------------------------------------------

    let LayoutSettings =
        Pattern.Config "LayoutSettings" {
            Required = []
            Optional =
                [
                    "hasHeaders", T<bool>
                    "constrainDragToContainer", T<bool>
                    "reorderEnabled", T<bool>
                    "popoutWholeStack", T<bool>
                    "blockedPopoutsThrowError", T<bool>
                    "closePopoutsOnUnload", T<bool>
                    "showPopoutIcon", T<bool>
                    "showMaximiseIcon", T<bool>
                    "showCloseIcon", T<bool>
                    "responsiveMode", ResponsiveMode.Type
                    "tabOverlapAllowance", T<int>
                    "reorderOnTabMenuClick", T<bool>
                    "tabControlOffset", T<int>
                    "popInOnClose", T<bool>
                ]
        }

    let LayoutDimensions =
        Pattern.Config "LayoutDimensions" {
            Required = []
            Optional =
                [
                    "borderWidth", T<int>
                    "borderGrabWidth", T<int>
                    "minItemHeight", T<float>
                    "defaultMinItemHeight", T<string>
                    "minItemWidth", T<float>
                    "defaultMinItemWidth", T<string>
                    "headerHeight", T<int>
                    "dragProxyWidth", T<int>
                    "dragProxyHeight", T<int>
                ]
        }

    let LayoutLabels =
        Pattern.Config "LayoutLabels" {
            Required = []
            Optional =
                [
                    "close", T<string>
                    "maximise", T<string>
                    "minimise", T<string>
                    "popin", T<string>
                    "popout", T<string>
                    "tabDropdown", T<string>
                ]
        }

    let LayoutHeader =
        Pattern.Config "LayoutHeader" {
            Required = []
            Optional =
                [
                    "show", T<bool> + Side.Type
                    "popout", T<bool> + T<string>
                    "popin", T<string>
                    "maximise", T<bool> + T<string>
                    "close", T<bool> + T<string>
                    "minimise", T<string>
                    "tabDropdown", T<bool> + T<string>
                ]
        }

    /// `PopoutLayoutConfig.Window`.
    let PopoutWindow =
        Pattern.Config "PopoutWindow" {
            Required = []
            Optional =
                [
                    "width", T<float>
                    "height", T<float>
                    "left", T<float>
                    "top", T<float>
                ]
        }

    /// `PopoutLayoutConfig` is a `LayoutConfig` plus the popout position, so the
    /// shared fields are spelled out here (WIG configs are flat).
    let PopoutLayoutConfig =
        Pattern.Config "PopoutLayoutConfig" {
            Required = []
            Optional =
                [
                    "root", ItemConfig.Type
                    "content", Type.ArrayOf ItemConfig.Type
                    "openPopouts", Type.ArrayOf TSelf
                    "settings", LayoutSettings.Type
                    "labels", LayoutLabels.Type
                    "header", LayoutHeader.Type
                    "parentId", T<string>
                    "indexInParent", T<int>
                    "dimensions", LayoutDimensions.Type
                    "window", PopoutWindow.Type
                ]
        }


    // ------------------------------------------------------------------
    // Resolved (saved) configs
    // ------------------------------------------------------------------

    let ResolvedItemConfig =
        Pattern.Config "ResolvedItemConfig" {
            Required = []
            Optional =
                [
                    "type", ItemType.Type
                    "content", Type.ArrayOf TSelf
                    "size", T<float>
                    "sizeUnit", SizeUnitEnum.Type
                    "minSize", T<float>
                    "minSizeUnit", SizeUnitEnum.Type
                    "id", T<string>
                    "isClosable", T<bool>
                ]
        }
        |+> Static [
            "isComponentItem" => TSelf?itemConfig ^-> T<bool>
            "isStackItem" => TSelf?itemConfig ^-> T<bool>
        ]
        |> Import "ResolvedItemConfig" "golden-layout"

    /// `ResolvedHeaderedItemConfig.Header`.
    let ResolvedItemHeaderConfig =
        Pattern.Config "ResolvedItemHeaderConfig" {
            Required = []
            Optional =
                [
                    "show", T<bool> + Side.Type
                    "popout", T<bool> + T<string>
                    "maximise", T<bool> + T<string>
                    "close", T<string>
                    "minimise", T<string>
                    "tabDropdown", T<bool> + T<string>
                ]
        }

    let ResolvedHeaderedItemConfig =
        Pattern.Config "ResolvedHeaderedItemConfig" {
            Required = []
            Optional =
                [
                    "header", ResolvedItemHeaderConfig.Type
                    "maximised", T<bool>
                ]
        }
        |=> Inherits ResolvedItemConfig.Type

    let ResolvedComponentItemConfig =
        Pattern.Config "ResolvedComponentItemConfig" {
            Required = []
            Optional =
                [
                    "title", T<string>
                    "reorderEnabled", T<bool>
                    "componentType", T<string>
                    "componentState", JsonValue
                ]
        }
        |=> Inherits ResolvedHeaderedItemConfig.Type
        |+> Static [
            "resolveComponentTypeName" => TSelf?itemConfig ^-> !? T<string>
            "createDefault" => !? T<string>?componentType * !? JsonValue?componentState * !? T<string>?title ^-> TSelf
        ]

    let ResolvedStackItemConfig =
        Pattern.Config "ResolvedStackItemConfig" {
            Required = []
            Optional =
                [
                    "content", Type.ArrayOf ResolvedComponentItemConfig.Type
                    "activeItemIndex", T<int>
                ]
        }
        |=> Inherits ResolvedHeaderedItemConfig.Type

    let ResolvedRowOrColumnItemConfig =
        Pattern.Config "ResolvedRowOrColumnItemConfig" {
            Required = []
            Optional =
                [
                    "content", Type.ArrayOf ResolvedItemConfig.Type
                ]
        }
        |=> Inherits ResolvedItemConfig.Type

    let ResolvedLayoutSettings =
        Pattern.Config "ResolvedLayoutSettings" {
            Required = []
            Optional =
                [
                    "constrainDragToContainer", T<bool>
                    "reorderEnabled", T<bool>
                    "popoutWholeStack", T<bool>
                    "blockedPopoutsThrowError", T<bool>
                    "closePopoutsOnUnload", T<bool>
                    "responsiveMode", ResponsiveMode.Type
                    "tabOverlapAllowance", T<int>
                    "reorderOnTabMenuClick", T<bool>
                    "tabControlOffset", T<int>
                    "popInOnClose", T<bool>
                ]
        }

    let ResolvedLayoutDimensions =
        Pattern.Config "ResolvedLayoutDimensions" {
            Required = []
            Optional =
                [
                    "borderWidth", T<int>
                    "borderGrabWidth", T<int>
                    "defaultMinItemHeight", T<float>
                    "defaultMinItemHeightUnit", SizeUnitEnum.Type
                    "defaultMinItemWidth", T<float>
                    "defaultMinItemWidthUnit", SizeUnitEnum.Type
                    "headerHeight", T<int>
                    "dragProxyWidth", T<int>
                    "dragProxyHeight", T<int>
                ]
        }

    let ResolvedLayoutHeader =
        Pattern.Config "ResolvedLayoutHeader" {
            Required = []
            Optional =
                [
                    "show", T<bool> + Side.Type
                    "popout", T<bool> + T<string>
                    "dock", T<string>
                    "maximise", T<bool> + T<string>
                    "minimise", T<string>
                    "close", T<bool> + T<string>
                    "tabDropdown", T<bool> + T<string>
                ]
        }

    let ResolvedPopoutWindow =
        Pattern.Config "ResolvedPopoutWindow" {
            Required = []
            Optional =
                [
                    "width", T<float>
                    "height", T<float>
                    "left", T<float>
                    "top", T<float>
                ]
        }

    let ResolvedPopoutLayoutConfig =
        Pattern.Config "ResolvedPopoutLayoutConfig" {
            Required = []
            Optional =
                [
                    "root", ResolvedItemConfig.Type
                    "openPopouts", Type.ArrayOf TSelf
                    "settings", ResolvedLayoutSettings.Type
                    "header", ResolvedLayoutHeader.Type
                    "dimensions", ResolvedLayoutDimensions.Type
                    "parentId", T<string>
                    "indexInParent", T<int>
                    "window", ResolvedPopoutWindow.Type
                    "resolved", T<bool>
                ]
        }

    let ResolvedLayoutConfig =
        Pattern.Config "ResolvedLayoutConfig" {
            Required = []
            Optional =
                [
                    "root", ResolvedItemConfig.Type
                    "openPopouts", Type.ArrayOf ResolvedPopoutLayoutConfig.Type
                    "settings", ResolvedLayoutSettings.Type
                    "header", ResolvedLayoutHeader.Type
                    "dimensions", ResolvedLayoutDimensions.Type
                    "resolved", T<bool>
                ]
        }
        |+> Static [
            "isPopout" => TSelf?config ^-> T<bool>
            "createDefault" => T<unit> ^-> TSelf
            "createCopy" => TSelf?config ^-> TSelf
            "minifyConfig" => TSelf?layoutConfig ^-> TSelf
            "unminifyConfig" => TSelf?minifiedConfig ^-> TSelf
        ]
        |> Import "ResolvedLayoutConfig" "golden-layout"

    // `LayoutConfig` is declared after the resolved configs so that its
    // namespace helpers can refer to `ResolvedLayoutConfig`.
    let LayoutConfig =
        Pattern.Config "LayoutConfig" {
            Required = []
            Optional =
                [
                    "root", ItemConfig.Type
                    "content", Type.ArrayOf ItemConfig.Type
                    "openPopouts", Type.ArrayOf PopoutLayoutConfig.Type
                    "dimensions", LayoutDimensions.Type
                    "settings", LayoutSettings.Type
                    "labels", LayoutLabels.Type
                    "header", LayoutHeader.Type
                ]
        }
        |+> Static [
            "isPopout" => TSelf?config ^-> T<bool>
            "fromResolved" => ResolvedLayoutConfig.Type?config ^-> TSelf
            "isResolved" => (TSelf + ResolvedLayoutConfig.Type)?configOrResolvedConfig ^-> T<bool>
        ]
        |> Import "LayoutConfig" "golden-layout"

    // ------------------------------------------------------------------
    // LayoutManager support types
    // ------------------------------------------------------------------

    /// `LayoutManager.Location`.
    let Location =
        Pattern.Config "Location" {
            Required = []
            Optional =
                [
                    "parentItem", ContentItemClass.Type
                    "index", T<int>
                ]
        }

    /// `LayoutManager.LocationSelector`.
    let LocationSelector =
        Pattern.Config "LocationSelector" {
            Required = []
            Optional =
                [
                    "typeId", LocationSelectorTypeId.Type
                    "index", T<int>
                ]
        }


    /// `DragSource.ComponentItemConfig` (deprecated upstream).
    let DragSourceComponentItemConfig =
        Pattern.Config "DragSourceComponentItemConfig" {
            Required = []
            Optional =
                [
                    "type", T<string>
                    "state", JsonValue
                    "title", T<string>
                ]
        }

    /// `GoldenLayout.ComponentFactoryFunction`.
    let ComponentFactoryFunction =
        fnOf
            [ "container", ComponentContainerClass.Type
              "state", JsonValue
              "virtual", T<bool> ]
            T<obj>

    /// `GoldenLayout.GetComponentConstructorCallback`.
    let GetComponentConstructorCallback =
        fnOf [ "config", ResolvedComponentItemConfig.Type ] T<obj>

    /// `ComponentContainer.BindableComponent`.
    let BindableComponent =
        Pattern.Config "BindableComponent" {
            Required = []
            Optional =
                [
                    "component", T<obj>
                    "virtual", T<bool>
                ]
        }

    /// `GoldenLayout.VirtuableComponent`.
    let VirtuableComponent =
        Pattern.Config "VirtuableComponent" {
            Required = []
            Optional =
                [
                    "rootHtmlElement", T<Dom.Element>
                ]
        }

    /// `GoldenLayout.ComponentInstantiator`.
    let ComponentInstantiator =
        Pattern.Config "ComponentInstantiator" {
            Required = []
            Optional =
                [
                    "constructor", T<obj>
                    "factoryFunction", ComponentFactoryFunction
                    "virtual", T<bool>
                ]
        }

    /// `VirtualLayout.BindComponentEventHandler`.
    let BindComponentEventHandler =
        fnOf
            [ "container", ComponentContainerClass.Type
              "itemConfig", ResolvedComponentItemConfig.Type ]
            BindableComponent.Type

    /// `VirtualLayout.UnbindComponentEventHandler`.
    let UnbindComponentEventHandler =
        fnOf [ "container", ComponentContainerClass.Type ] T<unit>

    /// `VirtualLayout.GetComponentEventHandler`.
    let GetComponentEventHandler =
        fnOf
            [ "container", ComponentContainerClass.Type
              "itemConfig", ResolvedComponentItemConfig.Type ]
            T<obj>

    /// `VirtualLayout.ReleaseComponentEventHandler`.
    let ReleaseComponentEventHandler =
        fnOf
            [ "container", ComponentContainerClass.Type
              "component", T<obj> ]
            T<unit>

    /// `ComponentContainer.StateRequestEventHandler`.
    let StateRequestEventHandler = fnOf [] JsonValue

    /// `ComponentContainer.VirtualRectingRequiredEvent`.
    let VirtualRectingRequiredEvent =
        fnOf
            [ "container", ComponentContainerClass.Type
              "width", T<float>
              "height", T<float> ]
            T<unit>

    /// `ComponentContainer.VirtualVisibilityChangeRequiredEvent`.
    let VirtualVisibilityChangeRequiredEvent =
        fnOf
            [ "container", ComponentContainerClass.Type
              "visible", T<bool> ]
            T<unit>

    /// `ComponentContainer.VirtualZIndexChangeRequiredEvent`.
    let VirtualZIndexChangeRequiredEvent =
        fnOf
            [ "container", ComponentContainerClass.Type
              "logicalZIndex", LogicalZIndex.Type
              "defaultZIndex", T<string> ]
            T<unit>

    /// `LayoutManager.BeforeVirtualRectingEvent`.
    let BeforeVirtualRectingEvent = fnOf [ "count", T<int> ] T<unit>

    /// `LayoutManager.AfterVirtualRectingEvent`.
    let AfterVirtualRectingEvent = fnOf [] T<unit>

    /// The callback `LayoutManager.NewDragSource` expects.
    let DragSourceItemConfigCallback =
        fnOf [] (DragSourceComponentItemConfig.Type + ComponentItemConfig.Type)

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    // One single-case event-name enum per event, so every `on`/`off`/`emit`
    // overload has a distinct CLR signature and a typed payload.
    let private eventDefs : (string * string * (string * Type.IType) list * bool) list =
        [
            "ActiveContentItemChanged", "activeContentItemChanged", [ "componentItem", ComponentItemClass.Type ], false
            "All", "__all", [ "args", T<obj[]> ], true
            "BeforeComponentRelease", "beforeComponentRelease", [ "component", T<obj> ], false
            "BeforeItemDestroyed", "beforeItemDestroyed", [ "bubblingEvent", BubblingEventClass.Type ], false
            "Blur", "blur", [ "bubblingEvent", BubblingEventClass.Type ], false
            "Close", "close", [], false
            "Closed", "closed", [], false
            "Destroy", "destroy", [], false
            "Drag", "drag", [ "offsetX", T<float>; "offsetY", T<float>; "event", PointerEventClass.Type ], false
            "DragStart", "dragStart", [ "originalX", T<float>; "originalY", T<float> ], false
            "DragStop", "dragStop", [ "event", PointerEventClass.Type ], false
            "Focus", "focus", [ "bubblingEvent", BubblingEventClass.Type ], false
            "Hide", "hide", [], false
            "Initialised", "initialised", [], false
            "ItemCreated", "itemCreated", [ "bubblingEvent", BubblingEventClass.Type ], false
            "ItemDestroyed", "itemDestroyed", [ "bubblingEvent", BubblingEventClass.Type ], false
            "ItemDropped", "itemDropped", [ "componentItem", ComponentItemClass.Type ], false
            "Maximised", "maximised", [], false
            "Minimised", "minimised", [], false
            "Open", "open", [], false
            "PopIn", "popIn", [], false
            "Resize", "resize", [], false
            "Show", "show", [], false
            "Shown", "shown", [], false
            "StackHeaderClick", "stackHeaderClick", [ "clickEvent", ClickBubblingEventClass.Type ], false
            "StackHeaderTouchStart", "stackHeaderTouchStart", [ "touchStartEvent", TouchStartBubblingEventClass.Type ], false
            "StateChanged", "stateChanged", [], false
            "Tab", "tab", [ "tab", TabClass.Type ], false
            "TabCreated", "tabCreated", [ "tab", TabClass.Type ], false
            "TitleChanged", "titleChanged", [ "title", T<string> ], false
            "UserBroadcast", "userBroadcast", [ "args", T<obj[]> ], true
            "WindowClosed", "windowClosed", [ "browserPopout", BrowserPopoutClass.Type ], false
            "WindowOpened", "windowOpened", [ "browserPopout", BrowserPopoutClass.Type ], false
        ]

    let private eventNameTypes =
        [
            for (netName, jsName, _, _) in eventDefs ->
                Pattern.EnumInlines (netName + "EventName") [ netName, quote jsName ]
        ]

    // ------------------------------------------------------------------
    // EventEmitter and the bubbling event payloads
    // ------------------------------------------------------------------

    let BubblingEvent =
        BubblingEventClass
        |+> Instance [
            Getter "name" T<string>
            Getter "target" EventEmitterClass.Type
            Getter "origin" EventEmitterClass.Type
                |> ObsoleteWithMessage "Use Target instead."
            Getter "isPropagationStopped" T<bool>
            "stopPropagation" => T<unit> ^-> T<unit>
        ]

    let ClickBubblingEvent =
        ClickBubblingEventClass
        |=> Inherits BubblingEventClass
        |+> Instance [
            Getter "mouseEvent" T<Dom.MouseEvent>
        ]

    let TouchStartBubblingEvent =
        TouchStartBubblingEventClass
        |=> Inherits BubblingEventClass
        |+> Instance [
            // Core has no `TouchEvent` model, so the touch payload stays a DOM event.
            Getter "touchEvent" T<Dom.Event>
        ]

    let EventEmitter =
        EventEmitterClass
        |+> Instance [
            yield "tryBubbleEvent" => T<string>?name * T<obj[]>?args ^-> T<unit>
            for (nameType, (_, _, args, varargs)) in List.zip eventNameTypes eventDefs do
                let callback = fnOf args T<unit>
                yield "on" => nameType.Type?eventName * callback?callback ^-> T<unit>
                yield "off" => nameType.Type?eventName * callback?callback ^-> T<unit>
                yield "addEventListener" => nameType.Type?eventName * callback?callback ^-> T<unit>
                yield "removeEventListener" => nameType.Type?eventName * callback?callback ^-> T<unit>
                yield "unbind" => nameType.Type?eventName * callback?callback ^-> T<unit>
                yield "emitBaseBubblingEvent" => nameType.Type?eventName ^-> T<unit>
                if varargs then
                    yield "emit" => nameType.Type?eventName *+ T<obj> ^-> T<unit>
                    yield "trigger" => nameType.Type?eventName *+ T<obj> ^-> T<unit>
                else
                    yield "emit" => (emitArgs nameType.Type args) ^-> T<unit>
                    yield "trigger" => (emitArgs nameType.Type args) ^-> T<unit>
        ]

    let EventHub =
        EventHubClass
        |=> Inherits EventEmitterClass
        |+> Instance [
            "emitUserBroadcast" => !+ T<obj> ^-> T<unit>
        ]

    // ------------------------------------------------------------------
    // Content items
    // ------------------------------------------------------------------

    let ContentItem =
        ContentItemClass
        |=> Inherits EventEmitterClass
        |+> Instance [
            Getter "layoutManager" LayoutManagerClass.Type
            "isGround" =@ T<bool>
            "isRow" =@ T<bool>
            "isColumn" =@ T<bool>
            "isStack" =@ T<bool>
            "isComponent" =@ T<bool>
            Getter "type" ItemType.Type
            "id" =@ T<string>
            Getter "parent" !? ContentItemClass.Type
            Getter "contentItems" (!| ContentItemClass.Type)
            Getter "isClosable" T<bool>
            Getter "element" T<Dom.Element>
            Getter "isInitialised" T<bool>
            "removeChild" => TSelf?contentItem * !? T<bool>?keepChild ^-> T<unit>
            "addChild" => TSelf?contentItem * !? T<int>?index * !? T<bool>?suspendResize ^-> T<int>
            "remove" => T<unit> ^-> T<unit>
            "popout" => T<unit> ^-> BrowserPopoutClass.Type
            "toConfig" => T<unit> ^-> ResolvedItemConfig.Type
            "tryBubbleEvent" => T<string>?name * T<obj[]>?args ^-> T<unit>
        ]
        |+> Static [
            "isComponentItem" => TSelf?item ^-> T<bool>
            "isComponentParentableItem" => TSelf?item ^-> T<bool>
        ]

    let ComponentParentableItem =
        ComponentParentableItemClass
        |=> Inherits ContentItemClass
        |+> Instance [
            Getter "focused" T<bool>
            "setActiveComponentItem" => ComponentItemClass.Type?item * T<bool>?focus * T<bool>?suppressFocusEvent ^-> T<unit>
        ]

    let ComponentItem =
        ComponentItemClass
        |=> Inherits ContentItemClass
        |+> Instance [
            Getter "componentType" T<string>
            "reorderEnabled" =@ T<bool>
            Getter "component" !? T<obj>
            Getter "container" ComponentContainerClass.Type
            Getter "parentItem" ComponentParentableItemClass.Type
            Getter "headerConfig" !? ResolvedItemHeaderConfig.Type
            Getter "title" T<string>
            Getter "tab" TabClass.Type
            Getter "focused" T<bool>
            "applyUpdatableConfig" => ResolvedComponentItemConfig.Type?config ^-> T<unit>
            "toConfig" => T<unit> ^-> ResolvedComponentItemConfig.Type
            "close" => T<unit> ^-> T<unit>
            "setTitle" => T<string>?title ^-> T<unit>
            "setTab" => TabClass.Type?tab ^-> T<unit>
            "focus" => !? T<bool>?suppressEvent ^-> T<unit>
            "blur" => !? T<bool>?suppressEvent ^-> T<unit>
        ]

    let RowOrColumn =
        RowOrColumnClass
        |=> Inherits ContentItemClass
        |+> Instance [
            "newComponent" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title * !? T<int>?index ^-> ComponentItemClass.Type
            "addComponent" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title * !? T<int>?index ^-> T<int>
            "newItem" => ItemConfig.Type?itemConfig * !? T<int>?index ^-> ContentItemClass.Type
            "addItem" => ItemConfig.Type?itemConfig * !? T<int>?index ^-> T<int>
            "addChild" => ContentItemClass.Type?contentItem * !? T<int>?index * !? T<bool>?suspendResize ^-> T<int>
            "removeChild" => ContentItemClass.Type?contentItem * T<bool>?keepChild ^-> T<unit>
            "replaceChild" => ContentItemClass.Type?oldChild * ContentItemClass.Type?newChild ^-> T<unit>
            "updateSize" => T<bool>?force ^-> T<unit>
            "toConfig" => T<unit> ^-> ResolvedRowOrColumnItemConfig.Type
        ]

    let Stack =
        StackClass
        |=> Inherits ComponentParentableItemClass
        |+> Instance [
            Getter "childElementContainer" T<Dom.Element>
            Getter "header" HeaderClass.Type
            Getter "headerShow" T<bool>
            Getter "headerSide" Side.Type
            Getter "headerLeftRightSided" T<bool>
            Getter "isMaximised" T<bool>
            Getter "stackParent" ContentItemClass.Type
            "setActiveContentItem" => ContentItemClass.Type?item ^-> T<unit>
                |> ObsoleteWithMessage "Use Stack.SetActiveComponentItem instead."
            "setActiveComponentItem" => ComponentItemClass.Type?componentItem * T<bool>?focus * !? T<bool>?suppressFocusEvent ^-> T<unit>
            "getActiveContentItem" => T<unit> ^-> !? ContentItemClass.Type
                |> ObsoleteWithMessage "Use Stack.GetActiveComponentItem instead."
            "getActiveComponentItem" => T<unit> ^-> !? ComponentItemClass.Type
            "newComponent" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title * !? T<int>?index ^-> ComponentItemClass.Type
            "addComponent" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title * !? T<int>?index ^-> T<int>
            "newItem" => ComponentItemConfig.Type?itemConfig * !? T<int>?index ^-> ContentItemClass.Type
            "addItem" => ComponentItemConfig.Type?itemConfig * !? T<int>?index ^-> T<int>
            "addChild" => ContentItemClass.Type?contentItem * !? T<int>?index * !? T<bool>?focus ^-> T<int>
            "removeChild" => ContentItemClass.Type?contentItem * T<bool>?keepChild ^-> T<unit>
            "toggleMaximise" => T<unit> ^-> T<unit>
            "maximise" => T<unit> ^-> T<unit>
            "minimise" => T<unit> ^-> T<unit>
            "toConfig" => T<unit> ^-> ResolvedStackItemConfig.Type
        ]

    // ------------------------------------------------------------------
    // Header / Tab / ComponentContainer / BrowserPopout
    // ------------------------------------------------------------------

    let Header =
        HeaderClass
        |=> Inherits EventEmitterClass
        |+> Instance [
            Getter "show" T<bool>
            Getter "side" Side.Type
            Getter "leftRightSided" T<bool>
            Getter "layoutManager" LayoutManagerClass.Type
            Getter "parent" StackClass.Type
            Getter "tabs" (!| TabClass.Type)
            Getter "lastVisibleTabIndex" T<int>
            Getter "element" T<Dom.Element>
            Getter "tabsContainerElement" T<Dom.Element>
            Getter "controlsContainerElement" T<Dom.Element>
        ]

    let Tab =
        TabClass
        |+> Instance [
            Getter "isActive" T<bool>
            Getter "componentItem" ComponentItemClass.Type
            Getter "contentItem" ComponentItemClass.Type
                |> ObsoleteWithMessage "Use Tab.ComponentItem instead."
            Getter "element" T<Dom.Element>
            Getter "titleElement" T<Dom.Element>
            Getter "closeElement" !? T<Dom.Element>
            "reorderEnabled" =@ T<bool>
            "setTitle" => T<string>?title ^-> T<unit>
            "setActive" => T<bool>?isActive ^-> T<unit>
        ]

    let ComponentContainer =
        ComponentContainerClass
        |=> Inherits EventEmitterClass
        |+> Instance [
            "stateRequestEvent" =@ !? StateRequestEventHandler
            "virtualRectingRequiredEvent" =@ !? VirtualRectingRequiredEvent
            "virtualVisibilityChangeRequiredEvent" =@ !? VirtualVisibilityChangeRequiredEvent
            "virtualZIndexChangeRequiredEvent" =@ !? VirtualZIndexChangeRequiredEvent
            Getter "width" T<float>
            Getter "height" T<float>
            Getter "parent" ComponentItemClass.Type
            Getter "componentType" T<string>
            Getter "virtual" T<bool>
            Getter "component" T<obj>
            Getter "tab" TabClass.Type
            Getter "title" T<string>
            Getter "layoutManager" LayoutManagerClass.Type
            Getter "isHidden" T<bool>
            Getter "visible" T<bool>
            Getter "state" !? JsonValue
            Getter "initialState" !? JsonValue
            Getter "element" T<Dom.Element>
            "getElement" => T<unit> ^-> T<Dom.Element> |> ObsoleteWithMessage "Use ComponentContainer.Element instead."
            "hide" => T<unit> ^-> T<unit>
            "show" => T<unit> ^-> T<unit>
            "focus" => !? T<bool>?suppressEvent ^-> T<unit>
            "blur" => !? T<bool>?suppressEvent ^-> T<unit>
            "close" => T<unit> ^-> T<unit>
            "replaceComponent" => ComponentItemConfig.Type?itemConfig ^-> T<unit>
            "getState" => T<unit> ^-> !? JsonValue |> ObsoleteWithMessage "Use ComponentContainer.InitialState instead."
            "extendState" => T<obj>?state ^-> T<unit> |> ObsoleteWithMessage "Use ComponentContainer.StateRequestEvent instead."
            "setState" => JsonValue?state ^-> T<unit> |> ObsoleteWithMessage "Use ComponentContainer.StateRequestEvent instead."
            "setTitle" => T<string>?title ^-> T<unit>
            "setBaseLogicalZIndex" => T<unit> ^-> T<unit>
            "setLogicalZIndex" => LogicalZIndex.Type?logicalZIndex ^-> T<unit>
        ]

    let BrowserPopout =
        BrowserPopoutClass
        |=> Inherits EventEmitterClass
        |+> Instance [
            "toConfig" => T<unit> ^-> ResolvedPopoutLayoutConfig.Type
            "getGlInstance" => T<unit> ^-> LayoutManagerClass.Type
            "getWindow" => T<unit> ^-> T<JavaScript.Window>
            "close" => T<unit> ^-> T<unit>
            "popIn" => T<unit> ^-> T<unit>
        ]

    // ------------------------------------------------------------------
    // Layout managers
    // ------------------------------------------------------------------

    let LayoutManager =
        LayoutManagerClass
        |=> Inherits EventEmitterClass
        |+> Static [
            Getter "defaultLocationSelectors" (!| LocationSelector.Type)
            Getter "afterFocusedItemIfPossibleLocationSelectors" (!| LocationSelector.Type)
        ]
        |+> Instance [
            "resizeWithContainerAutomatically" =@ T<bool>
            "resizeDebounceInterval" =@ T<float>
            "resizeDebounceExtendedWhenPossible" =@ T<bool>
            Getter "isSubWindow" T<bool>
            "layoutConfig" =@ ResolvedLayoutConfig.Type
            "beforeVirtualRectingEvent" =@ !? BeforeVirtualRectingEvent
            "afterVirtualRectingEvent" =@ !? AfterVirtualRectingEvent
            Getter "container" T<Dom.Element>
            Getter "isInitialised" T<bool>
            Getter "openPopouts" (!| BrowserPopoutClass.Type)
            Getter "width" !? T<float>
            Getter "height" !? T<float>
            Getter "eventHub" EventHubClass.Type
            Getter "rootItem" !? ContentItemClass.Type
            Getter "focusedComponentItem" !? ComponentItemClass.Type
            Getter "maximisedStack" !? StackClass.Type
            Getter "deprecatedConstructor" T<bool>
            "destroy" => T<unit> ^-> T<unit>
            "minifyConfig" => ResolvedLayoutConfig.Type?config ^-> ResolvedLayoutConfig.Type
                |> ObsoleteWithMessage "Use ResolvedLayoutConfig.MinifyConfig instead."
            "unminifyConfig" => ResolvedLayoutConfig.Type?config ^-> ResolvedLayoutConfig.Type
                |> ObsoleteWithMessage "Use ResolvedLayoutConfig.UnminifyConfig instead."
            "loadLayout" => LayoutConfig.Type?layoutConfig ^-> T<unit>
            "saveLayout" => T<unit> ^-> ResolvedLayoutConfig.Type
            "clear" => T<unit> ^-> T<unit>
            "toConfig" => T<unit> ^-> ResolvedLayoutConfig.Type
                |> ObsoleteWithMessage "Use LayoutManager.SaveLayout instead."
            "newComponent" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title ^-> ComponentItemClass.Type
            "newComponentAtLocation" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title * !? (!| LocationSelector.Type)?locationSelectors ^-> !? ComponentItemClass.Type
            "addComponent" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title ^-> Location.Type
            "addComponentAtLocation" => T<string>?componentType * !? JsonValue?componentState * !? T<string>?title * !? (!| LocationSelector.Type)?locationSelectors ^-> !? Location.Type
            "newItem" => ItemConfig.Type?itemConfig ^-> ContentItemClass.Type
            "newItemAtLocation" => ItemConfig.Type?itemConfig * !? (!| LocationSelector.Type)?locationSelectors ^-> !? ContentItemClass.Type
            "addItem" => ItemConfig.Type?itemConfig ^-> Location.Type
            "addItemAtLocation" => ItemConfig.Type?itemConfig * !? (!| LocationSelector.Type)?locationSelectors ^-> !? Location.Type
            "loadComponentAsRoot" => ComponentItemConfig.Type?itemConfig ^-> T<unit>
            "updateSize" => T<float>?width * T<float>?height ^-> T<unit>
                |> ObsoleteWithMessage "Use LayoutManager.SetSize instead."
            "setSize" => T<float>?width * T<float>?height ^-> T<unit>
            "updateRootSize" => !? T<bool>?force ^-> T<unit>
            "createAndInitContentItem" => ResolvedItemConfig.Type?config * ContentItemClass.Type?parent ^-> ContentItemClass.Type
            "findFirstComponentItemById" => T<string>?id ^-> !? ComponentItemClass.Type
            "createPopout" =>
                (ContentItemClass.Type + ResolvedItemConfig.Type)?itemConfigOrContentItem
                * ResolvedPopoutWindow.Type?positionAndSize
                * T<string>?parentId
                * T<int>?indexInParent
                ^-> BrowserPopoutClass.Type
            "closeAllOpenPopouts" => T<unit> ^-> T<unit>
            "newDragSource" =>
                T<Dom.Element>?element
                * DragSourceItemConfigCallback?itemConfigCallback
                ^-> DragSourceClass.Type
            "newDragSource" =>
                T<Dom.Element>?element
                * T<string>?componentType
                * !? JsonValue?componentState
                * !? JsonValue?title
                * !? T<string>?id
                ^-> DragSourceClass.Type
                |> ObsoleteWithMessage "Use the callback overload instead."
            "removeDragSource" => DragSourceClass.Type?dragSource ^-> T<unit>
            "focusComponent" => ComponentItemClass.Type?item * !? T<bool>?suppressEvent ^-> T<unit>
            "clearComponentFocus" => !? T<bool>?suppressEvent ^-> T<unit>
            "checkMinimiseMaximisedStack" => T<unit> ^-> T<unit>
        ]

    let VirtualLayout =
        VirtualLayoutClass
        |=> Inherits LayoutManagerClass
        |+> Instance [
            "getComponentEvent" =@ !? GetComponentEventHandler
            "releaseComponentEvent" =@ !? ReleaseComponentEventHandler
            "bindComponentEvent" =@ !? BindComponentEventHandler
            "unbindComponentEvent" =@ !? UnbindComponentEventHandler
            "destroy" => T<unit> ^-> T<unit>
            "init" => T<unit> ^-> T<unit>
            "clearHtmlAndAdjustStylesForSubWindow" => T<unit> ^-> T<unit>
            "checkAddDefaultPopinButton" => T<unit> ^-> T<bool>
        ]

    let GoldenLayout =
        GoldenLayoutClass
        |=> Inherits VirtualLayoutClass
        |+> Static [
            Constructor (
                !? T<Dom.Element>?container
                * !? BindComponentEventHandler?bindComponentEventHandler
                * !? UnbindComponentEventHandler?unbindComponentEventHandler
            )
            Constructor (LayoutConfig.Type?config * !? T<Dom.Element>?container)
                |> ObsoleteWithMessage "Load the layout config with LayoutManager.LoadLayout instead."
        ]
        |+> Instance [
            "registerComponent" =>
                T<string>?name
                * T<obj>?componentConstructorOrFactoryFtn
                * !? T<bool>?isVirtual
                ^-> T<unit>
                |> ObsoleteWithMessage "Use RegisterComponentFactoryFunction or RegisterComponentConstructor instead."
            "registerComponentConstructor" =>
                T<string>?typeName * T<obj>?componentConstructor * !? T<bool>?isVirtual ^-> T<unit>
            "registerComponentFactoryFunction" =>
                T<string>?typeName
                * ComponentFactoryFunction?componentFactoryFunction
                * !? T<bool>?isVirtual
                ^-> T<unit>
            "registerComponentFunction" =>
                GetComponentConstructorCallback ^-> T<unit>
                |> ObsoleteWithMessage "Use RegisterGetComponentConstructorCallback instead."
            "registerGetComponentConstructorCallback" =>
                GetComponentConstructorCallback ^-> T<unit>
            "getRegisteredComponentTypeNames" => T<unit> ^-> !| T<string>
            "getComponentInstantiator" => ResolvedComponentItemConfig.Type?config ^-> !? ComponentInstantiator.Type
            "fireBeforeVirtualRectingEvent" => T<int>?count ^-> T<unit>
        ]

    // ------------------------------------------------------------------
    // Errors
    // ------------------------------------------------------------------

    let ExternalError =
        ExternalErrorClass
        |+> Instance [
            Getter "type" T<string>
        ]

    let ApiError = ApiErrorClass |=> Inherits ExternalErrorClass
    let BindError = BindErrorClass |=> Inherits ExternalErrorClass

    let ConfigurationError =
        ConfigurationErrorClass
        |=> Inherits ExternalErrorClass
        |+> Instance [
            Getter "node" !? T<string>
        ]

    let PopoutBlockedError = PopoutBlockedErrorClass |=> Inherits ExternalErrorClass

    // ------------------------------------------------------------------
    // Assembly
    // ------------------------------------------------------------------

    let Assembly =
        Assembly [
            Namespace "WebSharper.GoldenLayout" [
                ItemType
                Side
                LogicalZIndex
                ResponsiveMode
                SizeUnit
                SizeUnitEnum
                LocationSelectorTypeId
                StyleConstants
                I18nStrings
                JsonValueHelpers
                ItemConfig
                ItemHeaderConfig
                HeaderedItemConfig
                ComponentItemConfig
                StackItemConfig
                RowOrColumnItemConfig
                LayoutSettings
                LayoutDimensions
                LayoutLabels
                LayoutHeader
                PopoutWindow
                PopoutLayoutConfig
                LayoutConfig
                ResolvedItemConfig
                ResolvedItemHeaderConfig
                ResolvedHeaderedItemConfig
                ResolvedComponentItemConfig
                ResolvedStackItemConfig
                ResolvedRowOrColumnItemConfig
                ResolvedLayoutSettings
                ResolvedLayoutDimensions
                ResolvedLayoutHeader
                ResolvedPopoutWindow
                ResolvedPopoutLayoutConfig
                ResolvedLayoutConfig
                Location
                LocationSelector
                DragSourceComponentItemConfig
                BindableComponent
                VirtuableComponent
                ComponentInstantiator
                PointerEventClass
                yield! (eventNameTypes |> List.map (fun t -> t :> CodeModel.NamespaceEntity))
                BubblingEvent
                ClickBubblingEvent
                TouchStartBubblingEvent
                EventEmitter
                EventHub
                ContentItem
                ComponentParentableItem
                ComponentItem
                RowOrColumn
                Stack
                Header
                Tab
                ComponentContainer
                BrowserPopout
                LayoutManager
                VirtualLayout
                GoldenLayout
                DragSourceClass
                ExternalError
                ApiError
                BindError
                ConfigurationError
                PopoutBlockedError
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member x.Assembly = Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()