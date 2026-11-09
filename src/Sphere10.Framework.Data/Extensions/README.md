# Extensions

Extension methods for `DataRow`, `DataTable`, `DataSet`, `IDataReader`, schema objects and LINQ queries.

These methods convert between ADO.NET objects and ordinary .NET values. `DataRowExtensions.Get<T>` reads a typed cell by name, index or column. `DataReaderExtensions.ToDataTable` and `ToDataTables` materialize a reader. `DataTableExtensions` adds entity rows, primary key setup, foreign key discovery, `Single`, `SingleOrDefault` and CSV output. `IEnumerableExtensions.ToDataTable` turns an object sequence into a table using its public properties.

`DBSchemaExtensions` converts row values to and from the storable string form described by a [schema](../Schema/README.md). `LinqToSqlExtensionMethods.AsHierarchy` builds parent and child trees from a self-referencing `IQueryable`. Tables built here can be bound with [Binding](../Binding/README.md) or written through the [DAC](../DAC/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DataReaderExtensions`](DataReaderExtensions.cs) | static class | Loads an `IDataReader` into one `DataTable` or into one table per result set. |
| [`DataRowExtensions`](DataRowExtensions.cs) | static class | Reads typed values from a `DataRow` by column name, index or `DataColumn`, singly or several at once. |
| [`DataSetExtensions`](DataSetExtensions.cs) | static class | Finds dependent columns, sets `DataSetDateTime` mode on every table and writes a `DataSet` to an XML string. |
| [`DataTableExtensions`](DataTableExtensions.cs) | static class | Adds entities as rows, sets primary keys, lists foreign key columns, returns single rows and formats a table as CSV. |
| [`DBSchemaExtensions`](DBSchemaExtensions.cs) | static class | Converts values between object and storable string form and builds multi-key dictionaries from schema-described rows. |
| [`IEnumerableExtensions`](IEnumerableExtensions.cs) | static class | Converts an object sequence into a `DataTable` with one column per public property. |
| [`LinqToSqlExtensionMethods`](LinqToSqlExtensions.cs) | static class | `AsHierarchy` overloads that build `HierarchyNode<TEntity>` trees from a self-referencing `IQueryable`. |
