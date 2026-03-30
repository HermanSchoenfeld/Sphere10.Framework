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
using Oracle.ManagedDataAccess.Client;

namespace Sphere10.Framework.Data;

public class OracleDAC : DACBase {

	public OracleDAC(string connectionString, ILogger logger = null)
		: base(connectionString, logger) {
	}

	public override DBMSType DBMSType => DBMSType.Oracle;

	public override IDbConnection CreateConnection() {
		return new OracleConnection(ConnectionString);
	}

	public override ISQLBuilder CreateSQLBuilder() {
		return new OracleSQLBuilder();
	}

	public override void EnlistInSystemTransaction(IDbConnection connection, Transaction transaction) {
		var oracleConnection = connection as OracleConnection;
		if (oracleConnection == null)
			throw new ArgumentException("Not an OracleConnection", nameof(connection));
		oracleConnection.EnlistTransaction(transaction);
	}

	public override void BulkInsert(DataTable table, BulkInsertOptions bulkInsertOptions, TimeSpan timeout) {
		throw new NotImplementedException();
	}

	protected override DataTable GetDenormalizedTableDescriptions() {
		return this.ExecuteQuery(
			@"SELECT 
	tc.TABLE_NAME AS ""TableName"",
	tc.COLUMN_NAME AS ""ColumnName"",
	tc.COLUMN_ID AS ""Position"",
	UPPER(tc.DATA_TYPE) AS ""Type"",
	NVL(tc.DATA_LENGTH, 0) AS ""Length"",
	NVL(tc.DATA_PRECISION, 0) AS ""Precision"",
	NVL(tc.DATA_SCALE, 0) AS ""Scale"",
	CASE WHEN tc.NULLABLE = 'Y' THEN '1' ELSE '0' END AS ""IsNullable"",
	uc_uq.CONSTRAINT_NAME AS ""UniqueName"",
	uc_pk.CONSTRAINT_NAME AS ""PrimaryKeyName"",
	CASE WHEN tc.IDENTITY_COLUMN = 'YES' THEN '1' ELSE '0' END AS ""IsAutoIncrement"",
	NULL AS ""Sequence"",
	uc_fk.CONSTRAINT_NAME AS ""ForeignKeyName"",
	rcc.TABLE_NAME AS ""ReferenceTableName"",
	rcc.COLUMN_NAME AS ""ReferenceColumnName"",
	'0' AS ""CascadeUpdate"",
	CASE WHEN uc_fk.DELETE_RULE = 'CASCADE' THEN '1' ELSE '0' END AS ""CascadeDelete""
FROM 
	USER_TAB_COLUMNS tc
	LEFT JOIN USER_CONS_COLUMNS ucc ON tc.TABLE_NAME = ucc.TABLE_NAME AND tc.COLUMN_NAME = ucc.COLUMN_NAME
	LEFT JOIN USER_CONSTRAINTS uc_pk ON ucc.CONSTRAINT_NAME = uc_pk.CONSTRAINT_NAME AND uc_pk.CONSTRAINT_TYPE = 'P'
	LEFT JOIN USER_CONSTRAINTS uc_uq ON ucc.CONSTRAINT_NAME = uc_uq.CONSTRAINT_NAME AND uc_uq.CONSTRAINT_TYPE = 'U'
	LEFT JOIN USER_CONSTRAINTS uc_fk ON ucc.CONSTRAINT_NAME = uc_fk.CONSTRAINT_NAME AND uc_fk.CONSTRAINT_TYPE = 'R'
	LEFT JOIN USER_CONS_COLUMNS rcc ON uc_fk.R_CONSTRAINT_NAME = rcc.CONSTRAINT_NAME AND rcc.POSITION = ucc.POSITION
WHERE 
	tc.TABLE_NAME NOT LIKE 'BIN$%'
ORDER BY tc.TABLE_NAME, tc.COLUMN_ID"
		);
	}

	protected override DataTable GetDenormalizedTriggerDescriptions() {
		return this.ExecuteQuery(@"SELECT TRIGGER_NAME AS ""Name"" FROM USER_TRIGGERS");
	}
}
