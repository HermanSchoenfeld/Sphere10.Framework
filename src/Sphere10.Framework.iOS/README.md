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


