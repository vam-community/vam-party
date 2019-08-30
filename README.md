# Party: A Virt-A-Mate Package Manager

[![build status](https://travis-ci.org/vam-community/vam-party.svg?branch=master)](https://travis-ci.org/vam-community/vam-party) [![codecov](https://codecov.io/gh/vam-community/vam-party/branch/master/graph/badge.svg)](https://codecov.io/gh/vam-community/vam-party) [![lgtm](https://img.shields.io/lgtm/alerts/g/vam-community/vam-party.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/vam-community/vam-party/alerts/) [![codacy](https://api.codacy.com/project/badge/Grade/1ac73e5240674a9ca5027e35a6b942bb)](https://www.codacy.com/app/acidbubbles/vam-party?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=vam-community/vam-party&amp;utm_campaign=Badge_Grade)

Under development - please do not use, yet.

Download and keep Virt-A-Mate scripts up to date.

This repository and the maintainers are not affiliated in any way with Virt-A-Mate or its developers.

## Installation

Download `party.exe` from the [latest release](https://github.com/vam-community/vam-party/releases) and put it directly in the Virt-A-Mate directory (next to `VaM.exe`). You'll then need to use a command-line tool (such as powershell or cmd), double-clicking on it won't help you much!

## Commands

Commands can be invoked using `party command-name (arguments...)`. See below for examples; not every option is documented here.

### `help`

You can show commands using `party -h`, or help on a specific command using `party command-name -h`.

### `search`

You can list packages from the registry:

    > party search
    - some-package 1.0.3 by Some User (used in 4 scenes)

You can also search by keyword:

    > party search some-keyword

If you want the command to run faster, you can opt-out of the saves folder analysis:

    > party search some-keyword --no-usage

If a script contains files that are hosted on an untrusted server (i.e. a server that can track you), if will be flagged as `[untrusted]`.

### `get`

Downloads a script locally. This will also validate hashes to make sure there was no tampering:

    > party get some-package

You can also install a specific version:

    > party get some-package --version 1.0.0

### `fix`

Scans for scenes referencing scripts that are available in the registry, and use them instead.

    > party fix

Since this can affect lots of files, you might want to make a dry run first, see what the script will do:

    > party get fix --noop

If you want the script to download matching packages, you can use `--get`:

    > party get fix --get

### `publish`

Helps publishing a new scene to the registry. First, clone [vam-registry](https://github.com/vam-community/vam-registry), and then you can run:

    > party publish "C:\...\My Script.cs" --package-name my-package --package-version 1.0.0 --registry "C:\...\vam-registry\v1\index.json"

You can also generate the JSON for your package directly in the console by omitting the `--registry` option. In this case, it will load registry information from GitHub directly.

If you do not provide a version and package name, party will ask for one. If it's a new package, it will ask you for information such as tags, repository link, etc.

### `status`

Prints the list of all installed scripts, identify the ones that are out of date, and flag scenes that reference scripts from a folder other than the party installation folder.

    > party status
    Analyzing the saves folder and downloading the scripts list from the registry...
    There were 3 errors in the saves folder. Run with --warnings to print them.
    some-script 1.0.0 "Script Name.cs" (referenced by 2 scenes)
    - scene\Some Scene.json
    - scene\Some Other Scene.json
    other-script 3.0.4 "My Script.cslist" (referenced by 0 scenes)

### `show`

Shows more information about a specific package:

    > party show some-package
    Package some-package, by Some User
    Description: This package does cool stuff
    Tags: tag1, tag2
    Repository: https://github.com/...
    Homepage: https://www.reddit.com/r/VAMscenes/...
    Files:
    - My Script.cs

## Configuration

You can create a `party.settings.json` next to the `party.exe` file to customize it's settings. You can check this [example](https://github.com/vam-community/vam-party/blob/master/Party.CLI/party.settings.json) to see what settings are supported.

## Contributing

This project uses .NET Core 3 preview 7.

To compile an executable ready for deployment, use:

    > dotnet publish -c Release -r win-x64

And get `party.exe` from `.\bin\Release\netcoreapp3.0\win-x64\publish\`. Yes, it's a single executable with everything in it!

If you make a pull request, make sure to follow the coding style, the `.editorconfig` settings, and run tests!

    > dotnet test ./Party.Shared.Tests

## License

[MIT](./LICENSE.md)
