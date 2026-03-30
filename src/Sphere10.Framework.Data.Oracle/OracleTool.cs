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
using Oracle.ManagedDataAccess.Client;

// ReSharper disable CheckNamespace
namespace Tools;

public static class Oracle {

	public static OracleDAC Open(string dataSource, string serviceName = null, string userId = null, string password = null, int? port = null, bool? pooling = null, int? minPoolSize = null, int? maxPoolSize = null,
	                             TimeSpan? connectionTimeout = null, ILogger logger = null) {
		return new OracleDAC(
			CreateConnectionString(dataSource, serviceName, userId, password, port, pooling, minPoolSize, maxPoolSize, connectionTimeout),
			logger
		);
	}

	public static bool TestConnectionString(string connectionString) {
		var dac = new OracleDAC(connectionString);
		try {
			using (dac.BeginScope()) {
				return true;
			}
		} catch {
			return false;
		}
	}

	public static string CreateConnectionString(string dataSource = null, string serviceName = null, string userId = null, string password = null, int? port = null, bool? pooling = null, int? minPoolSize = null, int? maxPoolSize = null,
	                                            TimeSpan? connectionTimeout = null) {
		var builder = new OracleConnectionStringBuilder();

		if (!string.IsNullOrWhiteSpace(dataSource)) {
			if (!string.IsNullOrWhiteSpace(serviceName)) {
				var actualPort = port ?? 1521;
				builder.DataSource = string.Format("(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={2})))", dataSource, actualPort, serviceName);
			} else {
				builder.DataSource = dataSource;
			}
		}

		if (!string.IsNullOrWhiteSpace(userId))
			builder.UserID = userId;

		if (!string.IsNullOrWhiteSpace(password))
			builder.Password = password;

		if (pooling.HasValue)
			builder.Pooling = pooling.Value;

		if (minPoolSize.HasValue)
			builder.MinPoolSize = minPoolSize.Value;

		if (maxPoolSize.HasValue)
			builder.MaxPoolSize = maxPoolSize.Value;

		if (connectionTimeout.HasValue)
			builder.ConnectionTimeout = (int)Math.Round(connectionTimeout.Value.TotalSeconds, 0);

		return builder.ToString();
	}
}
