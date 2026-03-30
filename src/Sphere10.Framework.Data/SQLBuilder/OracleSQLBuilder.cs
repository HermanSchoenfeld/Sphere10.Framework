// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Data;

public class OracleSQLBuilder : SQLBuilderBase {

	public override ISQLBuilder BeginTransaction() {
		// Oracle implicitly begins transactions; no explicit BEGIN needed
		return this;
	}

	public override ISQLBuilder CommitTransaction() {
		return Emit("COMMIT").EndOfStatement(SQLStatementType.TCL);
	}

	public override ISQLBuilder RollbackTransaction() {
		return Emit("ROLLBACK").EndOfStatement(SQLStatementType.TCL);
	}

	public override ISQLBuilder ObjectName(string objectName) {
		if (objectName.StartsWith("\""))
			return Emit(objectName);
		return Emit("\"").Emit(objectName).Emit("\"");
	}

	public override ISQLBuilder DisableAutoIncrementID(string table) {
		// Oracle identity columns accept explicit values without needing to disable
		return this;
	}

	public override ISQLBuilder EnableAutoIncrementID(string table) {
		// Oracle identity columns accept explicit values without needing to enable
		return this;
	}

	public override ISQLBuilder NextSequenceValue(string sequenceName) {
		return Emit(sequenceName).Emit(".NEXTVAL");
	}

	public override ISQLBuilder GetLastIdentity(string hint = null) {
		throw new NotSupportedException("Oracle does not have a generic last identity function. Use RETURNING clause or sequence.CURRVAL.");
	}

	public override ISQLBuilder VariableName(string variableName) {
		return Emit(":").Emit(variableName);
	}

	public override ISQLBuilder DeclareVariable(string variableName, Type type) {
		throw new NotSupportedException("Oracle variable declarations require PL/SQL blocks");
	}

	public override ISQLBuilder AssignVariable(string variableName, object value) {
		throw new NotSupportedException("Oracle variable assignment requires PL/SQL blocks");
	}

	public override ISQLBuilder EmitQueryResultLimit(int limit, int? offset = null) {
		// Oracle 12c+ supports FETCH FIRST / OFFSET syntax
		if (offset != null)
			Emit("OFFSET ").Emit(offset.Value.ToString()).Emit(" ROWS ");
		Emit("FETCH FIRST ").Emit(limit.ToString()).Emit(" ROWS ONLY");
		return this;
	}

	public override ISQLBuilder EndOfStatement(SQLStatementType statementType = SQLStatementType.DML) {
		Emit(";").NewLine();
		return base.EndOfStatement(statementType);
	}

	public override ISQLBuilder SelectValues(IEnumerable<object> values, string whereClause, params object[] whereClauseFormatArgs) {
		if (!values.Any())
			throw new ArgumentException("values is empty", nameof(values));
		Emit("SELECT ");
		foreach (var value in values.WithDescriptions()) {
			if (value.Index > 0)
				Emit(", ");

			if (value.Item == null) {
				Emit("NULL");
			} else if (value.Item is ColumnValue) {
				var columnValue = value.Item as ColumnValue;
				Emit("{0} AS ", columnValue.Value, SQLBuilderCommand.ColumnName(columnValue.ColumnName));
			} else {
				Literal(value.Item);
			}
		}
		Emit(" FROM DUAL");
		if (!string.IsNullOrEmpty(whereClause)) {
			Emit(" WHERE ").Emit(whereClause, whereClauseFormatArgs);
		}
		return this;
	}

	public override ISQLBuilder CreateBuilder() {
		return new OracleSQLBuilder();
	}
}
