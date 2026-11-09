# Schema

An in-memory model of a relational database schema with tables, columns, keys, constraints, triggers and dependency analysis.

`IDAC.GetSchema` in [DAC](../DAC/README.md) returns a `DBSchema`, and `SchemaCache` caches it per connection string. `DBSchema` holds `DBTableSchema` tables, `DBTriggerSchema` triggers and the `DBForeignKeySchema` relations between tables. It answers dependency questions such as `GetDependencies`, `GetDependencyList` and `GetTableRoutes`. Each table exposes columns, primary key, foreign keys, unique constraints, blob columns, key roots and its topological order and height in the foreign key graph.

`ArtificialKeys` describes primary keys, foreign keys and unique constraints that the database does not declare. It loads from XML and is applied to a schema with `DBSchema.ApplyArtificialKeys` or by setting `IDAC.ArtificialKeys`. `DBKeyType` flags how a key value is produced, and `DBKeyRoot` records the root table and column that a foreign key chain resolves to. Value conversion over these objects is in [Extensions](../Extensions/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ArtificialKeys`](ArtificialKeys.cs) | class | XML-serializable set of undeclared primary keys, foreign keys and unique constraints to overlay on a schema. |
| [`DBColumnSchema`](DBColumnSchema.cs) | class | Column metadata: position, type, nullability, key membership, uniqueness, auto-increment and sequence use. |
| [`DBForeignKeySchema`](DBForeignKeySchema.cs) | class | Foreign key with its columns, referenced table and columns, nullability and cascade rules. |
| [`DBKeyRoot`](DBKeyRoot.cs) | class | Root table and column that a key resolves to, with its auto-increment and sequence flags. |
| [`DBKeyType`](DBKeyType.cs) | enum | Flags for how a key or its root is produced: auto-calculated, sequenced, manually assigned integer or artificial. |
| [`DBObject`](DBObject.cs) | abstract class | Base for schema objects with a `Name` and defining `SQL`. |
| [`DBPrimaryKeySchema`](DBPrimaryKeySchema.cs) | class | Primary key with its column names, optional sequence and key type. |
| [`DBReference`](DBReference.cs) | struct | XML-serializable pair of `DBMSType` and connection string. |
| [`DBSchema`](DBSchema.cs) | class | Database schema with tables, triggers, relations, artificial key overlay and dependency and route queries. |
| [`DBTableSchema`](DBTableSchema.cs) | class | Table with columns, keys, constraints, blob columns, key roots and topological order. |
| [`DBTriggerSchema`](DBTriggerSchema.cs) | class | Trigger name and defining SQL. |
| [`DBUniqueConstraintSchema`](DBUniqueConstraintSchema.cs) | class | Unique constraint with its columns and key type. |

`ArtificialKeys.Table`, `ArtificialKeys.PrimaryKey`, `ArtificialKeys.ForeignKey`, `ArtificialKeys.UniqueConstraint` and `ArtificialKeys.Column` are public classes nested in `ArtificialKeys` that map its XML elements.
