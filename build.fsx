#load "tools/includes.fsx"
open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.GoldenLayout")
        .VersionFrom("WebSharper", versionSpec = "(,4.0)")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun f -> f.Net40)

let main =
    bt.WebSharper.Extension("WebSharper.GoldenLayout")
        .SourcesFromProject()
        .Embed([])
        .References(fun r -> [])

let tests =
    bt.WebSharper.SiteletWebsite("WebSharper.GoldenLayout.Tests")
        .SourcesFromProject()
        .Embed([])
        .References(fun r ->
            [
                r.Project(main)
                r.NuGet("WebSharper.Testing").Version("(,4.0)").Reference()
                r.NuGet("WebSharper.UI.Next").Version("(,4.0)").Reference()
            ])

bt.Solution [
    main
    tests

    bt.NuGet.CreatePackage()
        .Configure(fun c ->
            { c with
                Title = Some "WebSharper.GoldenLayout"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.goldenlayout"
                Description = "WebSharper extension for GoldenLayout"
                RequiresLicenseAcceptance = true })
        .Add(main)
]
|> bt.Dispatch
