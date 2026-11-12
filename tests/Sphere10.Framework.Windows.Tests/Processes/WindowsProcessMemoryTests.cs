// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsProcessMemoryTests {
	[Test]
	public async Task OwnedMemoryMapAndSlicesPreserveNativeBytesAndFailedReadStatus() {
		var bytes = Encoding.UTF8.GetBytes("Owned memory: café 東京 😀\0");
		var pointer = Marshal.AllocHGlobal(bytes.Length);
		using var free = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeHGlobal(pointer));
		Marshal.Copy(bytes, 0, pointer, bytes.Length);
		using var process = WindowsProcess.Open(Environment.ProcessId);
		var regions = await process.GetMemoryRegionsAsync();
		var address = unchecked((ulong)pointer.ToInt64());
		var region = regions.Single(row => address >= row.Address && address - row.Address < row.Size);
		Assert.That(region.IsReadable, Is.True);
		var read = await process.ReadMemoryAsync(address, bytes.Length);
		Assert.That(read.Bytes, Is.EqualTo(bytes));
		Assert.That(read.ErrorCode, Is.Zero);
		Assert.That(read.Process, Is.EqualTo(process.Identity));
		var scan = await process.GetMemoryStringsInRangeAsync(address, (ulong)bytes.Length, maximumBytes: bytes.Length);
		Assert.That(scan.BytesExamined, Is.EqualTo(bytes.Length));
		Assert.That(scan.Truncated, Is.False, "A fully consumed explicit range is complete even when its length equals the byte budget.");
		Assert.That(scan.Strings.Any(row => row.Encoding == "UTF-8" && row.Value == "Owned memory: café 東京 😀" && row.Address == address), Is.True);
		var denied = await process.ReadMemoryAsync(0, 32);
		Assert.That(denied.Bytes, Is.Empty);
		Assert.That(denied.IsPartial, Is.True);
		Assert.That(denied.ErrorCode, Is.Not.Zero);
	}

	[Test]
	public void Utf8ScannerRetainsSplitScalarsAndDoesNotDuplicateAsciiOrAcceptOverlongEncodings() {
		var scanner = new ProcessStringScanner(4, 100);
		var bytes = Encoding.UTF8.GetBytes("café東京😀\0");
		scanner.Feed(bytes[..4], 4, 0x1000, null);
		scanner.Feed(bytes[4..^2], bytes.Length - 6, 0x1004, null);
		scanner.Feed(bytes[^2..], 2, 0x1000 + (ulong)bytes.Length - 2, null);
		scanner.EndRegion();
		Assert.That(scanner.Strings.Single(row => row.Encoding == "UTF-8").Value, Is.EqualTo("café東京😀"));
		Assert.That(scanner.Strings.Single(row => row.Encoding == "UTF-8").Address, Is.EqualTo(0x1000));
		var ascii = new ProcessStringScanner(4, 100);
		var asciiBytes = Encoding.ASCII.GetBytes("pure ASCII\0");
		ascii.Feed(asciiBytes, asciiBytes.Length, 0, null);
		ascii.EndRegion();
		Assert.That(ascii.Strings.Count(row => row.Value == "pure ASCII"), Is.EqualTo(1));
		var malformed = new ProcessStringScanner(2, 100);
		malformed.Feed(new byte[] { 0xc0, 0xaf, 0xc0, 0xaf, 0 }, 5, 0, null);
		malformed.EndRegion();
		Assert.That(malformed.Strings.Any(row => row.Encoding == "UTF-8"), Is.False);
		var longRun = new ProcessStringScanner(2, 100);
		var longBytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("😀", 5000)) + "\0");
		longRun.Feed(longBytes, longBytes.Length, 0, null);
		longRun.EndRegion();
		var truncated = longRun.Strings.Single(row => row.Encoding == "UTF-8");
		Assert.That(truncated.Value.Length, Is.EqualTo(4096));
		Assert.That(char.IsLowSurrogate(truncated.Value[^1]), Is.True, "The bounded output must keep the final surrogate pair intact.");
		Assert.That(truncated.IsTruncated, Is.True);
	}

	[Test]
	public async Task MemoryBoundsAndCancellationAreNotSuccessfulEmptyInventories() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => process.GetMemoryRegionsAsync(cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => process.ReadMemoryAsync(0, 16, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => process.ReadMemoryAsync(0, 1048577), Throws.TypeOf<ArgumentOutOfRangeException>());
		await Assert.ThatAsync(() => process.GetMemoryRegionsAsync(1), Throws.TypeOf<InvalidOperationException>());
	}
}
