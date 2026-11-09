# Miscellaneous Win32 Helpers

Holds general Win32 helpers that have no better domain: message-parameter parsing, rich-edit and layered-window calls, an HGlobal scope and the Windows exception type.

`Win32Util` (`Tools.Windows.Win32`) splits message parameters with `LOWORD`, `HIWORD`, `GET_WHEEL_DELTA_WPARAM` and `GetMessagePosition`, and maps a `VirtualKey` from the [Hooks](../Hooks/README.md) domain to the Framework `Key` enum. It also wraps `SendMessage` overloads, rich-edit character formatting and `EM_FORMATRANGE` rendering into a bitmap, `UpdateLayeredWindow` from a bitmap, cursor creation from a bitmap and tab-caption updates. The caller owns a cursor handle from `CreateCursorHandle` and releases it with `DestroyCursor`.

`UnmanagedMemoryScope` owns one `Marshal.AllocHGlobal` block and frees it on disposal. Never use it for memory that another allocator such as `LocalFree` or `NetApiBufferFree` must release. `Tools.Scope.EnterUnmanagedMemoryScope` in the [Scopes](../Scopes/README.md) domain creates it. `WindowsException` is the Framework `SoftwareException` for Win32 failures and appends the system message of a Win32 error code.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`UnmanagedMemoryScope`](UnmanagedMemoryScope.cs) | struct | Readonly scope that owns an HGlobal allocation of a given size or a copied Unicode string and frees it on disposal |
| [`Win32Util`](Win32Util.cs) | class | `Tools.Windows.Win32` helpers for message-parameter words, wheel deltas, key mapping, rich-edit formatting, layered windows, cursors and structure marshalling |
| [`WindowsException`](WindowsException.cs) | class | `SoftwareException` for Win32 failures that appends the system description of a Win32 error code |
