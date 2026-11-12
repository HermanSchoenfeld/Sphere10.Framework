// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests.Files;

[TestFixture]
[NonParallelizable]
public class WindowsAlternateStreamTests {
	private string _path;
	private readonly byte[] _primary = { 0, 255, 1, 2, 0, 128, 64 };

	[SetUp]
	public void CreateOwnedPrimaryFile() {
		_path = Path.Combine(Path.GetTempPath(), "Sphere10-Owned-ADS-" + Guid.NewGuid().ToString("N") + ".bin");
		File.WriteAllBytes(_path, _primary);
	}

	[TearDown]
	public void DeleteOnlyTheOwnedPrimaryFileAndItsStreams() => File.Delete(_path);

	[Test]
	public void NativeFindStreamDataMatchesTheSdkUtf16Layout() {
		Assert.That(Marshal.SizeOf<WinAPI.KERNEL32.WIN32_FIND_STREAM_DATA>(), Is.EqualTo(600));
		Assert.That(Marshal.OffsetOf<WinAPI.KERNEL32.WIN32_FIND_STREAM_DATA>(nameof(WinAPI.KERNEL32.WIN32_FIND_STREAM_DATA.StreamName)).ToInt32(), Is.EqualTo(8));
	}

	[TestCase("payload")]
	[TestCase("Δεδομένα-資料")]
	[TestCase("$DATA")]
	public async Task NamedStreamCreateReadUpdateAndDeletePreserveBinaryPrimaryContents(string name) {
		var first = new byte[] { 0, 255, 0, 128, 1, 2 };
		var updated = new byte[] { 255, 0, 253 };
		await Tools.Windows.Files.WriteAlternateStreamAsync(_path, name, first, true);
		var streams = await Tools.Windows.Files.GetAlternateStreamsAsync(_path);
		Assert.That(streams, Has.Length.EqualTo(1), "The unnamed primary $DATA stream is excluded.");
		Assert.That(streams[0].Name, Is.EqualTo(name));
		Assert.That(streams[0].FilePath, Is.EqualTo(_path));
		Assert.That(streams[0].Length, Is.EqualTo(first.Length));
		Assert.That(await Tools.Windows.Files.ReadAlternateStreamAsync(_path, name, first.Length), Is.EqualTo(first));
		Assert.That(await File.ReadAllBytesAsync(_path), Is.EqualTo(_primary));
		await Tools.Windows.Files.WriteAlternateStreamAsync(_path, name, updated, false);
		Assert.That(await Tools.Windows.Files.ReadAlternateStreamAsync(_path, name, updated.Length), Is.EqualTo(updated), "Updating truncates the named stream, not the primary file.");
		await Tools.Windows.Files.DeleteAlternateStreamAsync(_path, name);
		Assert.That(Tools.Windows.Files.GetAlternateStreams(_path), Is.Empty);
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
		using var exclusive = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.That(exclusive.Length, Is.EqualTo(_primary.Length), "Async reads/writes and native enumeration must release every owned file/search handle.");
	}

	[Test]
	public void ReturnedStreamIsOwnedByTheCallerAndRemainsReadableUntilDisposed() {
		var stream = Tools.Windows.Files.OpenAlternateStream(_path, "owned", FileMode.CreateNew, FileAccess.ReadWrite);
		var handle = stream.SafeFileHandle;
		using (stream) {
			stream.Write(new byte[] { 1, 2, 0, 255 });
			stream.Position = 0;
			Assert.That(stream.ReadByte(), Is.EqualTo(1));
			Assert.That(stream.CanRead && stream.CanWrite, Is.True);
		}
		Assert.That(handle.IsClosed, Is.True);
		using var exclusive = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.That(exclusive.Length, Is.EqualTo(_primary.Length));
	}

	[Test]
	public async Task CreateNewAndUpdateMissingStreamNeverOverwriteAnotherStreamOrPrimaryBytes() {
		await Tools.Windows.Files.WriteAlternateStreamAsync(_path, "existing", new byte[] { 10, 20 }, true);
		await Assert.ThatAsync(async () => await Tools.Windows.Files.WriteAlternateStreamAsync(_path, "existing", new byte[] { 1 }, true), Throws.InstanceOf<IOException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Files.WriteAlternateStreamAsync(_path, "missing", new byte[] { 1 }, false), Throws.InstanceOf<FileNotFoundException>());
		Assert.That(await Tools.Windows.Files.ReadAlternateStreamAsync(_path, "existing", 2), Is.EqualTo(new byte[] { 10, 20 }));
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
	}

	[Test]
	public async Task ReadCapIsEnforcedWithoutLeakingTheNamedStreamHandle() {
		await Tools.Windows.Files.WriteAlternateStreamAsync(_path, "bounded", new byte[] { 1, 2, 3, 4 }, true);
		await Assert.ThatAsync(async () => await Tools.Windows.Files.ReadAlternateStreamAsync(_path, "bounded", 3), Throws.InstanceOf<InvalidOperationException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Files.ReadAlternateStreamAsync(_path, "bounded", 0), Throws.InstanceOf<ArgumentException>());
		await Tools.Windows.Files.DeleteAlternateStreamAsync(_path, "bounded");
		Assert.That(Tools.Windows.Files.GetAlternateStreams(_path), Is.Empty);
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("bad:name")]
	[TestCase("path/name")]
	[TestCase(@"path\name")]
	[TestCase("bad\0name")]
	public void InvalidNamesAreRejectedBeforeCreatingADataStream(string name) {
		Assert.That(() => WindowsFile.OpenAlternateStream(_path, name, FileMode.CreateNew, FileAccess.Write), Throws.InstanceOf<ArgumentException>());
		Assert.That(WindowsFile.GetAlternateStreams(_path), Is.Empty);
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
	}

	[Test]
	public void PrimaryFileMustExistAndCannotAlreadyBeAnAlternateStreamPath() {
		var missing = _path + ".missing";
		Assert.That(() => WindowsFile.OpenAlternateStream(missing, "payload", FileMode.CreateNew, FileAccess.Write), Throws.InstanceOf<FileNotFoundException>());
		Assert.That(File.Exists(missing), Is.False, "Opening a named stream must not create its missing primary file.");
		Assert.That(() => WindowsFile.GetAlternateStreams(_path + ":payload:$DATA"), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => WindowsFile.DeleteAlternateStream(_path + ":payload:$DATA", "nested"), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => WindowsFile.OpenAlternateStream(_path + ":payload:$DATA", "nested", FileMode.CreateNew, FileAccess.Write), Throws.InstanceOf<ArgumentException>());
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
	}

	[TestCase(null)]
	[TestCase("")]
	public void InvalidPrimaryPathsDoNotCreateOrChangeAnyFile(string path) {
		Assert.That(() => WindowsFile.GetAlternateStreams(path), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => WindowsFile.OpenAlternateStream(path, "payload", FileMode.CreateNew, FileAccess.Write), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => WindowsFile.DeleteAlternateStream(path, "payload"), Throws.InstanceOf<ArgumentException>());
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
	}

	[TestCase("enumerate")]
	[TestCase("read")]
	[TestCase("write")]
	[TestCase("delete")]
	public async Task PreCanceledOperationsCannotCreateDeleteOrOpenNamedStreams(string operation) {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var task = operation switch {
			"enumerate" => (Task)Tools.Windows.Files.GetAlternateStreamsAsync(_path, cancellation.Token),
			"read" => Tools.Windows.Files.ReadAlternateStreamAsync(_path, "missing", 100, cancellation.Token),
			"write" => Tools.Windows.Files.WriteAlternateStreamAsync(_path, "new", new byte[] { 1 }, true, cancellation.Token),
			_ => Tools.Windows.Files.DeleteAlternateStreamAsync(_path, "missing", cancellation.Token)
		};
		await Assert.ThatAsync(async () => await task, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(Tools.Windows.Files.GetAlternateStreams(_path), Is.Empty);
		Assert.That(File.ReadAllBytes(_path), Is.EqualTo(_primary));
	}
}
