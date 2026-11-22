# Window Title Bar

A small button kept beside the minimize, maximize and close buttons of other applications' windows, so an application can offer a per-window command or menu on every window of the desktop.

Windows gives a process no way to add a real button to another process's title bar without running code inside that process. `WindowTitleBarButtons` instead keeps one small window of its own over each title bar. The button never takes activation or keyboard focus, is transparent except for its image and a hover highlight, stays directly above its window in z-order, and is moved, hidden and removed with that window by following the desktop's window events. A click raises `Clicked` with the window's identity and the button's bounds, and the application decides what the click means.

```csharp
// On the application's STA UI thread.
var buttons = Tools.WinForms.Windowing.CreateTitleBarButtons();
buttons.Image = image;                 // borrowed: the caller keeps it alive
buttons.ToolTip = "Window options";
buttons.Clicked += click => WindowTitleBarButtons.ShowMenu(click, menu);
buttons.Start();
// ...
buttons.Dispose();                     // removes every button
```

## Which windows get a button

A button goes on every visible, unowned, captioned top-level window of a process whose identity could be read. Tool windows, windows that never take activation, cloaked windows and owned windows such as dialogs' popups get none. The component's own process is left out unless `IncludeOwnWindows` is set before starting. Derive from `WindowTitleBarButtons` and override `ShouldAttach` to narrow the set further, for example to one application. It is asked on the UI thread at every inventory, and a window that stops qualifying loses its button.

`Attached` and `Detached` report each window as its button appears and goes. Each of the four events (`Attached`, `Detached`, `Clicked`, `Failed`) has a protected `Notify` method and a virtual `On` hook, which a subclass overrides to react before subscribers do. `Windows` lists the windows that currently have one. The `WindowsWindowInfo` values are detached records from the Windows project's [Windowing](../../Sphere10.Framework.Windows/Windowing/README.md) domain, so verify a window's identity before acting on it.

## Placement

The button is a square of about three quarters of the caption's height, placed just left of the caption buttons. Its position comes from the window manager's caption button area (`DWMWA_CAPTION_BUTTON_BOUNDS`), in physical desktop pixels:

- The window manager draws caption buttons at the display's resolution and reports their area in physical pixels, but measured from the left of the window's own width. For a window that is not aware of its display's resolution, that width is smaller than the width on screen, so only the area's distance from the window's right-hand edge holds on screen. The component anchors the area to the physical right-hand edge and never scales its size.
- A window that draws its own frame can report an empty area, or one that is not at the right-hand end. Three standard caption buttons at the top right of the visible frame stand in for it.
- A maximized window hangs its resize border off the display. The button is clipped to the display's top edge.
- A minimized or hidden window shows no button, and the button returns when the window does. A minimized window stays in `Windows` and raises no `Detached`, so restoring it shows the button from the restore event alone.

An application that paints its own title bar, such as a browser with tabs or an editor with its own buttons there, can have content where the button would go. The component reads what the title bar shows left of the caption buttons and moves the button left to the first place where it keeps its gap from all of it:

- The title bar is read when a button appears and whenever the title bar's size changes, once that size has held for 0.4 s. Moving the window keeps the shift and reads nothing.
- The application's accessibility tree comes first ([`WindowsTitleBarReader`](../../Sphere10.Framework.Windows/Windowing/WindowsTitleBarReader.cs)): buttons, tabs, images, text and other controls that reach into the strip are taken as they are. An application that reports nothing there is read from its own composed pixels (`PrintWindow`), where columns that differ from the plain background are content. Windows covering the title bar, and full-screen colour effects, do not affect either reading.
- With no free place, or when the title bar cannot be read, the button stays beside the caption buttons. The button covers only its own square and passes no input to what is beneath it.

Reading another process can block on it, so the default `ReadTitleBarAsync` reads in an isolated helper of the same executable ([`WindowsTitleBarSession`](../../Sphere10.Framework.Windows/Windowing/WindowsTitleBarSession.cs)), started on first use and given 0.8 s per read. The executable dispatches the helper before its ordinary startup:

```csharp
[STAThread]
static int Main(string[] args) {
    if (WindowsUiAutomationUtil.TryRunTitleBarWorker(args, out var exitCode))
        return exitCode;
    // ... ordinary startup
}
```

A subclass may override `ReadTitleBarAsync` to read the title bar otherwise, as the tests do; returning content of source `None` keeps the button beside the caption buttons.

`TryGetBounds` reads one button's current physical bounds. `SetImage` gives one window's button its own image and tool tip, which is how an application shows per-window state; passing null returns it to the shared `Image` and `ToolTip`.

## Menus

`WindowTitleBarButtons.ShowMenu(click, menu)` opens a `ContextMenuStrip` beneath the clicked button, on the button's own display. The menu keeps the DPI awareness WinForms gave it when it was constructed, and WinForms keeps it on a screen worked out from display bounds cached in another context, so in a system-aware or unaware application it could open on another display. After it opens, the menu is moved natively in its own coordinates (`ThreadDpiAwarenessScope.OfWindow`) and its physical position read back and corrected, because Windows does not report the relation between the two consistently for such windows (a DPI-unaware test host's menu read as on its display in its own coordinates while it lay off every display). It opens above the button when there is no room below. A menu only closes on a click elsewhere while it owns the foreground, so the menu takes the foreground while it is open and hands it back to the button's window when it closes.

## Threading and ownership

Create, start, stop and dispose the component on one STA UI thread with a message loop. Its events are raised on that thread. Window inventories and their window-manager queries run on a worker, are requested by window events and are spaced a quarter of a second apart, so the component follows events and not a tick. Windows occasionally drops a window's events, so every two seconds each button is also checked against its window; when nothing changed that check asks nothing of the window manager beyond reading the window's place. A burst of events from a moving window places its button once, after the burst, and nothing is asked of the window manager when the button is already where it belongs. `Failed` reports a window event or inventory that could not be served; the component keeps running.

Images are borrowed and stay the caller's. Stopping or disposing removes every button at once, and a stopped component can be started again.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowTitleBarButtons`](WindowTitleBarButtons.cs) | class | Keeps one button beside the caption buttons of every captioned application window, and reports clicks. |
| [`WindowTitleBarButtonClick`](WindowTitleBarButtonClick.cs) | record | The window whose button was clicked and the button's physical screen bounds. |
| [`WindowTitleBarButtonForm`](Forms/WindowTitleBarButtonForm.cs) | internal form | The nonactivating, per-pixel transparent button window. |
| [`Tools.WinForms.Windowing`](../WindowHighlight/WindowsFormsWindowUtil.cs) | static class | `CreateTitleBarButtons()` factory, exposed as `Tools.WinForms.Windowing`. |
