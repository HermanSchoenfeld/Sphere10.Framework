// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EndianBinaryOwnershipTests {
	[TestCase(false)]
	[TestCase(true)]
	public void ExistingWriterConstructorsStillCloseTheirUnderlyingStream(bool explicitEncoding) {
		using var stream = new ObservedMemoryStream();
		var writer = explicitEncoding
			? new EndianBinaryWriter(EndianBitConverter.Little, stream, Encoding.UTF8)
			: new EndianBinaryWriter(EndianBitConverter.Little, stream);
		writer.Write(42);
		writer.Dispose();
		writer.Dispose();
		Assert.That(stream.CanWrite, Is.False);
		Assert.That(stream.Disposals, Is.EqualTo(1));
		Assert.That(stream.Flushes, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WriterClosesItsUnderlyingStreamWhenLeaveOpenIsFalse(bool explicitEncoding) {
		using var stream = new ObservedMemoryStream();
		var writer = explicitEncoding
			? new EndianBinaryWriter(EndianBitConverter.Little, stream, Encoding.UTF8, leaveOpen: false)
			: new EndianBinaryWriter(EndianBitConverter.Little, stream, leaveOpen: false);
		writer.Write(42);
		writer.Dispose();
		writer.Dispose();
		Assert.That(stream.CanWrite, Is.False);
		Assert.That(stream.Disposals, Is.EqualTo(1));
		Assert.That(stream.Flushes, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void BorrowingWriterFlushesWithoutClosingTheCallersStream(bool explicitEncoding) {
		using var stream = new ObservedMemoryStream();
		var writer = explicitEncoding
			? new EndianBinaryWriter(EndianBitConverter.Little, stream, Encoding.UTF8, leaveOpen: true)
			: new EndianBinaryWriter(EndianBitConverter.Little, stream, leaveOpen: true);
		writer.Write(42);
		writer.Dispose();
		writer.Dispose();
		Assert.That(stream.CanWrite, Is.True);
		Assert.That(stream.Disposals, Is.Zero);
		Assert.That(stream.Flushes, Is.EqualTo(1));
		Assert.That(() => writer.Write(1), Throws.TypeOf<ObjectDisposedException>());
		stream.WriteByte(0xFF);
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 42, 0, 0, 0, 0xFF }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExistingReaderConstructorsStillCloseTheirUnderlyingStream(bool explicitEncoding) {
		using var stream = new ObservedMemoryStream(new byte[] { 42, 0, 0, 0 });
		var reader = explicitEncoding
			? new EndianBinaryReader(EndianBitConverter.Little, stream, Encoding.UTF8)
			: new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(reader.ReadInt32(), Is.EqualTo(42));
		reader.Dispose();
		reader.Dispose();
		Assert.That(stream.CanRead, Is.False);
		Assert.That(stream.Disposals, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReaderClosesItsUnderlyingStreamWhenLeaveOpenIsFalse(bool explicitEncoding) {
		using var stream = new ObservedMemoryStream(new byte[] { 42, 0, 0, 0 });
		var reader = explicitEncoding
			? new EndianBinaryReader(EndianBitConverter.Little, stream, Encoding.UTF8, leaveOpen: false)
			: new EndianBinaryReader(EndianBitConverter.Little, stream, leaveOpen: false);
		Assert.That(reader.ReadInt32(), Is.EqualTo(42));
		reader.Dispose();
		reader.Dispose();
		Assert.That(stream.CanRead, Is.False);
		Assert.That(stream.Disposals, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void BorrowingReaderDoesNotCloseTheCallersStream(bool explicitEncoding) {
		using var stream = new ObservedMemoryStream(new byte[] { 42, 0, 0, 0, 0xFF });
		var reader = explicitEncoding
			? new EndianBinaryReader(EndianBitConverter.Little, stream, Encoding.UTF8, leaveOpen: true)
			: new EndianBinaryReader(EndianBitConverter.Little, stream, leaveOpen: true);
		Assert.That(reader.ReadInt32(), Is.EqualTo(42));
		reader.Dispose();
		reader.Dispose();
		Assert.That(stream.CanRead, Is.True);
		Assert.That(stream.Disposals, Is.Zero);
		Assert.That(() => reader.ReadInt32(), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(stream.ReadByte(), Is.EqualTo(0xFF));
	}

	private sealed class ObservedMemoryStream : MemoryStream {
		public ObservedMemoryStream() {
		}

		public ObservedMemoryStream(byte[] bytes)
			: base(bytes) {
		}

		public int Disposals { get; private set; }

		public int Flushes { get; private set; }

		public override void Flush() {
			Flushes++;
			base.Flush();
		}

		protected override void Dispose(bool disposing) {
			if (disposing)
				Disposals++;
			base.Dispose(disposing);
		}
	}
}
