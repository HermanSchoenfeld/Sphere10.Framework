// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Data;
using System.IO;
using NUnit.Framework;
using Sphere10.Framework.Data.Csv;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CsvReaderDisposalTests {
	[Test]
	public void ClosedDataReaderRejectsReadAndNavigation([Values] bool initialize, [Values] bool closeThroughDataReader) {
		using var reader = new CsvReader(new StringReader("First,Second\n1,2\n"), true);
		IDataReader dataReader = reader;
		if (initialize)
			Assert.That(reader.ReadNextRecord(), Is.True);
		var disposals = 0;
		reader.Disposed += (_, _) => disposals++;
		if (closeThroughDataReader)
			dataReader.Close();
		else
			reader.Dispose();

		Assert.That(dataReader.IsClosed, Is.True);
		Assert.That(() => dataReader.Read(), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => dataReader.NextResult(), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => dataReader.Depth, Throws.TypeOf<ObjectDisposedException>());
		Assert.That(reader.ReadNextRecord(), Is.False, "The direct reader retains its end-of-stream shortcut after closure.");
		dataReader.Close();
		reader.Dispose();
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(dataReader.IsClosed, Is.True);
	}

	[Test]
	public void MissingCurrentRecordTakesPrecedenceOverClosedState([Values] bool close) {
		using var reader = new CsvReader(new StringReader("First,Second\n1,2\n"), true);
		IDataReader dataReader = reader;
		if (close)
			dataReader.Close();

		Assert.That(() => dataReader.GetValue(0), Throws.TypeOf<InvalidOperationException>());
		Assert.That(dataReader.IsClosed, Is.EqualTo(close));
	}

	[Test]
	public void InitializedDirectAndDataRecordAccessRejectClosure([Values] bool closeThroughDataReader) {
		using var reader = new CsvReader(new StringReader("First,Second\n1,2\n"), true);
		IDataReader dataReader = reader;
		Assert.That(reader.ReadNextRecord(), Is.True);
		Assert.That(dataReader.GetValue(0), Is.EqualTo("1"));
		if (closeThroughDataReader)
			dataReader.Close();
		else
			reader.Dispose();

		Assert.That(() => dataReader.GetValue(0), Throws.TypeOf<ObjectDisposedException>().With.Property(nameof(ObjectDisposedException.ObjectName)).EqualTo(typeof(CsvReader).FullName));
		Assert.That(() => reader[1], Throws.TypeOf<ObjectDisposedException>().With.Property(nameof(ObjectDisposedException.ObjectName)).EqualTo(typeof(CsvReader).FullName));
	}
}
