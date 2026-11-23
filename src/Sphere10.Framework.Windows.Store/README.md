# Sphere10.Framework.Windows.Store

Windows Store distribution support: the product updater of an application distributed through the Microsoft Store. A Store build references this library and adds its module; every other build leaves it out and keeps the release server's updater.

The library targets a Windows SDK version (`net8.0-windows10.0.19041.0`, `net9.0-windows10.0.19041.0` and `net10.0-windows10.0.19041.0`, and `net11.0-windows10.0.19041.0` when the SDK supports .NET 11), whose target framework supplies the Windows Runtime projection. It references only the Application library and the core.

## The Store updater

`WindowsStoreProductUpdater` is a `ProductUpdaterBase` (see the [Application library's updates](../Sphere10.Framework.Application/Updates/README.md)).

- A check asks the Store for the package updates of the application and its optional packages (`StoreContext.GetAppAndOptionalStorePackageUpdatesAsync`). It answers a `WindowsStoreProductUpdate` whose version is that of the application's own package (or the highest optional package's when only those are offered), which is mandatory when any package update is, and which has no release notes, because the Store publishes none to the application.
- An installation hands the package updates to the Store (`RequestDownloadAndInstallStorePackageUpdatesAsync`). The Store shows its own consent, downloads, closes the application and starts it again. The Store's context is initialised with the window of the update notice (`IInitializeWithWindow`), as a desktop application must for the Store's dialogs to show, or with the process's main window when none is given. The installation answers `Completed` when the Store installed the update, `Declined` when the user cancelled in the Store's dialog, and fails with the Store's reason (low battery, Wi-Fi recommended or required, another error) otherwise. The packages' statuses are summed into one progress: waiting, downloading with the bytes of every package, then installing.
- A check never fails because the Store cannot offer updates. When the process has no package identity (an unpackaged or development run) or the Store refuses the request (a package not installed from the Store), it answers no update and logs why.

The WinForms notice and presenter work unchanged with this updater: the notice shows the version with **Download And Install** and **Later**, hides the release notes and download link it does not have, shows the Store's progress, and closes when the Store has installed the update. No exit is requested, because the Store restarts the application itself.

The Store is reached only through the protected virtual `GetPackageFamilyName`, `GetStorePackageUpdatesAsync` and `RequestDownloadAndInstallAsync`, so a subclass can run the updater's decisions without the Store, as the test project does.

## Selecting the Store updater at build time

`ModuleConfiguration` replaces the registered `ProductUpdaterBase`, including the release server's that `UseProductUpdates` registers, with `WindowsStoreProductUpdater`. A product selects it with an MSBuild property, so a Store build is compiled with the Store updater and every other build is unchanged:

```xml
<!-- dotnet build -p:Distribution=WindowsStore builds the Store distribution. -->
<PropertyGroup Condition="'$(Distribution)' == 'WindowsStore'">
	<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
	<DefineConstants>$(DefineConstants);WINDOWS_STORE</DefineConstants>
</PropertyGroup>

<ItemGroup Condition="'$(Distribution)' == 'WindowsStore'">
	<PackageReference Include="Sphere10.Framework.Windows.Store" />
</ItemGroup>
```

A product that references Framework source uses a `ProjectReference` to this project instead, and one that targets several frameworks sets `TargetFrameworks` (the Framework's own projects use `$(FrameworkWindowsStoreTargetFrameworks)`). The composition adds the module in the Store build only:

```csharp
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.AddModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
#if WINDOWS_STORE
	.AddModule<Sphere10.Framework.Windows.Store.ModuleConfiguration>()
#endif
	.UseMainForm<MainWindow>()
	.UseProductUpdates()
	.StartWinFormsApplication();
```

The [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) uses this switch: `dotnet build utils/Sphere10.Framework.Utils.WinFormsTester -p:Distribution=WindowsStore` builds it with the Store updater.

A Store update can be exercised only by a package the Store published and installed. A development run, a sideloaded package and the tests reach the "not available" answer instead.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | The Store distribution's module, which replaces the application's `ProductUpdaterBase` with `WindowsStoreProductUpdater` |
| [`StorePackageUpdateInfo`](StorePackageUpdateInfo.cs) | record | One package update the Store offered: package family, version, mandatory flag and the Store's own update object |
| [`WindowsStoreProductUpdate`](WindowsStoreProductUpdate.cs) | class | The Store's update: version, mandatory flag and the package updates to install, without release notes |
| [`WindowsStoreProductUpdater`](WindowsStoreProductUpdater.cs) | class | The updater that asks the Store for package updates and hands their installation to the Store |
