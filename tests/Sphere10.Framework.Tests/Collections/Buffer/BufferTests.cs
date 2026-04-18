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
using Sphere10.Framework.Collections;
using Sphere10.Framework.NUnit;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BufferTests {
	[Test]
	public void SinglePage(
		[Values(StorageType.MemoryPagedBuffer, StorageType.BinaryFile, StorageType.TransactionalBinaryFile)]
		StorageType storageType,
		[Values(1, 2, 10, 57, 173)] int pageSize,
		[Values(1, 2, int.MaxValue)] int maxOpenPages) {
		using (CreateMemPagedBuffer(storageType, pageSize, maxOpenPages * (long)pageSize, out var buffer)) {
			buffer.AddRange(Tools.Array.Gen<byte>(pageSize, 10));
			// Check page
			Assert.That(buffer.Pages.Count(), Is.EqualTo(1));
			Assert.That(buffer.Pages[0].Number, Is.EqualTo(0));
			Assert.That(buffer.Pages[0].MaxSize, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(buffer.Pages[0].Count, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].EndIndex, Is.EqualTo(pageSize - 1));
			Assert.That(buffer.Pages[0].Size, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].Dirty, Is.True);

			// Check value
			Assert.That(buffer[0], Is.EqualTo(10));
			Assert.That(buffer.Count, Is.EqualTo(pageSize));

		}
	}

	[Test]
	public void TwoPages(
		[Values(StorageType.MemoryPagedBuffer, StorageType.BinaryFile, StorageType.TransactionalBinaryFile)]
		StorageType storageType,
		[Values(1, 2, 10, 57, 173)] int pageSize) {

		using (CreateMemPagedBuffer(storageType, pageSize, 1 * pageSize, out var buffer)) {
			buffer.AddRange(Tools.Array.Gen<byte>(pageSize, 10));

			// Check Page 1
			Assert.That(buffer.Pages.Count(), Is.EqualTo(1));
			Assert.That(buffer.Pages[0].State, Is.EqualTo(PageState.Loaded));
			Assert.That(buffer.Pages[0].Number, Is.EqualTo(0));
			Assert.That(buffer.Pages[0].MaxSize, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(buffer.Pages[0].Count, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].EndIndex, Is.EqualTo(pageSize - 1));
			Assert.That(buffer.Pages[0].Size, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].Dirty, Is.True);

			// Add new page
			buffer.AddRange(Tools.Array.Gen<byte>(pageSize, 20));

			// Check pages 1 & 2
			Assert.That(buffer.Pages.Count(), Is.EqualTo(2));
			Assert.That(buffer.Pages[0].State, Is.EqualTo(PageState.Unloaded));
			Assert.That(buffer.Pages[0].Number, Is.EqualTo(0));
			Assert.That(buffer.Pages[0].MaxSize, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(buffer.Pages[0].Count, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[0].EndIndex, Is.EqualTo(pageSize - 1));
			Assert.That(buffer.Pages[0].Size, Is.EqualTo(pageSize));


			Assert.That(buffer.Pages[1].State, Is.EqualTo(PageState.Loaded));
			Assert.That(buffer.Pages[1].Number, Is.EqualTo(1));
			Assert.That(buffer.Pages[1].MaxSize, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[1].StartIndex, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[1].Count, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[1].EndIndex, Is.EqualTo(pageSize * 2 - 1));
			Assert.That(buffer.Pages[1].Size, Is.EqualTo(pageSize));
			Assert.That(buffer.Pages[1].Dirty, Is.True);

			// Check values
			Assert.That(buffer[0], Is.EqualTo(10));
			Assert.That(buffer[pageSize], Is.EqualTo(20));

		}
	}

	[Test]
	public void RemoveAll(
		[Values(StorageType.MemoryPagedBuffer, StorageType.BinaryFile, StorageType.TransactionalBinaryFile)]
		StorageType storageType,
		[Values(1, 2, 10, 57, 173)] int pageSize,
		[Values(1, 2, int.MaxValue)] int maxOpenPages) {
		using (CreateMemPagedBuffer(storageType, pageSize, maxOpenPages * (long)pageSize, out var buffer)) {
			buffer.AddRange(Tools.Array.Gen<byte>(pageSize, 10));
			buffer.RemoveRange(0, buffer.Count);
			Assert.That(buffer.Pages.Count, Is.EqualTo(0));
			Assert.That(buffer.Count, Is.EqualTo(0));
			Assert.That(buffer, Is.EqualTo(Enumerable.Empty<byte>()));
		}
	}

	[Test]
	public void RemoveAllExcept1(
		[Values(StorageType.MemoryPagedBuffer, StorageType.BinaryFile, StorageType.TransactionalBinaryFile)]
		StorageType storageType,
		[Values(2, 10, 57, 173)] int pageSize,
		[Values(1, 2, int.MaxValue)] int maxOpenPages) {
		using (CreateMemPagedBuffer(storageType, pageSize, maxOpenPages * (long)pageSize, out var buffer)) {
			buffer.AddRange(Tools.Array.Gen<byte>(pageSize, 10));
			buffer.RemoveRange(1, buffer.Count - 1);
			Assert.That(buffer.Pages.Count, Is.EqualTo(1));
			Assert.That(buffer.Count, Is.EqualTo(1));
			Assert.That(buffer, Is.EqualTo(new byte[] { 10 }));
		}
	}

	[Test]
	public void Rewind(
		[Values(StorageType.MemoryPagedBuffer, StorageType.BinaryFile, StorageType.TransactionalBinaryFile)]
		StorageType storageType,
		[Values(1, 2, 111)] int pageSize,
		[Values(1, 2, int.MaxValue)] int maxOpenPages) {
		var expected = new byte[] { 127, 17, 18, 19 };
		using (CreateMemPagedBuffer(storageType, pageSize, maxOpenPages * (long)pageSize, out var buffer)) {
			buffer.AddRange<byte>(127, 16, 15, 14, 13);
			buffer.RemoveRange(1, 4);
			Assert.That(buffer.Count, Is.EqualTo(1));
			Assert.That(buffer[0], Is.EqualTo(127));
			buffer.AddRange<byte>(17, 18, 19);
			Assert.That(buffer.Count, Is.EqualTo(4));
			Assert.That(buffer[0], Is.EqualTo(127));
			Assert.That(buffer[1], Is.EqualTo(17));
			Assert.That(buffer[2], Is.EqualTo(18));
			Assert.That(buffer[3], Is.EqualTo(19));
			Assert.That(buffer, Is.EqualTo(expected));
		}
	}

	[Test]
	public void IntegrationTests([Values] StorageType storageType, [Values(1, 10, 57, 173, 1111)] int pageSize, [Values(1, 2, 100)] int maxOpenPages) {
		var expected = new List<byte>();
		var maxCapacity = pageSize * maxOpenPages * 2;
		using (CreateBuffer(storageType, pageSize, maxOpenPages * pageSize, out var buffer)) {
			var mutateFromEndOnly = buffer is not MemoryBuffer && buffer is not StreamMappedBuffer;
			AssertEx.BufferIntegrationTest(buffer, maxCapacity, mutateFromEndOnly);
		}
	}


	public enum StorageType {
		MemoryBuffer,
		MemoryPagedBuffer,
		BinaryFile,
		TransactionalBinaryFile,
		StreamMappedBuffer,
	}


	private IDisposable CreateBuffer(StorageType storageType, int pageSize, long maxMemory, out IBuffer buffer) {
		switch (storageType) {
			case StorageType.MemoryBuffer:
				buffer = new MemoryBuffer(0, pageSize);
				return new Disposables();
			case StorageType.StreamMappedBuffer:
				buffer = new StreamMappedBuffer(new MemoryStream());
				return new Disposables();
		}

		var result = CreateMemPagedBuffer(storageType, pageSize, maxMemory, out var memBuffer);
		buffer = memBuffer;
		return result;
	}

	private IDisposable CreateMemPagedBuffer(StorageType storageType, int pageSize, long maxMemory, out IMemoryPagedBuffer buffer) {
		var disposables = new Disposables();

		switch (storageType) {
			case StorageType.MemoryBuffer:
				throw new InvalidOperationException();
			case StorageType.MemoryPagedBuffer:
				buffer = new MemoryPagedBuffer(pageSize, maxMemory);
				break;
			case StorageType.BinaryFile:
				var tmpFile = Tools.FileSystem.GetTempFileName(false);
				buffer = new FileMappedBuffer(PagedFileDescriptor.From(tmpFile, pageSize, maxMemory));
				disposables.Add(buffer);
				disposables.Add(new ActionScope(() => File.Delete(tmpFile)));
				break;
			case StorageType.TransactionalBinaryFile:
				var baseDir = Tools.FileSystem.GetTempEmptyDirectory(true);
				var fileName = Path.Combine(baseDir, "File.dat");
				buffer = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(fileName, baseDir, pageSize, maxMemory));
				disposables.Add(new ActionScope(() => Tools.FileSystem.DeleteDirectory(baseDir)));
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(storageType), storageType, null);
		}
		return disposables;
	}

}

