# Attributes

Declarative attributes for default dates, enum error messages and metadata, and file extensions.

These attributes carry data that other code reads through reflection. The `[EncryptedString]` member attribute is in [Sphere10.Framework.Application](../../Sphere10.Framework.Application/Encryption/README.md), with `ObjectEncryptor`, because it keys with the application secret.

`ErrorMessageAttribute` and `MetadataAttribute` annotate enum members. Their static helpers read the text back from an enum value, and `MetadataAttribute.GetValuesWithMetadata` lists an enum's values ordered by their `Sequence`. `DefaultDateAttribute` is a `DefaultValueAttribute` for `DateTime` members. Its offset constructor ignores `nowHourOffset` and passes `nowDayOffset` to `AddSeconds`, so the hour and second offsets have no effect.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DefaultDateAttribute`](DefaultDateAttribute.cs) | class | `DefaultValueAttribute` holding a parsed date string, or the current local or UTC time shifted by year, month, day, minute and millisecond offsets when the attribute is constructed. |
| [`ErrorMessageAttribute`](ErrorMessageAttribute.cs) | class | Attaches an error message to an enum member, with static `HasErrorMessage` and `GetErrorMessage` lookups. |
| [`FileExtensionAttribute`](FileExtensionAttribute.cs) | class | Declares the file extension associated with the decorated type or member. |
| [`MetadataAttribute`](MetadataAttribute.cs) | class | Attaches a description and sort sequence to an enum member, with static helpers that read them and list an enum's values in sequence order. |
