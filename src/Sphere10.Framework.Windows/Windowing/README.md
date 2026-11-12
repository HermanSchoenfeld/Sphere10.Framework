# Windowing

Read-only queries over other processes' native windows, verified window messaging, isolated UI Automation capture and thread DPI awareness scopes.

All windows here are borrowed. `WindowsWindow` enumerates windows, reads the window under a point or the pointer, and reads bounds in caller or physical desktop pixels. It returns detached `WindowsWindowInfo` records that capture handle, class, process, thread and process start time. Later calls such as `GetPhysicalBounds` and `WindowMessageSender.Send` check that identity again before acting, so a reused handle is not mistaken for the original window. `WindowsWindowLayering` answers z-order questions without activating anything. `Tools.Windows.Windowing` exposes the same operations as instance methods through `Tools.Windows.Windowing`, adds `AllowForeground`, and is the preferred entry point.

`WindowsUiAutomationUtil.CaptureAsync`, reached through `Tools.Windows.UiAutomation`, reads a selected window's UI Automation Control View in a separate helper process so a hung provider cannot block the application. The helper is the application's own executable started with `WorkerArgument`, so the application's `Main` must call `WindowsUiAutomationUtil.TryRunWorker(args, out var exitCode)` first and return `exitCode` when it returns `true`. `WindowsUiAutomationOptions` sets node, depth and time budgets, and the capture returns detached `WindowsUiAutomationElement` rows. `ThreadDpiAwarenessScope` changes the current thread's DPI awareness and restores it on dispose on the same thread. Highlight and picker UI over these queries is in the Windows Forms project's `WindowHighlight` domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DpiAwarenessContext`](DpiAwarenessContext.cs) | enum | Per-thread Windows DPI awareness modes from unaware to per-monitor v2 and GDI-scaled. |
| [`NativeWindowMessageResult`](NativeWindowMessageResult.cs) | record | Outcome of a sent or posted message with success flag, return value, Win32 error, send time and elapsed time. |
| [`ThreadDpiAwarenessScope`](ThreadDpiAwarenessScope.cs) | struct | Synchronous scope that sets the calling thread's DPI awareness and restores the previous context on dispose. |
| [`WindowMessageSender`](WindowMessageSender.cs) | static class | Sends or posts messages, synchronously or on a worker, to a window after re-verifying its captured identity. |
| [`WindowsUiAutomationCapture`](WindowsUiAutomationCapture.cs) | record | Result of a UI Automation capture with element rows, capture time, partial flag and availability notes. |
| [`WindowsUiAutomationElement`](WindowsUiAutomationElement.cs) | record | Detached UI Automation node metadata with tree position, identity, bounds, state, patterns and properties. |
| [`WindowsUiAutomationOptions`](WindowsUiAutomationOptions.cs) | record | Node, depth, timeout and pattern-support budgets for one capture, checked by `Validate`. |
| [`WindowsUiAutomationProperty`](SpecialTypes.cs) | record | One captured UI Automation property with ID, name, value and availability note. |
| [`WindowsUiAutomationUtil`](WindowsUiAutomationUtil.cs) | class | Captures UI Automation metadata through an owned helper process and runs that helper when started with `WorkerArgument`. |
| [`WindowsWindow`](WindowsWindow.cs) | static class | Enumerates windows, reads the window at a point or the pointer, and reads verified bounds without taking ownership. |
| [`WindowsWindowEnumerationScope`](WindowsWindowEnumerationScope.cs) | enum | Flags selecting top-level, descendant and message-only windows for enumeration. |
| [`WindowsWindowInfo`](WindowsWindowInfo.cs) | record | Detached window snapshot with handle, title, class, process, thread, start time, visibility, styles and DPI. |
| [`WindowsWindowLayering`](WindowsWindowLayering.cs) | static class | Finds visible click-through layered overlays above given windows and tests whether a window is one. |
| [`WindowsWindowTarget`](SpecialTypes.cs) | record | A window and its highlight bounds, with empty bounds when the owning process could not be verified. |
| [`Tools.Windows.Windowing`](WindowsWindowUtil.cs) | static class | Instance facade over window queries, verified bounds reads, verified messaging and foreground hand-off, exposed as `Tools.Windows.Windowing`. |
