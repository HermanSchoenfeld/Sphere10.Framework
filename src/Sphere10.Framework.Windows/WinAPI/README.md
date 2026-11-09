# WinAPI

Declares the Win32 and native Windows functions, structures, constants and COM interfaces that the rest of the library calls, grouped by DLL.

`WinAPI` is one public static partial class with a nested static class per native library, such as `WinAPI.KERNEL32`, `WinAPI.USER32` and `WinAPI.ADVAPI32`. Each nested class holds that library's P/Invoke declarations with their structures, enums, constants and safe handles, such as `WinAPI.KERNEL32.KernelHandle`. Large libraries are split over partial files named after the area they cover, such as `KERNEL32.Diagnostics.cs` for process snapshots. `WinAPI.cs` holds the geometry and window structures shared by several libraries.

This is the lowest of the library's three Windows layers. The domain folders wrap these declarations in typed resource APIs that own handles and buffers and return detached data, and applications build on those. Prefer the domain utilities under `Tools.Windows`, and add a declaration here only when a domain utility needs it. See the [Windows abstraction layers](../../../docs/guidelines/patterns/framework-reuse.md#windows-abstraction-layers) guideline for that boundary.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WinAPI`](WinAPI.cs) | static class | Partial root class of all native declarations, with shared window and geometry structures |
| [`WinAPI.ADVAPI32`](ADVAPI32.cs) | static class | `advapi32.dll`: tokens and privileges, account and SID lookup, logon and impersonation, security descriptors, registry key rename, service configuration, ETW session control and consumption, and Wait Chain Traversal in ADVAPI32.Threading.cs |
| [`WinAPI.COMCTL32`](COMCTL32.cs) | static class | `comctl32.dll` `DllGetVersion` and its version structure |
| [`WinAPI.COREDLL`](COREDLL.cs) | static class | `coredll.dll` `mouse_event` and `keybd_event` declarations |
| [`WinAPI.CRYPT32`](CRYPT32.cs) | static class | The `CERT_CONTEXT` structure used by signature inspection |
| [`WinAPI.DWMAPI`](DWMAPI.cs) | static class | `dwmapi.dll` window attributes and composition timing |
| [`WinAPI.GDI32`](GDI32.cs) | static class | `gdi32.dll` device contexts, bitmaps, brushes, blitting and text colour and mode |
| [`WinAPI.HHCTRL`](HHCTRL.cs) | static class | `hhctrl.ocx` `HtmlHelp` for compiled help files |
| [`WinAPI.IPHLPAPI`](IPHLPAPI.cs) | static class | `iphlpapi.dll` extended TCP and UDP owner tables, `SetTcpEntry` and interface rows |
| [`WinAPI.KERNEL32`](KERNEL32.cs) | static class | `kernel32.dll` processes, threads, memory, files and streams, consoles, libraries and resources, named pipes, device IO and processor groups, with PSS snapshots in KERNEL32.Diagnostics.cs, affinity, CPU Sets and mitigations in KERNEL32.ProcessScheduling.cs and waitable timers in KERNEL32.WaitableTimers.cs |
| [`WinAPI.MAGNIFICATION`](MAGNIFICATION.cs) | static class | `magnification.dll` initialization, magnifier window source, transform, colour effect and filter list, and full-screen transforms |
| [`WinAPI.NETAPI32`](NETAPI32.cs) | static class | `netapi32.dll` user, group, local group, membership and share management and `NetApiBufferFree` |
| [`WinAPI.NTDLL`](NTDLL.cs) | static class | `ntdll.dll` process and system information queries and status conversion, with object-directory, symbolic-link and object queries in NTDLL.Objects.cs |
| [`WinAPI.OLE32`](OLE32.cs) | static class | `ole32.dll` COM initialization and security |
| [`WinAPI.PDH`](PDH.cs) | static class | `pdh.dll` performance counter queries, raw values and value calculation |
| [`WinAPI.POWRPROF`](POWRPROF.cs) | static class | `powrprof.dll` power schemes and settings, power capabilities and information, and suspend state |
| [`WinAPI.PSAPI`](PSAPI.cs) | static class | `psapi.dll` process memory and system performance information, with working-set queries and watches in PSAPI.WorkingSet.cs |
| [`WinAPI.RICHEDIT`](RICHEDIT.cs) | static class | Rich-edit messages, character-format constants and the `CHARFORMAT`, `FORMATRANGE` and related structures |
| [`WinAPI.RSTRTMGR`](RSTRTMGR.cs) | static class | `rstrtmgr.dll` Restart Manager sessions for finding processes that use a file |
| [`WinAPI.SHELL32`](SHELL32.cs) | static class | `shell32.dll` change notification, Recycle Bin, known folders, `ShellExecuteEx` and icon extraction |
| [`WinAPI.SHLWAPI`](SHLWAPI.cs) | static class | `shlwapi.dll` `AssocQueryString` for file associations |
| [`WinAPI.TDH`](TDH.cs) | static class | `tdh.dll` `TdhGetEventInformation` for ETW event metadata |
| [`WinAPI.USER32`](USER32.cs) | static class | `user32.dll` windows, messages, input, hooks, cursors and icons, DPI, display configuration and session exit, with hot keys in [HotKeys.cs](HotKeys.cs), foreground permission in USER32.Foreground.cs and layered windows in [USER32.Layering.cs](USER32.Layering.cs) |
| [`WinAPI.UXTHEME`](UXTHEME.cs) | static class | `uxtheme.dll` visual style theme data and drawing |
| [`WinAPI.WINMM`](WINMM.cs) | static class | `winmm.dll` `PlaySound` and its flags |
| [`WinAPI.WINTRUST`](WINTRUST.cs) | static class | `wintrust.dll` `WinVerifyTrust` and signer and certificate chain helpers |
| [`WinAPI.WTSAPI32`](WTSAPI32.cs) | static class | `wtsapi32.dll` server handles, session enumeration and information, logoff, disconnect, messages, remote control and user configuration |

### Shared structures

| Type | Kind | Purpose |
|---|---|---|
| [`WinAPI.ICONENTRY`](WinAPI.cs) | struct | One image directory entry of an `.ico` file |
| [`WinAPI.ICONFILE`](WinAPI.cs) | struct | Header of an `.ico` file |
| [`WinAPI.LARGE_INTEGER`](WinAPI.cs) | struct | Native 64-bit integer as low and high 32-bit parts |
| [`WinAPI.POINT`](WinAPI.cs) | struct | Native point in pixel coordinates |
| [`WinAPI.RECT`](WinAPI.cs) | struct | Native rectangle given by its left, top, right and bottom edges |
| [`WinAPI.SIZE`](WinAPI.cs) | struct | Native width and height |
| [`WinAPI.WindowMessageFlags`](WinAPI.cs) | enum | Selected window message and hit-test values, including non-client mouse and edit-control messages |
| [`WinAPI.WINDOWPOS`](WinAPI.cs) | struct | Window position data carried by `WM_WINDOWPOSCHANGING` and `WM_WINDOWPOSCHANGED` |
| [`WinAPI.WindowPosFlags`](WinAPI.cs) | enum | `SetWindowPos` flags |
| [`WinAPI.WindowStyles`](WinAPI.cs) | enum | Window style values, holding the extended style `WS_EX_LAYERED` |
