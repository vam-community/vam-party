# Party API (Party.Shared.dll)

You can use the NuGet package (not published yet) or download the zip file directly, and reference `Party.Shared.dll` to access all functionality of Party. Here's an example to get started:

```csharp
var config = PartyConfigurationFactory.Create(vamFolder);
var controllerFactory = new PartyControllerFactory();
var checks = true; // Ensures operations are done in allowed folders
var controller = controllerFactory.Create(config, checks);
controller.HealthCheck();
SavesMap saves;
Registry registry;
using (var reporter = new YourOwnProgressReporter<ScanLocalFilesProgress>(StartProgress, ReportProgress, CompleteProgress))
{
  (saves, registry) = await controller.ScanLocalFilesAndAcquireRegistryAsync(null, filter, reporter).ConfigureAwait(false);
}
var matches = controller.MatchLocalFilesToRegistry(saves, registry);
// matches.HashMatches contains all local files matches to the remote registry, and saves contains the relationships between scenes and save files
```

The `YourProgressReporter` would be a class that decides how to display progress (a progress bar for example). See [ProgressReporter.cs](Party.Shared/Utils/ProgressReporter.cs) for an example.
