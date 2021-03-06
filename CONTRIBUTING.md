# Party: Contributing

## Compiling

You need [.NET Core 3 Preview 8](https://dotnet.microsoft.com/download/dotnet-core) or more recent on Windows.

Run `dotnet run --project .\Party.CLI -- arguments` to test locally.

Run `dotnet publish Party.CLI -c Release -r win-x64` to compile a bundled assembly into `.\bin\Release\netcoreapp3.0\win-x64\publish\`.

## Tests

Run `dotnet test` to run all tests.

## Architecture

The `Party.Shared` assembly is designed to be used by a command line interface (cli) as well as a graphical interface (gui).

The `Party.CLI` gets compiled into `party.exe`, and using [CoreRT](https://github.com/dotnet/corert) we generate a bundled `party.exe` that doesn't depend on anything.

## Issues

If you have a problem, please [create an issue](https://github.com/vam-community/vam-party/issues/new) and include the version of party you used, how to reproduce the problem, the error stack trace and if possible, the full output of the console as well as the involved files.

## Pull requests

Pull requests are welcome, but they _must_ follow the style of the surrounding code. Check out the `.editorconfig`! If possible, include new or updated unit tests.

## Code of conduct

- Stay calm, be polite. Remember this is done on our free time, for free.
- No pornographic or explicit discussions here. Let's keep this about the features and the code.
- Be inclusive. No discriminatory comments will be tolerated.

## Feature ideas

If you want to take on an idea, please [create an issue](https://github.com/vam-community/vam-party/issues/new) explaining how you intend on implementing it, so everyone can discuss it and the maintainers can approve your idea, so no one is disappointed!

- A new `party repair` command that finds all missing scripts in a scene and replaces them with a reference to the installed one, one already in the Saves folder, or downloads.
- Group by script, version, file or scene in `party status`. The default of grouping by script is not that useful.
- In `party upgrade` and `party status`, allow specifying a folder (e.g. everything in `Saves/scenes/My Scene`).
- Show scene statistics in `party show` such as how many atoms by types, how many scripts, etc.
- Show script statistics in `party show` such as how many lines, and the hash
- Colored output and output cleanup in `party show`
- Check for party updates at launch
- Happy paths in tests for all commands and controller methods
- Cache results (based on last modified date, i.e. a simple json file with the last results, remember to clear old entries on reload)
- The saves/registry match should simply return a table with a match type field, not three lists
- Review all controller namings, and move as much logic as possible out of commands (worst case provide a controller that handles steps)
- Make the folder structure a setting (e.g. script can be `[author]/[name]/[version]` but can be configured to be `[name]/[version]`)
- Revert a plugin, i.e. set a scene's plugin reference to a specific version (e.g. `party use some/scene.json some-plugin@1.0.0`)
- Enable System.CommandLine autocomplete, query the registry to get the available package names
- When scanning scenes, extract information about the scene
- Allow creating scene packages (i.e. make hash of scenes too, or bundle version numbers inside instead using a scene script)
- Do not assume "scripts" when getting package. If a package match, download it.
