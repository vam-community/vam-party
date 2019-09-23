# Party: A Virt-A-Mate Package Manager

[![build status](https://travis-ci.org/vam-community/vam-party.svg?branch=master)](https://travis-ci.org/vam-community/vam-party) [![codecov](https://codecov.io/gh/vam-community/vam-party/branch/master/graph/badge.svg)](https://codecov.io/gh/vam-community/vam-party) [![lgtm](https://img.shields.io/lgtm/alerts/g/vam-community/vam-party.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/vam-community/vam-party/alerts/) [![codacy](https://api.codacy.com/project/badge/Grade/1ac73e5240674a9ca5027e35a6b942bb)](https://www.codacy.com/app/acidbubbles/vam-party) ![Libraries.io dependency status for GitHub repo](https://img.shields.io/librariesio/github/vam-community/vam-party)

**This is an alpha version, it works but is still under heavy development. Backup your saves folder first!**

Find, download and keep Virt-A-Mate scripts up to date. Uses the [Virt-A-Mate registry](https://github.com/vam-community/vam-registry).

This repository and the maintainers are not affiliated in any way with Virt-A-Mate or its developers.

## Installation

Simply download `party.zip` from the [releases](https://github.com/vam-community/vam-party/releases) and put it in your `VaM` directory. You should end up with `(your vam install directory)/party/party.exe`.

## Examples

Find packages:

    > party search

Download a package:

    > party get scripts/improved-pov

Find out which scripts you're already using, and if updates are available:

    > party status

Upgrade your scenes and scripts with up-to-date versions from creators:

    > party upgrade Saves\scenes\My Scene.json
    > party upgrade improved-pov

Publish your own scripts:

    > party publish Custom\Scripts\My Script.cs

## Usage

All commands and their parameters can be found in the [commands documentation](USAGE.md).

## API

You can integrate with Party in your own software using the .Net [API](API.md).

## Contributing

Pull requests welcome! Find more information on the architecture, how to compile and how to contribute in the [contributing guide](CONTRIBUTING.md)

## License

[MIT](./LICENSE.md)
