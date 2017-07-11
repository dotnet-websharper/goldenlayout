namespace WebSharper.GoldenLayout.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Templating
open WebSharper.GoldenLayout

[<Require(typeof<GoldenLayout.Resources.DarkTheme>)>]
[<JavaScript>]
module Client =

    let a = 0

    type State = {Text: string}

    [<SPAEntryPoint>]
    let Main () =
        let gl =
            GoldenLayout(
                Layout(
                    Content = [|
                        ItemFactory.CreateRow(
                            Item(
                                Content =
                                    [|
                                        (ItemFactory.CreateComponent(
                                            Component(
                                                componentName = "[1] Component",
                                                ComponentState = ({Text = "This is the content of [1] Component"} :> obj)
                                            ),
                                            Item()))
                                    |]
                            )
                        )
                    |]
                )
            )
        Doc.Empty
        |> Doc.RunById "main"
