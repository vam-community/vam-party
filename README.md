# Party: A Virt-A-Mate Package Manager

[![build status](https://travis-ci.org/vam-community/vam-party.svg?branch=master)](https://travis-ci.org/vam-community/vam-party) [![codecov](https://codecov.io/gh/vam-community/vam-party/branch/master/graph/badge.svg)](https://codecov.io/gh/vam-community/vam-party) [![lgtm](https://img.shields.io/lgtm/alerts/g/vam-community/vam-party.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/vam-community/vam-party/alerts/) [![codacy](https://api.codacy.com/project/badge/Grade/1ac73e5240674a9ca5027e35a6b942bb)](https://www.codacy.com/app/acidbubbles/vam-party?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=vam-community/vam-party&amp;utm_campaign=Badge_Grade)

Under development - please do not use, yet.

Download and keep Virt-A-Mate scripts up to date.

This repository and the maintainers are not affiliated in any way with Virt-A-Mate or its developers.

## Features

- Search scripts by keywords
- Download scripts
- Keep multiple versions of scripts side by side
- Find out which scenes reference which plugin
- Match scripts to an online package
- Find available updates for scripts
- Publish scripts

## Usage

Simply download `party.exe` from the [releases](https://github.com/vam-community/vam-party/releases) and put it in your `VaM` directory.

Find packages:

    > party search

Download a package:

    > party get improved-pov

Upgrade your scenes with updated versions of your scripts:

    > party ugrade

Publish your own scripts:

    > party publish Saves\Scripts\My Script.cs

Find more information and more commands in the [commands documentation](USAGE.md).

## Contributing

Pull requests welcome! Find more information on the architecture, how to compile and how to contribute in the [contributing guide](CONTRIBUTING.md)

## License

[MIT](./LICENSE.md)
