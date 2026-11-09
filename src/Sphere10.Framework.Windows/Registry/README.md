# Registry

Reads and edits the Windows registry by exact hive, view and key path, and captures bounded key trees with their raw values.

`RegistryUtil` (`Tools.Windows.Registry`) has two API styles. The string-address methods (`OpenKey(host, address)`, `TryOpenKey`, `KeyExists`, `GetSubKeys(host, key)`) open local or remote keys from text such as `HKEY_LOCAL_MACHINE\SOFTWARE\...`, and `TryResolveHive` maps the full hive names. The `RegistryAddress` methods identify a local key by `RegistryHive`, `RegistryView` and sub-key path, and list sub-keys and value names, read `RegistryValue` records, create, rename and delete keys, and set, rename and delete values. Value writes and deletes accept the `RegistryValue` the caller last read and refuse to change a value that no longer matches. Writes through the utility are serialized. Keys returned by `OpenKey` and `OpenRegistryHive` belong to the caller.

`ReadTree` returns a `RegistryTree` whose values and sub-keys are cached futures that open the key only when read. `ReadTreeAsync` materializes the same tree on a worker within a maximum depth, entry count and time limit. A `RegistryValue` keeps the exact value kind, a copy of the raw data with expandable strings unexpanded, and any native read failure. The [Applications](../Applications/README.md) and [Startup](../Startup/README.md) domains read their registrations through this utility, and registry security descriptors are read with `SecurityTool` in the [Security](../Security/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`RegistryAddress`](RegistryAddress.cs) | struct | Record struct identifying a local key by hive, registry view and sub-key path |
| [`RegistrySubTree`](RegistrySubTree.cs) | record | A named child key and its `RegistryTree` contents |
| [`RegistryTree`](RegistryTree.cs) | record | Values and child keys of one key, held as cached futures so they load on first access |
| [`RegistryUtil`](RegistryUtil.cs) | class | `Tools.Windows.Registry` key opening, listing, value reads, guarded key and value edits and bounded tree capture |
| [`RegistryValue`](RegistryValue.cs) | record | One captured value with its address, name, exact kind, copied raw data and read error |
