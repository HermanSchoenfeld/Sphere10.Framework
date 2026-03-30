// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Npgsql;

namespace Sphere10.Framework.Data;

public class PostgreSQLDatabaseManager : DatabaseManagerBase {

	public override string GenerateConnectionString(string server, string database, string username, string password, int? port) {
		return Tools.PostgreSQL.CreateConnectionString(host: server, database: database, username: username, password: password, port: port);
	}

	public override bool DatabaseExists(string connectionString) {
		var builder = new NpgsqlConnectionStringBuilder(connectionString);
		return Tools.PostgreSQL.Exists(builder.Host, builder.Database, builder.Username, builder.Password, builder.Port);
	}

	public override void CreateApplicationDatabase(string connectionString, DatabaseGenerationDataPolicy dataPolicy, string databaseName) {
		throw new NotSupportedException();
	}

	protected override void DropDatabaseInternal(string connectionString) {
		var builder = new NpgsqlConnectionStringBuilder(connectionString);
		Tools.PostgreSQL.DropDatabase(builder.Host, builder.Database, builder.Username, builder.Password, builder.Port);
	}

	protected override void CreateEmptyDatabaseInternal(string connectionString) {
		var builder = new NpgsqlConnectionStringBuilder(connectionString);
		Tools.PostgreSQL.CreateDatabase(builder.Host, builder.Database, builder.Username, builder.Password, builder.Port);
	}
}
