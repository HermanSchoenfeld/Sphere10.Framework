# Attributes

Declarative attributes for default dates, encrypted string members, enum error messages and metadata, and file extensions.

These attributes carry data that other code reads through reflection. `EncryptedStringAttribute` marks a string field or property for AES encryption. `ObjectEncryptor` in [Objects](../Objects/README.md) applies it to every marked member, and the settings providers in `Sphere10.Framework.Application` use it for stored settings. Encryption needs the static `EncryptedStringAttribute.ApplicationSharedSecret`. The Application module sets it from the entry assembly's `AssemblyApplicationSecret` attribute, and `Encrypt` or `Decrypt` throws `InvalidOperationException` while it is unset. The salt comes from the `EncryptionSaltPolicy` flags in [Crypto](../Crypto/README.md), and `Pepper` is appended to it.

`ErrorMessageAttribute` and `MetadataAttribute` annotate enum members. Their static helpers read the text back from an enum value, and `MetadataAttribute.GetValuesWithMetadata` lists an enum's values ordered by their `Sequence`. `DefaultDateAttribute` is a `DefaultValueAttribute` for `DateTime` members. Its offset constructor ignores `nowHourOffset` and passes `nowDayOffset` to `AddSeconds`, so the hour and second offsets have no effect.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DefaultDateAttribute`](DefaultDateAttribute.cs) | class | `DefaultValueAttribute` holding a parsed date string, or the current local or UTC time shifted by year, month, day, minute and millisecond offsets when the attribute is constructed. |
| [`EncryptedStringAttribute`](EncryptedStringAttribute.cs) | class | Marks a string member for AES encryption with the application shared secret, a policy-derived salt and a pepper, through virtual `Encrypt` and `Decrypt` methods. |
| [`ErrorMessageAttribute`](ErrorMessageAttribute.cs) | class | Attaches an error message to an enum member, with static `HasErrorMessage` and `GetErrorMessage` lookups. |
| [`FileExtensionAttribute`](FileExtensionAttribute.cs) | class | Declares the file extension associated with the decorated type or member. |
| [`MetadataAttribute`](MetadataAttribute.cs) | class | Attaches a description and sort sequence to an enum member, with static helpers that read them and list an enum's values in sequence order. |
