module EasyBuild.Commands.Release

open Spectre.Console.Cli
open EasyBuild.Workspace
open EasyBuild.Tools.ChangelogGen
open EasyBuild.Tools.DotNet
open EasyBuild.Tools.Git
open System.IO

type ReleaseSettings() =
    inherit CommandSettings()

type ReleaseCommand() =
    inherit Command<ReleaseSettings>()
    interface ICommandLimiter<ReleaseSettings>

    override __.Execute(context, settings) =
        let newVersion =
            ChangelogGen.run (
                Workspace.``CHANGELOG.md``,
                forwardArguments = (context.Remaining.Raw |> Seq.toList)
            )

        let nupkgPath =
            DotNet.pack (projectFile = FileInfo Workspace.``Glutinum.Template.proj``)

        // DotNet.nugetPush nupkgPath

        // Git.addAll ()
        // Git.commitRelease newVersion
        // Git.push ()

        0
