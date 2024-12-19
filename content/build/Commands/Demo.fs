module EasyBuild.Commands.Demo

open Spectre.Console.Cli
open SimpleExec
open EasyBuild.Workspace
open EasyBuild.Tools.Fable
open EasyBuild.Tools.Vite

type DemoSettings() =
    inherit CommandSettings()

    [<CommandOption("-w|--watch")>]
    member val IsWatch = false with get, set

type DemoCommand() =
    inherit Command<DemoSettings>()
    interface ICommandLimiter<DemoSettings>

    override __.Execute(context, settings) =

        Command.Run("pnpm", "install", workingDirectory = Workspace.``.``)

        if settings.IsWatch then
            Async.Parallel
                [
                    Fable.watch (projFileOrDir = Workspace.demo.``.``, verbose = true)
                    |> Async.AwaitTask

                    Vite.watch (workingDirectory = Workspace.demo.``.``) |> Async.AwaitTask
                ]
            |> Async.RunSynchronously
            |> ignore

        else

            Fable.build (projFileOrDir = Workspace.demo.``.``)
            Vite.build (workingDirectory = Workspace.demo.``.``)

        0
