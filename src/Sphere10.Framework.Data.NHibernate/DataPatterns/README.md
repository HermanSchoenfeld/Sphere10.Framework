# Data Patterns

Provides the enum type-table pattern, an entity and FluentNHibernate map that store one row per enum value.

`TypeTable<TEnum>` is a lookup-table entity with an assigned `ID`, a `Name` and an optional `Description`. Derive a map from `TypeTableMap<TEnum>` to persist it. The table is named after the enum unless `TableName` is overridden, and `UniqueName` controls the unique constraint on `Name`. `NHDataGeneratorBase.CreateTypeTable<TEnum>()` in [Database](../Database/README.md) creates the rows and takes each description from the enum value's description.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`TypeTable`](TypeTable.cs) | abstract class | Non-generic lookup-row base with virtual `ID`, `Name` and `Description` properties |
| [`TypeTable<T>`](TypeTable.cs) | class | Lookup-row entity for the enum type `T` |
| [`TypeTableMap<T>`](TypeTableMap.cs) | abstract class | `ClassMap<TypeTable<T>>` with an assigned `ID`, a non-null `AnsiString` `Name` that is unique by default and a nullable `Description`, adjustable through `TableName` and `UniqueName` |
