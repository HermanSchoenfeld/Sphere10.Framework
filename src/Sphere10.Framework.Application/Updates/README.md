# Product updates

The updater that owns a product's update through one channel, the release server's contract, and the background checker that asks the updater after start-up and at a regular interval.

A `ProductUpdaterBase` owns the whole update of a product. `CheckForUpdateAsync` answers `ProductUpdateCheckResult.NoUpdate` or `ProductUpdateCheckResult.Available(update)`, and `InstallAsync(update, ownerWindow, cancellationToken)` installs an update it found. Each calls a protected abstract hook, `FetchUpdateAsync` and `InstallUpdateAsync`, which a channel implements. The template guards its arguments, reads a missing answer as no update, admits one installation at a time, logs and rethrows a failed installation (a cancelled one is neither logged nor reported) and raises the hook's progress through `InstallProgressChanged` as a `ProductUpdateProgress` (a `ProductUpdateStage` with the bytes downloaded and to download). An installation answers a `ProductUpdateInstallResult`: `ExitRequired` when an installer was started and the application must exit for it, `Completed` when the channel installed the update and restarts the application itself, and `Declined` when the user declined in the channel's own prompt.

`ProductUpdate` is what every channel shows: the `Version`, the `ReleaseNotes` when the channel publishes them (null otherwise) and `IsMandatory`. A channel derives from it for what its installation needs.

The release server is one channel. The application supplies an `IProductUpdateService` that asks its server what this installation can upgrade to and offers a `ReleaseServerProductUpdate`, which adds the installer's download address, file name, length, SHA-256 hash and arguments. The service decides whether an update is offered, for example by checking the installation's license, and the Framework knows nothing about the server. The Application module registers `NoOpProductUpdateService`, which never finds an update, unless the host has registered its own service, and registers `ReleaseServerProductUpdater` as the `ProductUpdaterBase` singleton unless the host, a platform or a distribution's module registered one first. That updater refuses an offer without its installer's address as a failed check. It cannot install, because downloading and running an installer is the platform's: the WinForms library's `WindowsReleaseServerProductUpdater` derives from it and installs, and `UseProductUpdates` registers it. A Windows Store distribution adds the Windows Store library's module, which replaces the updater with `WindowsStoreProductUpdater` (see [its guide](../../Sphere10.Framework.Windows.Store/README.md)).

Checking is opt-in. `UseProductUpdateChecks` on the framework builder, or `AddProductUpdateChecks` on a service collection, registers one `ProductUpdateChecker` over the registered updater, with a process initializer that starts it and a process finalizer that stops it. Starting only schedules a repeating job on the checker's own `Scheduler`, so start-up never waits for a check. The first check runs `ProductUpdateCheckOptions.InitialDelay` after start (30 seconds by default) and each later check runs `Interval` after the previous one ended (one hour by default). A failed check is logged and the schedule continues. `StopAsync` ends the schedule, cancels the check in flight through the token passed to the updater and completes when that check has ended, so a channel must honour its token.

`ProductUpdateChecker.UpdateAvailable` is raised on a worker thread, once for each version during the checker's lifetime. A handler returns promptly and hands any presentation to its UI thread, because the check does not end until every handler has returned. A handler that throws leaves the version unannounced, and the next check announces it again. An observer that accepted an update but could not present it, for example because another notice was open, calls `OfferAgain(update)`, and the next check that finds that version announces it again. `CheckAsync` runs a check on demand, joins a check that is already running and completes with the updater's answer. `AvailableUpdate` holds the update found by the most recent completed check.

`ReleaseServerProductUpdate.VerifyFile` confirms that a downloaded file is the declared installer and refuses a file when the update declares no valid hash. A caller that will run the file opens it with `FileShare.Read`, passes that stream to `VerifyFile(Stream)` and keeps it open until the file has been started. No other handle can then write, replace or delete the file, so the bytes that run are the bytes that were verified. The WinForms library presents updates and installs them, as [its Updates guide](../../Sphere10.Framework.Windows.Forms/Updates/README.md) describes.

## Example

The service below is the application's adapter over its own release server. `QueryReleaseAsync` and its result stand for that server's client.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

public class ReleaseServerUpdateService : IProductUpdateService {
	public async Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) {
		var release = await QueryReleaseAsync(cancellationToken);
		if (release == null)
			return ProductUpdateCheckResult.NoUpdate;
		return ProductUpdateCheckResult.Available(new ReleaseServerProductUpdate {
			Version = ApplicationVersion.Parse(release.Version),
			DownloadUrl = release.DownloadUrl,
			FileName = release.FileName,
			Length = release.Length,
			Hash = release.Sha256,
			ReleaseNotes = release.Notes,
			InstallerArguments = release.InstallerArguments
		});
	}
}
```

The host registers the service, enables the checks and observes the checker.

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework;
using Sphere10.Framework.Application;

Sphere10Framework.Instance
	.Build()
	.AddModule<ModuleConfiguration>()
	.AddServices(services => services.AddSingleton<IProductUpdateService, ReleaseServerUpdateService>())
	.UseProductUpdateChecks(options => options.Interval = TimeSpan.FromHours(6))
	.Start();

var checker = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<ProductUpdateChecker>();
checker.UpdateAvailable += update => SystemLog.Info($"Version {update.Version} is available.");
```

Another channel derives from the template and implements its two hooks.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

public class ExampleChannelUpdater : ProductUpdaterBase {
	protected override Task<ProductUpdateCheckResult> FetchUpdateAsync(CancellationToken cancellationToken)
		=> Task.FromResult(ProductUpdateCheckResult.Available(new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") }));

	protected override Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken) {
		NotifyInstallProgressChanged(new ProductUpdateProgress(ProductUpdateStage.Installing, 0, 0));
		return Task.FromResult(ProductUpdateInstallResult.Completed);
	}
}
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IProductUpdateService`](IProductUpdateService.cs) | interface | The release server's answer through `CheckForUpdateAsync`, implemented by the application over its server and asked by `ReleaseServerProductUpdater` |
| [`NoOpProductUpdateService`](NoOpProductUpdateService.cs) | class | Default service that never finds an update, registered by the Application module when the host supplies none |
| [`ProductUpdate`](ProductUpdate.cs) | class | An available version as every channel shows it: `Version`, `ReleaseNotes` (null when the channel publishes none) and `IsMandatory` |
| [`ProductUpdateChecker`](ProductUpdateChecker.cs) | class | Background checker over a `ProductUpdaterBase` with `Start`, `CheckAsync`, `OfferAgain`, `StopAsync`, the `UpdateAvailable` event and `AvailableUpdate`, running one repeating job on its own scheduler |
| [`ProductUpdateCheckerFinalizer`](ProductUpdateCheckerFinalizer.cs) | class | Process finalizer that stops the checker and waits for its check in flight |
| [`ProductUpdateCheckerInitializer`](ProductUpdateCheckerInitializer.cs) | class | Process initializer that starts the checker's schedule |
| [`ProductUpdateCheckOptions`](ProductUpdateCheckOptions.cs) | class | `InitialDelay` (30 seconds) and `Interval` (one hour) of the checks |
| [`ProductUpdateCheckResult`](ProductUpdateCheckResult.cs) | class | One check's answer, created with `NoUpdate` or `Available(update)`, which requires the update's version |
| [`ProductUpdateExtensions`](ProductUpdateExtensions.cs) | static class | `UseProductUpdateChecks` on `Sphere10FrameworkBuilder` and `AddProductUpdateChecks` on a service collection, which enable the checks |
| [`ProductUpdateInstallResult`](ProductUpdateInstallResult.cs) | enum | What an installation did: `Declined`, `ExitRequired` or `Completed` |
| [`ProductUpdateProgress`](ProductUpdateProgress.cs) | record | An installation's progress: its `Stage`, `BytesTransferred` and `BytesTotal` (zero while unknown) |
| [`ProductUpdaterBase`](ProductUpdaterBase.cs) | abstract class | Template owning a product's update through one channel: `CheckForUpdateAsync` and `InstallAsync` over the `FetchUpdateAsync` and `InstallUpdateAsync` hooks, with the `InstallProgressChanged` event |
| [`ProductUpdateStage`](ProductUpdateStage.cs) | enum | The stage `ProductUpdateProgress` reports: `Waiting`, `Connecting`, `Retrying`, `Downloading` or `Installing` |
| [`ReleaseServerProductUpdate`](ReleaseServerProductUpdate.cs) | class | A release server's update with its installer's download address, file name, length, SHA-256 hash and arguments, plus `VerifyFile` and `VerifyFileAsync` for a downloaded installer, by path or through an open stream that holds the file |
| [`ReleaseServerProductUpdater`](ReleaseServerProductUpdater.cs) | class | The release server's portable updater, registered by the Application module: checks through `IProductUpdateService`, refuses an offer without its installer's address and cannot install by itself |
