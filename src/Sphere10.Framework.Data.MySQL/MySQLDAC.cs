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
using MySqlConnector;

namespace Sphere10.Framework.Data;

public class MySQLDAC : DACBase {

	public MySQLDAC(string connectionString, ILogger logger = null)
		: base(connectionString, logger) {
	}

	public override DBMSType DBMSType => DBMSType.MySQL;

	public override IDbConnection CreateConnection() {
		return new MySqlConnection(ConnectionString);
	}

	public override ISQLBuilder CreateSQLBuilder() {
		return new MySQLBuilder();
	}

	public override void EnlistInSystemTransaction(IDbConnection connection, Transaction transaction) {
		var mysqlConnection = connection as MySqlConnection;
		if (mysqlConnection == null)
			throw new ArgumentException("Not a MySqlConnection", nameof(connection));
		mysqlConnection.EnlistTransaction(transaction);
	}

	public override void BulkInsert(DataTable table, BulkInsertOptions bulkInsertOptions, TimeSpan timeout) {
		throw new NotImplementedException();
	}

	protected override DataTable GetDenormalizedTableDescriptions() {
		return this.ExecuteQuery(
			@"SELECT 
	c.TABLE_NAME AS TableName,
	c.COLUMN_NAME AS ColumnName,
	c.ORDINAL_POSITION AS Position,
	UPPER(c.DATA_TYPE) AS Type,
	COALESCE(c.CHARACTER_MAXIMUM_LENGTH, 0) AS Length,
	COALESCE(c.NUMERIC_PRECISION, 0) AS `Precision`,
	COALESCE(c.NUMERIC_SCALE, 0) AS `Scale`,
	CASE WHEN c.IS_NULLABLE = 'YES' THEN '1' ELSE '0' END AS IsNullable,
	tc_uq.CONSTRAINT_NAME AS UniqueName,
	tc_pk.CONSTRAINT_NAME AS PrimaryKeyName,
	CASE WHEN c.EXTRA LIKE '%auto_increment%' THEN '1' ELSE '0' END AS IsAutoIncrement,
	NULL AS Sequence,
	tc_fk.CONSTRAINT_NAME AS ForeignKeyName,
	kcu_fk.REFERENCED_TABLE_NAME AS ReferenceTableName,
	kcu_fk.REFERENCED_COLUMN_NAME AS ReferenceColumnName,
	CASE WHEN rc.UPDATE_RULE = 'CASCADE' THEN '1' ELSE '0' END AS CascadeUpdate,
	CASE WHEN rc.DELETE_RULE = 'CASCADE' THEN '1' ELSE '0' END AS CascadeDelete
FROM 
	information_schema.COLUMNS c
	LEFT JOIN information_schema.KEY_COLUMN_USAGE kcu ON c.TABLE_SCHEMA = kcu.TABLE_SCHEMA AND c.TABLE_NAME = kcu.TABLE_NAME AND c.COLUMN_NAME = kcu.COLUMN_NAME
	LEFT JOIN information_schema.TABLE_CONSTRAINTS tc_pk ON kcu.CONSTRAINT_NAME = tc_pk.CONSTRAINT_NAME AND kcu.TABLE_SCHEMA = tc_pk.TABLE_SCHEMA AND tc_pk.CONSTRAINT_TYPE = 'PRIMARY KEY'
	LEFT JOIN information_schema.TABLE_CONSTRAINTS tc_uq ON kcu.CONSTRAINT_NAME = tc_uq.CONSTRAINT_NAME AND kcu.TABLE_SCHEMA = tc_uq.TABLE_SCHEMA AND tc_uq.CONSTRAINT_TYPE = 'UNIQUE' AND tc_pk.CONSTRAINT_NAME IS NULL
	LEFT JOIN information_schema.TABLE_CONSTRAINTS tc_fk ON kcu.CONSTRAINT_NAME = tc_fk.CONSTRAINT_NAME AND kcu.TABLE_SCHEMA = tc_fk.TABLE_SCHEMA AND tc_fk.CONSTRAINT_TYPE = 'FOREIGN KEY'
	LEFT JOIN information_schema.KEY_COLUMN_USAGE kcu_fk ON tc_fk.CONSTRAINT_NAME = kcu_fk.CONSTRAINT_NAME AND tc_fk.TABLE_SCHEMA = kcu_fk.TABLE_SCHEMA AND kcu_fk.REFERENCED_TABLE_NAME IS NOT NULL AND kcu_fk.COLUMN_NAME = c.COLUMN_NAME
	LEFT JOIN information_schema.REFERENTIAL_CONSTRAINTS rc ON tc_fk.CONSTRAINT_NAME = rc.CONSTRAINT_NAME AND tc_fk.TABLE_SCHEMA = rc.CONSTRAINT_SCHEMA
WHERE 
	c.TABLE_SCHEMA = DATABASE()
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION"
		);
	}

	protected override DataTable GetDenormalizedTriggerDescriptions() {
		return this.ExecuteQuery(@"SELECT TRIGGER_NAME AS Name FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE()");
	}
}
