# JSON Column Types

Provides NHibernate `IUserType` implementations that store an object as a JSON string column, optionally encrypted.

`JsonColumnType<T>` serializes with `Sphere10FrameworkJsonSerializer`, and `EncryptedJsonColumnTypeBase<T>` wraps that serializer in `EncryptedJsonSerializer` with a password. Both derive from `JsonColumnTypeBase<T>`, which accepts any `IJsonSerializer`. The column SQL type is a string of length `Tools.NHibernate.NVarCharMaxValue`, which NHibernate maps to an unbounded string column. A null or blank stored value reads back as a new `T`, and equality compares the serialized JSON. NHibernate constructs custom types itself, so give an encrypted type a derived class with a parameterless constructor that passes the password.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EncryptedJsonColumnTypeBase<T>`](EncryptedJsonColumnTypeBase.cs) | class | JSON column type that encrypts the serialized text with `EncryptedJsonSerializer` and the given password |
| [`JsonColumnType<T>`](JsonColumnType.cs) | class | JSON column type using `Sphere10FrameworkJsonSerializer` |
| [`JsonColumnTypeBase<T>`](JsonColumnTypeBase.cs) | abstract class | `IUserType` base that reads and writes `T` as JSON through a supplied `IJsonSerializer`, with deep copy by serialization round trip |
