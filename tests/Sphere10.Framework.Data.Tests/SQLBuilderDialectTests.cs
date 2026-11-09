// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SQLBuilderDialectTests {

	[Test]
	public void Firebird_SelectWithLimitAndOffset_EmitsFirstAndSkipBeforeDistinct() {
		var builder = new FirebirdSQLBuilder();

		builder.Select("USERS", columns: new object[] { "NAME" }, distinct: true, limit: 10, offset: 20);

		Assert.That(builder.ToString(), Is.EqualTo("SELECT FIRST 10 SKIP 20 DISTINCT \"NAME\" FROM \"USERS\""));
	}

	[Test]
	public void Firebird_ByteArrayLiteral_EmitsBinaryStringLiteral() {
		var builder = new FirebirdSQLBuilder();

		builder.Literal(new byte[] { 0x0A, 0xFF });

		Assert.That(builder.ToString(), Is.EqualTo("X'0AFF'").IgnoreCase);
	}

	[Test]
	public void MSSQL_SelectWithDistinctAndLimit_EmitsTopAfterDistinct() {
		var builder = new MSSQLBuilder();

		builder.Select("Users", columns: new object[] { "Name" }, distinct: true, limit: 10, orderByClause: "Name");

		Assert.That(builder.ToString(), Is.EqualTo("SELECT DISTINCT TOP 10 [Name] FROM [Users] ORDER BY Name"));
	}

	[TestCase(true, "ON")]
	[TestCase(false, "OFF")]
	public void MSSQL_AutoIncrementToggle_ChecksTheGivenTable(bool disable, string setting) {
		var builder = new MSSQLBuilder();

		if (disable)
			builder.DisableAutoIncrementID("Users");
		else
			builder.EnableAutoIncrementID("Users");

		Assert.That(builder.ToString().Trim(), Is.EqualTo($"IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = object_id('[Users]') AND is_identity = 1) SET IDENTITY_INSERT [Users] {setting};"));
	}

	[Test]
	public void MySQL_SelectWithLimitAndOffset_EmitsPagingAfterOrderBy() {
		var builder = new MySQLBuilder();

		builder.Select("Users", columns: new object[] { "Name" }, distinct: true, limit: 10, offset: 20, orderByClause: "Name");

		Assert.That(builder.ToString(), Is.EqualTo("SELECT DISTINCT `Name` FROM `Users` ORDER BY Name LIMIT 10 OFFSET 20"));
	}

	[Test]
	public void PostgreSQL_SelectWithLimitAndOffset_EmitsPagingAfterOrderBy() {
		var builder = new PostgreSQLBuilder();

		builder.Select("Users", columns: new object[] { "Name" }, limit: 10, offset: 20, orderByClause: "Name");

		Assert.That(builder.ToString(), Is.EqualTo("SELECT \"Name\" FROM \"Users\" ORDER BY Name LIMIT 10 OFFSET 20"));
	}

	[Test]
	public void Oracle_SelectWithLimitAndOffset_EmitsOffsetFetchAfterOrderBy() {
		var builder = new OracleSQLBuilder();

		builder.Select("USERS", columns: new object[] { "NAME" }, limit: 10, offset: 20, orderByClause: "NAME");

		Assert.That(builder.ToString(), Is.EqualTo("SELECT \"NAME\" FROM \"USERS\" ORDER BY NAME OFFSET 20 ROWS FETCH FIRST 10 ROWS ONLY"));
	}

	[Test]
	public void Oracle_EndOfStatement_OmitsSemicolon() {
		var builder = new OracleSQLBuilder();

		builder.Delete("USERS", new[] { new ColumnValue("ID", 1) });

		Assert.That(builder.ToString().Trim(), Is.EqualTo("DELETE FROM \"USERS\" WHERE \"ID\" = 1"));
	}
}
