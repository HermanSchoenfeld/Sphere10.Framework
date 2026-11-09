# ViewControllers

Legacy Xamarin.iOS view-controller bases for keyboard-aware forms, hosted views, menu grids and application screens.

`KeyboardAwareViewController` observes keyboard notifications and scrolls the first responder above the on-screen keyboard, and it requires its view to be a `UIScrollView`. Its `WhenReturn*` helpers chain return-key handling across text fields. `ApplicationScreenViewController` builds on it with lifecycle, navigation-button and disposal events and is marked `TODO needs work` in the source. `MenuCollectionViewController` shows `MenuOption` items from [UI](../UI/README.md) in a collection view. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionViewController`](ActionViewController.cs) | class | Hosts a supplied `UIView` and forwards disappear and autorotation decisions to optional delegates. |
| [`ApplicationScreenViewController`](ApplicationScreenViewController.cs) | class | Unfinished screen base with load, appear, disappear, back, menu, next, dismiss and disposing events over `KeyboardAwareViewController`. |
| [`KeyboardAwareViewController`](KeyboardAwareViewController.cs) | class | Scrolls the focused field above the keyboard, optionally ends editing on tap, and wires return-key focus and dismissal. |
| [`MenuCollectionViewController`](MenuCollectionViewController.cs) | class | Collection view controller that lays out `MenuOption` items with configurable insets, item size and spacing and calls a selection action when one is chosen. |
