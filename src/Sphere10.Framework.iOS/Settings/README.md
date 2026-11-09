# Settings

Legacy settings persistence in `NSUserDefaults`, as a Sphere10 settings provider and as a self-loading settings object.

`UserDefaultsSettingsProvider` is a `SyncSettingsProviderBase` from Sphere10.Framework.Application. It writes each `DefaultValue`-attributed public field and non-indexed property of a settings object under a key built from the settings type's full name, the optional instance ID and the member name, and it calls `Synchronize` after a save. `UserDefaultObject` is the older pattern, a `SettingsObject` whose constructor loads its own `DefaultValue` members when a stored instance exists and whose `Save` writes them back. Members whose type has no native user-defaults representation are stored as XML through `Tools.Xml`, so those types must be serializable by `XmlSerializer`. Both files compile only when `__IOS__` or `MONOMAC` is defined, and the project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`UserDefaultObject`](UserDefaultObject.cs) | abstract class | `SettingsObject` that loads and saves its `DefaultValue`-attributed fields and properties in `NSUserDefaults`. |
| [`UserDefaultsSettingsProvider`](UserDefaultsSettingsProvider.cs) | class | Synchronous settings provider that loads, saves, deletes and clears typed settings in `NSUserDefaults`. |
