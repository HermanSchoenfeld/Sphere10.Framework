# SQL Builder

A fluent, dialect-aware SQL script builder with provider dialects, decorators for batching, paging and table-based variables, and command and specification objects.

`ISQLBuilder` accumulates `SQLStatement` values through chained calls such as `Select`, `Insert`, `Update`, `Delete`, `CreateTable`, `BeginTransaction`, `DeclareVariable` and `Emit`, and `ToString` returns the script. `SQLBuilderBase` holds the statement list and the ANSI forms. The dialect classes override quoting, identity handling, sequences, transactions, type mapping and the position of `LIMIT`/`TOP` fragments (`SQLQueryResultLimitPosition`). Obtain the builder for a connection from `IDAC.CreateSQLBuilder` in [DAC](../DAC/README.md) rather than constructing a dialect directly.

`SQLBuilderDecorator` forwards every member to an inner builder. `BatchSQLBuilder` inserts a delimiter after each batch of statements and returns the script through `ToStringBatches`, and its `ToString` throws. `VeryLargeSQLBuilder` stores statements in paged `LargeCollection` containers and must be disposed. `SQLBuilderWithTableBasedVariables` emulates variables with a temporary table for DBMSs without script variables. `SQLBuilderCommand` and its sealed subclasses capture builder calls as objects that can be replayed later. The `Expressions` subfolder holds a SQL expression and table-specification model with per-DBMS hints. The model of an existing database, read through `IDAC.GetSchema`, is in [Schema](../Schema/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ANSI2003BaseSQLBuilder`](ANSI2003SQLBuilder.cs) | class | ANSI SQL:2003 builder that rejects transaction, identity-insert, sequence, last-identity and variable-name calls. |
| [`BatchSQLBuilder`](BatchSQLBuilder.cs) | class | Decorator that splits the script into batches of a fixed statement count, read with `ToStringBatches`. |
| [`CastCommand`](SQLBuilderCommand.cs) | class | Command that emits a cast of a literal, SQL fragment or variable to a .NET type. |
| [`ColumnNameCommand`](SQLBuilderCommand.cs) | class | Command that emits a quoted column name. |
| [`ColumnSpecification`](ColumnSpecification.cs) | class | Column name, .NET type, SQL data type and nullability used by `CreateTable`. |
| [`ColumnValue`](ColumnValue.cs) | class | Column name and value pair used for insert values, update assignments and match clauses. |
| [`EndOfStatementCommand`](SQLBuilderCommand.cs) | class | Command that ends the current statement with a given `SQLStatementType`. |
| [`FirebirdSQLBuilder`](FirebirdSQLBuilder.cs) | class | Firebird dialect with a configurable statement terminator. |
| [`GenericActionCommand`](SQLBuilderCommand.cs) | class | Command that runs an arbitrary `Action<ISQLBuilder>`. |
| [`ISQLBuilder`](ISQLBuilder.cs) | interface | Fluent contract for building DDL, DML, transaction and variable statements and reading the resulting script. |
| [`ISQLBuilderExtensions`](ISQLBuilder.cs) | static class | Adds `TableName`, `ColumnName`, `IndexName`, `TriggerName`, `InsertMany`, `Select`, `InsertSelect` and `QuickString` overloads. |
| [`LastIdentityCommand`](SQLBuilderCommand.cs) | class | Command that emits the dialect's last-inserted-identity expression. |
| [`LiteralCommand`](SQLBuilderCommand.cs) | class | Command that emits a formatted literal value. |
| [`MSSQLBuilder`](MSSQLBuilder.cs) | class | SQL Server dialect with `TOP` placement, identity-insert switching and `#` temporary and `@` in-memory table names. |
| [`MySQLBuilder`](MySQLBuilder.cs) | class | MySQL dialect with end-of-query `LIMIT` placement. |
| [`NextSequenceValueCommand`](SQLBuilderCommand.cs) | class | Command that emits the next value of a named sequence. |
| [`OracleSQLBuilder`](OracleSQLBuilder.cs) | class | Oracle dialect with sequence support and end-of-query result limits. |
| [`PostgreSQLBuilder`](PostgreSQLBuilder.cs) | class | PostgreSQL dialect with sequence support and end-of-query result limits. |
| [`PrimaryKeySpecification`](PrimaryKeySpecification.cs) | class | Primary key column list for a `TableSpecification`. |
| [`SQLBuilderBase`](SQLBuilderBase.cs) | abstract class | Statement accumulation, formatting and ANSI defaults shared by all dialect builders. |
| [`SQLBuilderCommand`](SQLBuilderCommand.cs) | abstract class | Deferred builder call with static factories for the command subclasses in the same file. |
| [`SQLBuilderDecorator`](SQLBuilderDecorator.cs) | abstract class | Forwards every `ISQLBuilder` member to a decorated builder. |
| [`SQLBuilderExtensions`](SQLBuilderExtensions.cs) | static class | Builds `Insert` and `Update` statements from a `DataRow`. |
| [`SQLBuilderStringValueKind`](SQLBuilderValueKind.cs) | enum | Whether a string argument is a literal, SQL code, a variable name or detected automatically. |
| [`SQLBuilderWithTableBasedVariables`](SQLBuilderWithTableBasedVariables.cs) | class | Decorator that stores declared variables in a temporary table and drops or clears it on `End`. |
| [`SQLExpressionCommand`](SQLBuilderCommand.cs) | class | Command that emits a formatted SQL expression with a declared result type. |
| [`SqliteSQLBuilder`](SqliteSQLBuilder.cs) | class | Sqlite dialect. |
| [`SQLQueryResultLimitPosition`](SQLQueryResultLimitPosition.cs) | enum | Where a dialect writes the result-limit fragment in a `SELECT`: before or after `DISTINCT`, or at the end. |
| [`SQLStatement`](SQLStatement.cs) | struct | One generated statement with its SQL text and `SQLStatementType`. |
| [`SQLStatementType`](SQLStatementType.cs) | enum | Statement category: DDL, DML, DCL or TCL. |
| [`TableNameCommand`](SQLBuilderCommand.cs) | class | Command that emits a table name for a `TableType`. |
| [`TableSpecification`](TableSpecification.cs) | class | Table name, type, columns and primary key passed to `CreateTable`. |
| [`TriggerNameCommand`](SQLBuilderCommand.cs) | class | Command that emits a quoted trigger name. |
| [`VariableNameCommand`](SQLBuilderCommand.cs) | class | Command that emits a dialect variable reference. |
| [`VeryLargeSQLBuilder`](VeryLargeSQLBuilder.cs) | class | Disposable decorator that keeps statements in paged large collections for very large scripts. |

### Expressions

| Type | Kind | Purpose |
|---|---|---|
| [`BaseSQLSpecificationObject`](Expressions/BaseSQLSpecificationObject.cs) | abstract class | Base for specification objects that carry a list of DBMS-specific `Hint` values. |
| [`ConstraintType`](Expressions/ConstraintType.cs) | enum | Table constraint kinds: primary key, unique, not null, check, default value and collate. |
| [`Hint`](Expressions/Hint.cs) | class | A hint value with the DBMS types it applies to. |
| [`SQL`](Expressions/SQL.cs) | class | Static factories for `SQLExpression` nodes such as names, literals, variables, raw SQL and `IS NULL`. |
| [`SQLExpression`](Expressions/SQLExpression.cs) | class | Immutable expression tree node with operator, child expressions and a typed value. |
| [`SQLExpressionType`](Expressions/SQLExpressionType.cs) | enum | Whether an expression is an inner node or a leaf. |
| [`SQLExpressionValueType`](Expressions/SQLExpressionValueType.cs) | enum | What a leaf value represents: schema, table or column name, literal, SQL text or variable. |
| [`SQLOperator`](Expressions/SQLOperator.cs) | enum | Comparison, arithmetic, bitwise, join, alias and existence operators for expression nodes. |
| [`SQLSortDirection`](Expressions/SQLSortDirection.cs) | enum | Ascending or descending sort order. |
| [`TableSpecification2`](Expressions/TableSpecification2.cs) | class | Table model with schema, typed columns and primary key, unique and check constraints as nested classes. |
| [`TableType`](Expressions/TableType.cs) | enum | Persistent, temporary or in-memory table. |
| [`TextEncoding`](Expressions/TextEncoding.cs) | enum | Column text encodings: UTF-8 and the UTF-16 variants. |
