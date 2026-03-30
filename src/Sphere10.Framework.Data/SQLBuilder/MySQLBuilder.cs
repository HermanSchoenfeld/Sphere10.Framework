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

public class MySQLBuilder : SQLBuilderBase {

	public override ISQLBuilder BeginTransaction() {
		return Emit("START TRANSACTION").EndOfStatement(SQLStatementType.TCL);
	}

	public override ISQLBuilder CommitTransaction() {
		return Emit("COMMIT").EndOfStatement(SQLStatementType.TCL);
	}

	public override ISQLBuilder RollbackTransaction() {
		return Emit("ROLLBACK").EndOfStatement(SQLStatementType.TCL);
	}

	public override ISQLBuilder ObjectName(string objectName) {
		if (objectName.StartsWith("`"))
			return Emit(objectName);
		return Emit("`").Emit(objectName).Emit("`");
	}

	public override ISQLBuilder DisableAutoIncrementID(string table) {
		// MySQL allows explicit values in auto_increment columns without needing to disable
		return this;
	}

	public override ISQLBuilder EnableAutoIncrementID(string table) {
		// MySQL allows explicit values in auto_increment columns without needing to enable
		return this;
	}

	public override ISQLBuilder NextSequenceValue(string sequenceName) {
		throw new NotSupportedException("MySQL does not support sequences (prior to 8.0.18 consider auto_increment)");
	}

	public override ISQLBuilder GetLastIdentity(string hint = null) {
		return Emit("LAST_INSERT_ID()");
	}

	public override ISQLBuilder VariableName(string variableName) {
		return Emit("@").Emit(variableName);
	}

	public override ISQLBuilder DeclareVariable(string variableName, Type type) {
		// MySQL uses SET @var = value; no explicit declaration needed
		return this;
	}

	public override ISQLBuilder AssignVariable(string variableName, object value) {
		return Emit("SET @").Emit(variableName).Emit(" = ").Literal(value).EndOfStatement();
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
		return new MySQLBuilder();
	}
}
