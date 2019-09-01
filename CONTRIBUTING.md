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
