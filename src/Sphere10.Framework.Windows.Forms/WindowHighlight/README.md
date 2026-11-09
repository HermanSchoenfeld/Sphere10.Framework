# Window Highlight

A click-through outline drawn over another window's physical desktop bounds, and a one-use picker that lets the user select a desktop window with the mouse.

`WindowHighlightScope` owns a hollow, nonactivating, input-transparent frame over a rectangle in physical desktop pixels. Set `Bounds` to follow a target and dispose the scope to remove the frame. Create, update and dispose it on the same STA UI thread. `Tools.Scope.EnterHighlightScope` is a shortcut for the constructor. Bounds usually come from `WindowsWindow.GetVisiblePhysicalBounds` in the Windows project's [Windowing](../../Sphere10.Framework.Windows/Windowing/README.md) domain.

`WindowPicker` combines the mouse and keyboard hooks with a highlight scope. Create it through `Tools.Windows.Forms.Windowing.CreatePicker(dispatcher)` or its constructors, await `PickAsync`, and dispose it on the dispatcher's thread. Releasing the left button accepts the highlighted window, and a right-click, Escape or cancellation cancels. `PickAsync` hides the form passed to it while picking. The result is a detached `WindowsWindowTarget`, so callers still verify the window's identity before acting on it.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowHighlightScope`](WindowHighlightScope.cs) | class | Scope that owns a topmost, click-through outline frame over caller-supplied physical bounds. |
