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
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PortableExecutableTests {
	[Test]
	public async Task NativeSystemImageContainsExportsResourcesAndUnwindStructure() {
		var image = await Tools.Windows.Files.InspectPortableExecutableAsync(Path.Combine(Environment.SystemDirectory, "kernel32.dll"));
		Assert.That(image.HasMetadata, Is.False);
		Assert.That(image.Entries.Any(row => row.Kind == "Import"), Is.True);
		Assert.That(image.Entries.Any(row => row.Kind == "Export"), Is.True);
		Assert.That(image.Entries.Any(row => row.Kind == "Resource"), Is.True);
		if (image.Machine == Machine.Amd64.ToString()) {
			Assert.That(image.Entries.Any(row => row.Kind == "RuntimeFunction"), Is.True);
			Assert.That(image.Entries.Any(row => row.Kind == "UnwindInfo"), Is.True);
		}
		Assert.That(image.Issues, Is.Empty);
	}

	[Test]
	public async Task InvalidOptionalDirectoryRetainsIndependentParsedStructureAndExplicitIssue() {
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		File.Copy(typeof(Tools.Windows.Files).Assembly.Location, path);
		long importOffset;
		using (var stream = File.OpenRead(path)) {
			using var reader = new PEReader(stream);
			importOffset = reader.PEHeaders.PEHeaderStartOffset + (reader.PEHeaders.PEHeader.Magic == PEMagic.PE32Plus ? 112 : 96) + 8;
		}
		using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) {
			stream.Position = importOffset;
			stream.Write(EndianBitConverter.Little.GetBytes(0xfffffff0U));
			stream.Write(EndianBitConverter.Little.GetBytes(40U));
		}
		var image = await Tools.Windows.Files.InspectPortableExecutableAsync(path);
		Assert.That(image.Issues.Any(issue => issue.StartsWith("Import:", StringComparison.Ordinal)), Is.True);
		Assert.That(image.Entries.Any(row => row.Kind == "ManagedType"), Is.True, "A malformed optional table must not erase independently readable metadata.");
		Assert.That(image.Entries.Any(row => row.Kind == "Import"), Is.False);
	}

	[Test]
	public async Task InBoxReaderInspectsManagedImageWithoutLoadingIt() {
		var path = typeof(Tools.Windows.Files).Assembly.Location;
		var image = await Tools.Windows.Files.InspectPortableExecutableAsync(path);
		Assert.That(image.FilePath, Is.EqualTo(path));
		Assert.That(image.HasMetadata, Is.True);
		Assert.That(image.Sections, Is.Not.Empty);
		Assert.That(image.Entries.Any(row => row.Kind == "ManagedType" && row.Name == typeof(WindowsFile).FullName), Is.True);
		Assert.That(image.Entries.Any(row => row.Kind == "CLR" && row.Name == "Assembly"), Is.True);
		Assert.That(image.Issues, Is.Empty);
		foreach (var section in image.Sections.Where(section => section.RawSize > 0)) {
			Assert.That(image.TryGetFileOffset(section.RelativeVirtualAddress, 1, out var offset), Is.True);
			Assert.That(offset, Is.EqualTo(section.FileOffset));
			Assert.That(image.TryGetRelativeVirtualAddress(offset, 1, out var relative), Is.True);
			Assert.That(relative, Is.EqualTo(section.RelativeVirtualAddress));
		}
		var bytes = await Tools.Windows.Files.ReadPortableExecutableRangeAsync(image, 0, 2);
		Assert.That(bytes, Is.EqualTo(new byte[] { 0x4d, 0x5a }));
	}

	[Test]
	public void MappingRejectsVirtualTailsFileTruncationOverlapsAndOverflow() {
		var image = new PortableExecutableInfo {
			FileLength = 0x800, HeaderSize = 0x200,
			Sections = new[] { new PortableExecutableSection(".text", 0x1000, 0x800, 0x200, 0x400, 0) }
		};
		Assert.That(image.TryGetFileOffset(0x1400, 1, out _), Is.False, "Zero-filled virtual tails are not file bytes.");
		Assert.That(image.TryGetFileOffset(uint.MaxValue, 2, out _), Is.False);
		Assert.That(image.TryGetFileOffset(0x13ff, 2, out _), Is.False);
		Assert.That(image.TryGetRelativeVirtualAddress(0x800, 1, out _), Is.False);
		var overlapping = new PortableExecutableInfo {
			FileLength = image.FileLength, HeaderSize = image.HeaderSize,
			Sections = new[] { image.Sections[0], image.Sections[0] with { Name = ".overlap" } }
		};
		Assert.That(overlapping.TryGetFileOffset(0x1000, 1, out _), Is.False);
		Assert.That(overlapping.TryGetRelativeVirtualAddress(0x200, 1, out _), Is.False);
	}

	[Test]
	public async Task ChangedImageRevisionIsRejectedBeforeCapturedOffsetsAreRead() {
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		File.Copy(typeof(Tools.Windows.Files).Assembly.Location, path);
		var image = await Tools.Windows.Files.InspectPortableExecutableAsync(path);
		using (var file = new FileStream(path, FileMode.Append, FileAccess.Write))
			file.WriteByte(0);
		await Assert.ThatAsync(() => Tools.Windows.Files.ReadPortableExecutableRangeAsync(image, 0, 2), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public async Task MalformedImagesAndCanceledReadsRemainFailures() {
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		Tools.FileSystem.WriteAllBytes(path, new byte[] { 0x4d, 0x5a, 0, 0 });
		await Assert.ThatAsync(() => Tools.Windows.Files.InspectPortableExecutableAsync(path), Throws.TypeOf<BadImageFormatException>());
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.Files.InspectPortableExecutableAsync(path, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.Files.ReadFileRangeAsync(path, 0, 1, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}
