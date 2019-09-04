# Party: Usage

## Installation

Download `party.exe` from the [latest release](https://github.com/vam-community/vam-party/releases) and put it directly in the Virt-A-Mate directory (next to `VaM.exe`). You'll then need to use a command-line tool (such as powershell or cmd), double-clicking on it won't help you much!

## Commands

Commands can be invoked using `party command-name (arguments...)`. See below for examples; not every option is documented here.

Common options:

- `--vam [vam install folder]` to specify where Virt-A-Mate is installed. You can use this if you download `party.exe` in another folder.

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

If a script contains files that are hosted on an untrusted server (i.e. a server that can track you), if will be flagged as `[untrusted]`. This works by using a whitelist (currently only github).

Options:

- `--no-usage` skips scanning the saves folder, outputs faster but won't tell you if you already have it
- `--warnings` prints warnings found while scanning your saves folder

### `get`

Downloads a script locally. This will also validate hashes to make sure there was no tampering:

    > party get some-package

You can also install a specific version:

    > party get some-package --version 1.0.0

Options:

- `--version` choose a specific version to install (defaults to the latest version)
- `--noop` do not install, just print what will happen when you do

### `upgrade`

Scans for scenes referencing scripts that are available in the registry, and use them instead:

    > party upgrade

Update a specific scene (and all scripts it uses) or script (and all scenes referencing it):

    > party upgrade Saves\Scripts\Some Script.cs

Update a specific package (and all scenes referencing it):

    > party upgrade my-package

Since this can affect lots of files, you might want to make a dry run first, see what the script will do:

    > party get upgrade --noop

If you want the script to download all packages that were found in the registry for you, you can use `--get`:

    > party get upgrade --get

If you want the script to fix all scenes that are referencing a script outside of the Party folder, `--fix`:

    > party get upgrade --fix

To avoid clutter, you can delete unused scripts after upgrading using `--clean`. So for an aggressive update:

    > party get upgrade --get --fix --clean

Options:

- `--all` upgrade everything
- `--get` downloads registered scripts that were not already downloaded
- `--fix` updates scenes referencing scripts that are not yet in the party folder
- `--clean` deletes the source script after scenes have been updated
- `--warnings` show warnings such as broken scenes or missing scripts
- `--noop` prints what the script will do, but won't actually do anything
- `--verbose` prints every change that will be done on every scene

### `status`

Prints the list of all installed scripts, identify the ones that are out of date, and flag scenes that reference scripts from a folder other than the party installation folder.

    > party status
    Analyzing the saves folder and downloading the scripts list from the registry...
    There were 3 errors in the saves folder. Run with --warnings to print them.
    some-script 1.0.0 "Script Name.cs" (referenced by 2 scenes)
    - scene\Some Scene.json
    - scene\Some Other Scene.json
    other-script 3.0.4 "My Script.cslist" (referenced by 0 scenes)

You can also specify a script, a scene or a package name to show if you don't want to list everything in your Saves folder.

    > party status some-package
    > party status "Saves\Scripts\Some Script.cs"
    > party status "Saves\scenes\Some Scene.json"

Options:

- `--scenes` shows every scene actually referencing the script
- `--warnings` prints warnings found while scanning your saves folder
- `--unregistered` prints every script that was found but did not match a package

If you also want to list unregistered scripts (scripts that did not match)

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

Options:

- `--warnings` prints warnings found while scanning your saves folder

### `publish`

Helps publishing a new scene to the registry. First, clone [vam-registry](https://github.com/vam-community/vam-registry), and then you can run:

    > party publish "C:\...\My Script.cs" "C:\...\Some Other Script.cs" --registry "C:\...\vam-registry\v1\index.json"

Note that you can also specify file URLs directly instead if they are already uploaded, as long as the end of the URL ends with the file name (e.g. `https://example.org/.../My%20Script.cs`), this is usually easier. Here is an example with all arguments and a url:

    > party publish "https://github.com/.../My%20Script.cs"--package-name my-package --package-version 1.0.0 --package-author "John Doe" --registry "C:\...\vam-registry\v1\index.json"

You can also generate the JSON for your package directly in the console by omitting the `--registry` option. In this case, it will load registry information from GitHub directly.

If you do not provide a version and package name, party will ask for one. If it's a new package, it will ask you for information such as tags, repository link, etc. Same thing for the list of files, you can let the publish command guide you completely:

    > party publish --registry "C:\...\vam-registry\v1\index.json"

For more details and a walkthrough, see the [instructions on vam-registry](https://github.com/vam-community/vam-registry/blob/master/INSTRUCTIONS.md).

Options:

- `--registry` to specify a cloned registry json to read and write to
- `--package-name` the name of the package (lowercase, underscore, hyphen and numbers only)
- `--package-author` the author name (allows spaces)
- `--package-version` the version of the package to publish, either in the format `0.0.0` or `0.0.0-suffix`.

## Configuration

You can create a `party.settings.json` next to the `party.exe` file to customize it's settings. You can check this [example](https://github.com/vam-community/vam-party/blob/master/Party.CLI/party.settings.json) to see what settings are supported.
