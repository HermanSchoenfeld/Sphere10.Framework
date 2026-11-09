# Shell

Wraps Windows Shell operations: launching and opening items, file associations, known folders, the Recycle Bin, Shell namespace items and verbs, shortcuts and file icons.

`ShellUtility` (`Tools.Windows.Shell`) is the entry point. It launches targets and URIs, opens directories, Shell parsing names and property sheets, highlights a file in Explorer and starts tools from the system directory. It resolves file associations as `ShellFileAssociationInfo`, reads and redirects known folders, and queries and empties the Recycle Bin. `GetShellFolderItems` reads up to a maximum number of items of a Shell namespace such as `shell:RecycleBinFolder` as a `WindowsShellItemEnumeration`. A selected `WindowsShellItemInfo` can then list its verbs, invoke one verb or delete a Recycle Bin item permanently after it is checked again. Shell work that needs a COM single-threaded apartment runs on an owned STA in the async variants. Once a mutation has been dispatched, cancellation does not hide its result. A `Process` returned by `Launch` is an observation handle that the caller disposes, and disposing it does not end the launched program.

`ShellLink` wraps the COM `IShellLink` object to create, load, edit and save `.lnk` shortcuts. `LoadWithoutResolve` reads the stored fields only, while `Open` can resolve a missing target. `ShellUtility.ReadShortcutAsync` returns the stored fields as `WindowsShortcutInfo` without resolving the target. `FileIcon` extracts a file's Shell icon, display name and type name through `SHGetFileInfo`. `FileIconCache` loads icons from `FileIconLocation` values on a worker and lends cached images at one size. Startup shortcuts are read by the [Startup](../Startup/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FileIcon`](FileIcon.cs) | class | Extracts the Shell icon, display name and type name of a file through `SHGetFileInfo` with selectable flags |
| [`FileIconCache`](FileIconCache.cs) | class | Disposable cache that loads icons for `FileIconLocation` values on a worker within a time budget and lends images at a fixed size |
| [`FileIconLocation`](FileIconLocation.cs) | struct | Record struct of an executable, DLL or icon file path and its icon index or negative resource ID |
| [`ShellFileAssociationInfo`](ShellFileAssociationInfo.cs) | record | Strings the Shell resolves for an extension or ProgID and verb: ProgID, friendly names, executable, command, icon, delegate and app ID, with per-string HRESULTs |
| [`ShellLink`](ShellLink.cs) | class | Disposable `IShellLink` wrapper to create, load, resolve, edit and save `.lnk` shortcuts and read their icons |
| [`ShellUtility`](ShellUtility.cs) | class | `Tools.Windows.Shell` launching, opening, associations, known folders, Recycle Bin, Shell items and verbs, and shortcut creation and reading |
| [`WindowsRecycleBinInfo`](WindowsRecycleBinInfo.cs) | record | Recycle Bin size and item count for one drive root or all drives |
| [`WindowsShellItemEnumeration`](WindowsShellItemEnumeration.cs) | record | Bounded list of Shell items with the native total count and a limited flag |
| [`WindowsShellItemInfo`](WindowsShellItemInfo.cs) | record | One Shell folder item with namespace, parsing name, display name, type, folder flag, size, original directory and deletion date |
