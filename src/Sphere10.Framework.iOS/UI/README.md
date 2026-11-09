# UI

Legacy Xamarin.iOS views, view controllers, awaitable dialogs, calendar controls, table helpers and gesture-recognizer subclasses.

`MessageBox`, `ActionSheet`, `DropDownList`, `MenuPresenter` and `ModalHelper` wrap UIKit alerts, action sheets, popovers and modal presentation in methods that return a `Task`, so callers can await the user's choice. `GenericTableViewSource<T>` with `GenericTableViewSection<T>` and `GenericCellDescriptor` renders sectioned items in a `UITableView` without a hand-written source. The gesture-recognizer `Ex` classes take an `Action` handler in their constructor and detach it on disposal. `ControlsTableViewController`, `TestViewController`, `TestPagesDataSource` and `LoadingHUDViewController` are demonstration screens rather than reusable controls. `ProgressHUD.cs` is entirely commented out and declares no type.

Layout helpers in [Extensions](../Extensions/README.md) dock views with `DockTarget`, and the keyboard-aware controllers are in [ViewControllers](../ViewControllers/README.md). `CalendarMonthView.cs` and `LoadingHUDView.cs` carry an additional MIT permission notice below the Sphere10 header. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionPopoverPresentationDelegate`](ActionPopoverPresentationDelegate.cs) | class | Popover presentation delegate that runs an action when the popover is dismissed. |
| [`ActionSheet`](ActionSheet.cs) | static class | Shows a `UIActionSheet` from a view, bar button item, tab bar or toolbar and returns the chosen option index, or null. |
| [`AmountEditorViewController`](AmountEditorViewController.cs) | class | View controller that edits a decimal amount in a large `UIDecimalField`. |
| [`Animations`](Animations.cs) | class | Fade helpers that animate one or more views to an alpha value, including a fade-in-then-out sequence and an awaitable fade. |
| [`AppRater`](AppRater.cs) | static class | Counts launches and significant events in `NSUserDefaults` and prompts the user to rate the app in the App Store. |
| [`BadgeCell`](BadgeCell.cs) | class | `UITableViewCell` that can show a `BadgeView`. |
| [`BadgeView`](BadgeView.cs) | class | Rounded `UILabel` badge that shows a number or text with configurable colors and border. |
| [`CalendarDayEventView`](DayCalendar.cs) | class | View of one event in the day calendar with title, location, start and end dates. |
| [`CalendarDayView`](CalendarMonthView.cs) | class | One day cell of the month calendar with available, today, selected and marked states. |
| [`CalendarDaytimelineView`](DayCalendar.cs) | class | Scrollable day timeline that lays out `CalendarDayEventView` events by hour. |
| [`CalendarMonthView`](CalendarMonthView.cs) | class | Month calendar view with month navigation, date selection and marked-day callbacks. |
| [`CalendarMonthViewController`](CalendarMonthViewController.cs) | class | View controller that hosts a `CalendarMonthView`. |
| [`ControlsTableViewController`](ControlsTableViewController.cs) | class | Demonstration table that opens sample image-list and rotating view controllers. |
| [`DateSelected`](CalendarMonthView.cs) | delegate | Callback that receives the date chosen in a `CalendarMonthView`. |
| [`DockTarget`](DockTarget.cs) | class | Edge target for `AddSubviewDock`, either the container, another view, a fixed length or a percentage of the container. |
| [`DockTargetType`](DockTargetType.cs) | enum | `DockToView`, `DockToContainer`, `FixedLength` or `PercentageOfContainer`. |
| [`DropDownList`](DropDownList.cs) | static class | Shows a list or sectioned list of items as an awaitable selection and returns a `DropDownListSelection<T>`. |
| [`DropDownListSelection<T>`](DropDownListSelection.cs) | class | Result of `DropDownList.Show` that reports whether an item was selected and which one. |
| [`FullScaleImageView`](FullScaleImageView.cs) | class | Scroll view that shows an image at full scale. |
| [`FullScaleImageViewController`](FullScaleImageView.cs) | class | View controller that hosts a `FullScaleImageView`. |
| [`GenericCellDescriptor`](GenericCellDescriptor.cs) | class | Describes a table cell's image, title, description, badge, accessory, selection action and extra configuration. |
| [`GenericTableViewCell`](GenericTableViewCell.cs) | class | `BadgeCell` used by `GenericTableViewSource<T>` with adjusted subview layout. |
| [`GenericTableViewSection<T>`](GenericTableViewSection.cs) | class | Table section with a title, footer, items and a collapsed state that raises `CollapsedChanged`. |
| [`GenericTableViewSource<T>`](GenericTableViewSource.cs) | class | `UITableViewSource` that renders sections of items through a `GenericCellDescriptor` per item. |
| [`IPagedViewDataSource`](PagedViewController.cs) | interface | Supplies the page view controllers and page count of a `PagedViewController`. |
| [`ISpecialDisposable`](ISpecialDisposable.cs) | interface | `SpecialDispose` releases a reusable cell's resources outside normal disposal. |
| [`ImageListView`](ImageListViewController.cs) | class | Empty `UIScrollView` subclass used by `ImageListViewController`. |
| [`ImageListViewController`](ImageListViewController.cs) | class | View controller that fills its view with an `ImageListView`. |
| [`LevelMeter`](LevelMeter.cs) | class | View that draws audio queue level-meter states as bars. |
| [`LoadingHUDView`](LoadingHUDView.cs) | class | Rounded overlay with an activity indicator, title and message that starts and stops animating on demand. |
| [`LoadingHUDViewController`](LoadingHUDViewController.cs) | class | Demonstration screen that shows large and small `LoadingHUDView` overlays. |
| [`MenuOption`](MenuOption.cs) | class | Menu entry with a tag, title and image, used by `MenuPresenter` and `MenuCollectionViewController`. |
| [`MenuPresenter`](MenuPresenter.cs) | static class | Shows `MenuOption` entries as a context menu or popover anchored to a view and returns the chosen option. |
| [`MessageBox`](MessageBox.cs) | static class | Shows an awaitable `UIAlertController` message and returns the `MessageBoxResult` of the button pressed. |
| [`MessageBoxButton`](MessageBox.cs) | enum | `OK`, `OKCancel`, `YesNo`, `YesNoCancel` or `None`, the button set of a `MessageBox`. |
| [`MessageBoxResult`](MessageBox.cs) | enum | `None`, `OK`, `Cancel`, `Yes` or `No`, the button that closed a `MessageBox`. |
| [`ModalFrameController`](ModalHelper.cs) | class | Frame controller that presents a content view controller modally with a Done action. |
| [`ModalHelper`](ModalHelper.cs) | static class | Presents a view controller modally and returns a task that completes, optionally with a result, when it is dismissed. |
| [`MonthChanged`](CalendarMonthView.cs) | delegate | Callback that receives the month shown after a `CalendarMonthView` changes month. |
| [`MonthGridView`](CalendarMonthView.cs) | class | Grid of `CalendarDayView` cells for one month that handles touch selection. |
| [`MultidateCalendarViewController`](MultidateCalendarViewController.cs) | class | Calendar and table screen that selects a `Period` by choosing its start and end dates. |
| [`MyMoviePlayer`](MyMoviePlayer.cs) | class | View that plays a movie URL with `MPMoviePlayerController` and runs a `Done` action at the end. |
| [`NoOverlapTableViewCell`](NoOverlapTableViewCell.cs) | class | Table cell that lays out its text and detail labels so they do not overlap the accessory. |
| [`PagedViewController`](PagedViewController.cs) | class | Horizontally paged scroll view controller over an `IPagedViewDataSource`. |
| [`Period`](MultidateCalendarViewController.cs) | class | Date range with `DateFrom` and `DateTo`. |
| [`PickerData`](UIComboBox.cs) | class | `UIPickerViewModel` that supplies the rows of a `UIComboBox` picker. |
| [`PickerView`](UIComboBox.cs) | class | `UIPickerView` that binds items by display and value members for a `UIComboBox`. |
| [`RotatingViewController`](RotatingViewController.cs) | class | Container that swaps between portrait, landscape-left and landscape-right view controllers as the device rotates. |
| [`ScrollViewWithHeader`](ScrollViewWithHeader.cs) | class | View that scrolls and zooms content while keeping a header, and optionally a row header, aligned with it. |
| [`Selecting`](MultidateCalendarViewController.cs) | enum | `From` or `To`, the end of the period being chosen. |
| [`TapableView`](TapableView.cs) | class | View that raises `Tapped` and `DoubleTapped` events. |
| [`TestPagesDataSource`](ControlsTableViewController.cs) | class | Demonstration `IPagedViewDataSource` with colored pages. |
| [`TestViewController`](ControlsTableViewController.cs) | class | Demonstration view controller that autorotates and uses the full screen layout. |
| [`UIButtonEx`](UIButtonEx.cs) | abstract class | `UIButton` base whose `Frame` setter applies size constraints first to work around a border-drawing bug. |
| [`UICollectionViewImageCell`](UICollectionViewImageCell.cs) | class | Collection cell that shows an image and implements `ISpecialDisposable`. |
| [`UIComboBox`](UIComboBox.cs) | class | Text field that opens a fading picker of items bound by display and value members. |
| [`UIDecimalField`](UIDecimalField.cs) | class | Text field that accepts only decimal input and exposes its `Value`. |
| [`UIFixedOrientationViewController`](UIFixedOrientationViewController.cs) | class | View controller that forces a fixed interface orientation while it is shown. |
| [`UIGlassyButton`](UIGlassyButton.cs) | class | Custom-drawn button with a glassy gradient, title color and highlight color. |
| [`UIHorizontalButton`](UIHorizontalButton.cs) | class | Custom-drawn button with gradient background and shine layers and a highlight color. |
| [`UIImageViewEx`](UIImageViewEx.cs) | class | Interactive `UIImageView` that raises `TouchBegan` and `TouchEnded` events. |
| [`UILongPressGestureRecognizerEx`](UILongPressGestureRecognizerEx.cs) | class | Long-press recognizer that calls an `Action` handler and detaches it on disposal. |
| [`UIPaintingView`](UIPaintingView.cs) | class | OpenGL ES view that records and draws finger strokes, for example a signature, and can erase them. |
| [`UIPaintingViewController`](UIPaintingViewController.cs) | class | Fixed-orientation controller that hosts a `UIPaintingView` with a Clear action. |
| [`UIPanGestureRecognizerEx`](UIPanGestureRecognizerEx.cs) | class | Pan recognizer that calls an `Action` handler and detaches it on disposal. |
| [`UIScrollViewEx`](UIScrollViewEx.cs) | class | Scroll view that draws a linear gradient background between two colors from its top-left to its bottom-right corner. |
| [`UISwipeGestureRecognizerEx`](UISwipeGestureRecognizerEx.cs) | class | Swipe recognizer that calls an `Action` handler and detaches it on disposal. |
| [`UITableViewControllerWithBugFix`](UITableViewControllerWithBugFix.cs) | class | Table view controller that resets nonzero top content and scroll-indicator insets before layout. |
| [`UITapGestureRecognizerEx`](UITapGestureRecognizerEx.cs) | class | Tap recognizer that calls an `Action` handler and detaches it on disposal. |
| [`UITextViewEx`](UITextViewEx.cs) | class | Text view with a gray border, rounded corners and a supplied background color. |
| [`UIWebImageView`](UIWebImageView.cs) | class | Image view that downloads its image from a URL with `NSUrlConnection`. |
| [`UIWebImageViewController`](UIWebImageViewController.xib.cs) | class | XIB-based demonstration controller that downloads images into its `UIWebImageView` outlet, which `UIWebImageViewController.xib.designer.cs` declares. |
| [`UIZoomableViewController`](UIZoomableViewController.cs) | class | View controller that places a content view in a zoomable scroll view and fits its zoom scales to the view. |
| [`ViewTap`](TapableView.cs) | delegate | Callback that receives the tapped `UIView` of a `TapableView`. |
