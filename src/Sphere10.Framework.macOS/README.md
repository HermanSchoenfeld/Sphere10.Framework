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

## ⚡ 10-Second Example

Show a `DataTable` in an `NSTableView`, then confirm an export with a modal alert:

```csharp
using System.Data;
using System.IO;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var customers = new DataTable();
customers.Columns.Add("Name");
customers.Columns.Add("Email");
customers.Rows.Add("Alice", "alice@example.com");
customers.Rows.Add("Bob", "bob@example.com");

customersTableView.SetDataSourceEx(customers);
customersTableView.SizeColumnsByContent();

if (CocoaTool.AskYN("Export {0} customers?", customers.Rows.Count))
	File.WriteAllText(exportPath, customers.ToCSV());
```

`SetDataSourceEx` replaces the table's columns with one column per `DataColumn`, hides columns whose `ColumnMapping` is `Hidden` and installs an `ADOTableViewDataSource`. Edits in the table view write back to the `DataTable`.

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

## 💡 Examples

Nothing builds this source, so these examples show what its real types do in a MonoMac app. `ADOTableViewDataSource`, `SetDataSourceEx(DataTable)` and `DataTable.ToCSV` are declared in the `Sphere10.Framework.Data` namespace, and the other types in `Sphere10.Framework`. Where the folder has no type for a task, the text names the AppKit API that covers it.

### System Information & Capabilities

The folder has no system-information type. Collect the values through .NET and show them in a read-only table:

```csharp
var info = new DataTable();
info.Columns.Add("Property");
info.Columns.Add("Value");
info.Rows.Add("OS version", Environment.OSVersion.VersionString);
info.Rows.Add("Processor cores", Environment.ProcessorCount);
info.Rows.Add("64-bit process", Environment.Is64BitProcess);
info.Rows.Add("Machine name", Environment.MachineName);

systemTableView.SetDataSourceEx(info);
systemTableView.Delegate = new StandardTableViewDelegate();
systemTableView.SizeColumsEqually();
```

`StandardTableViewDelegate` lets the user select rows and blocks column selection and editing. `SizeColumsEqually` keeps its misspelled name in the source.

### File Dialogs & Operations

Pick paths with `NSOpenPanel` and `NSSavePanel`, because the folder has no file-dialog wrapper. `CocoaTool.AskSave` asks whether to save before a document closes, and the extensions write tables and images to files:

```csharp
switch (CocoaTool.AskSave("Save changes to this report?")) {
	case SaveResponse.Save:
		File.WriteAllText(reportPath, reportTable.ToCSV(delimiter: "\t"));
		chartImage.SaveToFile(Path.ChangeExtension(reportPath, ".png"));
		break;
	case SaveResponse.DontSave:
		break;
	case SaveResponse.Cancel:
		return;
}
```

The alert's buttons are **Save**, **Cancel** and **Don't Save**. `NSImage.SaveToFile` always writes PNG data, whatever the file extension.

### Notifications

The folder has no `NSUserNotificationCenter` wrapper. `CocoaTool` reports information, errors and questions in modal `NSAlert` dialogs instead:

```csharp
CocoaTool.ReportInfo("Import finished.");

try {
	ImportCustomers();
} catch (Exception error) {
	CocoaTool.ReportError(error);
}

if (!CocoaTool.AskYN("Delete {0} archived orders?", archivedCount))
	return;
```

`ReportError(Exception)` shows `ToDisplayString()` of the exception. `ReportInfo` and `ReportError` show the same plain alert, because neither sets an alert style.

### Pasteboard (Clipboard) Management

The folder has no pasteboard wrapper. `ToCSV` produces tab-separated text that spreadsheets accept from the general pasteboard:

```csharp
var pasteboard = NSPasteboard.GeneralPasteboard;
pasteboard.ClearContents();
pasteboard.SetStringForType(ordersTable.ToCSV("\t"), NSPasteboard.NSStringType);
```

For drag and copy from a table, pass a `writeRows` function to `ActionTableViewDataSource`, which receives the row indexes and the pasteboard to write.

### Window Management

`CocoaTool.GetNIBName<T>()` returns the `[Register]` name of an `NSWindow` or `NSView` subclass, and the window controller loads the NIB of that name:

```csharp
[Register("CustomerWindow")]
public partial class CustomerWindow : NSWindow {
	public CustomerWindow(IntPtr handle) : base(handle) {
	}
}

var controller = new NSWindowController(CocoaTool.GetNIBName<CustomerWindow>());
controller.ShowWindow(this);

var pixels = customerView.ConvertRectToBacking(customerView.Bounds);
```

`GetNIBName<T>()` throws a `SoftwareException` when the type has no registration, more than one registration, or is neither a view nor a window. `ConvertRectToBacking` converts view points to backing-store pixels, as on a Retina display. `CocoaTool.GetEventSourceFromSender` returns the `NSView` behind an action sender or an `NSNotification`.

### Dock Integration

The folder has no Dock API. Its image conversions produce the `NSImage` that `NSApplication.ApplicationIconImage` shows in the Dock:

```csharp
using (var badged = Image.FromFile(badgedIconPath))
	NSApplication.SharedApplication.ApplicationIconImage = badged.ToNSImage();

var downloadedIcon = File.ReadAllBytes(iconPath).ToNSImage();
```

`Image.ToNSImage()` converts a `System.Drawing` image through PNG data, and `NSImage.ToImage()` converts back.

### Menu Bar Applications

The folder has no status-item wrapper. `NSImageNames` names the AppKit system template images that suit a menu-bar icon:

```csharp
var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
statusItem.Image = NSImage.ImageNamed(NSImageNames.NSImageNameActionTemplate);
statusItem.HighlightMode = true;
statusItem.Menu = statusMenu;
```

### System Services & Gestures

`ActionMouseDelegate` receives the mouse events of an `NSTrackingArea` and forwards each one to an `Action<NSEvent>`:

```csharp
_hover = new ActionMouseDelegate {
	View = previewView,
	MouseEnteredAction = e => previewView.AlphaValue = 1f,
	MouseExitedAction = e => previewView.AlphaValue = 0.6f,
	MouseMovedAction = e => ShowPosition(previewView.ConvertPointFromView(e.LocationInWindow, null))
};

previewView.AddTrackingArea(new NSTrackingArea(
	previewView.Bounds,
	NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.ActiveInKeyWindow,
	_hover,
	null
));
```

Keep the delegate in a field, so that it lives as long as the view. Subclass `MouseDelegate` instead when the handlers belong in one class. The folder has no gesture-recognizer helpers.

### Combo Boxes

`SetDataSourceEx` binds a typed list to an `NSComboBox`, and `NSComboBoxDelegateEx` raises its selection events:

```csharp
countryComboBox.SetDataSourceEx(countries, country => country.Name);
countryComboBox.SelectByItem(currentCountry, (x, y) => x.Code == y.Code);

_countryEvents = new NSComboBoxDelegateEx();
_countryEvents.SelectionChanged += (sender, args) => ShowCountry(countryComboBox.GetSelectedItem<Country>());
countryComboBox.Delegate = _countryEvents;
```

The list starts with an empty item unless `includeEmptyRow` is `false`. `GetSelectedItem<T>()` returns `default` for that item, and selecting `null` with `SelectByItem` clears the selection. The data source completes typed text to the first item that starts with it, ignoring case.

### Table Views from Delegates

`ActionTableViewDataSource` and `ActionTableViewDelegate` take each override as an optional constructor argument:

```csharp
ordersTableView.DataSource = new ActionTableViewDataSource(
	getRowCount: tableView => orders.Count,
	getObjectValue: (tableView, column, row) => column.GetIndex() == 0
		? orders[row].Number.ToNSString()
		: orders[row].Total.ToNSNumber(),
	sortDescriptorsChanged: (tableView, oldDescriptors) => { }
);

ordersTableView.Delegate = new ActionTableViewDelegate(
	getRowHeightFunc: (tableView, row) => 20f,
	shouldSelectRowFunc: (tableView, row) => true,
	selectionDidChangeAction: notification => ShowOrder(orders[ordersTableView.SelectedRow])
);
```

Each override whose argument is left out returns the default value, such as `false`, `0` or `null`. `SortDescriptorsChanged` calls its action without a null check, so pass `sortDescriptorsChanged` even when the table does not sort.

## 🔧 Porting notes

- `CocoaTool.ReportResult` reads `result.Success`, which the current `Result` declares as a static member. A port reads `result.IsSuccess`.
- `CocoaTool.GenerateComboBoxDataSourceFromEnum` and `NSComboBox.ClearDataSource` call `DescriptionAttribute.GetValuesWithDescriptions`, which the current Framework does not declare.
- `Sphere10.Framework.Data` declares the same `DataTable.ToCSV` extension in the same namespace, so a port drops this copy. The Data version also escapes header names and quotes fields for the chosen delimiter.
- Current .NET for macOS bindings use the `AppKit` and `Foundation` namespaces without the `MonoMac.` prefix, and many of their index and size parameters are `nint` or `nfloat`.

## 📚 Related Projects

- [Sphere10.Framework.iOS](../Sphere10.Framework.iOS/README.md) - legacy Xamarin.iOS library whose settings classes also compile when `MONOMAC` is defined
- [Sphere10.Framework.Android](../Sphere10.Framework.Android/README.md) - legacy Xamarin.Android library
- [Sphere10.Framework](../Sphere10.Framework/README.md) - core library that supplies `SystemLog`, `Result` and `Tools.Lambda`
- [Sphere10 Framework](../../README.md) - catalogue of the maintained libraries

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


