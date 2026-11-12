# Peripherals

Platform-neutral keyboard and mouse event models, key and scan codes, and the device-hook contracts that native hooks implement.

The folder defines the data and contracts only. `IKeyboardHook` and `IMouseHook` raise `KeyEvent` and `MouseEvent` subclasses, and `IActiveApplicationMonitor` reports the foreground process name that each event carries. `BaseKeyboardHook` and `BaseMouseHook` turn raw input into those events. The mouse base derives motion start, motion and motion stop from movement and `MovingStoppedInterval`, and tracks click and motion-start positions. `IMouseHook.ShouldIntercept` and `IKeyboardHook.ShouldIntercept` let a callback consume input before other applications see it. The Windows implementations, `WindowsKeyboardHook`, `WindowsMouseHook` and `PollDrivenActiveApplicationMonitor`, are in the [Windows project](../../Sphere10.Framework.Windows/README.md).

A hook's lifecycle is `InstallHook`, `StartHook`, `StopHook`, `DisableHook(timespan)` and `UninstallHook`, reflected in `DeviceHookStatus`. `StartHook` installs the hook when needed, and disposing a hook stops and uninstalls it. `ProcessAsyncronously`, which defaults to `true`, makes the hooks raise events asynchronously instead of on the native input callback. Keep `ShouldIntercept` callbacks immediate, and pair a consumed button press with its release.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Key`](Key.cs) | enum | Virtual key identifiers for keyboard events. |
| [`KeyEvent`](KeyEvent.cs) | class | Keyboard event arguments with process name, character, key, scan code, state, modifier flags and time. |
| [`KeyState`](KeyState.cs) | enum | Key down, key up or unknown. |
| [`MouseButton`](MouseButton.cs) | enum | Flags for the left, right, middle and two extra mouse buttons. |
| [`MouseButtonState`](MouseButtonState.cs) | enum | Button down, button up or none. |
| [`MouseClickEvent`](MouseClickEvent.cs) | class | Mouse event with the clicked buttons, their state and the click type. |
| [`MouseClickType`](MouseClickType.cs) | enum | No click, single, double or triple click. |
| [`MouseEvent`](MouseEvent.cs) | class | Base mouse event arguments with process name, screen position and time. |
| [`MouseMotionType`](MouseMotionType.cs) | enum | Motion started, moving or stopped. |
| [`MouseMoveEvent`](MouseMoveEvent.cs) | class | Mouse movement event with motion type, distances since motion start, last event and last click, and an optional monotonic timestamp. |
| [`MouseWheelEvent`](MouseWheelEvent.cs) | class | Mouse event carrying the wheel delta. |
| [`ScanCode`](ScanCode.cs) | enum | Hardware keyboard scan codes as 16-bit values. |

### Hooks

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationChangedEvent`](Hooks/ApplicationChangedEvent.cs) | class | Event arguments for a foreground application switch with target and source process names, switch time and the source's open duration. |
| [`BaseDeviceHook`](Hooks/BaseDeviceHook.cs) | abstract class | Hook lifecycle with status tracking, timed disabling, a finalizer and disposal that stops and uninstalls the hook. |
| [`BaseKeyboardHook`](Hooks/BaseKeyboardHook.cs) | abstract class | Keyboard hook base that converts raw key input into key down, key up and activity events with the active application name. |
| [`BaseMouseHook`](Hooks/BaseMouseHook.cs) | abstract class | Mouse hook base that converts raw input into click, scroll, activity and motion start, motion and stop events, with optional button interception. |
| [`DeviceHookStatus`](Hooks/DeviceHookStatus.cs) | enum | Hook is uninstalled, disabled or active. |
| [`IActiveApplicationMonitor`](Hooks/IActiveApplicationMonitor.cs) | interface | Reports the active application name and raises `ApplicationChanged` on a switch. |
| [`IDeviceHook`](Hooks/IDeviceHook.cs) | interface | Disposable hook with status, asynchronous processing flag and install, start, stop, disable and uninstall operations. |
| [`IKeyboardHook`](Hooks/IKeyboardHook.cs) | interface | Device hook raising key down, key up and key activity events, with an interception callback. |
| [`IMouseHook`](Hooks/IMouseHook.cs) | interface | Device hook raising motion, click, scroll and activity events, exposing positions, simulating button input and consuming button input through a `ShouldIntercept` callback. |
| [`NoOpActiveApplicationMonitor`](Hooks/NoOpActiveApplicationMonitor.cs) | class | Application monitor that never raises events and reports `N/A` as the active application. |
