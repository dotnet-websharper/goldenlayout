namespace WebSharper.GoldenLayout.Extension

open WebSharper
open WebSharper.InterfaceGenerator

module Definition =
    let LayoutConfigClass = "LayoutConfig"

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
