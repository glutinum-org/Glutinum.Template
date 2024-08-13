module EasyBuild.Main

open Spectre.Console.Cli
open EasyBuild.Commands.Release
open SimpleExec
open System.Runtime.InteropServices

[<EntryPoint>]
let main args =

    let app = CommandApp()

    app.Configure(fun config ->
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            config.Settings.ApplicationName <- "./build.cmd"
        else
            config.Settings.ApplicationName <- "./build.sh"

        config
            .AddCommand<ReleaseCommand>("release")
            .WithDescription(
                "Package a new version of the library and publish it to NuGet. This also updates the demo."
            )
        |> ignore

    )

    app.Run(args)
