# Encryption

Attribute-driven encryption of string fields and properties with the application secret, used by encrypted settings.

Mark a string field or property with `[EncryptedString]`, then call `ObjectEncryptor.EncryptMembers(obj)` before storing the object and `ObjectEncryptor.DecryptMembers(obj)` after loading it. Both rewrite every marked public and non-public instance member of the object in place, and neither descends into nested objects. A member with two encryption attributes, or whose type cannot hold a string, throws `SoftwareException`. Settings providers whose `EncryptSettings` is `true` make these calls, as [Settings](../Settings/README.md#encryption) describes. The types are in the `Sphere10.Framework.Application` namespace.

`EncryptedStringAttribute` encrypts with AES through `Tools.Crypto.EncryptStringAES`, keyed by the static `ApplicationSharedSecret`. The Application module sets that secret from the entry assembly's [`AssemblyApplicationSecretAttribute`](../Attributes/README.md) when it initializes, and encrypting or decrypting a non-empty value without a secret throws `InvalidOperationException`. The salt is the `Pepper` (default `"00000000"`) unless `Policy` adds machine values: `EncryptionSaltPolicy` flags append the MAC address (through `Tools.Network` from [Sphere10.Framework.Communications](../../Sphere10.Framework.Communications/Network/README.md)), the executable's creation time, the machine name, the user name, the operating-system version or the processor count. A machine-bound salt makes stored values unreadable on another machine or after that value changes. Derive from the attribute and override its `Get*` hooks to supply other salt values.

```csharp
using Sphere10.Framework.Application;

public class Credentials {
	public string UserName { get; set; }

	[EncryptedString(Policy = EncryptionSaltPolicy.Custom | EncryptionSaltPolicy.MachineName)]
	public string Password { get; set; }
}

EncryptedStringAttribute.ApplicationSharedSecret ??= "your-application-secret"; // the Application module sets it from AssemblyApplicationSecret
var credentials = new Credentials { UserName = "admin", Password = "s3cret" };
ObjectEncryptor.EncryptMembers(credentials); // Password now holds the cipher text
ObjectEncryptor.DecryptMembers(credentials); // Password is "s3cret" again
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EncryptedStringAttribute`](EncryptedStringAttribute.cs) | class | Marks a string member for AES encryption with the application secret, and computes the salt from its `Policy` and `Pepper`. |
| [`EncryptionSaltPolicy`](EncryptionSaltPolicy.cs) | enum | Flags that select the salt sources: `None`, `MACAddress`, `ExecutableFileCreationTime`, `OperatingSystemVersion`, `MachineName`, `UserName`, `ProcessorCount` and `Custom` (the pepper). |
| [`ObjectEncryptor`](ObjectEncryptor.cs) | static class | `EncryptMembers` and `DecryptMembers`, which rewrite an object's `[EncryptedString]` fields and properties in place. |
