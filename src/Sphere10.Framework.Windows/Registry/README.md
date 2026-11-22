# Registry

Reads and edits the Windows registry by exact hive, view and key path, and captures bounded key trees with their raw values.

`RegistryUtil` (`Tools.Windows.Registry`) has two API styles. The string-address methods (`OpenKey(host, address)`, `TryOpenKey`, `KeyExists`, `GetSubKeys(host, key)`) open local or remote keys from text such as `HKEY_LOCAL_MACHINE\SOFTWARE\...`, and `TryResolveHive` maps the full hive names. The `RegistryAddress` methods identify a local key by `RegistryHive`, `RegistryView` and sub-key path, and list sub-keys and value names, read `RegistryValue` records, create, rename and delete keys, and set, rename and delete values. Value writes and deletes accept the `RegistryValue` the caller last read and refuse to change a value that no longer matches. Writes through the utility are serialized. Keys returned by `OpenKey` and `OpenRegistryHive` belong to the caller.

`ReadTree` returns a `RegistryTree` whose values and sub-keys are cached futures that open the key only when read. `ReadTreeAsync` materializes the same tree on a worker within a maximum depth, entry count and time limit. A `RegistryValue` keeps the exact value kind, a copy of the raw data with expandable strings unexpanded, and any native read failure. The [Applications](../Applications/README.md) and [Startup](../Startup/README.md) domains read their registrations through this utility, and registry security descriptors are read with `SecurityTool` in the [Security](../Security/README.md) domain.

`LoadHive(filePath, name)` and `LoadHiveAsync` load a hive file, such as the `NTUSER.DAT` of a user who is not signed in, as the sub-key `name` of `HKEY_USERS` and return a `RegistryHiveMount`. Its `Address` is the loaded hive's root key, so the `RegistryAddress` methods read and edit it like any other key. Loading requires an elevated administrator, and the backup and restore privileges are enabled for the call only. Windows refuses a hive that is already loaded, which is the case for the user hive of anyone signed in, so read that hive at `HKEY_USERS\SID` instead. `ReadUserProfiles` in the [Security](../Security/README.md) domain lists the profile hives and whether each is loaded. While a hive is loaded Windows holds its file and that user cannot sign in, so unload it as soon as the work is done. `Unload` and `UnloadAsync` write the hive's changes to the file and release it. Windows refuses to unload while any key of the hive is open, so dispose every key first. `Unload` throws on that refusal and leaves the hive loaded, whereas disposing the mount tries once and does not throw.

```csharp
// On a worker thread, elevated
using var mount = Tools.Windows.Registry.LoadHive(@"C:\Users\Alice\NTUSER.DAT", "Alice-Offline");
var run = mount.Address with { SubKeyPath = mount.Name + @"\Software\Microsoft\Windows\CurrentVersion\Run" };
foreach (var name in Tools.Windows.Registry.GetValueNames(run))
	Console.WriteLine(name);
mount.Unload();
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`RegistryAddress`](RegistryAddress.cs) | struct | Record struct identifying a local key by hive, registry view and sub-key path |
| [`RegistryHiveMount`](RegistryHiveMount.cs) | class | A hive file loaded as a sub-key of `HKEY_USERS`, with its `Name`, `FilePath`, root `Address`, `IsLoaded`, and `Unload` or disposal to release it |
| [`RegistrySubTree`](RegistrySubTree.cs) | record | A named child key and its `RegistryTree` contents |
| [`RegistryTree`](RegistryTree.cs) | record | Values and child keys of one key, held as cached futures so they load on first access |
| [`RegistryUtil`](RegistryUtil.cs) | class | `Tools.Windows.Registry` key opening, listing, value reads, guarded key and value edits, hive file loading and bounded tree capture |
| [`RegistryValue`](RegistryValue.cs) | record | One captured value with its address, name, exact kind, copied raw data and read error |
