// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Data.SQLite;
using System.Globalization;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate.Dialect;
using NUnit.Framework;
using Sphere10.Framework.Data.NHibernate;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
public class DecimalColumnTypeTests {
	[Test]
	public void ProviderDecimalConventionPreservesExistingSchema() {
		string[] Schema(bool applyConvention) => Fluently.Configure()
			.Database(SQLiteConfiguration.Standard.InMemory())
			.Mappings(mapping => {
				var fluent = mapping.FluentMappings.Add<AmountMap>();
				if (applyConvention)
					fluent.Conventions.Add(new DecimalColumnConvention());
			})
			.BuildConfiguration()
			.GenerateSchemaCreationScript(new SQLiteDialect());
		Assert.That(Schema(true), Is.EqualTo(Schema(false)), "Provider-specific reads must not alter existing decimal column definitions.");
	}

	[TestCase("0.2")]
	[TestCase("0.3")]
	[TestCase("4.90")]
	[TestCase("-12345.6789")]
	public void NumericColumnRetainsDecimalValue(string text) {
		var expected = decimal.Parse(text, CultureInfo.InvariantCulture);
		using var connection = new SQLiteConnection("Data Source=:memory:;");
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText = "CREATE TABLE Sample (Amount NUMERIC); INSERT INTO Sample VALUES (@amount);";
		command.Parameters.AddWithValue("@amount", expected);
		command.ExecuteNonQuery();
		command.CommandText = "SELECT Amount FROM Sample";
		using var reader = command.ExecuteReader();
		Assert.That(reader.Read(), Is.True);
		Assert.That(new DecimalColumnType().Get(reader, 0, null), Is.EqualTo(expected));
	}

	#region Inner Types

	public class AmountEntity {
		public virtual int Id { get; set; }
		public virtual decimal Amount { get; set; }
		public virtual decimal? OptionalAmount { get; set; }
	}

	public class AmountMap : ClassMap<AmountEntity> {
		public AmountMap() {
			Id(entity => entity.Id);
			Map(entity => entity.Amount).Precision(12).Scale(4);
			Map(entity => entity.OptionalAmount).Precision(18).Scale(6);
		}
	}

	#endregion
}
