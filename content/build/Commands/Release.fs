module EasyBuild.Commands.Release

open Spectre.Console.Cli
open SimpleExec
open LibGit2Sharp
open EasyBuild.Workspace
open System.Linq
open System.Text.RegularExpressions
open System
open System.ComponentModel
open System.IO
open BlackFox.CommandLine
open EasyBuild.Utils.Dotnet
open Semver
open EasyBuild.Commands.Demo
open EasyBuild.Commands.Publish
open EasyBuild.CommitParser
open EasyBuild.CommitParser.Types

let capitalizeFirstLetter (text: string) =
    (string text.[0]).ToUpper() + text.[1..]

type ReleaseSettings() =
    inherit CommandSettings()

    [<CommandOption("--major")>]
    [<Description("Bump the major version (X.0.0)")>]
    member val BumpMajor = false with get, set

    [<CommandOption("--minor")>]
    [<Description("Bump the minor version (0.X.0)")>]
    member val BumpMinor = false with get, set

    [<CommandOption("--patch")>]
    [<Description("Bump the patch version (0.0.X)")>]
    member val BumpPatch = false with get, set

    [<CommandOption("--force-version")>]
    [<Description("Force a specific version")>]
    member val ForceVersion = None with get, set

type ReleaseCommand() =
    inherit Command<ReleaseSettings>()
    interface ICommandLimiter<ReleaseSettings>

    override __.Execute(context, settings) =

        // TODO: Replace libgit2sharp with using CLI directly
        // libgit2sharp seems all nice at first, but I find the API to be a bit cumbersome
        // when manipulating the repository for (commit, stage, etc.)
        // It also doesn't support SSH
        use repository = new Repository(Workspace.``.``)

        if repository.Head.FriendlyName <> "main" then
            failwith "You must be on the main branch to publish"

        if repository.RetrieveStatus().IsDirty then
            failwith "You have uncommitted changes"

        let changelogContent =
            File.ReadAllText(Workspace.``CHANGELOG.md``).Replace("\r\n", "\n").Split('\n')

        let changelogConfigSection =
            changelogContent
            |> Array.skipWhile (fun line -> "<!-- EasyBuild: START -->" <> line)
            |> Array.takeWhile (fun line -> "<!-- EasyBuild: END -->" <> line)

        let lastReleasedCommit =
            let regex = Regex("^<!-- last_commit_released:\s(?'hash'\w*) -->$")

            changelogConfigSection
            |> Array.tryPick (fun line ->
                let m = regex.Match(line)

                if m.Success then
                    Some m.Groups.["hash"].Value
                else
                    None
            )

        let commitFilter = CommitFilter()
        // If we found a last released commit, use it as the starting point
        // Otherwise, not providing a starting point seems to get all commits
        if lastReleasedCommit.IsSome then
            commitFilter.ExcludeReachableFrom <- lastReleasedCommit.Value

        let commits = repository.Commits.QueryBy(commitFilter).ToList()

        let releaseCommits =
            commits
            // Parse the commit to get the commit information
            |> Seq.choose (fun commit ->
                match
                    Parser.tryParseCommitMessage CommitParserConfig.Default commit.Message,
                    commit
                with
                | Ok semanticCommit, commit ->
                    Some
                        {|
                            OriginalCommit = commit
                            SemanticCommit = semanticCommit
                        |}
                | Error _, _ -> None
            )
            // Only include commits that are feat or fix
            |> Seq.filter (fun commits ->
                match commits.SemanticCommit.Type with
                | "feat"
                | "fix" -> true
                | _ -> false
            )

        if Seq.isEmpty releaseCommits then
            printfn "No commits found to make a release"
            0
        else

            DemoCommand().Execute(context, DemoSettings()) |> ignore

            let lastChangelogVersion = Changelog.tryGetLastVersion Workspace.``CHANGELOG.md``

            // Should user bump version take priority over commits infered version bump?
            // Should we make the user bump version mutually exclusive?

            let shouldBumpMajor =
                settings.BumpMajor
                || releaseCommits |> Seq.exists (fun commit -> commit.SemanticCommit.BreakingChange)

            let shouldBumpMinor =
                settings.BumpMinor
                || releaseCommits |> Seq.exists (fun commit -> commit.SemanticCommit.Type = "feat")

            let shouldBumpPatch =
                settings.BumpPatch
                || releaseCommits |> Seq.exists (fun commit -> commit.SemanticCommit.Type = "fix")

            let refVersion =
                match lastChangelogVersion with
                | Some version -> version.Version
                | None -> Semver.SemVersion(0, 0, 0)

            let newVersion =
                if shouldBumpMajor then
                    refVersion.WithMajor(refVersion.Major + 1).WithMinor(0).WithPatch(0)
                elif shouldBumpMinor then
                    refVersion.WithMinor(refVersion.Minor + 1).WithPatch(0)
                elif shouldBumpPatch then
                    refVersion.WithPatch(refVersion.Patch + 1)
                else
                    failwith "No version bump required"

            let newVersionLines = ResizeArray()

            let inline appendLine (line: string) = newVersionLines.Add(line)

            let inline newLine () = newVersionLines.Add("")

            appendLine ($"## {newVersion}")
            newLine ()

            releaseCommits
            |> Seq.groupBy (fun commit -> commit.SemanticCommit.Type)
            |> Seq.iter (fun (commitType, commitGroup) ->
                match commitType with
                | "feat" -> appendLine "### 🚀 Features"
                | "fix" -> appendLine "### 🐞 Bug Fixes"
                // Other types are not included in the changelog
                | _ -> ()

                newLine ()

                for commit in commitGroup do
                    let githubCommitUrl sha =
                        $"https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/%s{sha}"

                    let shortSha = commit.OriginalCommit.Sha.Substring(0, 7)
                    let commitUrl = githubCommitUrl commit.OriginalCommit.Sha

                    let description = capitalizeFirstLetter commit.SemanticCommit.Description

                    $"- %s{description} ([%s{shortSha}](%s{commitUrl}))" |> appendLine
            )

            newLine ()

            // TODO: Add contributors list
            // TODO: Add breaking changes list

            let rec removeConsecutiveEmptyLines
                (previousLineWasBlank: bool)
                (result: string list)
                (lines: string list)
                =
                match lines with
                | [] -> result
                | line :: rest ->
                    // printfn $"%A{String.IsNullOrWhiteSpace(line)}"
                    if previousLineWasBlank && String.IsNullOrWhiteSpace(line) then
                        removeConsecutiveEmptyLines true result rest
                    else
                        removeConsecutiveEmptyLines
                            (String.IsNullOrWhiteSpace(line))
                            (result @ [ line ])
                            rest

            let newChangelogContent =
                [
                    // Add title and description of the original changelog
                    yield!
                        changelogContent
                        |> Seq.takeWhile (fun line -> "<!-- EasyBuild: START -->" <> line)

                    // Ad EasyBuild metadata
                    "<!-- EasyBuild: START -->"
                    $"<!-- last_commit_released: {commits[0].Sha} -->"
                    "<!-- EasyBuild: END -->"
                    ""

                    // New version
                    yield! newVersionLines

                    // Add the rest of the changelog
                    yield!
                        changelogContent |> Seq.skipWhile (fun line -> not (line.StartsWith("##")))
                ]
                |> removeConsecutiveEmptyLines false []
                |> String.concat "\n"

            File.WriteAllText(Workspace.``CHANGELOG.md``, newChangelogContent)

            let escapedPackageReleasesNotes =
                newVersionLines
                |> Seq.toList
                |> removeConsecutiveEmptyLines false []
                |> String.concat "\n"
                // Escape quotes and commas
                |> fun text -> text.Replace("\"", "\\\\\\\"").Replace(",", "%2c")

            // Clean up the src/bin folder
            if Directory.Exists VirtualWorkspace.src.bin.``.`` then
                Directory.Delete(VirtualWorkspace.src.bin.``.``, true)

            let struct (standardOutput, _) =
                Command.ReadAsync(
                    "dotnet",
                    CmdLine.empty
                    |> CmdLine.appendRaw "pack"
                    |> CmdLine.appendRaw Workspace.src.``GlueTemplate.fsproj``
                    |> CmdLine.appendRaw "-c Release"
                    |> CmdLine.appendRaw $"-p:PackageVersion=\"%s{newVersion.ToString()}\""
                    |> CmdLine.appendRaw
                        $"-p:PackageReleaseNotes=\"%s{escapedPackageReleasesNotes}\""
                    |> CmdLine.toString
                )
                |> Async.AwaitTask
                |> Async.RunSynchronously

            let m =
                Regex.Match(
                    standardOutput,
                    "Successfully created package '(?'nupkgPath'.*\.nupkg)'"
                )

            if not m.Success then
                failwith $"Failed to find nupkg path in output:\n{standardOutput}"

            let nugetKey = Environment.GetEnvironmentVariable("NUGET_KEY")

            if nugetKey = null then
                failwith "Please set the NUGET_KEY environment variable, you can get it from https://www.nuget.org/account/apikeys"

            Nuget.push (
                m.Groups.["nupkgPath"].Value,
                nugetKey
            )

            Command.Run("git", "add .")

            Command.Run(
                "git",
                CmdLine.empty
                |> CmdLine.appendRaw "commit"
                |> CmdLine.appendPrefix "-m" $"chore: release {newVersion.ToString()}"
                |> CmdLine.toString
            )

            Command.Run("git", "push")

            PublishCommand().Execute(context, PublishSettings(SkipBuild = true)) |> ignore

            0
