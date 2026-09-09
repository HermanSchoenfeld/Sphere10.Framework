// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EncryptedStreamTests {
	private static readonly byte[] _initialCounter = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
	private static readonly byte[] _plaintext = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710");
	private static readonly byte[] _key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
	private static readonly byte[] _ciphertext = Convert.FromHexString("874d6191b620e3261bef6864990db6ce9806f66b7970fdff8617187bb9fffdff5ae4df3edbd5d35e5b4f09020db03eab1e031dda2fbe03d1792170a0f3009cee");

	// NIST SP 800-38A, appendix F.5: independent AES-128/192/256 CTR known-answer vectors.
	[TestCase("2b7e151628aed2a6abf7158809cf4f3c", "874d6191b620e3261bef6864990db6ce9806f66b7970fdff8617187bb9fffdff5ae4df3edbd5d35e5b4f09020db03eab1e031dda2fbe03d1792170a0f3009cee")]
	[TestCase("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b", "1abc932417521ca24f2b0459fe7e6e0b090339ec0aa6faefd5ccc2c6f4ce8e941e36b26bd1ebc670d1bd1d665620abf74f78a7f6d29809585a97daec58c6b050")]
	[TestCase("603deb1015ca71be2b73aef0857d77811f352c073b6108d72d9810a30914dff4", "601ec313775789a5b7a7f504bbf3d228f443e3ca4d62b59aca84e990cacaf5c52b0930daa23de94ce87017ba2d84988ddfc9c58db67aada613c2dd08457941a6")]
	public void MatchesPublishedVectors(string keyHex, string ciphertextHex) {
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, Convert.FromHexString(keyHex), _initialCounter, leaveOpen: true);
		stream.Write(_plaintext);
		Assert.That(storage.ToArray(), Is.EqualTo(Convert.FromHexString(ciphertextHex)));
		stream.Position = 0;
		var result = new byte[_plaintext.Length];
		stream.ReadExactly(result);
		Assert.That(result, Is.EqualTo(_plaintext));
	}

	[Test]
	public void EveryWritePreservesLength([Values(0, 1, 10, 15, 16, 17, 31, 64, 100003)] int length, [Values(1, 7, 16, 33)] int chunkSize) {
		var plaintext = Enumerable.Range(0, length).Select(index => (byte)index).ToArray();
		var original = (byte[])plaintext.Clone();
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, _key, _initialCounter, leaveOpen: true);
		for (var offset = 0; offset < length; offset += chunkSize) {
			stream.Write(plaintext, offset, Math.Min(chunkSize, length - offset));
			Assert.That(storage.Length, Is.EqualTo(Math.Min(offset + chunkSize, length)));
		}
		stream.Flush();
		Assert.That(storage.Length, Is.EqualTo(length));
		Assert.That(plaintext, Is.EqualTo(original), "Encryption must not mutate caller buffers.");
		stream.Position = 0;
		var result = new byte[length];
		stream.ReadExactly(result);
		Assert.That(result, Is.EqualTo(plaintext));
		Assert.That(stream.ReadByte(), Is.EqualTo(-1));
	}

	[TestCase(1)]
	[TestCase(10)]
	[TestCase(17)]
	public void WriteChunkingDoesNotChangeCiphertext(int chunkSize) {
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, _key, _initialCounter);
		for (var offset = 0; offset < _plaintext.Length; offset += chunkSize)
			stream.Write(_plaintext, offset, Math.Min(chunkSize, _plaintext.Length - offset));
		Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(15)]
	[TestCase(16)]
	[TestCase(37)]
	public void SeekingReadsAtCorrectCounterOffset(int offset) {
		using var storage = new MemoryStream(_ciphertext, writable: false);
		storage.Position = offset;
		using var stream = CreateStream(storage, _key, _initialCounter);
		var result = new byte[_plaintext.Length - offset];
		stream.ReadExactly(result);
		Assert.That(result, Is.EqualTo(_plaintext.Skip(offset).ToArray()));
		Assert.That(stream.Seek(-1, SeekOrigin.End), Is.EqualTo(_plaintext.Length - 1));
		Assert.That(stream.ReadByte(), Is.EqualTo(_plaintext[^1]));
		stream.Seek(-2, SeekOrigin.Current);
		Assert.That(stream.ReadByte(), Is.EqualTo(_plaintext[^2]));
	}

	[Test]
	public void ShortNonSeekableReadsPreserveKeystreamPosition() {
		using var storage = new ShortReadStream(new MemoryStream(_ciphertext));
		using var stream = CreateStream(storage, _key, _initialCounter);
		var result = new byte[_plaintext.Length];
		stream.ReadExactly(result);
		Assert.That(result, Is.EqualTo(_plaintext));
		Assert.That(stream.CanSeek, Is.False);
		Assert.That(() => stream.Seek(0, SeekOrigin.Begin), Throws.InstanceOf<NotSupportedException>());
	}

	[Test]
	public async Task AsyncArrayMemoryAndCopyEncryptAndDecrypt() {
		using var storage = new MemoryStream();
		await using var stream = CreateStream(storage, _key, _initialCounter, leaveOpen: true);
		await stream.WriteAsync(_plaintext, 0, 10, CancellationToken.None);
		await stream.WriteAsync(_plaintext.AsMemory(10));
		await stream.FlushAsync();
		Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
		stream.Position = 0;
		var result = new byte[_plaintext.Length];
		Assert.That(await stream.ReadAsync(result, 0, 10, CancellationToken.None), Is.EqualTo(10));
		await stream.ReadExactlyAsync(result.AsMemory(10));
		Assert.That(result, Is.EqualTo(_plaintext));
		stream.Position = 0;
		using var destination = new MemoryStream();
		await stream.CopyToAsync(destination, 7, CancellationToken.None);
		Assert.That(destination.ToArray(), Is.EqualTo(_plaintext));
	}

	[Test]
	public void ByteAndLegacyAsyncOperationsDoNotBypassEncryption() {
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, _key, _initialCounter);
		stream.WriteByte(_plaintext[0]);
		var state = new object();
		var writeResult = stream.BeginWrite(_plaintext, 1, _plaintext.Length - 1, null, state);
		Assert.That(writeResult.AsyncState, Is.SameAs(state));
		stream.EndWrite(writeResult);
		Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
		stream.Position = 0;
		Assert.That(stream.ReadByte(), Is.EqualTo(_plaintext[0]));
		var result = new byte[_plaintext.Length];
		result[0] = _plaintext[0];
		var readResult = stream.BeginRead(result, 1, result.Length - 1, null, state);
		Assert.That(stream.EndRead(readResult), Is.EqualTo(result.Length - 1));
		Assert.That(result, Is.EqualTo(_plaintext));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposalHonorsOwnership(bool leaveOpen) {
		using var storage = new MemoryStream();
		var stream = CreateStream(storage, _key, _initialCounter, leaveOpen);
		stream.Write(_plaintext.AsSpan(0, 10));
		await stream.DisposeAsync();
		stream.Close();
		Assert.That(stream.CanRead || stream.CanWrite || stream.CanSeek, Is.False);
		Assert.That(storage.CanWrite, Is.EqualTo(leaveOpen));
		Assert.That(() => stream.WriteByte(1), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => stream.ReadByte(), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(storage.ToArray().Length, Is.EqualTo(10), "Disposal must not append padding.");
	}

	[Test]
	public void CopiesKeyAndCounterParameters() {
		var key = (byte[])_key.Clone();
		var counter = (byte[])_initialCounter.Clone();
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, key, counter);
		Array.Clear(key);
		Array.Clear(counter);
		stream.Write(_plaintext);
		Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
	}

	[Test]
	public void CounterExhaustionFailsBeforeWriting() {
		var counter = Enumerable.Repeat((byte)255, 16).ToArray();
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, _key, counter);
		Assert.That(() => stream.Write(new byte[17]), Throws.InvalidOperationException);
		Assert.That(storage.Length, Is.Zero);
		stream.Write(new byte[16]);
		Assert.That(() => stream.WriteByte(1), Throws.InvalidOperationException);
		Assert.That(storage.Length, Is.EqualTo(16));
	}

	[Test]
	public async Task CancellationDoesNotAdvanceCipherPosition() {
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, _key, _initialCounter);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(async () => await stream.WriteAsync(_plaintext.AsMemory(), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(stream.Position, Is.Zero);
		await stream.WriteAsync(_plaintext);
		stream.Position = 0;
		Assert.That(async () => await stream.ReadAsync(new byte[10].AsMemory(), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(stream.Position, Is.Zero);
		Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
	}

	[TestCase(0)]
	[TestCase(15)]
	[TestCase(17)]
	[TestCase(33)]
	public void RejectsInvalidKeyLength(int length) {
		using var storage = new MemoryStream();
		Assert.That(() => CreateStream(storage, new byte[length], _initialCounter), Throws.ArgumentException);
		Assert.That(storage.CanWrite, Is.True);
	}

	[Test]
	public void RejectsInvalidArgumentsAndUnencryptedGaps() {
		using var storage = new MemoryStream();
		Assert.That(() => CreateStream(null, _key, _initialCounter), Throws.ArgumentNullException);
		Assert.That(() => CreateStream(storage, null, _initialCounter), Throws.ArgumentNullException);
		Assert.That(() => CreateStream(storage, _key, new byte[15]), Throws.ArgumentException);
		using var stream = CreateStream(storage, _key, _initialCounter);
		Assert.That(() => stream.Write(null, 0, 1), Throws.ArgumentNullException);
		Assert.That(() => stream.Read(new byte[2], 1, 2), Throws.ArgumentException);
		Assert.That(() => stream.SetLength(20), Throws.InstanceOf<NotSupportedException>());
		stream.Position = 2;
		Assert.That(() => stream.WriteByte(1), Throws.InvalidOperationException);
		Assert.That(storage.Length, Is.Zero);
	}

	[Test]
	public void EofAtLastCounterDoesNotRequireAnotherCounterBlock() {
		var counter = Enumerable.Repeat((byte)255, 16).ToArray();
		using var storage = new MemoryStream();
		using var stream = CreateStream(storage, _key, counter);
		stream.Write(new byte[16]);
		stream.Position = 0;
		var buffer = new byte[64];
		Assert.That(stream.Read(buffer), Is.EqualTo(16));
		Assert.That(buffer, Is.EqualTo(new byte[64]));
		Assert.That(stream.ReadByte(), Is.EqualTo(-1));
	}

	[Test]
	public void SeekingWritesUseAbsoluteCounterPosition() {
		using var storage = new MemoryStream(new byte[64], writable: true);
		using var stream = CreateStream(storage, _key, _initialCounter);
		// Write each plaintext region only once, in reverse order.
		for (var offset = 48; offset >= 0; offset -= 16) {
			stream.Position = offset;
			stream.Write(_plaintext.AsSpan(offset, 16));
		}
		Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
	}

	[Test]
	public async Task NonSeekableAsyncReadsAndWritesUseContinuousCounterPositions() {
		using var storage = new MemoryStream();
		using (var wrapper = new ShortReadStream(storage)) {
			using var writer = CreateStream(wrapper, _key, _initialCounter, leaveOpen: true);
			await writer.WriteAsync(_plaintext.AsMemory(0, 10));
			await writer.WriteAsync(_plaintext.AsMemory(10));
			Assert.That(storage.ToArray(), Is.EqualTo(_ciphertext));
			storage.Position = 0;
			using var reader = CreateStream(wrapper, _key, _initialCounter, leaveOpen: true);
			var result = new byte[_plaintext.Length];
			await reader.ReadExactlyAsync(result);
			Assert.That(result, Is.EqualTo(_plaintext));
		}
	}

	[TestCase(0, 1, false)]
	[TestCase(0, 1, true)]
	[TestCase(7, 10, false)]
	[TestCase(7, 10, true)]
	[TestCase(15, 17, false)]
	[TestCase(15, 17, true)]
	[TestCase(37, 3, false)]
	[TestCase(37, 3, true)]
	public async Task OverwritesTouchOnlyRequestedBytes(int offset, int count, bool useAsync) {
		using var storage = new WriteTrackingStream();
		using var stream = new EncryptedStream<WriteTrackingStream>(storage, _key, _initialCounter, leaveOpen: true);
		stream.Write(_plaintext);
		var originalCiphertext = storage.ToArray();
		storage.Writes.Clear();
		var replacement = Enumerable.Repeat((byte)0xA5, count).ToArray();
		stream.Position = offset;
		if (useAsync)
			await stream.WriteAsync(replacement.AsMemory());
		else
			stream.Write(replacement);
		Assert.That(storage.Writes, Is.EqualTo(new[] { ((long)offset, count) }));
		Assert.That(stream.Position, Is.EqualTo(offset + count));
		Assert.That(storage.Length, Is.EqualTo(_plaintext.Length));
		var updatedCiphertext = storage.ToArray();
		Assert.That(updatedCiphertext.Take(offset), Is.EqualTo(originalCiphertext.Take(offset)));
		Assert.That(updatedCiphertext.Skip(offset + count), Is.EqualTo(originalCiphertext.Skip(offset + count)));
		var expected = (byte[])_plaintext.Clone();
		replacement.CopyTo(expected, offset);
		stream.Position = 0;
		var result = new byte[expected.Length];
		await stream.ReadExactlyAsync(result);
		Assert.That(result, Is.EqualTo(expected));
	}

	[Test]
	public void RejectsMissingInitialCounter() {
		using var storage = new MemoryStream();
		Assert.That(() => new EncryptedStream(storage, _key, null), Throws.ArgumentNullException);
		Assert.That(storage.CanWrite, Is.True);
	}

	[Test]
	public void CounterExhaustionFailsBeforeReading() {
		using var storage = new MemoryStream(new byte[17], writable: false);
		using var stream = new EncryptedStream(storage, _key, Enumerable.Repeat((byte)255, 16).ToArray());
		Assert.That(() => stream.Read(new byte[17]), Throws.InvalidOperationException);
		Assert.That(storage.Position, Is.Zero);
	}

	private static EncryptedStream CreateStream(Stream stream, byte[] key, byte[] initialCounter, bool leaveOpen = false)
		=> new(stream, key, initialCounter, leaveOpen);

	private sealed class WriteTrackingStream : MemoryStream {
		public List<(long Position, int Count)> Writes { get; } = new();

		public override void Write(byte[] buffer, int offset, int count) {
			Writes.Add((Position, count));
			base.Write(buffer, offset, count);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			Write(buffer.ToArray(), 0, buffer.Length);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ShortReadStream : StreamDecorator {
		public ShortReadStream(Stream stream)
			: base(stream) {
		}

		public override bool CanSeek => false;
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override int Read(Span<byte> buffer) => InnerStream.Read(buffer[..Math.Min(3, buffer.Length)]);
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			=> InnerStream.ReadAsync(buffer[..Math.Min(3, buffer.Length)], cancellationToken);
	}
}
