// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Data;

public interface IDatabaseManager {

	event EventHandlerEx<DatabaseCreatedEventArgs> DatabaseCreated;
	event EventHandlerEx<DatabaseSchemasCreatedEventArgs> DatabaseSchemasCreated;
	event EventHandlerEx<string> DatabaseDropped;

	string GenerateConnectionString(string server, string database, string username, string password, int? port);

	bool DatabaseExists(string connectionString);

	void DropDatabase(string connectionString);

	void CreateEmptyDatabase(string connectionString);

	void CreateApplicationDatabase(string connectionString, DatabaseGenerationDataPolicy dataPolicy, string databaseName);
}

