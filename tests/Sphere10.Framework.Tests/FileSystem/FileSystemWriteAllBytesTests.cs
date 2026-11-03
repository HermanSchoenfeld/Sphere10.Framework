// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>Checks file creation, replacement and borrowed-stream ownership in the shared byte writer.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FileSystemWriteAllBytesTests {
	/// <summary>Verifies writes create or truncate files without owning the input.</summary>
	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void WritesCreateOrTruncateFilesWithoutOwningTheInput(bool existing, bool fromStream) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "bytes.bin");
		if (existing)
			File.WriteAllBytes(path, new byte[100]);
		var expected = new byte[] { 1, 2, 3 };
		using var source = new MemoryStream(new byte[] { 9, 8, 1, 2, 3 });
		source.Position = 2;

		if (fromStream)
			Tools.FileSystem.WriteAllBytes(path, source);
		else
			Tools.FileSystem.WriteAllBytes(path, expected);

		Assert.That(File.ReadAllBytes(path), Is.EqualTo(fromStream ? new byte[] { 9, 8, 1, 2, 3 } : expected));
		Assert.That(source.CanRead, Is.True);
		if (fromStream)
			Assert.That(source.Position, Is.EqualTo(2));
	}
}
