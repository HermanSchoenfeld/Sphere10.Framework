# Objects

Reflection-based object cloning, deep comparison, attribute-driven defaults and encryption, and the `Tools.Object` helpers that front them.

`Tools.Object` is the entry point. It creates instances and resolves types by name, converts values with `ChangeType`, clones or copies members shallowly or deeply, compares two object graphs, applies `[DefaultValue]` attributes, and encrypts or decrypts members marked with `EncryptedStringAttribute`. The cloners implement `IObjectCloner`. `ShallowObjectCloner` copies member values, `DeepObjectCloner` recurses through the graph and tracks visited objects with `Reference<T>` keys so that shared and cyclic references are cloned once, and `MobileCompatibleObjectCloner` copies only same-named, assignable fields and readable-writable properties. `DeepObjectComparer` walks two graphs member by member and records each `Difference`, with options to ignore members, collection order, private members and attributes.

`ObjectWithDefaultValues` is a base class whose constructor applies `[DefaultValue]` attributes to its own fields and properties. `Void` is a type with no instances, used as a generic argument where no value exists, as the two-way handshake handlers in [Protocol](../Protocol/README.md) do. `ICloneable<T>` adds typed cloning over `System.ICloneable`. The call-interceptor contracts have no implementation in this repository. Faster member access is in [Introspection](../Introspection/README.md), and type activation and conversion live in [Types](../Types/README.md). The folder also holds `IAsyncLoadable.cs`, `IAsyncSaveable.cs` and `ISaveable.cs`, whose contents are commented out. The live loading and saving contracts are in [Loadable](../Loadable/README.md) and [Saveable](../Saveable/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CallInterceptOption`](CallInterceptOption.cs) | enum | Whether an intercepted call does nothing, returns the intercepted result, throws or returns the default. |
| [`DeepObjectCloner`](DeepObjectCloner.cs) | class | `IObjectCloner` that deep-clones or deep-copies an object graph, preserving shared references and skipping configured types. |
| [`DeepObjectComparer`](DeepObjectComparer.cs) | class | Compares two object graphs through properties, fields, collections, dictionaries and data sets, and reports the differences found. |
| [`ICallInterceptor`](ICallInterceptor.cs) | interface | Interceptor contract exposing the chosen `CallInterceptOption` and methods to cancel with an exception or the default result. |
| [`ICallInterceptor<TResult>`](ICallInterceptorT.cs) | interface | Typed interceptor that can cancel a call with a supplied result and exposes the intercepted result. |
| [`ICloneable<T>`](ICloneableT.cs) | interface | Typed `Clone()` with a default implementation of `System.ICloneable.Clone`. |
| [`IObjectCloner`](IObjectCloner.cs) | interface | Contract to clone an object and to copy members from one object to another. |
| [`InterceptOption`](InterceptOption.cs) | enum | Same choices as `CallInterceptOption`, kept as a separate enum. |
| [`InvalidInterceptedValueException`](InvalidInterceptedValueException.cs) | class | `SoftwareException` raised when an intercepted value does not have the expected type. |
| [`MobileCompatibleObjectCloner`](MobileCompatibleObjectCloner.cs) | class | Shallow `IObjectCloner` that copies matching non-nested fields and readable-writable properties by name. |
| [`ObjectEncryptor`](ObjectEncryptor.cs) | static class | Encrypts or decrypts every string field and property marked with `EncryptedStringAttribute` in place. |
| [`ObjectWithDefaultValues`](ObjectWithDefaultValues.cs) | abstract class | Base class that applies `[DefaultValue]` attributes on construction, with `RestoreDefaultValues` and a static `SetDefaults` for any object. |
| [`ProjectionMemoizer`](ProjectionMemoizer.cs) | class | Thread-safe map that remembers one projection per item and returns and removes it on `ForgetProjection`. It is declared in the global namespace. |
| [`Reference`](Reference.cs) | class | Identity wrapper whose equality is reference equality on the wrapped object, created with `Reference.For`. |
| [`Reference<TObject>`](Reference.cs) | class | Typed identity wrapper with explicit conversions to and from the wrapped object. |
| [`ShallowObjectCloner`](ShallowObjectCloner.cs) | class | `IObjectCloner` that creates a new instance and copies member values without recursing. |
| [`Tools.Object`](ObjectTool.cs) | tools | Creates and resolves types, converts values, clones, copies, compares, applies defaults, encrypts members, binary-serializes objects and formats values as SQL literals. |
| [`Void`](Void.cs) | class | Sealed type with no instances whose `Value` is `null`, used as a placeholder generic argument. |
