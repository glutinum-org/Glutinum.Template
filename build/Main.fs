module EasyBuild.Main

open Spectre.Console.Cli
open EasyBuild.Commands.Demo
open EasyBuild.Commands.Release
open EasyBuild.Commands.Publish
open SimpleExec

[<EntryPoint>]
let main args =

    Command.Run("dotnet", "husky install")

    let app = CommandApp()

    app.Configure(fun config ->
        config.Settings.ApplicationName <- "./build.sh"

        config
            .AddCommand<DemoCommand>("demo")
            .WithDescription("Command related to working on the demo project.")
        |> ignore

        config
            .AddCommand<ReleaseCommand>("release")
            .WithDescription(
                "Package a new version of the library and publish it to NuGet. This also updates the demo."
            )
        |> ignore

        config
            .AddCommand<PublishCommand>("publish")
            .WithDescription("Publish the demo to GitHub Pages.")
        |> ignore

    )

    app.Run(args)
