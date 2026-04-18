// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using System.IO;
namespace Sphere10.Framework.Data.Tests;

[TestFixture]
public class SqliteToolTest {
	[Test]
	public void ExistsByFilePath_False() {
		Assert.That(Tools.Sqlite.ExistsByPath(GenerateTempFilename()), Is.False);
	}

	[Test]
	public void ExistsByFilePath_True() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path);
			Assert.That(Tools.Sqlite.ExistsByPath(path), Is.True);
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}


	[Test]
	public void Create() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path);
			System.Console.WriteLine("File Size is " + Tools.FileSystem.GetFileSize(path));
			Assert.That(File.Exists(path), Is.True);
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Create_Skip() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path);
			Assert.DoesNotThrow(() => Tools.Sqlite.Create(path, existsPolicy: AlreadyExistsPolicy.Skip));
			Assert.That(File.Exists(path), Is.True);
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Create_Overwrite() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path);
			Assert.DoesNotThrow(() => Tools.Sqlite.Create(path, existsPolicy: AlreadyExistsPolicy.Overwrite));
			Assert.That(File.Exists(path), Is.True);
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Create_Throws() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path);
			Assert.Catch<Exception>(() => Tools.Sqlite.Create(path));
			Assert.That(File.Exists(path), Is.True);
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Ignore("Dependency issue")]
	[Test]
	public void Create_Password() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path, "password");
			Assert.Catch<Exception>(() => {
				using (var x = Tools.Sqlite.Open(path, "wrong password").BeginScope(true)) {
					var y = ((SqliteDAC)x.DAC).SelectSqliteMaster();
				}
			});
			Assert.DoesNotThrow(() => {
				using (var x = Tools.Sqlite.Open(path, "password").BeginScope(true)) {
					var y = ((SqliteDAC)x.DAC).SelectSqliteMaster();
				}
			});
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Create_PageSize_4096() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path, pageSize: 4096);
			var pageSize = dac1.ExecuteScalar<long>("PRAGMA page_size;");
			Assert.That(pageSize, Is.EqualTo(4096));
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Create_PageSize_32768() {
		var path = GenerateTempFilename();
		try {
			var dac1 = Tools.Sqlite.Create(path, pageSize: 32768);
			var pageSize = dac1.ExecuteScalar<long>("PRAGMA page_size;");
			Assert.That(pageSize, Is.EqualTo(32768));
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Drop() {
		var path = GenerateTempFilename();
		try {
			var dac = Tools.Sqlite.Create(path);
			Assert.That(Tools.Sqlite.ExistsByPath(path), Is.True);
			Tools.Sqlite.Drop(path);
			Assert.That(Tools.Sqlite.ExistsByPath(path), Is.False);
		} finally {
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void Drop_Throws() {
		Assert.Catch<Exception>(() => Tools.Sqlite.Drop(GenerateTempFilename()));
	}

	[Test]
	public void Drop_NoThrows() {
		Assert.DoesNotThrow(() => Tools.Sqlite.Drop(GenerateTempFilename(), false));
	}

	protected string GenerateTempFilename() {
		string path = Path.GetTempFileName();
		File.Delete(path);
		return path;
	}

}

