// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsProcessMemoryStreamTests {
	[Test]
	public async Task ReadsOnlyTheRequestedBytesAcrossSeeksAndBlockBoundaries() {
		var expected = Enumerable.Range(0, 70000).Select(index => (byte)(index * 31)).ToArray();
		var pinned = GCHandle.Alloc(expected, GCHandleType.Pinned);
		using var release = Tools.Scope.ExecuteOnDispose(pinned.Free);
		using var process = WindowsProcess.Open(Environment.ProcessId);
		using var stream = new WindowsProcessMemoryStream(process, (ulong)pinned.AddrOfPinnedObject().ToInt64(), (ulong)expected.Length);
		Assert.That(stream.CanSeek, Is.True);
		Assert.That(stream.CanWrite, Is.False);
		Assert.That(stream.Length, Is.EqualTo(expected.Length));
		var window = new byte[4096];
		stream.Seek(65000, SeekOrigin.Begin);
		var read = await stream.ReadAsync(window, 0, window.Length);
		Assert.That(read, Is.EqualTo(4096));
		Assert.That(window, Is.EqualTo(expected.Skip(65000).Take(4096)));
		Assert.That(stream.Position, Is.EqualTo(69096));
		Assert.That(stream.Read(window, 0, window.Length), Is.EqualTo(904), "A read at the end returns only the bytes left in the range.");
		Assert.That(window.Take(904), Is.EqualTo(expected.Skip(69096)));
		Assert.That(stream.Read(window, 0, window.Length), Is.Zero);
		stream.Position = 0;
		Assert.That(stream.ReadByte(), Is.EqualTo(expected[0]));
	}

	[Test]
	public void UnreadableMemoryFailsWithTheWindowsErrorInsteadOfInventingBytes() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		using var stream = new WindowsProcessMemoryStream(process, 0x10, 4096);
		Assert.That(() => stream.Read(new byte[16], 0, 16), Throws.InstanceOf<Win32Exception>());
		Assert.That(() => stream.Write(new byte[1], 0, 1), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => stream.Position = 5000, Throws.InstanceOf<ArgumentException>());
	}
}
