// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Oracle.ManagedDataAccess.Client;

namespace Sphere10.Framework.Data;

public class OracleDatabaseManager : DatabaseManagerBase {

	public override string GenerateConnectionString(string server, string database, string username, string password, int? port) {
		return Tools.Oracle.CreateConnectionString(dataSource: server, serviceName: database, userId: username, password: password, port: port);
	}

	public override bool DatabaseExists(string connectionString) {
		// Oracle doesn't have databases in the same sense — test connectivity to the service
		return Tools.Oracle.TestConnectionString(connectionString);
	}

	public override void CreateApplicationDatabase(string connectionString, DatabaseGenerationDataPolicy dataPolicy, string databaseName) {
		throw new NotSupportedException();
	}

	protected override void DropDatabaseInternal(string connectionString) {
		throw new NotSupportedException("Oracle database drop is not supported through this interface. Use Oracle administration tools.");
	}

	protected override void CreateEmptyDatabaseInternal(string connectionString) {
		throw new NotSupportedException("Oracle database creation is not supported through this interface. Use Oracle administration tools.");
	}
}
