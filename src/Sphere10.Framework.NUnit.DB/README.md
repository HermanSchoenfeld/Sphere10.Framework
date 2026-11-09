<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.NUnit.DB

Sphere10.Framework.NUnit.DB provides `DACTestFixture`, an NUnit base fixture for testing data-access code built on Sphere10.Framework.Data. A test opens a database scope that creates a fresh SQLite or SQL Server database and drops it when the scope ends, and the fixture adds assertions that compare table contents.

## NuGet usage

Install `Sphere10.Framework.NUnit.DB` in a .NET 10 test project. The package depends on `Sphere10.Framework.NUnit`, `Sphere10.Framework.Data`, `Sphere10.Framework.Data.Sqlite`, `Sphere10.Framework.Data.MSSQL` and NUnit, which restore automatically. Test discovery still requires the NUnit test adapter and Microsoft.NET.Test.Sdk in the consuming test project.

Inside this repository, `tests/Sphere10.Framework.Data.Tests` references `src/Sphere10.Framework.NUnit.DB/Sphere10.Framework.NUnit.DB.csproj` instead.

## Example

```csharp
using NUnit.Framework;
using Sphere10.Framework.Data;
using Sphere10.Framework.NUnit;

[TestFixture]
public class CustomerTests : DACTestFixture {
	private static TableSpecification CustomerTable => new() {
		Name = "Customer",
		Type = TableType.Persistent,
		Columns = new[] {
			new ColumnSpecification { Name = "ID", Type = typeof(int), DataType = "INTEGER", Nullable = false },
			new ColumnSpecification { Name = "Name", Type = typeof(string), DataType = "VARCHAR(100)", Nullable = true }
		},
		PrimaryKey = new PrimaryKeySpecification { Columns = new[] { "ID" } }
	};

	[Test]
	[TestCaseSource(nameof(DBMS))]
	public void InsertsACustomer(DBMSType dbmsType) {
		using (var dac = EnterCreateDatabaseScope(dbmsType, CustomerTable)) {
			dac.Insert("Customer", new[] { new ColumnValue("ID", 1), new ColumnValue("Name", "Ada") });
			Assert.That(dac.Count("Customer"), Is.EqualTo(1));
		}
	}
}
```

`DBMS` lists `DBMSType.Sqlite` only, so this test runs against a temporary SQLite file. The scope returns a `UnitTestDAC`, which is an `IDAC` decorator that drops the database when it is disposed. `DACTestFixture` and `UnitTestDAC` are in the `Sphere10.Framework.NUnit` namespace.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.NUnit.DB` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`DACTestFixture`](DACTestFixture.cs) | abstract class | NUnit fixture base that creates and drops temporary SQLite or SQL Server databases, duplicates DACs and asserts table row counts and contents. Its `DBMS` case source lists only `DBMSType.Sqlite`. |
| [`UnitTestDAC`](UnitTestDAC.cs) | class | `DACDecorator` that runs its end action, which drops the temporary database, when it is disposed. |
<!-- domains:end -->

## Testing against SQL Server

`EnterCreateDatabaseScope` also accepts `DBMSType.SQLServer`. It then creates a database with a random name on the server that the fixture was constructed with, and it drops that database at the end of the scope. The parameterless constructor uses `localhost`, the `sa` login and an empty password. To use another server, call the `DACTestFixture(mssqlServer, mssqlUser, mssqlPassword)` constructor from your fixture and supply a test-case source that includes `DBMSType.SQLServer`. Other `DBMSType` values throw `NotImplementedException`.

## What's Included

| `DACTestFixture` member | Purpose |
|---|---|
| `DBMS` | The static test-case source, which lists `DBMSType.Sqlite` |
| `EnterCreateDatabaseScope(dbmsType, tables)` | Creates an empty database and the given `TableSpecification` tables, and returns a `UnitTestDAC` that drops the database when disposed |
| `EnterCreateEmptyDatabaseScope(dbmsType)` | Does the same without creating tables |
| `CreateEmptyDatabase`, `DropDatabase` | The virtual create and drop steps behind the scopes |
| `DuplicateDAC(dac)` | Opens a second `IDAC` on the same database, for concurrency tests |
| `AssertSameTableRowCount` | Compares the row counts of a table in two databases |
| `AssertSameTableDataIncludingPK`, `AssertSameTableDataExcludingPK` | Compare a table's data in two databases, with or without the primary-key values, and print the rows that differ. Both need a table with a primary key, and the primary-key columns can be anywhere in the table. |
| `AssertSingleRow`, `AssertManyRows`, `AssertManyRowsInOrder` | Compare a table's rows with expected values, in any order for `AssertManyRows` and in order for the other two |
| `PrintTableData` | Writes a table's rows to the console |

## Dependencies

- **Sphere10.Framework.NUnit**: Core NUnit utilities
- **Sphere10.Framework.Data**: Data access framework
- **Sphere10.Framework.Data.Sqlite** and **Sphere10.Framework.Data.MSSQL**: The providers that the fixture creates databases with
- **NUnit**: NUnit testing framework

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


