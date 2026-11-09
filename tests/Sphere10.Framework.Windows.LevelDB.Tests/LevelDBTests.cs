// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Sphere10.Framework.Windows.LevelDB;
// ReSharper disable CheckNamespace

namespace Sphere10.Framework.UnitTests;

[TestFixture]
public class LevelDBTests {
	static string testPath;

	// Each test gets its own directory, so destroying a database never touches files outside it.
	static string CleanTestDB() {
		testPath = Path.Combine(Path.GetTempPath(), "Sphere10.Framework.Windows.LevelDB.Tests", Guid.NewGuid().ToString("N"));
		return testPath;
	}

	[TearDown]
	public void DeleteTestDB() {
		if (testPath != null && Directory.Exists(testPath))
			Directory.Delete(testPath, true);
		testPath = null;
	}

	[Test]
	public void Intro() {
		using (var database = new DB(CleanTestDB(), new Options() { CreateIfMissing = true })) {
			database.Put("key1", "value1");
			Assert.That(database.Get("key1"), Is.EqualTo("value1"));
			Assert.That(database.Get("key1") != null, Is.True);
			database.Delete("key1");
			Assert.That(database.Get("key1") != null, Is.False);
			Assert.That(database.Get("key1"), Is.Null);
		}
	}

	[Test]
	public void TestOpen() {
		Assert.That(() => {
				var path = CleanTestDB();
				using (var db = new DB(path, new Options { CreateIfMissing = true })) {
				}

				using (var db = new DB(path, new Options { ErrorIfExists = true })) {
				}
			},
			Throws.TypeOf<LevelDBException>()
		);
	}

	[Test]
	public void TestCRUD() {
		var path = CleanTestDB();

		using (var db = new DB(path, new Options { CreateIfMissing = true })) {
			db.Put("Tampa", "green");
			db.Put("London", "red");
			db.Put("New York", "blue");

			Assert.That("green", Is.EqualTo(db.Get("Tampa")));
			Assert.That("red", Is.EqualTo(db.Get("London")));
			Assert.That("blue", Is.EqualTo(db.Get("New York")));

			db.Delete("New York");

			Assert.That(db.Get("New York"), Is.Null);

			db.Delete("New York");
		}
	}

	[Test]
	public void TestRepair() {
		TestCRUD();
		DB.Repair(testPath, new Options());
	}

	[Test]
	public void TestIterator() {
		var path = CleanTestDB();

		using (var db = new DB(path, new Options { CreateIfMissing = true })) {
			db.Put("Tampa", "green");
			db.Put("London", "red");
			db.Put("New York", "blue");

			var expected = new[] { "London", "New York", "Tampa" };

			var actual = new List<string>();
			using (var iterator = db.CreateIterator(new ReadOptions())) {
				iterator.SeekToFirst();
				while (iterator.IsValid()) {
					var key = iterator.GetStringKey();
					actual.Add(key);
					iterator.Next();
				}
			}

			Assert.That(actual, Is.EqualTo(expected));

		}
	}

	[Test]
	public void TestEnumerable() {
		var path = CleanTestDB();

		using (var db = new DB(path, new Options { CreateIfMissing = true })) {
			db.Put("Tampa", "green");
			db.Put("London", "red");
			db.Put("New York", "blue");

			var expected = new[] { "London", "New York", "Tampa" };
			var actual = from kv in db as IEnumerable<KeyValuePair<string, string>>
			             select kv.Key;

			Assert.That(actual.ToArray(), Is.EqualTo(expected));
		}
	}

	[Test]
	public void TestSnapshot() {
		var path = CleanTestDB();

		using (var db = new DB(path, new Options { CreateIfMissing = true })) {
			db.Put("Tampa", "green");
			db.Put("London", "red");
			db.Delete("New York");

			using (var snapShot = db.CreateSnapshot()) {
				var readOptions = new ReadOptions { Snapshot = snapShot };

				db.Put("New York", "blue");

				Assert.That("green", Is.EqualTo(db.Get("Tampa", readOptions)));
				Assert.That("red", Is.EqualTo(db.Get("London", readOptions)));

				// Snapshot taken before key was updates
				Assert.That(db.Get("New York", readOptions), Is.Null);
			}

			// can see the change now
			Assert.That("blue", Is.EqualTo(db.Get("New York")));

		}
	}

	[Test]
	public void TestGetProperty() {
		var path = CleanTestDB();

		using (var db = new DB(path, new Options { CreateIfMissing = true })) {
			var r = new Random(0);
			var data = "";
			for (var i = 0; i < 1024; i++) {
				data += 'a' + r.Next(26);
			}

			for (int i = 0; i < 5 * 1024; i++) {
				db.Put(string.Format("row{0}", i), data);
			}

			var stats = db.PropertyValue("leveldb.stats");

			Assert.That(stats, Is.Not.Null);
			Assert.That(stats.Contains("Compactions"), Is.True);
		}
	}

	[Test]
	public void TestWriteBatch() {
		var path = CleanTestDB();

		using (var db = new DB(path, new Options { CreateIfMissing = true })) {
			db.Put("NA", "Na");

			using (var batch = new WriteBatch()) {
				batch.Delete("NA")
					.Put("Tampa", "Green")
					.Put("London", "red")
					.Put("New York", "blue");
				db.Write(batch);
			}

			var expected = new[] { "London", "New York", "Tampa" };
			var actual = from kv in db as IEnumerable<KeyValuePair<string, string>>
			             select kv.Key;

			Assert.That(actual.ToArray(), Is.EqualTo(expected));
		}
	}

	[Test]
	public void TestWriteBatchIterate() {
		var puts = new List<string>();
		var deletes = new List<string>();
		using (var batch = new WriteBatch()) {
			batch.Put("Tampa", "Green")
				.Delete("London")
				.Put("Paris", "blue");

			batch.Iterate(IntPtr.Zero,
				(state, key, keyLength, value, valueLength) => puts.Add($"{Marshal.PtrToStringUTF8(key, (int)keyLength)}={Marshal.PtrToStringUTF8(value, (int)valueLength)}"),
				(state, key, keyLength) => deletes.Add(Marshal.PtrToStringUTF8(key, (int)keyLength)));
		}

		Assert.That(puts, Is.EqualTo(new[] { "Tampa=Green", "Paris=blue" }));
		Assert.That(deletes, Is.EqualTo(new[] { "London" }));
	}

	[Test]
	public void TestSnapshotDisposedAfterItsDB() {
		var path = CleanTestDB();
		var db = new DB(path, new Options { CreateIfMissing = true });
		var snapShot = db.CreateSnapshot();
		db.Dispose();

		Assert.That(() => snapShot.Dispose(), Throws.Nothing);
		GC.KeepAlive(db);
	}
}

