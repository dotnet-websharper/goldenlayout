namespace WebSharper.GoldenLayout.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Templating
open WebSharper.GoldenLayout

[<Require(typeof<GoldenLayout.Resources.BaseCss>)>]
[<Require(typeof<GoldenLayout.Resources.LightTheme>)>]
[<JavaScript>]
module Client =

    type State = {Text: string}
    type IndexTemplate = Template<"index.html", ClientLoad.FromDocument>

    [<SPAEntryPoint>]
    let Main () =

        let tabName = Var.Create "filename"

        let component_ id = 
            ItemFactory.CreateComponent(
                Component(
                    componentName = "example",
                    ComponentState = ({Text = "This is the content of Component " + string id} :> obj)
                ),
                Item(
                    Title = "My " + string id + ". component",
                    IsClosable = true
                )
            )
        Console.Log component_
        let gl =
            GoldenLayout(
                Layout(
                    Content = [|
                        ItemFactory.CreateStack(
                            Stack(),
                            Item(
                                Content = (
                                    seq { 1..3 }
                                    |> Seq.map (fun id -> component_ id)
                                    |> Array.ofSeq
                                ),
                                Id = Union1Of2 "main-window",
                                IsClosable = false
                            )
                        )
                    |],
                    Settings = LayoutSettings(
                        ShowCloseIcon = false,
                        ShowMaximiseIcon = false,
                        ShowPopoutIcon = false
                    )
                ),
                JQuery.Of "#gl-container"
            )
        gl.RegisterComponent(
            "example", 
            fun (container, s) ->
                let state = s :?> State
                (h2 [text state.Text]).Html
                |> container.GetElement().Html
                |> ignore
        )
        gl.Init()
        //gl.Root.GetItemsById("main-window").[0]
        IndexTemplate.AddTemplate()
            .TabName(tabName)
            .TabCreate(fun () ->
                let component_ =
                    ItemFactory.CreateComponent(
                        Component(
                            componentName = "example",
                            ComponentState = {Text = tabName.Value + ": I was just created dynamically!"}
                        ),
                        Item(
                            Title = tabName.Value
                        )
                    )
                gl.Root.GetItemsById("main-window").[0].AddChild(component_)
                Console.Log "length:"
                Console.Log (gl.Root.GetItemsById("main-window").Length)
            ).LogButton(fun () -> Console.Log gl)
            .Doc()
            
        |> Doc.RunById "menu"
