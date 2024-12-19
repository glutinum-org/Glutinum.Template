module EasyBuild.Commands.Release

open Spectre.Console.Cli
open EasyBuild.Workspace
open EasyBuild.Commands.Demo
open EasyBuild.Commands.Publish
open EasyBuild.Tools.ChangelogGen
open EasyBuild.Tools.DotNet
open EasyBuild.Tools.Git

type ReleaseSettings() =
    inherit CommandSettings()

type ReleaseCommand() =
    inherit Command<ReleaseSettings>()
    interface ICommandLimiter<ReleaseSettings>

    override __.Execute(context, _settings) =
        DemoCommand().Execute(context, DemoSettings()) |> ignore

        let newVersion =
            ChangelogGen.run(
                Workspace.``CHANGELOG.md``,
                forwardArguments = (context.Remaining.Raw |> Seq.toList)
            )

        let nupkgPath = DotNet.pack Workspace.src.``.``

        DotNet.nugetPush nupkgPath

        Git.addAll ()
        Git.commitRelease newVersion
        Git.push ()

        PublishCommand().Execute(context, PublishSettings(SkipBuild = true)) |> ignore

        0
