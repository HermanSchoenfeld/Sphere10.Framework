<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🍎 Sphere10.Framework.macOS

This folder holds legacy MonoMac source for AppKit table views, combo boxes, modal alerts and Cocoa extension methods. It has no project file, so nothing builds, tests or packages a `Sphere10.Framework.macOS` assembly. Use it only as reference source, for example when porting a table or combo-box helper to a current .NET macOS project.

## 📊 Status

| Area | State |
|---|---|
| Project | The folder has no `.csproj`, and neither `src/Sphere10.Framework (Win).sln` nor `src/Sphere10.Framework (CrossPlatform).sln` includes its files. |
| CI and packages | CI and `scripts/pack.ps1` build only those solutions, so they never compile this source. |
| Bindings | The files import the MonoMac binding namespaces `MonoMac.AppKit`, `MonoMac.Foundation`, `MonoMac.CoreGraphics` and `MonoMac.ObjCRuntime`. |
| Framework dependencies | The source uses core `Sphere10.Framework` types such as `SystemLog`, `Result` and `Tools.Lambda`. |
| Tests | No test project covers it. |

## 🏗️ Contents

Most types are declared in the `Sphere10.Framework` namespace.

| File | Contents |
|---|---|
| [`CocoaTool.cs`](CocoaTool.cs) | `CocoaTool` static helpers. They build `NSComboBoxDataSource` instances from an enum or from name-value pairs, show modal `NSAlert` prompts (`AskYN`, `AskSave`, `ReportInfo`, `ReportError`, `ReportResult`), run a function asynchronously (`Async`) and read the registered NIB name of a view or window type (`GetNIBName<T>`). |
| [`ActionTableViewDataSource.cs`](ActionTableViewDataSource.cs), [`ActionTableViewDelegate.cs`](ActionTableViewDelegate.cs) | `NSTableView` data source and delegate whose overrides call the optional `Func` and `Action` arguments passed to their constructors. |
| [`ADOTableViewDataSource.cs`](ADOTableViewDataSource.cs) | `ADOTableViewDataSource` reads and writes the cells of a `System.Data.DataTable` for an `NSTableView`. |
| [`StandardTableViewDelegate.cs`](StandardTableViewDelegate.cs) | `StandardTableViewDelegate` allows row selection and blocks column selection and editing. |
| [`ActionComboBoxDataSource.cs`](ActionComboBoxDataSource.cs), [`ComboBoxDataSourceEx.cs`](ComboBoxDataSourceEx.cs) | `NSComboBox` data sources that take delegates, or a typed item list with an optional empty first item. |
| [`ComboBoxItem.cs`](ComboBoxItem.cs) | `ComboBoxItem`, an `NSObject` combo-box item with an integer `Key` and a string `Value`. |
| [`NSComboBoxDelegate.cs`](NSComboBoxDelegate.cs) | `NSComboBoxDelegateEx`, which raises selection and pop-up events. |
| [`MouseDelegate.cs`](MouseDelegate.cs), [`ActionMouseDelegate.cs`](ActionMouseDelegate.cs) | Mouse-tracking delegates. `ActionMouseDelegate` forwards each event to an `Action<NSEvent>` property. |
| [`NSTextFieldDelegateEx.cs`](NSTextFieldDelegateEx.cs) | `NSTextFieldDelegateEx`, a stub whose overrides do nothing. |
| [`NSImageNames.cs`](NSImageNames.cs) | `NSImageNames` constants for the AppKit system template images. |
| [`SaveResponse.cs`](SaveResponse.cs) | `SaveResponse` (`Save`, `DontSave`, `Cancel`), which `CocoaTool.AskSave` returns. |
| [`Extensions/`](Extensions) | Extension methods for AppKit and Foundation types such as `NSView`, `NSTableView`, `NSTableColumn`, `NSCell`, `NSComboBox`, `NSColor`, `NSImage` and `NSDate`, conversions such as `ToNSString`, `ToNSNumber`, `ToNSFont`, `ToNSObject` and `FromNSObject<T>`, `DataTable.ToCSV`, reflection helpers and the `SizingMask` enum. |
| [`MyClass.cs`](MyClass.cs), [`AssemblyInfo.cs`](AssemblyInfo.cs) | Project-template leftovers, namely an empty `MyClass` and template assembly attributes. |

## 📚 Related Projects

- [Sphere10.Framework.iOS](../Sphere10.Framework.iOS/README.md) - legacy Xamarin.iOS library whose settings classes also compile when `MONOMAC` is defined
- [Sphere10.Framework.Android](../Sphere10.Framework.Android/README.md) - legacy Xamarin.Android library
- [Sphere10.Framework](../Sphere10.Framework/README.md) - core library that supplies `SystemLog`, `Result` and `Tools.Lambda`
- [Sphere10 Framework](../../README.md) - catalogue of the maintained libraries

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


