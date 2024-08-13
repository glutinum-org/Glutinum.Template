# Manual for the template

If you are looking for documentation about the project, you should refer to the [README.md](./README.md) file.

## Architecture

- `src/` contains the source code of your binding
- `demo/` contains a demo application that you can use to test your binding. It can also be publish on GitHub pages to have an interactive demo.
- `build/` contains the build process for this repository.

    It allows you to:

    - Run the demo locally
    - Build and publish the demo on GitHub pages
    - Make a release of your binding on NuGet (it will also publish the demo on GitHub pages)

## Configuration files

- `Directory.UserConfig.props` is the file where you are expected to put configuration unique to your project.

- `Directory.Packages.props` contains the list of NuGet packages that are used in this project. You can add or remove packages from this file.

    The easiest way to add a package is to use the `dotnet add package` command. It will automatically add the package to all files that need it.

    ```bash
    # For the main project
    dotnet add src package Fable.Core
    # For the demo project
    dotnet add demo package Fable.Core
    ```

- `Directory.Build.props` contains a set of default rules that should works for any project. Only modify it if you know what you are doing. ⚠️

The template pre-configures a lot of things for you via the file `Directory.Build.props`, you should not need to modify it. If you do so, please make sure you know what you are doing.

## Commit convention

This repository is set to generate release and changelog based on the commit history.

To make sure, you follow the convention, it is configured to use [EasyBuild.CommitLinter](https://github.com/easybuild-org/EasyBuild.CommitLinter) to validate the commit messages.

## Run the demo

To run the demo, you can use the following command:

```bash
./build.sh demo --watch
```

## Make a release

You need to have a NuGet API saved in `NUGET_KEY` environment variable, you can get it from https://www.nuget.org/account/apikeys.

To make a release, you can use the following command:

```bash
./build.sh release

# Check the help for more information
./build.sh release --help
```
