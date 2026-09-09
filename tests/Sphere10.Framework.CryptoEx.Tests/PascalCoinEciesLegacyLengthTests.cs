// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.CryptoEx.EC;

namespace Sphere10.Framework.CryptoEx.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PascalCoinEciesLegacyLengthTests {
	public static IEnumerable<TestCaseData> OversizedLegacyCiphertexts() {
		var resourceName = typeof(PascalCoinEciesLegacyLengthTests).Namespace + ".Resources.pascalCoinEciesOversizedVectors.json";
		using var resource = typeof(PascalCoinEciesLegacyLengthTests).Assembly.GetManifestResourceStream(resourceName);
		Assert.That(resource, Is.Not.Null, "Original managed ECIES ciphertext vectors must be embedded in the test assembly.");
		using var document = JsonDocument.Parse(resource);

		foreach (var vector in document.RootElement.EnumerateArray()) {
			var length = vector.GetProperty("Length").GetInt32();
			var ciphertext = vector.GetProperty("Ciphertext").GetString().ToHexByteArray();
			yield return new TestCaseData(length, ciphertext).SetName($"TryDecrypt_OriginalManagedCiphertext_{length}ZeroBytes");
		}
	}

	[TestCaseSource(nameof(OversizedLegacyCiphertexts))]
	public void TryDecrypt_OriginalManagedOversizedCiphertext_PreservesEveryByte(int length, byte[] ciphertext) {
		// Captured from the previous managed writer, which accepted large messages and wrapped its UInt16 length fields.
		// Original assembly SHA-256: 5ae5d47a7d9b3d460483344c9d8963e2c828e042efd9d853802954741d4ffa59.
		var scheme = new ECDSA(ECDSAKeyType.SECP256K1);
		var privateKeyBytes = new byte[scheme.KeySize];
		privateKeyBytes[^1] = 1;
		Assert.That(scheme.TryParsePrivateKey(privateKeyBytes, out var privateKey), Is.True);

		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.True, "Reader compatibility must include historical ciphertext beyond the native writer limit.");
		Assert.That(decrypted, Is.EqualTo(new byte[length]), "Recover the original length across UInt16 wrapping and preserve all genuine zero bytes.");
	}
}
