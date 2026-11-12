# NT Object Namespace

Reads NT object-manager directories and symbolic links and decodes object access masks, without changing or following any object.

`Tools.Windows.Objects` lists the entries of an object directory such as `\` or `\Device` with `ReadDirectoryAsync`. The result is a `WindowsObjectDirectory` bounded by an entry count, a retained-character budget (`MaximumDirectoryCharacters` is 2 MiB) and a five-second native query budget. `IsPartial` and `LimitReason` say which limit stopped the read. `ReadSymbolicLinkAsync` returns a symbolic link's target without following it. `DecodeAccessMask` names the standard and type-specific rights of a mask for an object type and keeps every undecoded bit as hexadecimal. Handle-level object information comes from `FileHandleService` in the [Files](../Files/README.md) domain.

## Contents

`Tools.Windows.COM` creates typed COM wrappers for Shell links, BITS, WMI and
UI Automation. These explicit wrappers support Native AOT without runtime COM
activation. Dispose returned objects on their owning apartment; a WMI query
owns its connection on the calling thread and its objects must be disposed before
the query. UI Automation uses an MTA and named `PropertyId` values; its wrapper
owns the cache and walker and its returned elements each own their interface.

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.Windows.COM`](COM.cs) | static class | Creates explicit COM wrappers for Native AOT, centralizing activation and generated-wrapper ownership |
| [`WindowsUiAutomationComObject`](WindowsUiAutomationComObject.cs) | class | Owns UI Automation, cache and walker interfaces and returns disposable typed elements with named property IDs |
| [`WindowsVariantMarshaller`](WindowsVariantMarshaller.cs) | static class | Marshals scalar, byte-array and one-dimensional native SAFEARRAY values without runtime-generated COM marshalling |
| [`WindowsObjectDirectory`](SpecialTypes.cs) | record | Entries of one object directory with its path, partial flag and the reason a limit stopped the read |
| [`WindowsObjectEntry`](SpecialTypes.cs) | record | One object directory entry with name, object type and full path |
| [`Tools.Windows.Objects`](WindowsObjectUtil.cs) | static class | Bounded object-directory listing, symbolic-link reading, access-mask decoding and retained-size accounting |
