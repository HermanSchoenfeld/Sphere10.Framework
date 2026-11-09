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

## 🏗️ Contents

Types are declared in the `Sphere10.Framework.Android` namespace.

| File | Contents |
|---|---|
| [`AndroidSettingsProvider.cs`](AndroidSettingsProvider.cs) | `AndroidSettingsProvider`, whose parameterless constructor passes the `ApplicationSettings` folder under `Environment.SpecialFolder.ApplicationData` to its base class. |
| [`Resources/values/Strings.xml`](Resources/values/Strings.xml) | The project template's `hello` and `app_name` strings. |
| [`Resources/Resource.designer.cs`](Resources/Resource.designer.cs) | Generated Android resource IDs, including those of the AppCompat support library. |
| [`Resources/AboutResources.txt`](Resources/AboutResources.txt) | The project template's notes on Android resources. |
| [`Properties/AssemblyInfo.cs`](Properties/AssemblyInfo.cs) | Template assembly attributes, with the title `Sphere10.Android` and version `1.0.0.0`. |

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

## 📚 Related Projects

- [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md) - settings objects and providers that `AndroidSettingsProvider` builds on
- [Sphere10.Framework.iOS](../Sphere10.Framework.iOS/README.md) - legacy Xamarin.iOS library
- [Sphere10.Framework.macOS](../Sphere10.Framework.macOS/README.md) - legacy MonoMac source
- [Sphere10 Framework](../../README.md) - catalogue of the maintained libraries

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


