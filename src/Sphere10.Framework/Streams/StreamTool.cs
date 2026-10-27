// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Sphere10.Framework;

// ReSharper disable CheckNamespace
namespace Tools;

public static class Streams {


	public static void ShiftBytes(Stream stream, long fromIndex, long count, long toIndex, int blockSize = Sphere10FrameworkDefaults.DefaultBufferOperationBlockSize) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentGT(blockSize, 0, nameof(blockSize));
		if (count == 0)
			return;

		Guard.ArgumentNotNegative(count, nameof(count));

		// Copy backwards for a right overlap, so scratch space stays bounded by blockSize.
		var backwards = toIndex >= fromIndex && toIndex - fromIndex < count;
		var bufferLength = (int)Math.Min(count, blockSize);
		using BorrowArrayScope<byte>? rented = bufferLength <= 1024 ? null : Scope.EnterBorrowArrayScope(bufferLength);
		Span<byte> block = rented.HasValue ? rented.Value.Span : stackalloc byte[bufferLength];
		var remaining = count;
		// Shift the bytes in blocks read from left-to-right, except when a right overlap requires reading backwards.
		while (remaining > 0) {
			var nextBlockSize = (int)Math.Min(remaining, bufferLength);
			var offset = backwards ? remaining - nextBlockSize : count - remaining;
			stream.Seek(fromIndex + offset, SeekOrigin.Begin);
			if (stream.Read(block[..nextBlockSize]) != nextBlockSize)
				throw new InternalErrorException("Failed to read bytes from stream");
			stream.Seek(toIndex + offset, SeekOrigin.Begin);
			stream.Write(block[..nextBlockSize]);
			remaining -= nextBlockSize;
		}

		// Preserve the established final position after restoring a right-overlap segment.
		stream.Position = backwards ? fromIndex + count : toIndex + count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void RouteStream(Stream readStream, Stream writeStream, int blockSizeInBytes = Sphere10FrameworkDefaults.DefaultBufferOperationBlockSize, bool closeReadStream = false, bool closeWriteStream = false) {
		Guard.Argument(writeStream != readStream, nameof(writeStream), "Cannot route to same stream");
		// Optimized for reading a stream of unknown length
		using (var buffer = Scope.EnterBorrowArrayScope(blockSizeInBytes)) {
			int bytesRead;
			while ((bytesRead = readStream.Read(buffer.Item, 0, blockSizeInBytes)) > 0) {
				writeStream.Write(buffer.Item, 0, bytesRead);
			}
		}
		if (closeReadStream)
			readStream.Close();

		if (closeWriteStream)
			writeStream.Close();
	}

	public static void RouteStream(Stream readStream, Stream writeStream, long length, int blockSizeInBytes = Sphere10FrameworkDefaults.DefaultBufferOperationBlockSize, bool closeReadStream = false, bool closeWriteStream = false) {
		Guard.Argument(writeStream != readStream, nameof(writeStream), "Cannot route to same stream");
		// Optimized for reading a known length of bytes
		using (var buffer = Scope.EnterBorrowArrayScope(blockSizeInBytes)) {
			var reads = length / blockSizeInBytes;
			for (var i = 0; i < reads; i++) {
				var bytesRead = readStream.Read(buffer.Item, 0, blockSizeInBytes);
				writeStream.Write(buffer.Item, 0, bytesRead);
				length -= bytesRead;
				if (bytesRead < blockSizeInBytes || length == 0) {
					// encountered last block early
					length = 0;
					break;
				}
			}

			// left-over 
			if (length > 0) {
				var bytesRead = readStream.Read(buffer.Item, 0, (int)length);
				writeStream.Write(buffer.Item, 0, bytesRead);
			}
		}

		if (closeReadStream)
			readStream.Close();

		if (closeWriteStream)
			writeStream.Close();
	}

	public static byte[] ReadByteArray(Stream stream, int blockSizeInBytes = Sphere10FrameworkDefaults.DefaultBufferOperationBlockSize, bool closeStream = true) {
		using (var memoryStream = new MemoryStream()) {
			RouteStream(stream, memoryStream, blockSizeInBytes, closeStream, true);
			return memoryStream.ToArray();
		}
	}

	public static void WriteStreamToFile(Stream readStream, string filePath, FileMode fileMode = FileMode.Create, bool createDirectories = false, int blockSizeInBytes = Sphere10FrameworkDefaults.DefaultBufferOperationBlockSize, bool closeReadStream = true) {

		#region Argument Validaton

		if (String.IsNullOrEmpty(filePath)) {
			throw new ArgumentException("Invalid file path", "filePath");
		}
		if (readStream == null) {
			throw new ArgumentNullException("readStream");
		}

		#endregion

		try {
			if (createDirectories) {
				var directory = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(directory)) {
					Directory.CreateDirectory(directory);
				}
			}
			using (var writeStream = new FileStream(filePath, fileMode, FileAccess.Write)) {
				RouteStream(readStream, writeStream, blockSizeInBytes, closeReadStream, false);
			}
		} catch (Exception error) {
			throw new Exception("Failed to write file '{0}'".FormatWith(filePath), error);
		}
	}


	public static bool CompareFileStreams(FileStream fs1, FileStream fs2) {
		Debug.Assert(fs1 != null);
		Debug.Assert(fs2 != null);

		// compare lengths
		if (fs1.Length != fs2.Length) {
			return false;
		}

		return CompareStreams(fs1, fs2);
	}

	public static bool CompareStreams(Stream s1, Stream s2) {
		Debug.Assert(s1 != null);
		Debug.Assert(s2 != null);
		var BufferLength = 32768;

		// A buffer for each stream
		using var buffer1 = Scope.EnterBorrowArrayScope(BufferLength);
		using var buffer2 = Scope.EnterBorrowArrayScope(BufferLength);
		// Number of bytes valid within each buffer
		var buffer1Valid = 0;
		var buffer2Valid = 0;

		// Index within the buffer for each stream
		var buffer1Index = 0;
		var buffer2Index = 0;

		while (true) {
			// Read any more data if we need to
			if (buffer1Index == buffer1Valid) {
				buffer1Valid = s1.Read(buffer1.Item, 0, BufferLength);
				buffer1Index = 0;
			}

			if (buffer2Index == buffer2Valid) {
				buffer2Valid = s2.Read(buffer2.Item, 0, BufferLength);
				buffer2Index = 0;
			}

			// We've read to the end of both streams simultaneously
			if (buffer1Valid == 0 && buffer2Valid == 0) {
				return true;
			}

			// We've read to the end of one stream but not the other
			if (buffer1Valid == 0 || buffer2Valid == 0) {
				return false;
			}

			// compare each byte in buffer
			if (buffer1.Item[buffer1Index] != buffer2.Item[buffer2Index]) {
				return false;
			}

			buffer1Index++;
			buffer2Index++;
		}
	}

	public static void GZipCompress(Stream input, Stream output) {
		using (var compressor = new GZipStream(output, CompressionMode.Compress, true))
			RouteStream(input, compressor, blockSizeInBytes: Sphere10FrameworkDefaults.OptimalCompressWriteBlockSize);
	}

	public static void GZipDecompress(Stream input, Stream output) {
		using (var decompressor = new GZipStream(input, CompressionMode.Decompress, true)) {
			// WARNING: do not seek to beginnning here! Client code responsible for that
			RouteStream(decompressor, output, blockSizeInBytes: Sphere10FrameworkDefaults.OptimalCompressWriteBlockSize);
		}
	}


	public static void Encrypt(Stream input, Stream output, string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC) {
		using var algorithm = Crypto.PrepareSymmetricAlgorithm(password, salt, paddingMode, cipherMode);
		Tools.Crypto.EncryptStream(input, output, algorithm);
	}

	public static void Decrypt(Stream input, Stream output, string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC) {
		using var algorithm = Crypto.PrepareSymmetricAlgorithm(password, salt, paddingMode, cipherMode);
		Tools.Crypto.DecryptStream(input, output, algorithm);
	}

	public static void Encrypt<TSymmetricAlgorithm>(Stream input, Stream output, string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC)
		where TSymmetricAlgorithm : SymmetricAlgorithm, new() {
		Tools.Crypto.EncryptStream(input, output, Crypto.PrepareSymmetricAlgorithm<TSymmetricAlgorithm>(password, salt, paddingMode, cipherMode));
	}

	public static void Decrypt<TSymmetricAlgorithm>(Stream input, Stream output, string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC)
		where TSymmetricAlgorithm : SymmetricAlgorithm, new() {
		Tools.Crypto.DecryptStream(input, output, Crypto.PrepareSymmetricAlgorithm<TSymmetricAlgorithm>(password, salt, paddingMode, cipherMode));
	}
}


