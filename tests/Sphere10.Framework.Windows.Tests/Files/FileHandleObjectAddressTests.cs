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
using Sphere10.Framework.Windows.Files;
using BclSafeFileHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public sealed class FileHandleObjectAddressTests {
	[Test]
	public void NullHandleIsRejected() => Assert.That(() => FileHandleService.GetObjectAddress(null), Throws.ArgumentNullException);

	[TestCase(0)]
	[TestCase(-1)]
	public void InvalidHandleIsRejected(int value) {
		using var handle = new BclSafeFileHandle(new IntPtr(value), false);
		Assert.That(() => FileHandleService.GetObjectAddress(handle), Throws.ArgumentException);
	}

	[Test]
	public void ClosedHandleIsRejectedBeforeNativeEnumeration() {
		var handle = new BclSafeFileHandle(new IntPtr(1), false);
		handle.Dispose();
		Assert.That(() => FileHandleService.GetObjectAddress(handle), Throws.InstanceOf<ObjectDisposedException>());
	}

	[Test]
	[Explicit("Requires native handle-table query permission to verify real owned file-object identities.")]
	[Category("NativeCapture")]
	public void OwnedOpenFileObjectAddressesRemainStableAndDistinct() {
		var path = Tools.FileSystem.GenerateTempFilename();
		using var delete = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		using var first = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var second = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
		var firstAddress = FileHandleService.GetObjectAddress(first.SafeFileHandle);
		var secondAddress = FileHandleService.GetObjectAddress(second.SafeFileHandle);
		Assert.That(firstAddress, Is.Not.Zero);
		Assert.That(secondAddress, Is.Not.Zero.And.Not.EqualTo(firstAddress), "Separate opens of the same path own distinct native file objects.");
		Assert.That(FileHandleService.GetObjectAddress(first.SafeFileHandle), Is.EqualTo(firstAddress));

		// Querying borrows both handles; ordinary IO must remain usable after each query releases its pin.
		first.WriteByte(42);
		first.Flush();
		Assert.That(second.ReadByte(), Is.EqualTo(42));
		TestContext.WriteLine($"Owned file object identities: first 0x{firstAddress:X}; second 0x{secondAddress:X}.");
	}
}
