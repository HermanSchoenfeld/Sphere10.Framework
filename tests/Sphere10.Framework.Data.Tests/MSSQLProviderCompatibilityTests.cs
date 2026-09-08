// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MSSQLProviderCompatibilityTests {

	[TestCase("Active Directory Password")]
	[TestCase("Active Directory Integrated")]
	[TestCase("Active Directory Interactive")]
	[TestCase("Active Directory Service Principal")]
	[TestCase("Active Directory Device Code Flow")]
	[TestCase("Active Directory Managed Identity")]
	[TestCase("Active Directory MSI")]
	[TestCase("Active Directory Default")]
	[TestCase("Active Directory Workload Identity")]
	public void EntraAuthentication_HasBuiltInProvider(string authentication) {
		var builder = new SqlConnectionStringBuilder($"Authentication={authentication}");

		var provider = SqlAuthenticationProvider.GetProvider(builder.Authentication);

		Assert.That(provider, Is.Not.Null, "The matching SqlClient Azure extension must be available at runtime.");
		Assert.That(provider.IsSupported(builder.Authentication), Is.True);
		Assert.That(provider.GetType().Assembly.GetName().Name, Is.EqualTo("Microsoft.Data.SqlClient.Extensions.Azure"));
	}

	[Test]
	public void CreateConnection_OmittedEncryption_PreservesOptionalEncryption() {
		using var connection = new MSSQLDAC("Server=localhost;Database=Example").CreateConnection();
		var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
		Assert.That(connection, Is.InstanceOf<SqlConnection>());
		Assert.That(builder.Encrypt, Is.EqualTo(SqlConnectionEncryptOption.Optional));
	}

	[TestCase("True")]
	[TestCase("False")]
	[TestCase("Strict")]
	public void CreateConnection_ExplicitEncryption_PreservesRequestedSetting(string encryption) {
		var connectionString = $"Server=localhost;Database=Example;Encrypt={encryption}";
		using var connection = new MSSQLDAC(connectionString).CreateConnection();
		var expected = new SqlConnectionStringBuilder(connectionString);
		var actual = new SqlConnectionStringBuilder(connection.ConnectionString);
		Assert.That(actual.Encrypt, Is.EqualTo(expected.Encrypt));
	}

	[Test]
	public void CreateConnectionString_OmittedEncryption_PreservesOptionalEncryption() {
		var connectionString = Tools.MSSQL.CreateConnectionString(server: "localhost");
		Assert.That(new SqlConnectionStringBuilder(connectionString).Encrypt, Is.EqualTo(SqlConnectionEncryptOption.Optional));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void CreateConnectionString_ExplicitEncryption_PreservesRequestedSetting(bool encryption) {
		var connectionString = Tools.MSSQL.CreateConnectionString(server: "localhost", encrypt: encryption);
		var expected = encryption ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional;
		Assert.That(new SqlConnectionStringBuilder(connectionString).Encrypt, Is.EqualTo(expected));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void CreateConnectionString_LegacyApplicationIntent_PreservesIntent(bool readOnly) {
#pragma warning disable CS0618 // Verify that existing callers can still use the legacy enum contract.
		var legacyIntent = readOnly ? System.Data.SqlClient.ApplicationIntent.ReadOnly : System.Data.SqlClient.ApplicationIntent.ReadWrite;
#pragma warning restore CS0618
		var connectionString = Tools.MSSQL.CreateConnectionString(server: "localhost", applicationIntent: legacyIntent);
		var expected = readOnly ? ApplicationIntent.ReadOnly : ApplicationIntent.ReadWrite;
		Assert.That(new SqlConnectionStringBuilder(connectionString).ApplicationIntent, Is.EqualTo(expected));
	}
}
