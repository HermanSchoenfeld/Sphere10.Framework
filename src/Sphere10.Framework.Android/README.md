<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🤖 Sphere10.Framework.Android

Sphere10.Framework.Android is a legacy Xamarin.Android class library whose only hand-written type is `AndroidSettingsProvider`, a settings provider rooted at the app's `Environment.SpecialFolder.ApplicationData` folder. No solution, CI job or packaging script builds it. Use it only as reference source, for example when porting the provider to a current .NET Android project.

## 📊 Status

| Area | State |
|---|---|
| Solutions | Neither `src/Sphere10.Framework (Win).sln` nor `src/Sphere10.Framework (CrossPlatform).sln` includes the project. |
| CI | The build job in `.github/workflows/main.yml` builds the cross-platform solution, so CI never compiles this project. |
| Packages | `scripts/pack.ps1` packs the Windows solution, so no NuGet package is built from this project. |
| Project format | `Sphere10.Framework.Android.csproj` is a legacy (non-SDK) Xamarin.Android project. It imports `$(MSBuildExtensionsPath)\Xamarin\Android\Xamarin.Android.CSharp.targets` and targets Android 8.1 (`TargetFrameworkVersion` `v8.1`). |
| Framework references | Its project references point at `Sphere10.Framework` and `Sphere10.Framework.Application`, which now target `net10.0`. |
| Source | `AndroidSettingsProvider` derives from `DirectoryFileSettingsProvider` in `Sphere10.Framework.Application`. No build verifies the source against the current Framework. |
| Tests | No test project covers it. |

## ⚡ 10-Second Example

Register `AndroidSettingsProvider` as the user settings provider at start-up, then load, change and save a settings record:

```csharp
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Android;
using Sphere10.Framework.Application;

public class AppPreferences : SettingsObject {
	[DefaultValue(0)]
	public int LaunchCount { get; set; }

	[DefaultValue(true)]
	public bool ShowTips { get; set; }
}

Sphere10Framework.Instance
	.Build()
	.AddServices(services => services.AddSingleton(new Local<ISettingsProvider>(new CachedSettingsProvider(new AndroidSettingsProvider()))))
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.Start();

var preferences = UserSettings.Get<AppPreferences>();
preferences.LaunchCount++;
preferences.Save();
```

The provider writes each record as a JSON file, here `AppPreferences.setting`, into the `ApplicationSettings` folder under `Environment.SpecialFolder.ApplicationData`. `[DefaultValue]` sets the value that a member holds until a saved value replaces it. `CachedSettingsProvider` keeps one shared object per record, so every call to `UserSettings.Get<AppPreferences>()` returns the same instance.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Android` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Resources](Resources/README.md) | The Android resource files of this legacy Xamarin.Android library and the resource-ID class generated from them. | [source](Resources/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`AndroidSettingsProvider`](AndroidSettingsProvider.cs) | class | `DirectoryFileSettingsProvider` that stores settings in the `ApplicationSettings` folder under `Environment.SpecialFolder.ApplicationData`. |
<!-- domains:end -->

## 🏗️ Contents

Types are declared in the `Sphere10.Framework.Android` namespace.

| File | Contents |
|---|---|
| [`AndroidSettingsProvider.cs`](AndroidSettingsProvider.cs) | `AndroidSettingsProvider`, whose parameterless constructor passes the `ApplicationSettings` folder under `Environment.SpecialFolder.ApplicationData` to its base class. |
| [`Resources/values/Strings.xml`](Resources/values/Strings.xml) | The project template's `hello` and `app_name` strings. |
| [`Resources/AboutResources.txt`](Resources/AboutResources.txt) | The project template's notes on Android resources. |

## 💡 Examples

No current solution builds this library, so these examples show what its one real type, `AndroidSettingsProvider`, does with the current `Sphere10.Framework.Application` settings API. The library wraps no Android device, permission, notification, camera or location API. Each example below pairs one of those tasks with the settings record that an app keeps for it, and the platform API that does the rest is named in the text. The examples assume the provider is registered as in the [10-second example](#-10-second-example).

### Device Information & Sensors

Read sensors through `Android.Hardware.SensorManager`, and keep per-sensor state in a settings record whose ID names the sensor:

```csharp
public class SensorCalibration : SettingsObject {
	[DefaultValue(0f)]
	public float OffsetX { get; set; }

	[DefaultValue(0f)]
	public float OffsetY { get; set; }

	[DefaultValue(0f)]
	public float OffsetZ { get; set; }
}

var accelerometer = UserSettings.Get<SensorCalibration>("Accelerometer");
accelerometer.OffsetX = -x;
accelerometer.OffsetY = -y;
accelerometer.OffsetZ = 9.81f - z;
accelerometer.Save();

var gyroscopeCalibrated = UserSettings.Has<SensorCalibration>("Gyroscope");
```

Each ID gets its own file, such as `SensorCalibration_Accelerometer.setting`.

### Runtime Permission Handling

Request permissions through the activity, and record what the user has already been asked:

```csharp
public class PermissionPrompts : SettingsObject {
	[DefaultValue(false)]
	public bool CameraRationaleShown { get; set; }

	[DefaultValue(0)]
	public int LocationDenials { get; set; }
}

var prompts = UserSettings.Get<PermissionPrompts>();
if (!prompts.CameraRationaleShown) {
	// Explain why the app needs the camera, then request Manifest.Permission.Camera
	prompts.CameraRationaleShown = true;
	prompts.Save();
}

if (!locationGranted) {
	prompts.LocationDenials++;
	prompts.Save();
}
```

### Local Notifications

Post notifications through `NotificationManager`, and keep the user's notification preferences in a settings record. `OnChanged` fires after a successful save, which is the point at which to reschedule reminders:

```csharp
public class NotificationPreferences : SettingsObject {
	[DefaultValue(true)]
	public bool RemindersEnabled { get; set; }

	[DefaultValue(15)]
	public int ReminderLeadMinutes { get; set; }

	public override Result Validate() {
		return ReminderLeadMinutes is >= 0 and <= 1440
			? Result.Success
			: Result.Error("Reminder lead time must be between 0 and 1440 minutes.");
	}
}

var notifications = UserSettings.Get<NotificationPreferences>();
notifications.OnChanged += () => RescheduleReminders(notifications);

notifications.ReminderLeadMinutes = 30;
if (notifications.Validate().IsSuccess)
	notifications.Save();
```

`Save()` does not call `Validate()`, so standalone code checks it first. A failed save does not raise `OnChanged`.

### Camera & Media Capture

Capture photos and video through the platform camera intents or `Android.Hardware.Camera2`. Load and save capture preferences with the asynchronous forms, which run the provider's file access on a worker thread:

```csharp
public class CaptureSettings : SettingsObject {
	[DefaultValue(85)]
	public int JpegQuality { get; set; }

	[DefaultValue("")]
	public string LastAlbum { get; set; }
}

public static async Task RememberAlbumAsync(string album, CancellationToken cancellationToken) {
	var capture = await UserSettings.GetAsync<CaptureSettings>(cancellationToken: cancellationToken);
	capture.LastAlbum = album;
	await capture.SaveAsync(cancellationToken);
}
```

### Location Services

Read locations through `Android.Locations.LocationManager`. The provider also works on its own, without Framework start-up:

```csharp
public class LastKnownLocation : SettingsObject {
	[DefaultValue(0.0)]
	public double Latitude { get; set; }

	[DefaultValue(0.0)]
	public double Longitude { get; set; }

	public DateTime? RecordedUtc { get; set; }
}

var androidProvider = new AndroidSettingsProvider();
ISettingsProvider provider = androidProvider;

var location = provider.Get<LastKnownLocation>();
location.Latitude = latitude;
location.Longitude = longitude;
location.RecordedUtc = DateTime.UtcNow;
location.Save();

Console.WriteLine(androidProvider.BaseDirectory);   // .../ApplicationSettings
Console.WriteLine(provider.Has<LastKnownLocation>()); // True

location.Delete();
```

`Get<T>()` and `Has<T>()` are default members of `ISettingsProvider`, so call them through a variable of that interface type. `BaseDirectory` belongs to the concrete provider. A record from `provider.Get<T>()` carries that provider, so its `Save()` and `Delete()` write through it.

### Background Services & Job Scheduling

The library wraps no Android service. The core `Scheduler` runs recurring jobs inside the app process, here one that records each sync in a settings record:

```csharp
public class SyncState : SettingsObject {
	public DateTime? LastSyncUtc { get; set; }
}

var sync = JobBuilder
	.For(() => {
		var state = UserSettings.Get<SyncState>();
		state.LastSyncUtc = DateTime.UtcNow;
		state.Save();
	})
	.Called("Sync")
	.Repeat.OnInterval(TimeSpan.FromMinutes(15))
	.Build();
Scheduler.Asynchronous.AddJob(sync);
```

The scheduler stops with the process. Work that must run after Android ends the process belongs in `WorkManager` or `JobScheduler`.

## 📦 Dependencies

| Dependency | Kind | Version |
|---|---|---|
| `Mono.Android` | Framework assembly | Android 8.1 (`v8.1`) |
| `Xamarin.Android.Support.v7.AppCompat` | NuGet package | `27.0.2.1` |
| [Sphere10.Framework](../Sphere10.Framework/README.md) | Project reference | Repository source |
| [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md) | Project reference | Repository source |

`AndroidSettingsProvider.cs` does not use the AppCompat package.

## 🔧 Porting notes

- `AndroidSettingsProvider` builds on `DirectoryFileSettingsProvider`, whose constructor takes the settings directory and an optional `DirectorySettingsProviderPolicy`.
- A current .NET Android build needs an SDK-style project, because this project uses the legacy Xamarin.Android format.

The provider ports as a small subclass in the new project. This one also passes `UseFullyQualifiedTypeNameInFilename`, so settings types with the same name in different namespaces get separate files:

```csharp
using System;
using System.IO;
using Sphere10.Framework.Application;

public sealed class AppDataSettingsProvider : DirectoryFileSettingsProvider {
	public AppDataSettingsProvider()
		: base(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ApplicationSettings"),
			DirectorySettingsProviderPolicy.Default | DirectorySettingsProviderPolicy.UseFullyQualifiedTypeNameInFilename
		) {
	}
}
```

## 📚 Related Projects

- [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md) - settings objects and providers that `AndroidSettingsProvider` builds on
- [Sphere10.Framework.iOS](../Sphere10.Framework.iOS/README.md) - legacy Xamarin.iOS library
- [Sphere10.Framework.macOS](../Sphere10.Framework.macOS/README.md) - legacy MonoMac source
- [Sphere10 Framework](../../README.md) - catalogue of the maintained libraries

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


