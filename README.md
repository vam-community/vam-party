# Party: A Virt-A-Mate Package Manager

[![Build Status](https://travis-ci.org/vam-community/vam-party.svg?branch=master)](https://travis-ci.org/vam-community/vam-party) [![Coverage Status](https://coveralls.io/repos/github/vam-community/vam-party/badge.svg)](https://coveralls.io/github/vam-community/vam-party)

Under development - please do not use, yet.

Download and keep Virt-A-Mate scripts up to date.

This repository and the maintainers are not affiliated in any way with Virt-A-Mate or its developers.

## Installation

TBD

## Commands

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

TBD

## Contributing

This project uses .NET Core 3 preview 7.

To compile an executable ready for deployment, use:

    > dotnet publish -c Release -r win-x64

And get `party.exe` from `.\bin\Release\netcoreapp3.0\win-x64\publish\`. Yes, it's a single executable with everything in it!

If you make a pull request, make sure to follow the coding style, the `.editconfig` settings, and run tests!

    > dotnet test ./Party.Shared.Tests

## License

[MIT](./LICENSE.md)
