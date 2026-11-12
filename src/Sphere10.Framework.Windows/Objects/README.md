# NT Object Namespace

Reads NT object-manager directories and symbolic links and decodes object access masks, without changing or following any object.

`Tools.Windows.Objects` lists the entries of an object directory such as `\` or `\Device` with `ReadDirectoryAsync`. The result is a `WindowsObjectDirectory` bounded by an entry count, a retained-character budget (`MaximumDirectoryCharacters` is 2 MiB) and a five-second native query budget. `IsPartial` and `LimitReason` say which limit stopped the read. `ReadSymbolicLinkAsync` returns a symbolic link's target without following it. `DecodeAccessMask` names the standard and type-specific rights of a mask for an object type and keeps every undecoded bit as hexadecimal. Handle-level object information comes from `FileHandleService` in the [Files](../Files/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowsObjectDirectory`](SpecialTypes.cs) | record | Entries of one object directory with its path, partial flag and the reason a limit stopped the read |
| [`WindowsObjectEntry`](SpecialTypes.cs) | record | One object directory entry with name, object type and full path |
| [`Tools.Windows.Objects`](WindowsObjectUtil.cs) | static class | Bounded object-directory listing, symbolic-link reading, access-mask decoding and retained-size accounting |
