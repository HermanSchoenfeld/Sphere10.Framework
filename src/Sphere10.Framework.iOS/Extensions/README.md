# Extensions

Legacy Xamarin.iOS extension methods for UIKit, Foundation and CoreGraphics types, with conversions to and from .NET and System.Drawing types.

Like the rest of this project, these files are reference source that no solution or CI job builds, as the [project README](../README.md) explains. Most classes are in the `Sphere10.Framework.iOS` namespace. `CGPointExtensions`, `CGRectExtensions` and `CGSizeExtensions` are in `Sphere10.Framework`. The `UIImageExtensions` image operations mirror the image helpers of `Tools.iOSTool` in the project root. `UIViewExtensions.AddSubviewDock` positions a subview with `DockTarget` values from [UI](../UI/README.md), and `ShowLoadingHUD` reports progress through `IProgressDelegate`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionProgressDelegate`](UIViewExtensions.cs) | class | `IProgressDelegate` that forwards each progress message to an `Action<string>`. |
| [`AssemblyExtensions`](AssemblyExtensions.cs) | static class | `FromResource` copies an embedded resource of an assembly into an `NSData`. |
| [`CGPointExtensions`](CGPointExtensions.iOS.cs) | static class | Converts `CGPoint` to `Point` and `PointF` and adds distance, addition and subtraction. |
| [`CGRectExtensions`](CGRectExtensions.iOS.cs) | static class | Converts `CGRect` to `Rectangle` and `RectangleF` and adds corner and center points, resizing, translation, expansion and intersection helpers. |
| [`CGSizeExtensions`](CGSizeExtensions.iOS.cs) | static class | Converts `CGSize` to `Size` and `SizeF` and clips, shrinks and expands sizes. |
| [`DateExtensions`](NSDateExtensions.cs) | static class | Converts between `DateTime` and `NSDate`, including a nullable `DateTime` conversion. |
| [`IProgressDelegate`](UIViewExtensions.cs) | interface | Receives progress messages from work run under `ShowLoadingHUD`. |
| [`NSBundleExtensions`](NSBundleExtensions.cs) | static class | `ExtractFile` copies a file from an `NSBundle` to a destination file or folder. |
| [`NSDataExtensions`](NSDataExtensions.cs) | static class | `ToByteArray` copies an `NSData` into a byte array. |
| [`NSStringExtensions`](NSStringExtensions.cs) | static class | `ToNSString` converts a `string` to an `NSString`. |
| [`PointExtensions`](PointExtensions.cs) | static class | `ToCGPoint` converts a `System.Drawing.Point`. |
| [`PointFExtensions`](PointFExtensions.cs) | static class | `ToCGPoint` converts a `System.Drawing.PointF`. |
| [`RectangleExtensions`](RectangleExtensions.cs) | static class | `ToCGRect` converts a `System.Drawing.Rectangle`. |
| [`RectangleFFExtensions`](RectangleFExtensions.cs) | static class | `ToCGRect` converts a `System.Drawing.RectangleF`. |
| [`ScrollViewObserver`](UIScrollViewExtensions.cs) | class | Key-value observer that keeps a zoomable content view positioned inside its `UIScrollView`. |
| [`SizeExtensions`](SizeExtensions.cs) | static class | `ToCGSize` converts a `System.Drawing.Size`. |
| [`SizeFExtensions`](SizeFExtensions.cs) | static class | `ToCGSize` converts a `System.Drawing.SizeF`. |
| [`UIBarButtonItemExtensions`](UIBarButtonItemExtensions.cs) | static class | Hides and shows a `UIBarButtonItem`. |
| [`UIButtonViewExtensions`](UIButtonViewExtensions.cs) | static class | `SetTitleForAllStates` sets a `UIButton` title for every control state. |
| [`UICollectionViewExtensions`](UICollectionViewExtensions.cs) | static class | `DeselectAll` clears the selection of a `UICollectionView`. |
| [`UIColorExtensions`](UIColorExtensions.cs) | static class | Converts between `UIColor` and `System.Drawing.Color`. |
| [`UIDatePickerExtensions`](UIDatePickerExtensions.cs) | static class | Gets and sets a `UIDatePicker` date as a `DateTime`. |
| [`UIImageExtensions`](UIImageExtensions.cs) | static class | Creates a `UIImage` from bytes and crops, resizes, zooms, antialiases, removes a color from and saves images as JPEG. |
| [`UINavigationItemExtensions`](UINavigationItemExtensions.cs) | static class | `AddLeftItem` adds a left bar button to a `UINavigationItem`, optionally skipping one already present. |
| [`UIPopoverPresentationControllerExtensions`](UIPopoverPresentationControllerExtensions.cs) | static class | `SetPresentationAnchor` anchors a popover to an `NSObject` such as a view or bar button item, or to a table or collection cell named by a `UITargetReference`. |
| [`UIScrollViewExtensions`](UIScrollViewExtensions.cs) | static class | Sets and removes a zoomable content view and sets the touch count of a scroll view's pan gesture. |
| [`UITableViewExtensions`](UITableViewExtensions.cs) | static class | `DeselectAllRows` clears the selection of a `UITableView`. |
| [`UIViewControllerExtensions`](UIViewControllerExtensions.cs) | static class | Runs work on the UI thread, finds the top presented view controller and computes the usable content area. |
| [`UIViewExtensions`](UIViewExtensions.cs) | static class | Docks subviews, removes and disposes subviews, draws rounded rectangles and shows a loading HUD while an action runs. |
| [`XamarinNumericExtensions`](XamarinNumericExtensions.cs) | static class | `ClipTo` for `nfloat`, `nint` and `nuint`, and `Repeat` to run an action `nint` times. |
