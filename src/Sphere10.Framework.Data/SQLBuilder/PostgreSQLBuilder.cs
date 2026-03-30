// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Data;

public class PostgreSQLBuilder : SQLBuilderBase {

	public override ISQLBuilder BeginTransaction() {
		return Emit("BEGIN").EndOfStatement(SQLStatementType.TCL);
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
		// PostgreSQL serial/identity columns accept explicit values without needing to disable
		return this;
	}

	public override ISQLBuilder EnableAutoIncrementID(string table) {
		// PostgreSQL serial/identity columns accept explicit values without needing to enable
		return this;
	}

	public override ISQLBuilder NextSequenceValue(string sequenceName) {
		return Emit("nextval('").Emit(sequenceName).Emit("')");
	}

	public override ISQLBuilder GetLastIdentity(string hint = null) {
		return Emit("lastval()");
	}

	public override ISQLBuilder VariableName(string variableName) {
		throw new NotSupportedException("PostgreSQL does not support variables in plain SQL");
	}

	public override ISQLBuilder DeclareVariable(string variableName, Type type) {
		throw new NotSupportedException("PostgreSQL does not support variable declarations in plain SQL");
	}

	public override ISQLBuilder AssignVariable(string variableName, object value) {
		throw new NotSupportedException("PostgreSQL does not support variable assignment in plain SQL");
	}

	public override ISQLBuilder EmitQueryResultLimit(int limit, int? offset = null) {
		Emit("LIMIT ").Emit(limit.ToString());
		if (offset != null)
			Emit(" OFFSET ").Emit(offset.Value.ToString());
		return this;
	}

	public override ISQLBuilder EndOfStatement(SQLStatementType statementType = SQLStatementType.DML) {
		Emit(";").NewLine();
		return base.EndOfStatement(statementType);
	}

	public override ISQLBuilder CreateBuilder() {
		return new PostgreSQLBuilder();
	}
}
