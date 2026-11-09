# JSON

JSON serialization through `Tools.Json` and a small `IJsonSerializer` abstraction with decorator and encryption support.

`Tools.Json` reads and writes objects with Newtonsoft.Json, to and from strings, files and text readers or writers, and writes indented output. `IJsonSerializer` is the injectable form with generic `Serialize` and `Deserialize`. `Sphere10FrameworkJsonSerializer` implements it with `Tools.Json`, `SerializerDecorator` forwards to an inner serializer, and `EncryptedJsonSerializer` uses the decorator to AES-encrypt the JSON text with a password through `Tools.Crypto`.

The file overloads of `Tools.Json` use different default encodings. `WriteToFile` defaults to `Encoding.UTF8`, which writes a byte-order mark. `ReadFromFile` defaults to `Encoding.Unicode` and relies on that mark, so read a UTF-8 file without a byte-order mark with an explicit encoding. XML serialization with the same shape lives in [XML](../XML/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EncryptedJsonSerializer`](EncryptedJsonSerializer.cs) | class | `IJsonSerializer` decorator that AES-encrypts serialized JSON with a password and decrypts before deserializing. |
| [`IJsonSerializer`](IJsonSerializer.cs) | interface | Contract for generic JSON `Serialize<T>` to string and `Deserialize<T>` from string. |
| [`SerializerDecorator`](SerializerDecorator.cs) | class | Forwards `IJsonSerializer` calls to an inner serializer through virtual members. |
| [`Sphere10FrameworkJsonSerializer`](Sphere10JsonSerializer.cs) | class | `IJsonSerializer` implemented with `Tools.Json`. |
| [`Tools.Json`](JsonTool.cs) | tools | Newtonsoft.Json read and write helpers for strings, files, readers and writers. |
