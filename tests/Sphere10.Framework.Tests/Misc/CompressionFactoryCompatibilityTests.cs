// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Sphere10.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CompressionFactoryCompatibilityTests {

	[TestCase(null)]
	[TestCase("")]
	[TestCase("review-password")]
	public void DefaultTextPipelineRoundTrips(string password) {
		const string text = "Compression compatibility: £ byte text";
		var compressed = Tools.Text.CompressText(text, password);
		Assert.That(Tools.Text.DecompressText(compressed, password), Is.EqualTo(text));
	}

	[TestCase(null)]
	[TestCase("review-password")]
	public void DefaultAndLegacyGenericTextPipelinesInteroperate(string password) {
		const string text = "Compression compatibility";
#pragma warning disable SYSLIB0021 // Exercise the existing generic API with its previous default provider.
		var legacy = Tools.Text.CompressText<AesManaged>(text, password);
		var current = Tools.Text.CompressText(text, password);
		Assert.That(Tools.Text.DecompressText(legacy, password), Is.EqualTo(text));
		Assert.That(Tools.Text.DecompressText<AesManaged>(current, password), Is.EqualTo(text));
#pragma warning restore SYSLIB0021
	}

	[Test]
	public void HistoricalCompressedFixtureDecryptsViaBothApis() {
		var ciphertext = Convert.FromBase64String("ygDJxXW1cmWreV7ITx8FkGsPa9a90m9EwbI66WyOi8TLljTHi238IzZV34BfYCuOy1ZVBtE9KjyOYms8FrbSwQ==");
		Assert.That(Tools.Text.DecompressText(ciphertext, "warning-test-password"), Is.EqualTo("Compression compatibility"));
#pragma warning disable SYSLIB0021 // Verify compatibility against the former concrete provider.
		Assert.That(Tools.Text.DecompressText<AesManaged>(ciphertext, "warning-test-password"), Is.EqualTo("Compression compatibility"));
#pragma warning restore SYSLIB0021
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("review-password")]
	public void DefaultAndLegacyGenericFilePipelinesInteroperate(string password) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var source = Path.Combine(directory, "source.bin");
		var compressed = Path.Combine(directory, "compressed.bin");
		var restored = Path.Combine(directory, "restored.bin");
		var legacy = Path.Combine(directory, "legacy.bin");
		var legacyRestored = Path.Combine(directory, "legacy-restored.bin");
		var data = Enumerable.Range(0, 4096).Select(index => (byte)index).ToArray();
		Tools.FileSystem.AppendAllBytes(source, data);
		Tools.FileSystem.CompressFile(source, compressed, password);
		Tools.FileSystem.DecompressFile(compressed, restored, password);
		using var restoredStream = File.OpenRead(restored);
		Assert.That(restoredStream.ReadAll(), Is.EqualTo(data));
#pragma warning disable SYSLIB0021 // Exercise the retained generic file APIs against the former provider.
		Tools.FileSystem.DecompressFile<AesManaged>(compressed, legacyRestored, password);
		Tools.FileSystem.CompressFile<AesManaged>(source, legacy, password);
#pragma warning restore SYSLIB0021
		using var legacyRestoredStream = File.OpenRead(legacyRestored);
		Assert.That(legacyRestoredStream.ReadAll(), Is.EqualTo(data));
		var currentRestored = Path.Combine(directory, "current-restored.bin");
		Tools.FileSystem.DecompressFile(legacy, currentRestored, password);
		using var currentRestoredStream = File.OpenRead(currentRestored);
		Assert.That(currentRestoredStream.ReadAll(), Is.EqualTo(data));
	}

	[Test]
	public void DefaultAlgorithmRetainsAesStreamParameters() {
		using var algorithm = Tools.Crypto.PrepareSymmetricAlgorithm("review-password");
		Assert.That(algorithm.KeySize, Is.EqualTo(256));
		Assert.That(algorithm.BlockSize, Is.EqualTo(128));
		Assert.That(algorithm.Padding, Is.EqualTo(PaddingMode.PKCS7));
		Assert.That(algorithm.Mode, Is.EqualTo(CipherMode.CBC));
		Assert.That(algorithm.Key, Is.EqualTo(PBKDF2.DeriveKey("review-password", Array.Empty<byte>(), 100, 32)));
	}

	[Test]
	public void SystemRandomFactorySupportsEmptyAndPopulatedBuffers() {
		var random = new SystemCRNG();
		Assert.That(() => random.NextBytes(Array.Empty<byte>()), Throws.Nothing);
		Assert.That(() => random.NextBytes(new byte[64]), Throws.Nothing);
	}
}
