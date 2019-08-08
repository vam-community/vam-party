# Party: A Virt-A-Mate Package Manager

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

TBD

## License

[MIT](./LICENSE.md)
