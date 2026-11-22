# Product updates

The "New Version Available" notice, the release server's Windows updater and the installer it runs, enabled for an application with one call.

`UseProductUpdates` on the framework builder enables the background checks of the [Application library's update checker](../../Sphere10.Framework.Application/Updates/README.md), registers `WindowsReleaseServerProductUpdater` as the application's `ProductUpdaterBase` unless the host registered its own, and registers a `ProductUpdatePresenter` with the process tasks that start and stop it. The application supplies the `IProductUpdateService` that decides what it can upgrade to. Without that service the default one never finds an update, and without the call nothing is checked. A Windows Store distribution also adds the [Windows Store library's](../../Sphere10.Framework.Windows.Store/README.md) module, which replaces the updater with `WindowsStoreProductUpdater`; the notice and the presenter are the same for both.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.AddModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
	.AddServices(services => services.AddSingleton<IProductUpdateService, ReleaseServerUpdateService>())
	.UseMainForm<MainWindow>()
	.UseProductUpdates()
	.StartWinFormsApplication();
```

`ReleaseServerUpdateService` is the application's own service and `MainWindow` its main form. `UseProductUpdates(options => ...)` changes the checker's `InitialDelay` (30 seconds) and `Interval` (one hour).

When the checker announces an update, the presenter posts the notice to the UI thread through `IUserInterfaceServices` and the check ends at once, so neither the checker nor a shutdown waits for the user. `ProductUpdateDialog` shows the product, its current and new versions, the release notes when the update has them and, for a release-server update, a link to the download address, with the actions **Download And Install** and **Later**. An update without release notes or a download link, such as the Windows Store's, gives them no space. One notice is shown at a time, and a main window hidden in the notification area gets a notice centred on the screen with its own taskbar button. An update that could not be shown, because a notice was open or the application was exiting, is handed back to the checker with `OfferAgain`, so the next check announces it again. A newer version found while an older notice is open is therefore shown by the first check after that notice closes.

**Download And Install** calls the updater's `InstallAsync` with the notice as the owner of any prompt it shows, and the notice shows the `InstallProgressChanged` progress with a progress bar, the download's waiting state and **Cancel**. **Cancel** and Escape stop the installation and leave the notice open, and closing the notice cancels it. A failed installation is reported in the notice, and a cancelled one, or one the user declined in the channel's own prompt, is not; each leaves the notice open with its choices. When the updater has taken the update over, the notice closes with its `ProductUpdateInstallResult`.

`WindowsReleaseServerProductUpdater` installs through a `ProductUpdateInstaller`. It first checks the installer's file name, which comes from the server. Only a plain file name ending in `.exe` or `.msi` is accepted, so a name with a path, a colon, a reserved device name, a trailing dot or space, or another extension is refused before anything is downloaded. It then downloads the installer into a new temporary folder with a BITS job named after the product and the process, so a second running instance keeps its own job. The job's states become installation stages (`Queued` is Waiting, `Connecting` is Connecting, `TransientError` is Retrying). A download that makes no progress for two minutes fails with BITS' explanation.

The downloaded file is opened denying write and delete to every other handle, verified against the update's SHA-256 hash through that handle, and held until the installer process has been started, so the bytes that run are the bytes that were verified. After a verified download the updater starts the installer and answers `ExitRequired`, and the presenter asks the application to exit through `IUserInterfaceServices.Exit()`. That is the ordinary exit path, so its confirmation and guards still apply and unsaved work can keep the application open. An updater that answers `Completed`, as the Windows Store's does, restarts the application itself, and no exit is requested.

An `.msi` package is run as `msiexec /i` with the verified path. An `.exe` is started directly, without the shell. Only when Windows answers that the executable requires elevation is it started again through the shell's `runas` verb, because a direct start cannot show the consent prompt that an installer's manifest asks for. The update's arguments reach the installer as one string and never pass through a command interpreter.

A failed download, a file that does not match and a launch that fails or is declined are logged by the updater and reported in the notice, and a cancelled download is neither. In each of those cases the temporary folder is removed and the application keeps running. A started installer keeps its folder, since it outlives the application. A notice that fails is logged, reported and offered again by the next check.

To present an update found by other means, such as a Check For Updates command that awaited `ProductUpdateChecker.CheckAsync`, call `ProductUpdatePresenter.ShowAsync(update)` on the UI thread. It answers a `ProductUpdateNoticeResult`. Derive from `ProductUpdatePresenter` and register the subclass to replace the notice (`ShowNoticeAsync`) or the exit request (`RequestExit`), and derive from `WindowsReleaseServerProductUpdater` to replace the installer (`CreateInstaller`).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ProductUpdateDialog`](ProductUpdateDialog.cs) | class | The "New Version Available" notice with Download And Install, Later, installation progress and Cancel, which installs through its `Updater` and closes with `InstallResult` set once the updater took the update over |
| [`ProductUpdateInstaller`](ProductUpdateInstaller.cs) | class | Disposable owner of one release-server update's installer: `DownloadAsync` checks the file name, downloads with BITS into a temporary folder and verifies the hash through a handle it keeps, and `LaunchAsync` starts the held file |
| [`ProductUpdateNoticeExtensions`](ProductUpdateNoticeExtensions.cs) | static class | `UseProductUpdates` on `Sphere10FrameworkBuilder`, which enables update checks, the Windows release-server updater and the presenter |
| [`ProductUpdateNoticeResult`](ProductUpdateNoticeResult.cs) | enum | Outcome of `ProductUpdatePresenter.ShowAsync`: `NotShown`, `Declined` or `Installing` |
| [`ProductUpdatePresenter`](ProductUpdatePresenter.cs) | class | Observes the update checker, shows the notice on the UI thread, asks the application to exit when the updater started an installer and hands back to the checker what it could not show |
| [`ProductUpdatePresenterFinalizer`](ProductUpdatePresenterFinalizer.cs) | class | Process finalizer that stops the presenter |
| [`ProductUpdatePresenterInitializer`](ProductUpdatePresenterInitializer.cs) | class | Process initializer that starts the presenter before the checker starts |
| [`WindowsReleaseServerProductUpdater`](WindowsReleaseServerProductUpdater.cs) | class | The release server's updater on Windows: checks as `ReleaseServerProductUpdater` does and installs through a `ProductUpdateInstaller`, answering `ExitRequired` |
