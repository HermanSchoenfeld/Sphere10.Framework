// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Npgsql;

namespace Sphere10.Framework.Data;

public class PostgreSQLDAC : DACBase {

	public PostgreSQLDAC(string connectionString, ILogger logger = null)
		: base(connectionString, logger) {
	}

	public override DBMSType DBMSType => DBMSType.PostgreSQL;

	public override IDbConnection CreateConnection() {
		return new NpgsqlConnection(ConnectionString);
	}

	public override ISQLBuilder CreateSQLBuilder() {
		return new PostgreSQLBuilder();
	}

	public override void EnlistInSystemTransaction(IDbConnection connection, Transaction transaction) {
		var npgsqlConnection = connection as NpgsqlConnection;
		if (npgsqlConnection == null)
			throw new ArgumentException("Not an NpgsqlConnection", nameof(connection));
		npgsqlConnection.EnlistTransaction(transaction);
	}

	public override void BulkInsert(DataTable table, BulkInsertOptions bulkInsertOptions, TimeSpan timeout) {
		throw new NotImplementedException();
	}

	protected override DataTable GetDenormalizedTableDescriptions() {
		return this.ExecuteQuery(
			@"SELECT 
	c.table_name AS ""TableName"",
	c.column_name AS ""ColumnName"",
	c.ordinal_position AS ""Position"",
	UPPER(c.data_type) AS ""Type"",
	COALESCE(c.character_maximum_length, 0) AS ""Length"",
	COALESCE(c.numeric_precision, 0)::int AS ""Precision"",
	COALESCE(c.numeric_scale, 0)::int AS ""Scale"",
	CASE WHEN c.is_nullable = 'YES' THEN '1' ELSE '0' END AS ""IsNullable"",
	CASE WHEN tc_uq.constraint_name IS NOT NULL AND tc_pk.constraint_name IS NULL THEN tc_uq.constraint_name ELSE NULL END AS ""UniqueName"",
	tc_pk.constraint_name AS ""PrimaryKeyName"",
	CASE WHEN c.column_default LIKE 'nextval(%' THEN '1' ELSE '0' END AS ""IsAutoIncrement"",
	NULL AS ""Sequence"",
	tc_fk.constraint_name AS ""ForeignKeyName"",
	ccu_fk.table_name AS ""ReferenceTableName"",
	ccu_fk.column_name AS ""ReferenceColumnName"",
	CASE WHEN rc.update_rule = 'CASCADE' THEN '1' ELSE '0' END AS ""CascadeUpdate"",
	CASE WHEN rc.delete_rule = 'CASCADE' THEN '1' ELSE '0' END AS ""CascadeDelete""
FROM 
	information_schema.columns c
	LEFT JOIN information_schema.key_column_usage kcu ON c.table_schema = kcu.table_schema AND c.table_name = kcu.table_name AND c.column_name = kcu.column_name
	LEFT JOIN information_schema.table_constraints tc_pk ON kcu.constraint_name = tc_pk.constraint_name AND kcu.table_schema = tc_pk.table_schema AND tc_pk.constraint_type = 'PRIMARY KEY'
	LEFT JOIN information_schema.table_constraints tc_uq ON kcu.constraint_name = tc_uq.constraint_name AND kcu.table_schema = tc_uq.table_schema AND tc_uq.constraint_type = 'UNIQUE'
	LEFT JOIN information_schema.table_constraints tc_fk ON kcu.constraint_name = tc_fk.constraint_name AND kcu.table_schema = tc_fk.table_schema AND tc_fk.constraint_type = 'FOREIGN KEY'
	LEFT JOIN information_schema.referential_constraints rc ON tc_fk.constraint_name = rc.constraint_name AND tc_fk.table_schema = rc.constraint_schema
	LEFT JOIN information_schema.constraint_column_usage ccu_fk ON rc.unique_constraint_name = ccu_fk.constraint_name AND rc.unique_constraint_schema = ccu_fk.constraint_schema
WHERE 
	c.table_schema = 'public'
ORDER BY c.table_name, c.ordinal_position"
		);
	}

	protected override DataTable GetDenormalizedTriggerDescriptions() {
		return this.ExecuteQuery(@"SELECT trigger_name AS ""Name"" FROM information_schema.triggers WHERE trigger_schema = 'public'");
	}
}
