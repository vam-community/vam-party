# Party: A Virt-A-Mate Package Manager

[![Build Status](https://travis-ci.org/vam-community/vam-party.svg?branch=master)](https://travis-ci.org/vam-community/vam-party) [![codecov](https://codecov.io/gh/vam-community/vam-party/branch/master/graph/badge.svg)](https://codecov.io/gh/vam-community/vam-party) [![lgtm](https://img.shields.io/lgtm/alerts/g/vam-community/vam-party)](https://lgtm.com/projects/g/vam-community/vam-party/) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/1ac73e5240674a9ca5027e35a6b942bb)](https://www.codacy.com/app/acidbubbles/vam-party?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=vam-community/vam-party&amp;utm_campaign=Badge_Grade)

Under development - please do not use, yet.

Download and keep Virt-A-Mate scripts up to date.

This repository and the maintainers are not affiliated in any way with Virt-A-Mate or its developers.

## Installation

Download `party.exe` from the [latest release](https://github.com/vam-community/vam-party/releases) and put it directly in the Virt-A-Mate directory (next to `VaM.exe`). You'll then need to use a command-line tool (such as powershell or cmd), double-clicking on it won't help you much!

## Commands

Commands can be invoked using `party command-name (arguments...)`. See below for examples.

### `help`

You can show commands using `party help`, or help on a specific command using `party help command-name`.

### `search`

Search lists all packages from the registry.

    > party search
    - some package by some user (v1.0.0)
    - ...

### `scripts`

Lists all scripts in your VaM installation.

    > party scripts
    - SomeScript.cs (2 copies used by 3 scenes)
    - ...

### `scenes`

Lists all scenes in your VaM installation.

    > party scenes
    - scene\Anonymous\Some Scene.json

### `package`

Prepares the output JSON for publishing on the registry.

    > party package Scripts\Anonymous\My Plugin.cs
    { ... }

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
