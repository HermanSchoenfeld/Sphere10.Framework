// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;
using Sphere10.Framework.Data;
using MySqlConnector;

// ReSharper disable CheckNamespace
namespace Tools;

public static class MySQL {

	private const string CheckDatabaseExistsQuery = "SELECT CASE WHEN EXISTS(SELECT 1 FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = '{0}') THEN 1 ELSE 0 END";

	public static MySQLDAC Open(string server, string database, string username = null, string password = null, int? port = null, bool? pooling = null, int? minPoolSize = null, int? maxPoolSize = null,
	                            TimeSpan? connectionTimeout = null, TimeSpan? commandTimeout = null, string characterSet = null, string applicationName = null, bool? sslMode = null, ILogger logger = null) {
		return new MySQLDAC(
			CreateConnectionString(server, database, username, password, port, pooling, minPoolSize, maxPoolSize, connectionTimeout, commandTimeout, characterSet, applicationName, sslMode),
			logger
		);
	}

	public static bool Exists(string server, string databaseName, string username = null, string password = null, int? port = null) {
		if (string.IsNullOrWhiteSpace(server))
			throw new ArgumentNullException(nameof(server));

		if (string.IsNullOrWhiteSpace(databaseName))
			throw new ArgumentNullException(nameof(databaseName));

		var connString = CreateConnectionString(server, "information_schema", username, password, port);
		var dac = new MySQLDAC(connString);
		using (dac.BeginScope()) {
			return dac.ExecuteScalar<bool>(
				CheckDatabaseExistsQuery.FormatWith(databaseName)
			);
		}
	}

	public static bool TestConnectionString(string connectionString) {
		var dac = new MySQLDAC(connectionString);
		try {
			using (dac.BeginScope()) {
				return true;
			}
		} catch {
			return false;
		}
	}

	public static void CreateDatabase(string server, string databaseName, string username = null, string password = null, int? port = null, AlreadyExistsPolicy existsPolicy = AlreadyExistsPolicy.Error) {
		if (string.IsNullOrWhiteSpace(server))
			throw new ArgumentNullException(nameof(server));

		if (string.IsNullOrWhiteSpace(databaseName))
			throw new ArgumentNullException(nameof(databaseName));

		var connString = CreateConnectionString(server, "information_schema", username, password, port);
		var dac = new MySQLDAC(connString);
		using (dac.BeginScope()) {
			var alreadyExists = dac.ExecuteScalar<bool>(CheckDatabaseExistsQuery.FormatWith(databaseName));

			var shouldDrop = false;
			var shouldCreate = false;
			if (alreadyExists) {
				switch (existsPolicy) {
					case AlreadyExistsPolicy.Skip:
						break;
					case AlreadyExistsPolicy.Overwrite:
						shouldDrop = true;
						shouldCreate = true;
						break;
					case AlreadyExistsPolicy.Error:
					default:
						throw new SoftwareException("Database '{0}' already exists on server '{1}'", databaseName, server);
				}
			} else {
				shouldCreate = true;
			}

			if (shouldDrop)
				dac.ExecuteNonQuery(string.Format("DROP DATABASE `{0}`", databaseName));

			if (shouldCreate)
				dac.ExecuteNonQuery(string.Format("CREATE DATABASE `{0}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", databaseName));
		}
	}

	public static void DropDatabase(string server, string databaseName, string username = null, string password = null, int? port = null, bool throwIfNotExists = true) {
		if (string.IsNullOrWhiteSpace(server))
			throw new ArgumentNullException(nameof(server));

		if (string.IsNullOrWhiteSpace(databaseName))
			throw new ArgumentNullException(nameof(databaseName));

		var connString = CreateConnectionString(server, "information_schema", username, password, port);
		var dac = new MySQLDAC(connString);
		using (dac.BeginScope()) {
			var exists = dac.ExecuteScalar<bool>(CheckDatabaseExistsQuery.FormatWith(databaseName));

			if (exists) {
				dac.ExecuteNonQuery(string.Format("DROP DATABASE `{0}`", databaseName));
			} else if (throwIfNotExists) {
				throw new SoftwareException("Unable to drop database '{0}' as it did not exist", databaseName);
			}
		}
	}

	public static string CreateConnectionString(string server = null, string database = null, string username = null, string password = null, int? port = null, bool? pooling = null, int? minPoolSize = null, int? maxPoolSize = null,
	                                            TimeSpan? connectionTimeout = null, TimeSpan? commandTimeout = null, string characterSet = null, string applicationName = null, bool? sslMode = null) {
		var builder = new MySqlConnectionStringBuilder();

		if (!string.IsNullOrWhiteSpace(server))
			builder.Server = server;

		if (!string.IsNullOrWhiteSpace(database))
			builder.Database = database;

		if (!string.IsNullOrWhiteSpace(username))
			builder.UserID = username;

		if (!string.IsNullOrWhiteSpace(password))
			builder.Password = password;

		if (port.HasValue)
			builder.Port = (uint)port.Value;

		if (pooling.HasValue)
			builder.Pooling = pooling.Value;

		if (minPoolSize.HasValue)
			builder.MinimumPoolSize = (uint)minPoolSize.Value;

		if (maxPoolSize.HasValue)
			builder.MaximumPoolSize = (uint)maxPoolSize.Value;

		if (connectionTimeout.HasValue)
			builder.ConnectionTimeout = (uint)Math.Round(connectionTimeout.Value.TotalSeconds, 0);

		if (commandTimeout.HasValue)
			builder.DefaultCommandTimeout = (uint)Math.Round(commandTimeout.Value.TotalSeconds, 0);

		if (!string.IsNullOrWhiteSpace(characterSet))
			builder.CharacterSet = characterSet;

		if (!string.IsNullOrWhiteSpace(applicationName))
			builder.ApplicationName = applicationName;

		if (sslMode.HasValue)
			builder.SslMode = sslMode.Value ? MySqlSslMode.Required : MySqlSslMode.None;

		return builder.ToString();
	}
}
