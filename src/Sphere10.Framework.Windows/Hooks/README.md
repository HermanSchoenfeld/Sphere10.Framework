# Input Hooks

Installs low-level Windows mouse and keyboard hooks and hooks over the desktop's window events, simulates input and tracks the foreground application.

`Tools.Windows.Hooks` creates fresh `WindowsMouseHook` and `WindowsKeyboardHook` instances and exposes the input operations of `HooksTool`. Getting the utility installs nothing. Each created hook is uninstalled until the caller calls `InstallHook`, and the caller disposes it. The hooks derive the Framework's `BaseMouseHook` and `BaseKeyboardHook` and install `WH_MOUSE_LL` and `WH_KEYBOARD_LL` hooks, so the installing thread needs a message loop. The mouse hook reports that movement has stopped after `DefaultMovingStoppedInterval` (100 ms) unless another interval is given.

Each hook asks an `IActiveApplicationMonitor` which application receives the input. The default `PollDrivenActiveApplicationMonitor` reads the foreground window's process path on each query and raises `ApplicationChanged` when it differs from the previous one. A hook created with a supplied monitor borrows it and does not dispose it. `HooksTool` sends simulated keyboard, mouse-button and wheel input through `SendInput` and checks physical button state with `GetAsyncKeyState`. `VirtualKey` lists the Win32 virtual-key codes, and `Tools.Windows.Win32.VirtualKeyToKey` in the [Misc](../Misc/README.md) domain maps them to the Framework `Key` enum.

`WindowsWindowEventHook` is the hook over accessible window events (WinEvents): windows being created, shown, hidden, moved, reordered, brought to the foreground, minimized, restored or scrolled, in every process. `Tools.Windows.Hooks.CreateWindowEventHook(firstEvent, lastEvent)` creates one over a range of the `EVENT_` constants in `WinAPI.USER32`; a consumer that needs two ranges creates two hooks. The hook is out of context, so Windows queues each event to the installing thread: install, uninstall and dispose it on one thread with a message loop, and `EventRaised` is raised there with a `WindowsWindowEvent` value. A hook sees every event of its range on the whole desktop, thousands a second on a busy one, so a handler rejects what it does not want (usually by `IsWindowObject` and the window handle) before any native call and defers real work until the callback has returned. The [Windowing](../Windowing/README.md) media source and exclusion tracker and the Windows Forms title-bar buttons are built on it.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HooksTool`](HooksTool.cs) | static class | Simulates keyboard, mouse-button and mouse-wheel input with `SendInput` and reports whether any physical mouse button is down |
| [`PollDrivenActiveApplicationMonitor`](PollDrivenActiveApplicationMonitor.cs) | class | `IActiveApplicationMonitor` that resolves the foreground window's executable on each call and raises `ApplicationChanged` on a switch |
| [`VirtualKey`](VirtualKey.cs) | enum | Win32 virtual-key codes for keys and mouse buttons |
| [`Tools.Windows.Hooks`](WindowsHookUtil.cs) | static class | `Tools.Windows.Hooks` factory for caller-owned mouse, keyboard and window-event hooks and the `HooksTool` input operations |
| [`WindowsKeyboardHook`](WindowsKeyboardHook.cs) | class | `BaseKeyboardHook` implementation over a `WH_KEYBOARD_LL` hook that tags events with the active application |
| [`WindowsWindowEvent`](WindowsWindowEvent.cs) | record struct | One accessible window event: its type, window, object and child identifiers, thread and time, with `IsWindowObject` for events about the window itself |
| [`WindowsWindowEventHook`](WindowsWindowEventHook.cs) | class | Out-of-context hook over one range of accessible window events, raised on the installing thread |
| [`WindowsMouseHook`](WindowsMouseHook.cs) | class | `BaseMouseHook` implementation over a `WH_MOUSE_LL` hook with moving-stopped detection and input simulation |
