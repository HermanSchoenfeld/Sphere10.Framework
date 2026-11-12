# FluentNHibernate Conventions

Provides FluentNHibernate conventions for table naming, foreign-key naming, enum mapping and string and binary column sizes.

Add the conventions an application needs to `FluentMappings.Conventions` when it builds its `FluentConfiguration`, usually inside `NHDatabaseManagerBase.GetFluentConfig` from [Database](../Database/README.md). Each convention is independent, so choose only those that match the target schema. `AnsiStringConvention` and `StringColumnLengthConvention` both act on string properties, and `BinaryColumnLengthConvention` emits the SQL Server type `varbinary(MAX)`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AnsiStringConvention`](AnsiStringConvention.cs) | class | Maps every `string` property with the `AnsiString` custom type, which produces non-Unicode columns |
| [`BinaryColumnLengthConvention`](BinaryColumnLengthConvention.cs) | class | Maps `byte[]` properties with length `int.MaxValue` and SQL type `varbinary(MAX)` |
| [`CoreConventions`](CoreConventions.cs) | class | Names many-to-many tables as the owning entity followed by the plural of the other entity, such as `CustomerOrders` |
| [`DecimalColumnConvention`](DecimalColumnConvention.cs) | class | Selects provider decimal reads for decimal and nullable decimal properties without changing SQL column definitions |
| [`EnumConvention`](EnumConvention.cs) | class | Maps enum and nullable enum properties with their enum type as the custom type, which stores the numeric value |
| [`ForeignKeyConventions`](ForeignKeyConventions.cs) | class | Names foreign-key columns after the property, or the type when there is no property, with an `ID` suffix |
| [`StringColumnLengthConvention`](StringColumnLengthConvention.cs) | class | Gives `string` properties without an explicit length a length of 4000 |
