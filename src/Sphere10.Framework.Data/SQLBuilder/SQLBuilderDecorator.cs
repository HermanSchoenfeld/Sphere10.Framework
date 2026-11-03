// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Data;

public abstract class SQLBuilderDecorator : ISQLBuilder {
	protected SQLBuilderDecorator(ISQLBuilder internalBuilder) {
		DecoratedBuilder = internalBuilder;
		LeafBuilder = this;
	}

	protected ISQLBuilder DecoratedBuilder { get; }

	public ISQLBuilder LeafBuilder {
		get => DecoratedBuilder.LeafBuilder;
		set => DecoratedBuilder.LeafBuilder = value;
	}

	public virtual ICollection<SQLStatement> CreateContainerForStatements() {
		return DecoratedBuilder.CreateContainerForStatements();
	}

	public virtual ISQLBuilder Clear() {
		DecoratedBuilder.Clear();
		return this;
	}

	public virtual ISQLBuilder NewLine() {
		DecoratedBuilder.NewLine();
		return this;
	}

	public virtual ISQLBuilder End() {
		DecoratedBuilder.End();
		return this;
	}

	public virtual ISQLBuilder BeginTransaction() {
		DecoratedBuilder.BeginTransaction();
		return this;
	}

	public virtual ISQLBuilder CommitTransaction() {
		DecoratedBuilder.CommitTransaction();
		return this;
	}

	public virtual ISQLBuilder RollbackTransaction() {
		DecoratedBuilder.RollbackTransaction();
		return this;
	}

	public virtual ISQLBuilder EndOfStatement(SQLStatementType statementType = SQLStatementType.DML) {
		DecoratedBuilder.EndOfStatement(statementType);
		return this;
	}

	public virtual ISQLBuilder DisableAutoIncrementID(string table) {
		DecoratedBuilder.DisableAutoIncrementID(table);
		return this;
	}

	public virtual ISQLBuilder EnableAutoIncrementID(string table) {
		DecoratedBuilder.EnableAutoIncrementID(table);
		return this;
	}

	public ISQLBuilder CreateTable(TableSpecification tableSpecification) {
		DecoratedBuilder.CreateTable(tableSpecification);
		return this;
	}

	public ISQLBuilder DropTable(string tableName, TableType tableType) {
		DecoratedBuilder.DropTable(tableName, tableType);
		return this;
	}

	public ISQLBuilder SelectValues(IEnumerable<object> values, string whereClause, params object[] whereClauseFormatArgs) {
		DecoratedBuilder.SelectValues(values, whereClause, whereClauseFormatArgs);
		return this;
	}

	public virtual ISQLBuilder Select(string tableName, IEnumerable<object> columns = null, bool distinct = false, int? limit = null, int? offset = null, IEnumerable<ColumnValue> columnMatches = null, string whereClause = null,
	                                  string orderByClause = null, bool endStatement = false) {
		DecoratedBuilder.Select(tableName, columns: columns, distinct: distinct, limit: limit, offset: offset, columnMatches: columnMatches, whereClause: whereClause, orderByClause: orderByClause, endStatement: endStatement);
		return this;
	}

	public virtual ISQLBuilder Insert(string table, IEnumerable<ColumnValue> values) {
		DecoratedBuilder.Insert(table, values);
		return this;
	}

	public ISQLBuilder InsertMany(string table, IEnumerable<string> columns, IEnumerable<IEnumerable<object>> values) {
		DecoratedBuilder.InsertMany(table, columns, values);
		return this;
	}

	public virtual ISQLBuilder Update(string table, IEnumerable<ColumnValue> setColumns, IEnumerable<ColumnValue> matchColumns = null, string whereClause = null) {
		DecoratedBuilder.Update(table, setColumns, matchColumns: matchColumns, whereClause: whereClause);
		return this;
	}

	public virtual ISQLBuilder Delete(string table, IEnumerable<ColumnValue> matchColumns) {
		DecoratedBuilder.Delete(table, matchColumns);
		return this;
	}

	public ISQLBuilder TableName(string tableName, TableType tableType) {
		DecoratedBuilder.TableName(tableName, tableType);
		return this;
	}

	public virtual ISQLBuilder ObjectName(string objectName) {
		DecoratedBuilder.TableName(objectName);
		return this;
	}

	public virtual ISQLBuilder Literal(object value) {
		DecoratedBuilder.Literal(value);
		return this;
	}

	public ISQLBuilder NextSequenceValue(string sequenceName) {
		DecoratedBuilder.NextSequenceValue(sequenceName);
		return this;
	}

	public virtual ISQLBuilder GetLastIdentity(string hint) {
		DecoratedBuilder.GetLastIdentity(hint);
		return this;
	}

	public virtual ISQLBuilder DefaultValues() {
		DecoratedBuilder.DefaultValues();
		return this;
	}

	public virtual ISQLBuilder VariableName(string variableName) {
		DecoratedBuilder.VariableName(variableName);
		return this;
	}

	public virtual ISQLBuilder DeclareVariable(string variableName, Type type) {
		DecoratedBuilder.DeclareVariable(variableName, type);
		return this;
	}

	public virtual ISQLBuilder AssignVariable(string variableName, object value) {
		DecoratedBuilder.AssignVariable(variableName, value);
		return this;
	}

	public virtual ISQLBuilder EmitQueryResultLimit(int limit, int? offset = null) {
		DecoratedBuilder.EmitQueryResultLimit(limit, offset: offset);
		return this;
	}

	public virtual ISQLBuilder EmitDistinct() {
		DecoratedBuilder.EmitDistinct();
		return this;
	}

	public virtual ISQLBuilder Emit(string statement, params object[] formatArgs) {
		DecoratedBuilder.Emit(statement, formatArgs);
		return this;
	}

	public virtual ISQLBuilder TypeToSqlType(Type type, bool preferUnicode = true) {
		DecoratedBuilder.TypeToSqlType(type, preferUnicode);
		return this;
	}

	public virtual ISQLBuilder Cast(SQLBuilderStringValueKind valueKind, object value, Type type) {
		DecoratedBuilder.Cast(valueKind, value, type);
		return this;
	}

	public override string ToString() {
		return DecoratedBuilder.ToString();
	}

	public IEnumerable<SQLStatement> Statements => DecoratedBuilder.Statements;

	public virtual ISQLBuilder CreateBuilder() {
		return DecoratedBuilder.CreateBuilder();
	}

	public virtual ISequentialGuidGenerator CreateSequentialGuidGenerator(long capacity, bool regenerateOnEmpty = false) {
		return DecoratedBuilder.CreateSequentialGuidGenerator(capacity, regenerateOnEmpty);
	}

	public virtual int StatementCount {
		get => DecoratedBuilder.StatementCount;
		set => DecoratedBuilder.StatementCount = value;
	}

	public virtual int VariableDeclarationCount {
		get => DecoratedBuilder.VariableDeclarationCount;
		set => DecoratedBuilder.VariableDeclarationCount = value;
	}

	public string ConvertTypeToSQLType(Type type, bool preferUnicode = true, string maxLen = "4000") {
		return DecoratedBuilder.ConvertTypeToSQLType(type, preferUnicode, maxLen);
	}

	public Type ConvertSQLTypeToType(string sqlType, int? length) {
		return DecoratedBuilder.ConvertSQLTypeToType(sqlType, length);
	}

	public string FormatCreateTable(TableType tableType) {
		return DecoratedBuilder.FormatCreateTable(tableType);
	}

	public string FormatCreateTableEnd(TableType type) {
		return DecoratedBuilder.FormatCreateTableEnd(type);
	}
}

