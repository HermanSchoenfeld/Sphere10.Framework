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
using Npgsql;

// ReSharper disable CheckNamespace
namespace Tools;

public static class PostgreSQL {

	private const string CheckDatabaseExistsQuery = "SELECT CASE WHEN EXISTS(SELECT 1 FROM pg_database WHERE datname = '{0}') THEN 1 ELSE 0 END";

	public static PostgreSQLDAC Open(string host, string database, string username = null, string password = null, int? port = null, bool? pooling = null, int? minPoolSize = null, int? maxPoolSize = null,
	                                 TimeSpan? connectionTimeout = null, TimeSpan? commandTimeout = null, string searchPath = null, string applicationName = null, bool? sslMode = null, ILogger logger = null) {
		return new PostgreSQLDAC(
			CreateConnectionString(host, database, username, password, port, pooling, minPoolSize, maxPoolSize, connectionTimeout, commandTimeout, searchPath, applicationName, sslMode),
			logger
		);
	}

	public static bool Exists(string host, string databaseName, string username = null, string password = null, int? port = null) {
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));

		if (string.IsNullOrWhiteSpace(databaseName))
			throw new ArgumentNullException(nameof(databaseName));

		var connString = CreateConnectionString(host, "postgres", username, password, port);
		var dac = new PostgreSQLDAC(connString);
		using (dac.BeginScope()) {
			return dac.ExecuteScalar<bool>(
				CheckDatabaseExistsQuery.FormatWith(databaseName)
			);
		}
	}

	public static bool TestConnectionString(string connectionString) {
		var dac = new PostgreSQLDAC(connectionString);
		try {
			using (dac.BeginScope()) {
				return true;
			}
		} catch {
			return false;
		}
	}

	public static void CreateDatabase(string host, string databaseName, string username = null, string password = null, int? port = null, AlreadyExistsPolicy existsPolicy = AlreadyExistsPolicy.Error) {
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));

		if (string.IsNullOrWhiteSpace(databaseName))
			throw new ArgumentNullException(nameof(databaseName));

		var connString = CreateConnectionString(host, "postgres", username, password, port);
		var dac = new PostgreSQLDAC(connString);
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
						throw new SoftwareException("Database '{0}' already exists on server '{1}'", databaseName, host);
				}
			} else {
				shouldCreate = true;
			}

			if (shouldDrop)
				dac.ExecuteNonQuery(string.Format("DROP DATABASE \"{0}\"", databaseName));

			if (shouldCreate)
				dac.ExecuteNonQuery(string.Format("CREATE DATABASE \"{0}\"", databaseName));
		}
	}

	public static void DropDatabase(string host, string databaseName, string username = null, string password = null, int? port = null, bool throwIfNotExists = true) {
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));

		if (string.IsNullOrWhiteSpace(databaseName))
			throw new ArgumentNullException(nameof(databaseName));

		var connString = CreateConnectionString(host, "postgres", username, password, port);
		var dac = new PostgreSQLDAC(connString);
		using (dac.BeginScope()) {
			var exists = dac.ExecuteScalar<bool>(CheckDatabaseExistsQuery.FormatWith(databaseName));

			if (exists) {
				// Terminate existing connections
				dac.ExecuteNonQuery(string.Format("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{0}' AND pid <> pg_backend_pid()", databaseName));
				dac.ExecuteNonQuery(string.Format("DROP DATABASE \"{0}\"", databaseName));
			} else if (throwIfNotExists) {
				throw new SoftwareException("Unable to drop database '{0}' as it did not exist", databaseName);
			}
		}
	}

	public static string CreateConnectionString(string host = null, string database = null, string username = null, string password = null, int? port = null, bool? pooling = null, int? minPoolSize = null, int? maxPoolSize = null,
	                                            TimeSpan? connectionTimeout = null, TimeSpan? commandTimeout = null, string searchPath = null, string applicationName = null, bool? sslMode = null) {
		var builder = new NpgsqlConnectionStringBuilder();

		if (!string.IsNullOrWhiteSpace(host))
			builder.Host = host;

		if (!string.IsNullOrWhiteSpace(database))
			builder.Database = database;

		if (!string.IsNullOrWhiteSpace(username))
			builder.Username = username;

		if (!string.IsNullOrWhiteSpace(password))
			builder.Password = password;

		if (port.HasValue)
			builder.Port = port.Value;

		if (pooling.HasValue)
			builder.Pooling = pooling.Value;

		if (minPoolSize.HasValue)
			builder.MinPoolSize = minPoolSize.Value;

		if (maxPoolSize.HasValue)
			builder.MaxPoolSize = maxPoolSize.Value;

		if (connectionTimeout.HasValue)
			builder.Timeout = (int)Math.Round(connectionTimeout.Value.TotalSeconds, 0);

		if (commandTimeout.HasValue)
			builder.CommandTimeout = (int)Math.Round(commandTimeout.Value.TotalSeconds, 0);

		if (!string.IsNullOrWhiteSpace(searchPath))
			builder.SearchPath = searchPath;

		if (!string.IsNullOrWhiteSpace(applicationName))
			builder.ApplicationName = applicationName;

		if (sslMode.HasValue)
			builder.SslMode = sslMode.Value ? Npgsql.SslMode.Require : Npgsql.SslMode.Disable;

		return builder.ToString();
	}
}
