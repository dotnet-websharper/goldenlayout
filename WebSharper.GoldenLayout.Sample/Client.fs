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

    let a = 0

    type State = {Text: string}

    [<SPAEntryPoint>]
    let Main () =

        let component_ id = 
            ItemFactory.CreateComponent(
                Component(
                    componentName = "example",
                    ComponentState = ({Text = "This is the content of Component " + string id} :> obj)
                ),
                Item(Title = "My " + string id + ". component")
            )
        Console.Log component_
        let gl =
            GoldenLayout(
                Layout(
                    Content = [|
                        ItemFactory.CreateRow(
                            Item(
                                Content = (
                                    seq { 1..3 }
                                    |> Seq.map (fun id -> component_ id)
                                    |> Array.ofSeq
                                )
                            )
                        )
                    |]
                ),
                JQuery.Of "#gl-cont"
            )
        gl.RegisterComponent(
            "example", 
            fun (container, s) ->
                let state = s :?> State
                Console.Log state
                container.GetElement().Text state.Text
                |> ignore
        )
        gl.Init()
        Doc.Empty
        |> Doc.RunAppendById "main"
