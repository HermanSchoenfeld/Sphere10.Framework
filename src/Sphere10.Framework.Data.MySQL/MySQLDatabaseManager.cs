// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MySqlConnector;

namespace Sphere10.Framework.Data;

public class MySQLDatabaseManager : DatabaseManagerBase {

	public override string GenerateConnectionString(string server, string database, string username, string password, int? port) {
		return Tools.MySQL.CreateConnectionString(server: server, database: database, username: username, password: password, port: port);
	}

	public override bool DatabaseExists(string connectionString) {
		var builder = new MySqlConnectionStringBuilder(connectionString);
		return Tools.MySQL.Exists(builder.Server, builder.Database, builder.UserID, builder.Password, (int)builder.Port);
	}

	public override void CreateApplicationDatabase(string connectionString, DatabaseGenerationDataPolicy dataPolicy, string databaseName) {
		throw new NotSupportedException();
	}

	protected override void DropDatabaseInternal(string connectionString) {
		var builder = new MySqlConnectionStringBuilder(connectionString);
		Tools.MySQL.DropDatabase(builder.Server, builder.Database, builder.UserID, builder.Password, (int)builder.Port);
	}

	protected override void CreateEmptyDatabaseInternal(string connectionString) {
		var builder = new MySqlConnectionStringBuilder(connectionString);
		Tools.MySQL.CreateDatabase(builder.Server, builder.Database, builder.UserID, builder.Password, (int)builder.Port);
	}
}
