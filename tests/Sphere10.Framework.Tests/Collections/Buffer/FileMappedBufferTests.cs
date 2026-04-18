// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.IO;
using Sphere10.Framework.NUnit;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FileMappedBufferTests {

	[Test]
	public void SingleByteFile_Save() {
		var expected = new byte[] { 127 };
		var fileName = Tools.FileSystem.GetTempFileName(true);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From( fileName, 1, 1))) {

				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(0));
				binaryFile.AddRange(expected);

				// Check page
				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].StartIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].EndIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Dirty, Is.True);
			}
			// Check saved
			Assert.That(File.ReadAllBytes(fileName), Is.EqualTo(expected));
		}
	}

	[Test]
	public void SingleByteFile_Load() {
		var expected = new byte[] { 127 };
		var fileName = Tools.FileSystem.GetTempFileName(true);
		Tools.FileSystem.AppendAllBytes(fileName, expected);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From( fileName, 1, 1), FileAccessMode.Read | FileAccessMode.AutoLoad)) {

				// Check page
				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].StartIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].EndIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Dirty, Is.False);

				// Check value
				Assert.That(binaryFile, Is.EqualTo(expected));
				Assert.That(binaryFile.Count, Is.EqualTo(1));
				Assert.That(binaryFile.CalculateTotalSize(), Is.EqualTo(1));
			}
			// Check file unchanged
			Assert.That(File.ReadAllBytes(fileName), Is.EqualTo(expected));
		}
	}

	[Test]
	public void Grow() {
		var preExistingBytes = new byte[] { 127 };
		var appendedBytes = new byte[] { 17 };
		var expected = Tools.Array.Concat<byte>(preExistingBytes, appendedBytes);
		var fileName = Tools.FileSystem.GetTempFileName(true);
		Tools.FileSystem.AppendAllBytes(fileName, preExistingBytes);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From (fileName, 1, 1))) {

				binaryFile.AddRange(appendedBytes);

				// Check pages 1 & 2
				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(2));
				Assert.That(binaryFile.Pages[0].State == PageState.Unloaded, Is.True);
				Assert.That(binaryFile.Pages[0].StartIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].EndIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Dirty, Is.False);

				Assert.That(binaryFile.Pages[1].State == PageState.Loaded, Is.True);
				Assert.That(binaryFile.Pages[1].StartIndex, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].EndIndex, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].Dirty, Is.True);

				// Check value
				Assert.That(binaryFile, Is.EqualTo(expected));
				Assert.That(binaryFile.Count, Is.EqualTo(2));
				Assert.That(binaryFile.CalculateTotalSize(), Is.EqualTo(2));
			}
			// Check file was saved appended
			Assert.That(File.ReadAllBytes(fileName), Is.EqualTo(expected));
		}
	}

	[Test]
	public void NibbleFile_Save() {
		var expected = new byte[] { 127, 17 };
		var fileName = Tools.FileSystem.GetTempFileName(true);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 1, 1))) {

				binaryFile.Add(expected[0]);

				// Check Page 1
				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].State == PageState.Loaded, Is.True);
				Assert.That(binaryFile.Pages[0].StartIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].EndIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Dirty, Is.True);

				// Add new page
				binaryFile.Add(expected[1]);

				// Check pages 1 & 2
				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(2));
				Assert.That(binaryFile.Pages[0].State == PageState.Unloaded, Is.True);
				Assert.That(binaryFile.Pages[0].StartIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].EndIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Dirty, Is.False);

				Assert.That(binaryFile.Pages[1].State == PageState.Loaded, Is.True);
				Assert.That(binaryFile.Pages[1].StartIndex, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].EndIndex, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].Dirty, Is.True);
			}

			// Check saved
			Assert.That(File.ReadAllBytes(fileName), Is.EqualTo(expected));
		}
	}

	[Test]
	public void Rewind() {
		var expected = new byte[] { 127, 17, 18, 19 };
		var fileName = Tools.FileSystem.GetTempFileName(true);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 1, 1))) {

				binaryFile.AddRange<byte>(127, 16, 15, 14, 13);
				binaryFile.RemoveRange(1, 4);
				Assert.That(binaryFile.Count, Is.EqualTo(1));
				Assert.That(binaryFile[0], Is.EqualTo(127));
				binaryFile.AddRange<byte>(17, 18, 19);
				Assert.That(binaryFile.Count, Is.EqualTo(4));
				Assert.That(binaryFile, Is.EqualTo(expected));
			}
			// Check saved
			Assert.That(File.ReadAllBytes(fileName), Is.EqualTo(expected));
		}
	}

	[Test]
	public void NibbleFile_Load() {
		var expected = new byte[] { 127, 17 };
		var fileName = Tools.FileSystem.GetTempFileName(true);
		Tools.FileSystem.AppendAllBytes(fileName, expected);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 1, 1))) {

				// Check pages 1 & 2
				Assert.That(binaryFile.Pages.Count(), Is.EqualTo(2));
				Assert.That(binaryFile.Pages[0].State == PageState.Unloaded, Is.True);
				Assert.That(binaryFile.Pages[0].StartIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[0].EndIndex, Is.EqualTo(0));
				Assert.That(binaryFile.Pages[0].Dirty, Is.False);

				Assert.That(binaryFile.Pages[1].State == PageState.Unloaded, Is.True);
				Assert.That(binaryFile.Pages[1].StartIndex, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].Count, Is.EqualTo(1));
				Assert.That(binaryFile.Pages[1].EndIndex, Is.EqualTo(1));

				// Check values
				Assert.That(binaryFile, Is.EqualTo(expected));
			}
			// Check file unchanged
			Assert.That(File.ReadAllBytes(fileName), Is.EqualTo(expected));
		}
	}

	[Test]
	public void ReadOnly_SinglePage() {
		var expected = Enumerable.Range(0, 8).Select(x => (byte)x).ToArray();
		var fileName = Tools.FileSystem.GetTempFileName(true);
		Tools.FileSystem.AppendAllBytes(fileName, expected);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName)))
		using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From (fileName, 8, 4*8))) {

			for (var i = 0; i < 8; i++)
				Assert.That(binaryFile[i], Is.EqualTo(expected[i]));
		}
	}

	[Test]
	public void ReadOnly_MultiPage() {
		var expected = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
		var fileName = Tools.FileSystem.GetTempFileName(true);
		Tools.FileSystem.AppendAllBytes(fileName, expected);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName)))
		using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 8, 4 * 8))) {

			for (var i = 0; i < 256; i++)
				Assert.That(binaryFile[i], Is.EqualTo(expected[i]));
		}
	}

	[Test]
	public void ReadOnly_Update() {
		var expected = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
		var fileName = Tools.FileSystem.GetTempFileName(true);
		Tools.FileSystem.AppendAllBytes(fileName, System.Linq.Enumerable.Reverse(expected).ToArray());
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			// first load the file and sort them
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 8, 4 * 8))) {

				QuickSorter.Sort(binaryFile, Comparer<byte>.Default);
				for (var i = 0; i < 256; i++)
					Assert.That(binaryFile[i], Is.EqualTo(expected[i]));
			}

			// check file is as expected
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 8, 4 * 8))) {

				for (var i = 0; i < 256; i++)
					Assert.That(binaryFile[i], Is.EqualTo(expected[i]));
			}
		}
	}

	public void GetRandomRange(Random rng, int count, out int startIX, out int endIX) {
		var index1 = rng.Next(0, count);
		var index2 = rng.Next(0, count);
		startIX = Math.Min(index1, index2);
		endIX = Math.Max(index1, index2);
	}

	[Test]
	[Sequential]
	public void IntegrationTests(
		[Values(1, 10, 57, 173, 1111)] int maxCapacity,
		[Values(1, 1, 3, 31, 13)] int pageSize,
		[Values(1, 1, 7, 2, 19)] int maxOpenPages) {
		var fileName = Tools.FileSystem.GetTempFileName(true);
		using (Tools.Scope.ExecuteOnDispose(() => File.Delete(fileName))) {
			using (var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(fileName, pageSize, maxOpenPages * pageSize))) {
				AssertEx.ListIntegrationTest<byte>(binaryFile, maxCapacity, (rng, i) => rng.NextBytes(i), mutateFromEndOnly: true);
			}
		}
	}
}

