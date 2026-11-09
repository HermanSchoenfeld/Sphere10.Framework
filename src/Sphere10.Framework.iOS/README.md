<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 📱 Sphere10.Framework.iOS

Sphere10.Framework.iOS is a legacy Xamarin.iOS class library of UIKit views, view controllers, awaitable dialogs and extension methods, with StoreKit, network-reachability and address-book helpers. No solution, CI job or packaging script builds it. Use it only as reference source, for example when porting a control to a current .NET iOS project.

## 📊 Status

| Area | State |
|---|---|
| Solutions | Neither `src/Sphere10.Framework (Win).sln` nor `src/Sphere10.Framework (CrossPlatform).sln` includes the project. |
| CI | The build job in `.github/workflows/main.yml` builds the cross-platform solution, so CI never compiles this project. |
| Packages | `scripts/pack.ps1` packs the Windows solution, so no NuGet package is built from this project. |
| Project format | `Sphere10.Framework.iOS.csproj` is a legacy (non-SDK) Xamarin.iOS project. It imports `$(MSBuildExtensionsPath)\Xamarin\iOS\Xamarin.iOS.CSharp.targets` and references the `Xamarin.iOS` and `OpenTK-1.0` assemblies. |
| Framework references | Its project references point at `Sphere10.Framework`, `Sphere10.Framework.Application`, `Sphere10.Framework.Data` and `Sphere10.Framework.Drawing`, which now target `net10.0`. |
| Source | The settings classes use `Guard.ArgumentNotNull` from `Sphere10.Framework` and `Tools.Xml` from `Sphere10.Framework.Data`. No build verifies the source against the current Framework. |
| Tests | No test project covers it. |

## ⚡ 10-Second Example

Check the connection with `Reachability`, then ask the user with an awaitable `MessageBox`:

```csharp
using Sphere10.Framework.iOS;

if (Reachability.InternetConnectionStatus() == NetworkStatus.NotReachable) {
	await MessageBox.Show("Connect to the internet to sync your data.", "Offline");
	return;
}

var answer = await MessageBox.Show("Sync now?", "Sync", MessageBoxButton.YesNo);
if (answer == MessageBoxResult.Yes)
	await SyncAsync();
```

`MessageBox.Show` presents a `UIAlertController` in a new window above the top-most view controller, and its task completes with the button that the user tapped.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.iOS` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [AddressBook](AddressBook/README.md) | A legacy Xamarin.iOS helper that asks the user for address-book access. | [source](AddressBook/) |
| [AppStore](AppStore/README.md) | A legacy Xamarin.iOS StoreKit wrapper that fetches App Store product data and purchases products asynchronously. | [source](AppStore/) |
| [Extensions](Extensions/README.md) | Legacy Xamarin.iOS extension methods for UIKit, Foundation and CoreGraphics types, with conversions to and from .NET and System.Drawing types. | [source](Extensions/) |
| [Images](Images/README.md) | PNG images that the legacy iOS project embeds as resources for its calendar view, combo box and placeholder images. | [source](Images/) |
| [Network](Network/README.md) | A legacy Xamarin.iOS network-reachability helper that reports remote-host, internet and local Wi-Fi status. | [source](Network/) |
| [ObjC](ObjC/README.md) | Legacy P/Invoke declarations for the Objective-C runtime's associated-object and message-send functions. | [source](ObjC/) |
| [Settings](Settings/README.md) | Legacy settings persistence in `NSUserDefaults`, as a Sphere10 settings provider and as a self-loading settings object. | [source](Settings/) |
| [UI](UI/README.md) | Legacy Xamarin.iOS views, view controllers, awaitable dialogs, calendar controls, table helpers and gesture-recognizer subclasses. | [source](UI/) |
| [ViewControllers](ViewControllers/README.md) | Legacy Xamarin.iOS view-controller bases for keyboard-aware forms, hosted views, menu grids and application screens. | [source](ViewControllers/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.iOSTool`](iOSTool.cs) | tools | Static UIKit helpers for keyboard height, animation waits, video thumbnails, labels, top-most view controller lookup, Objective-C runtime properties, orientation, JPEG saving and image cropping, resizing, zooming and tinting. |
<!-- domains:end -->

## 🏗️ Contents

Most types are declared in the `Sphere10.Framework.iOS` namespace. `iOSTool` is declared in the `Tools` namespace, so callers reach it as `Tools.iOSTool`.

| Location | Contents |
|---|---|
| [`iOSTool.cs`](iOSTool.cs) | `Tools.iOSTool` static helpers for keyboard height, animation waits, video thumbnails, labels, top-most view controller lookup, Objective-C associated objects, JPEG saving, and image cropping, resizing, zooming and tinting. |
| [`UI/`](UI) | Awaitable dialogs (`MessageBox`, `ActionSheet`, `DropDownList`, `MenuPresenter`, `ModalHelper`), calendar views (`CalendarMonthView`, `MultidateCalendarViewController`), input controls (`UIComboBox`, `UIDecimalField`, `AmountEditorViewController`), table helpers (`GenericTableViewSource<T>`, `GenericTableViewCell`, `BadgeCell`), image views (`UIImageViewEx`, `UIWebImageView`, `FullScaleImageView`), `LoadingHUDView`, `UIPaintingView`, `AppRater` and gesture-recognizer subclasses. |
| [`ViewControllers/`](ViewControllers) | `KeyboardAwareViewController` scrolls the focused field above the on-screen keyboard and requires its view to be a `UIScrollView`. `ActionViewController` hosts a supplied view, and `MenuCollectionViewController` shows `MenuOption` items in a horizontal collection. `ApplicationScreenViewController` is an unfinished screen base class marked `TODO needs work`. |
| [`Extensions/`](Extensions) | Extension methods for UIKit, Foundation and CoreGraphics types such as `UIView`, `UIImage`, `UIColor`, `UITableView`, `NSDate` and `CGRect`, and conversions such as `ToNSString`, `ToNSDate` and `ToUIColor`. |
| [`Settings/`](Settings) | `UserDefaultsSettingsProvider` is a `SyncSettingsProviderBase` that stores each `DefaultValue`-attributed member of a settings object in `NSUserDefaults`. `UserDefaultObject` is a `SettingsObject` that loads and saves itself there. Both files compile only when `__IOS__` or `MONOMAC` is defined. |
| [`AppStore/`](AppStore) | `iTunesAppStoreManager` looks up StoreKit products and purchases them. A purchase returns a `PurchaseResult` that carries a `TransactionResult`. |
| [`Network/`](Network) | `Reachability` reports remote-host, internet and local Wi-Fi reachability as a `NetworkStatus`. |
| [`AddressBook/`](AddressBook) | `AddressBookTool.RequestAddressBookAuthorization` requests access to the address book. |
| [`ObjC/`](ObjC) | `iOSAPI` declares P/Invoke entry points for `objc_setAssociatedObject`, `objc_getAssociatedObject` and `objc_msgSend`, and `AssociationPolicy` lists the association policies. |
| [`Images/`](Images) | Calendar and placeholder PNGs that the project embeds as resources. |

`UI/CalendarMonthView.cs` and `UI/LoadingHUDView.cs` carry an additional MIT permission notice below the Sphere10 header. Keep it when you copy either file.

## 💡 Examples

No current solution builds this library, so no build checks these examples. They show what its real types do in a Xamarin.iOS app. Where the library has no type for a task, the text names the platform API that covers it.

### Device Information & Capabilities

The library has no device-information type. `Tools.iOSTool`, `Reachability` and `iTunesAppStoreManager` report the capabilities that its other types depend on:

```csharp
using Sphere10.Framework.iOS;

var hasRearCamera = Tools.iOSTool.IsCameraAvailable;
var orientation = Tools.iOSTool.CurrentInterfaceOrientation;
var isLandscape = Tools.iOSTool.IsLandscapeOrientation(orientation);
var usableFrame = Tools.iOSTool.FrameForOrientation(orientation);   // below the status bar
var localWifi = Reachability.LocalWifiConnectionStatus();
var canPay = new iTunesAppStoreManager().CanMakePayments;
```

Read the model and system version from `UIDevice.CurrentDevice`.

### Permission Request Patterns

`AddressBookTool` requests address-book access and reports whether the user granted it:

```csharp
using Sphere10.Framework.iOS;

if (!await AddressBookTool.RequestAddressBookAuthorization()) {
	await MessageBox.Show("Allow contact access in Settings to invite friends.", "Contacts");
	return;
}
```

It shows the system prompt only while the status is `NotDetermined`, and it returns `true` only for `Authorized`. It uses the AddressBook framework, which Apple has replaced with Contacts. Request camera, microphone and location access through `AVCaptureDevice` and `CLLocationManager`, because the library has no helper for them.

### Local Notifications & Alerts

The library has no `UNUserNotificationCenter` wrapper, so schedule local notifications through that platform API. For alerts and choices inside the app, it offers the awaitable `MessageBox` and `ActionSheet`:

```csharp
using Sphere10.Framework.iOS;
using UIKit;

var delete = await MessageBox.Show("Delete this draft?", "Drafts", MessageBoxButton.YesNo, yesIsDestructive: true);
if (delete == MessageBoxResult.Yes)
	DeleteDraft();

var choice = await ActionSheet.ShowFromButtonItem(shareButton, true, "Share", UIActionSheetStyle.Default, "Mail", "Messages", "Copy Link");
if (choice.HasValue)
	Share(choice.Value);   // 0 = Mail, 1 = Messages, 2 = Copy Link
```

`ActionSheet` adds a **Cancel** action, and its task returns `null` when the user picks it.

### Camera & Photo Access

Capture photos with `UIImagePickerController`, then size, crop and save them with the `UIImage` extensions. `Tools.iOSTool` also renders a poster frame for a video file:

```csharp
using System.IO;
using CoreGraphics;
using Sphere10.Framework;
using Sphere10.Framework.iOS;

if (!Tools.iOSTool.IsCameraAvailable)
	return;

// photo is the UIImage that the picker returned
using var thumbnail = photo.Resize(new CGSize(160, 160), ResizeMethod.AspectFill);
thumbnail.SaveAsJpegWithCompression(Path.Combine(cacheFolder, "thumb.jpg"), 80);

var square = photo.Crop(new CGRect(0, 0, photo.Size.Width, photo.Size.Width));

if (Tools.iOSTool.TryGenerateVideoThumbnail2(videoFile, new CGSize(160, 90), out var poster))
	posterView.Image = poster;
```

`IsCameraAvailable` checks the rear camera. The JPEG compression runs from 0 to 100. `TryGenerateVideoThumbnail2` reads a frame near the start of the video through `AVAssetImageGenerator` and fills the requested size with it. `TryGenerateVideoThumbnail` uses the deprecated `MPMoviePlayerController` instead.

### Location Services

The library has no location API, so read positions through `CLLocationManager`. `UserDefaultsSettingsProvider` can keep the last known position in `NSUserDefaults`:

```csharp
using System.ComponentModel;
using Sphere10.Framework.Application;
using Sphere10.Framework.iOS;

public class LastKnownLocation : SettingsObject {
	[DefaultValue(0.0)]
	public double Latitude { get; set; }

	[DefaultValue(0.0)]
	public double Longitude { get; set; }
}

ISettingsProvider provider = new UserDefaultsSettingsProvider();

var location = (LastKnownLocation)provider.NewSetting(typeof(LastKnownLocation));
location.Latitude = coordinate.Latitude;
location.Longitude = coordinate.Longitude;
location.Save();

var restored = (LastKnownLocation)provider.LoadSetting(typeof(LastKnownLocation));
```

The provider stores only members marked `[DefaultValue]`, each under the key `<type full name>.<member>`, or `<type full name>.<ID>.<member>` for a record with an ID. Load a saved record with `LoadSetting`, not `Get<T>()`. `ContainsSetting` looks for a key ending in `.ID`, which no save writes because `SettingsObject.ID` has no `[DefaultValue]`. `Get<T>()` therefore always returns a new record with its defaults.

### Background Task Execution

`UIView.ShowLoadingHUD` runs an action on a worker thread behind a `LoadingHUDView`, and the action reports progress through `IProgressDelegate`. `Tools.iOSTool.InvokeOnUIThread` brings work from a background task back to the main thread:

```csharp
using System.Threading.Tasks;
using Sphere10.Framework.iOS;

View.ShowLoadingHUD("Exporting", progress => {
	progress.NotifyMesssage("Writing records");
	ExportRecords();
	progress.NotifyMesssage("Compressing");
	CompressExport();
});

await Task.Run(async () => {
	var orders = await FetchOrdersAsync();
	await Tools.iOSTool.InvokeOnUIThread(() => {
		ShowOrders(orders);
		return Task.CompletedTask;
	});
});
```

`ShowLoadingHUD` returns after the action finishes, and it keeps the run loop turning while it waits. It logs an exception from the action and rethrows it as a `SoftwareException`. `InvokeOnUIThread` checks for a captured exception before it waits for the function, so a failure after the function's first `await` does not reach the caller. The library wraps no `BGTaskScheduler` or background-fetch API.

### Lists & Drop-Down Choices

`GenericTableViewSource<T>` fills a `UITableView` from a list of items, and `DropDownList` shows the same kind of list in a popover:

```csharp
using Sphere10.Framework.iOS;
using UIKit;

tableView.Source = new GenericTableViewSource<Customer>(
	tableView,
	UITableViewCellStyle.Subtitle,
	customers,
	customer => new GenericCellDescriptor {
		Title = customer.Name,
		Description = customer.Email,
		Accessory = UITableViewCellAccessory.DisclosureIndicator
	},
	customer => ShowCustomer(customer),
	title: "Customers"
);

var selection = await DropDownList.Show(filterButton, UITableViewCellStyle.Default, regions, region => new GenericCellDescriptor { Title = region.Name }, "Region");
if (selection.IsItemSelected)
	ApplyRegionFilter(selection.SelectedItem);
```

`SelectedItem` throws when nothing was selected, so check `IsItemSelected` first.

### In-App Purchases

`iTunesAppStoreManager` fetches StoreKit products, caches them by product ID and purchases them:

```csharp
using Sphere10.Framework.iOS;

var store = new iTunesAppStoreManager();
if (!store.CanMakePayments)
	return;

await store.PrefetchProductData(new[] { "com.example.pro", "com.example.themes" });

var product = await store.GetProductData("com.example.pro");
var purchase = await store.PurchaseProduct(product);
switch (purchase.Result) {
	case TransactionResult.Purchased:
	case TransactionResult.Restored:
		UnlockPro(purchase.PurchaseDate);
		break;
	case TransactionResult.Failed:
		await MessageBox.Show(purchase.Error.LocalizedDescription, "Purchase failed");
		break;
}
```

A product lookup retries up to three times before it throws. The purchaser finishes each transaction as soon as StoreKit reports it, before the app has unlocked anything, as a comment in the source warns.

### Network Reachability

`Reachability` probes the host in `HostName` and raises `ReachabilityChanged` when any probe that it has scheduled changes:

```csharp
using Sphere10.Framework.iOS;

Reachability.HostName = "api.example.com";
Reachability.ReachabilityChanged += (sender, args) => UpdateOfflineBanner(Reachability.RemoteHostStatus());
UpdateOfflineBanner(Reachability.RemoteHostStatus());
```

Set `HostName` before the first `RemoteHostStatus()` call, because that call creates the host probe once. `HostName` defaults to `www.google.com`. `IsHostReachable(host)` checks another host once without scheduling a probe.

## 📦 Dependencies

| Dependency | Kind | Version |
|---|---|---|
| `Xamarin.iOS`, `OpenTK-1.0` | Framework assemblies | Not pinned |
| `System.Drawing.Common` | `packages.config` entry | `4.5.1` |
| [Sphere10.Framework](../Sphere10.Framework/README.md) | Project reference | Repository source |
| [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md) | Project reference | Repository source |
| [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) | Project reference | Repository source |
| [Sphere10.Framework.Drawing](../Sphere10.Framework.Drawing/README.md) | Project reference | Repository source |

## 🔧 Porting notes

- Members whose type has no native user-defaults representation are stored as XML through `Tools.Xml`, so their types must be serializable by `XmlSerializer`.
- A current .NET iOS build needs an SDK-style project, because this project uses the legacy Xamarin.iOS format.

## 📚 Related Projects

- [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md) - settings objects and providers that the `Settings` folder builds on
- [Sphere10.Framework.Android](../Sphere10.Framework.Android/README.md) - legacy Xamarin.Android library
- [Sphere10.Framework.macOS](../Sphere10.Framework.macOS/README.md) - legacy MonoMac source
- [Tools reference](../../docs/tools-reference.md) - the `Tools.*` facades, including `Tools.iOSTool`
- [Sphere10 Framework](../../README.md) - catalogue of the maintained libraries

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


