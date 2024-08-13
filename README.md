# Glutinum.Template

[![](https://img.shields.io/badge/Sponsors-EA4AAA?style=for-the-badge)](https://mangelmaxime.github.io/sponsors/)

Glutinum.Template is an opinionated template for creating Fable bindings/libraries.

> [!NOTE]
> If you are looking for a template to create a standard F# project, you should look at [MiniScaffold](https://github.com/TheAngryByrd/MiniScaffold).

It features:

- Project configuration with validation for common mistakes

    For example, it will check that you have `FablePackageType` set so the package will be listed in the [Fable package registry](https://fable.io/packages/).

- Enforce commit message conventions via [EasyBuild.CommitLinter](https://github.com/easybuild-org/EasyBuild.CommitLinter)
- Automatic versioning and changelog generation based on the git history
- Enforce code style with [Fantomas](https://fsprojects.github.io/fantomas/)
    - The code is automatically formatted on commit
- Easy release thanks to a `build` orchestror

## Usage

To use this template, you can run the following command:

```bash
dotnet new -i "Glutinum.Template::*"
```

Then you can create a new project with:

```bash
dotnet new glutinum -n MyProject
```