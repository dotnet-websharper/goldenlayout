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
namespace WebSharper.GoldenLayout.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open WebSharper.UI.Templating
open WebSharper.GoldenLayout

[<JavaScript>]
module Client =

    // The template is the whole document, loaded from the DOM.
    type IndexTemplate = Template<"index.html", ClientLoad.FromDocument>

    // -----------------------------------------------------------------
    // Page state
    // -----------------------------------------------------------------

    let componentType = "hello"

    let status = Var.Create "Create a layout, then drive it with the toolbar."
    let events = Var.Create ""
    let info = Var.Create "-"
    let layoutJson = Var.Create "(nothing saved yet)"
    let savedInfo = Var.Create "nothing saved yet"
    let registered = Var.Create "-"

    let themeStorageKey = "websharper-goldenlayout-theme"
    let theme = Var.Create "light"
    let themeLabel = Var.Create "🌙 Dark mode"

    let mutable layout : option<GoldenLayout> = None
    let mutable saved : option<ResolvedLayoutConfig> = None
    let mutable dragSource : option<DragSource> = None
    let mutable tabCounter = 0

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    let append (v: Var<string>) (line: string) =
        v.Value <-
            if v.Value = "" then line
            else v.Value + "\n" + line

    let logEvent (line: string) = append events line

    let setStatus (line: string) =
        status.Value <- line

    /// Unwraps an `Optional<'T>` (Golden Layout returns `undefined` for
    /// "not there" values).
    let defined (o: Optional<'T>) =
        if Optional.isDefined o && not (isNull (box o.Value)) then Some o.Value else None

    /// Config fields are plain (possibly `undefined`) properties, not `Optional`.
    let present (x: 'T) =
        if isNull (box x) then None else Some x

    /// Golden Layout defers its own initialisation to DOMContentLoaded when it
    /// is constructed while the document is still parsing, so loading a layout
    /// has to wait for that point too.
    [<Inline "document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', $0) : $0()">]
    let whenParsed (f: unit -> unit) = X<unit>

    let withLayout (f: GoldenLayout -> unit) =
        match layout with
        | Some gl -> f gl
        | None -> setStatus "No layout right now."

    let applyTheme () =
        JS.Document.DocumentElement.SetAttribute("data-theme", theme.Value)
        themeLabel.Value <-
            if theme.Value = "dark" then "☀️ Light mode" else "🌙 Dark mode"
        // Golden Layout ships a light and a dark theme; keep the layout in
        // sync with the page.
        let link = JS.Document.GetElementById "gl-theme"
        if not (isNull link) then
            link.SetAttribute(
                "href",
                if theme.Value = "dark" then "css/themes/goldenlayout-dark-theme.css"
                else "css/themes/goldenlayout-light-theme.css"
            )

    let toggleTheme () =
        theme.Value <- (if theme.Value = "dark" then "light" else "dark")
        try JS.Window.LocalStorage.SetItem(themeStorageKey, theme.Value) with _ -> ()
        applyTheme ()

    // -----------------------------------------------------------------
    // Components
    // -----------------------------------------------------------------

    let componentItem title state =
        ComponentItemConfig(
            Type = ItemType.Component,
            ComponentType = componentType,
            Title = title,
            ComponentState = box state,
            IsClosable = true,
            ReorderEnabled = true
        )

    let stateText (state: obj) =
        if isNull state then "(no state)" else string state

    /// Renders a Golden Layout component into its container. The binding
    /// hands out the container's element, so WebSharper.UI can take over.
    let renderComponent (container: ComponentContainer) (title: string) (body: Doc) =
        // Note: `container.Virtual`/`container.Component` are only valid once
        // Golden Layout has bound the component, i.e. after the factory returns.
        let initial =
            match defined container.InitialState with
            | Some s -> stateText s
            | None -> "(no state)"
        let meta =
            "componentType: " + container.ComponentType
            + " · visible: " + string container.Visible
            + " · initial state: " + initial
        let doc =
            div [Attr.Class "gl-component"] [
                h3 [Attr.Class "gl-component-title"] [text title]
                div [Attr.Class "gl-component-body"] [body]
                p [Attr.Class "gl-component-meta"] [text meta]
                button [
                    Attr.Class "btn btn-small"
                    on.click (fun _ _ -> setStatus ("Clicked inside \"" + title + "\"."))
                ] [text "Log a click"]
            ]
        Doc.Run container.Element doc
        box container.Element

    let registerComponents (gl: GoldenLayout) =
        gl.RegisterComponentFactoryFunction(
            componentType,
            fun (container: ComponentContainer) (state: obj) (isVirtual: bool) ->
                renderComponent container container.Title (p [] [text (stateText state)])
        )
        gl.RegisterComponentFactoryFunction(
            "clock",
            fun (container: ComponentContainer) (_: obj) (_: bool) ->
                renderComponent container container.Title
                    (p [] [text ("The time is " + System.DateTime.Now.ToString("HH:mm:ss") + ".")])
        )
        registered.Value <- String.concat ", " (gl.GetRegisteredComponentTypeNames())

    // -----------------------------------------------------------------
    // Layout config
    // -----------------------------------------------------------------

    let initialLayout () =
        tabCounter <- 3
        let header =
            LayoutHeader(
                Show = Union2Of2 Side.Top,
                Popout = Union2Of2 "pop out",
                Maximise = Union2Of2 "maximise"
            )
        LayoutConfig(
            Root =
                StackItemConfig(
                    Type = ItemType.Stack,
                    Content =
                        [|
                            componentItem "Welcome" "This component is part of the initial layout."
                            componentItem "Second tab" "Drag the tabs around, split stacks with a drop."
                        |],
                    ActiveItemIndex = 0
                ),
            Settings =
                LayoutSettings(
                    ConstrainDragToContainer = true,
                    ReorderEnabled = true,
                    PopoutWholeStack = false,
                    ResponsiveMode = ResponsiveMode.Onload
                ),
            Dimensions = LayoutDimensions(BorderWidth = 3, HeaderHeight = 26),
            Header = header
        )

    // -----------------------------------------------------------------
    // Events (typed one overload per event)
    // -----------------------------------------------------------------

    let refreshInfo () =
        withLayout (fun gl ->
            if not gl.IsInitialised then
                info.Value <- "not initialised yet"
            else
                let root =
                    match defined gl.RootItem with
                    | Some r -> string r.Type + " (id \"" + r.Id + "\", " + string r.ContentItems.Length + " children)"
                    | None -> "none"
                let focused =
                    match defined gl.FocusedComponentItem with
                    | Some c -> c.Title
                    | None -> "none"
                let maximised =
                    match defined gl.MaximisedStack with
                    | Some s -> string s.Type
                    | None -> "no"
                let size =
                    let w =
                        match defined gl.Width with
                        | Some w -> string w
                        | None -> "-"
                    let h =
                        match defined gl.Height with
                        | Some h -> string h
                        | None -> "-"
                    w + " × " + h
                info.Value <-
                    "root: " + root
                    + " · focused: " + focused
                    + " · maximised stack: " + maximised
                    + " · size: " + size
                    + " · popouts: " + string gl.OpenPopouts.Length
                    + " · registered: " + String.concat ", " (gl.GetRegisteredComponentTypeNames())
                    + " · deprecatedCtor: " + string gl.DeprecatedConstructor)

    let hookEvents (gl: GoldenLayout) =
        gl.On(InitialisedEventName.Initialised, fun () ->
            logEvent "initialised"
            refreshInfo ())
        gl.On(StateChangedEventName.StateChanged, fun () ->
            logEvent "stateChanged"
            refreshInfo ())
        gl.On(TabCreatedEventName.TabCreated, fun tab ->
            logEvent ("tabCreated: " + tab.ComponentItem.Title))
        gl.On(TitleChangedEventName.TitleChanged, fun title ->
            logEvent ("titleChanged: " + title))
        gl.On(ActiveContentItemChangedEventName.ActiveContentItemChanged, fun item ->
            logEvent ("activeContentItemChanged: " + item.Title))
        gl.On(ItemCreatedEventName.ItemCreated, fun e ->
            logEvent ("itemCreated: " + e.Name))
        gl.On(ItemDestroyedEventName.ItemDestroyed, fun e ->
            logEvent ("itemDestroyed: " + e.Name))
        gl.On(FocusEventName.Focus, fun e ->
            logEvent ("focus: " + e.Name))
        gl.On(BlurEventName.Blur, fun e ->
            logEvent ("blur: " + e.Name))
        gl.On(StackHeaderClickEventName.StackHeaderClick, fun e ->
            logEvent ("stackHeaderClick at x=" + string e.MouseEvent.ClientX))
        gl.On(WindowOpenedEventName.WindowOpened, fun (popout: BrowserPopout) ->
            let cfg = popout.ToConfig()
            let manager = popout.GetGlInstance()
            logEvent ("windowOpened: parentId \"" + cfg.ParentId + "\", window width "
                + string cfg.Window.Width
                + ", popout layout initialised: " + string manager.IsInitialised)
            refreshInfo ())
        gl.On(WindowClosedEventName.WindowClosed, fun _ ->
            logEvent "windowClosed"
            refreshInfo ())
        gl.On(UserBroadcastEventName.UserBroadcast, fun args ->
            logEvent ("userBroadcast: " + string args.Length + " argument(s)"))

    // -----------------------------------------------------------------
    // Layout lifecycle
    // -----------------------------------------------------------------

    let createLayout () =
        match JS.Document.GetElementById "layout" with
        | null -> setStatus "The layout container is missing."
        | container ->
            let gl = GoldenLayout(container)
            gl.ResizeWithContainerAutomatically <- true
            registerComponents gl
            hookEvents gl
            layout <- Some gl
            whenParsed (fun () ->
                gl.LoadLayout(initialLayout ())
                refreshInfo ()
                setStatus "Layout created and loaded.")

    let destroyLayout () =
        withLayout (fun gl ->
            gl.Destroy()
            layout <- None
            saved <- None
            info.Value <- "-"
            setStatus "Layout destroyed.")

    // -----------------------------------------------------------------
    // Toolbar actions
    // -----------------------------------------------------------------

    let addComponent () =
        withLayout (fun gl ->
            tabCounter <- tabCounter + 1
            let title = "Tab " + string tabCounter
            let location = gl.AddComponent(componentType, box ("Added from the toolbar, tab " + string tabCounter), title)
            setStatus ("AddComponent → index " + string location.Index + " in \"" + location.ParentItem.Id + "\".")
            refreshInfo ())

    let addComponentAtLocation () =
        withLayout (fun gl ->
            tabCounter <- tabCounter + 1
            let selectors = [| LocationSelector(TypeId = LocationSelectorTypeId.FirstStack, Index = 0) |]
            match defined (gl.NewComponentAtLocation(componentType, box "Added at a location selector", "Located " + string tabCounter, selectors)) with
            | Some item -> setStatus ("NewComponentAtLocation created \"" + item.Title + "\".")
            | None -> setStatus "No location selector matched."
            refreshInfo ())

    let addClock () =
        withLayout (fun gl ->
            let item = gl.NewComponent("clock", box "Added by NewComponent", "Clock")
            setStatus ("NewComponent(clock) → \"" + item.Title + "\".")
            refreshInfo ())

    let saveLayout () =
        withLayout (fun gl ->
            let cfg = gl.SaveLayout()
            saved <- Some cfg
            layoutJson.Value <-
                string (JSON.Stringify(cfg))
            savedInfo.Value <-
                "saved: " + string (JSON.Stringify(cfg).Length) + " chars, root: "
                + (match present cfg.Root with Some r -> string r.Type | None -> "none")
            setStatus "SaveLayout() captured the current layout."
            // Exercise the resolved-config helpers.
            let minified = ResolvedLayoutConfig.MinifyConfig(cfg)
            let restored = ResolvedLayoutConfig.UnminifyConfig(minified)
            layoutJson.Value <-
                layoutJson.Value + "\n\n-- minified: " + string (JSON.Stringify(minified).Length)
                + " chars, unminified root: "
                + (match present restored.Root with Some r -> string r.Type | None -> "none"))

    let loadSaved () =
        withLayout (fun gl ->
            match saved with
            | Some cfg ->
                gl.LoadLayout(LayoutConfig.FromResolved(cfg))
                setStatus "LoadLayout(LayoutConfig.FromResolved(saved)) done."
                refreshInfo ()
            | None -> setStatus "Nothing saved yet — press Save first.")

    let clearLayout () =
        withLayout (fun gl ->
            gl.Clear()
            refreshInfo ()
            setStatus "Clear() emptied the layout.")

    let maximiseFocused () =
        withLayout (fun gl ->
            match defined gl.FocusedComponentItem with
            | Some item ->
                let stack = As<Stack> (box item.ParentItem)
                stack.ToggleMaximise()
                setStatus ("Toggled maximise on stack \"" + stack.Id + "\"; maximised stack is now "
                    + (match defined gl.MaximisedStack with Some _ -> "set" | None -> "none") + ".")
                refreshInfo ()
            | None -> setStatus "Focus a tab first.")

    let closeFocused () =
        withLayout (fun gl ->
            match defined gl.FocusedComponentItem with
            | Some item ->
                let title = item.Title
                item.Close()
                setStatus ("Closed \"" + title + "\".")
                refreshInfo ()
            | None -> setStatus "Nothing focused to close.")

    let popoutFocused () =
        withLayout (fun gl ->
            match defined gl.FocusedComponentItem with
            | Some item when item.IsClosable ->
                // `BrowserPopout.ToConfig()` only works once the popout window
                // has loaded, so read it from the WindowOpened event instead.
                let _ = item.Popout()
                setStatus ("Popout requested for \"" + item.Title + "\"; open popouts: "
                    + string gl.OpenPopouts.Length + ".")
                refreshInfo ()
            | _ -> setStatus "Focus a closable component first.")

    let renameFocused () =
        withLayout (fun gl ->
            match defined gl.FocusedComponentItem with
            | Some item ->
                let newTitle = item.Title + " *"
                item.SetTitle newTitle
                item.Container.SetTitle newTitle
                item.Tab.SetTitle newTitle
                setStatus ("Renamed to \"" + newTitle + "\" via ComponentItem/ComponentContainer/Tab.")
                refreshInfo ()
            | None -> setStatus "Focus a tab first.")

    let firstStack (root: ContentItem) : option<Stack> =
        if root.IsStack then Some (As<Stack> (box root))
        else
            root.ContentItems
            |> Array.tryPick (fun child ->
                if child.IsStack then Some (As<Stack> (box child)) else None)

    let focusNext () =
        withLayout (fun gl ->
            match defined gl.RootItem with
            | None -> setStatus "The layout is empty."
            | Some root ->
                match firstStack root with
                | None -> setStatus "No stack found."
                | Some stack ->
                    let header : Header = stack.Header
                    let tabs : Tab[] = header.Tabs
                    if tabs.Length = 0 then setStatus "The stack has no tabs."
                    else
                        let next : Tab =
                            match Array.tryFind (fun (t: Tab) -> not t.IsActive) tabs with
                            | Some t -> t
                            | None -> tabs.[0]
                        stack.SetActiveComponentItem(next.ComponentItem, true)
                        gl.FocusComponent(next.ComponentItem)
                        setStatus ("Activated \"" + next.ComponentItem.Title + "\" (header on "
                            + string header.Side + ", " + string tabs.Length + " tabs, last visible "
                            + string header.LastVisibleTabIndex + ").")
                        refreshInfo ())

    let broadcast () =
        withLayout (fun gl ->
            gl.EventHub.EmitUserBroadcast([| box "hello from the sample"; box tabCounter |])
            setStatus "EventHub.EmitUserBroadcast sent to every window.")

    let resize () =
        withLayout (fun gl ->
            let c = gl.Container
            gl.SetSize(c.ClientWidth, c.ClientHeight)
            gl.UpdateRootSize(true)
            setStatus ("SetSize(" + string c.ClientWidth + ", " + string c.ClientHeight + ") + UpdateRootSize(true).")
            refreshInfo ())

    let inspect () =
        withLayout (fun gl ->
            refreshInfo ()
            let rootConfig =
                match defined gl.RootItem with
                | Some r -> r.ToConfig()
                | None -> ResolvedItemConfig()
            layoutJson.Value <- string (JSON.Stringify(rootConfig))
            let cfg = initialLayout ()
            setStatus ("Serialised RootItem.ToConfig(); ItemConfig.IsStack(root) = "
                + string (ItemConfig.IsStack(cfg.Root))
                + ", ComponentItemConfig.componentTypeToTitle = \""
                + ComponentItemConfig.ComponentTypeToTitle "hello"
                + "\"; default location selectors: "
                + string LayoutManager.DefaultLocationSelectors.Length + "."))

    let addDragSource () =
        withLayout (fun gl ->
            match dragSource with
            | Some _ -> setStatus "A drag source is already installed."
            | None ->
                match JS.Document.GetElementById "drag-card" with
                | null -> setStatus "The drag card is missing."
                | card ->
                    let ds =
                        gl.NewDragSource(
                            card,
                            fun () ->
                                tabCounter <- tabCounter + 1
                                componentItem ("Dragged " + string tabCounter) "Dropped from the drag card"
                        )
                    dragSource <- Some ds
                    setStatus "NewDragSource installed — drag the card into the layout.")

    let removeDragSource () =
        withLayout (fun gl ->
            match dragSource with
            | Some ds ->
                gl.RemoveDragSource(ds)
                dragSource <- None
                setStatus "RemoveDragSource done."
            | None -> setStatus "No drag source installed.")

    let tryErrors () =
        try
            failwith "boom"
        with e ->
            setStatus ("Caught " + e.Message + " — Golden Layout errors extend ExternalError.")

    // -----------------------------------------------------------------
    // Entry point
    // -----------------------------------------------------------------

    [<SPAEntryPoint>]
    let Main () =
        // Restore the theme before the first render.
        let stored = try JS.Window.LocalStorage.GetItem(themeStorageKey) with _ -> null
        if stored = "dark" || stored = "light" then theme.Value <- stored
        applyTheme ()

        IndexTemplate()
            .status(status.V)
            .events(events.V)
            .info(info.V)
            .layoutJson(layoutJson.V)
            .savedInfo(savedInfo.V)
            .registered(registered.V)
            .themeLabel(themeLabel.V)
            .ToggleTheme(fun _ -> toggleTheme ())
            .CreateLayout(fun _ -> createLayout ())
            .DestroyLayout(fun _ -> destroyLayout ())
            .AddComponent(fun _ -> addComponent ())
            .AddAtLocation(fun _ -> addComponentAtLocation ())
            .AddClock(fun _ -> addClock ())
            .SaveLayout(fun _ -> saveLayout ())
            .LoadSaved(fun _ -> loadSaved ())
            .ClearLayout(fun _ -> clearLayout ())
            .Maximise(fun _ -> maximiseFocused ())
            .CloseFocused(fun _ -> closeFocused ())
            .Popout(fun _ -> popoutFocused ())
            .Rename(fun _ -> renameFocused ())
            .FocusNext(fun _ -> focusNext ())
            .Broadcast(fun _ -> broadcast ())
            .Resize(fun _ -> resize ())
            .Inspect(fun _ -> inspect ())
            .AddDragSource(fun _ -> addDragSource ())
            .RemoveDragSource(fun _ -> removeDragSource ())
            .TryErrors(fun _ -> tryErrors ())
            .Bind()

        // The template is bound now: the container and the drag card exist.
        createLayout ()
        let style = StyleConstants.DefaultComponentDragZIndex
        logEvent ("style constants: base=" + StyleConstants.DefaultComponentBaseZIndex
            + ", drag=" + style + ", maximised=" + StyleConstants.DefaultComponentStackMaximisedZIndex)
        logEvent ("i18n: " + string I18nStrings.IdCount + " messages; first: "
            + I18nStrings.Strings.[0])