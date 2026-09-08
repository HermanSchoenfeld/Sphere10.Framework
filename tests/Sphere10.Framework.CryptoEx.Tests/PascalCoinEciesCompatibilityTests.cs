// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.CryptoEx.EC;

namespace Sphere10.Framework.CryptoEx.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PascalCoinEciesCompatibilityTests {
	private const int _headerSize = 6;
	private const int _cipherBlockSize = 16;
	private const int _macSize = 16;
	private const int _maximumPlaintextLength = 32000;

	public static IEnumerable<TestCaseData> CapturedCiphertexts() {
		var resourceName = typeof(PascalCoinEciesCompatibilityTests).Namespace + ".Resources.pascalCoinEciesCompatibilityVectors.json";
		using var resource = typeof(PascalCoinEciesCompatibilityTests).Assembly.GetManifestResourceStream(resourceName);
		Assert.That(resource, Is.Not.Null, "The captured PascalCoin compatibility vectors must be embedded in the test assembly.");
		using var document = JsonDocument.Parse(resource);
		var privateScalar = document.RootElement.GetProperty("PrivateScalar").GetString();

		foreach (var vector in document.RootElement.GetProperty("Vectors").EnumerateArray()) {
			var curve = Tools.Enums.ParseEnum<ECDSAKeyType>(vector.GetProperty("Curve").GetString(), false);
			var source = vector.GetProperty("Source").GetString();
			var caseName = vector.GetProperty("Case").GetString();
			var plaintext = vector.GetProperty("Plaintext").GetString().ToHexByteArray();
			var ciphertext = vector.GetProperty("Ciphertext").GetString().ToHexByteArray();
			yield return new TestCaseData(curve, privateScalar, ciphertext, plaintext).SetName($"TryDecrypt_Captured_{source}_{curve}_{caseName}");
		}
	}

	[TestCaseSource(nameof(CapturedCiphertexts))]
	public void TryDecrypt_CapturedCiphertext_RecoversOriginalBytes(ECDSAKeyType curve, string privateScalar, byte[] ciphertext, byte[] plaintext) {
		// These include native records and historical managed records with an extra aligned padding block.
		var scheme = CreateScheme(curve, out var privateKey, privateScalar);
		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.True);
		Assert.That(decrypted, Is.EqualTo(plaintext), "Recover every original byte, including genuine trailing zeros.");
	}

	[Test]
	public void Encrypt_BinaryMessage_UsesNativeHeaderAndPreservesBytes(
		[Values] ECDSAKeyType curve,
		[Values(0, 1, 15, 16, 17, 31, 32, 33, 98, _maximumPlaintextLength)] int length
	) {
		var scheme = CreateScheme(curve, out var privateKey);
		var plaintext = new byte[length];
		for (var index = 0; index < plaintext.Length; index++)
			plaintext[index] = (byte)(index % 256);
		if (plaintext.Length > 0)
			plaintext[^1] = 0; // Force the binary-data case that used to fail nondeterministically.

		var ciphertext = scheme.IES.Encrypt(plaintext, scheme.DerivePublicKey(privateKey));
		var expectedBodyLength = ((length + _cipherBlockSize - 1) / _cipherBlockSize) * _cipherBlockSize;
		Assert.That(ciphertext[0], Is.EqualTo(scheme.CompressedPublicKeySize));
		Assert.That(ciphertext[1], Is.EqualTo(_macSize));
		Assert.That(EndianBitConverter.Little.ToUInt16(ciphertext, 2), Is.EqualTo(length));
		Assert.That(EndianBitConverter.Little.ToUInt16(ciphertext, 4), Is.EqualTo(expectedBodyLength));
		Assert.That(ciphertext.Length, Is.EqualTo(_headerSize + scheme.CompressedPublicKeySize + _macSize + expectedBodyLength));
		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.True);
		Assert.That(decrypted, Is.EqualTo(plaintext));
	}

	[Test]
	public void Encrypt_MessageExceedingNativeLimit_IsRejected([Values] ECDSAKeyType curve) {
		var scheme = CreateScheme(curve, out var privateKey);
		var publicKey = scheme.DerivePublicKey(privateKey);
		Assert.That(() => scheme.IES.Encrypt(new byte[_maximumPlaintextLength + 1], publicKey), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase("key_zero")]
	[TestCase("key_short")]
	[TestCase("key_long")]
	[TestCase("key_invalid_encoding")]
	[TestCase("mac_zero")]
	[TestCase("mac_short")]
	[TestCase("mac_long")]
	[TestCase("body_zero")]
	[TestCase("body_unaligned")]
	[TestCase("body_short")]
	[TestCase("body_long")]
	[TestCase("original_exceeds_body")]
	[TestCase("truncated_header")]
	[TestCase("truncated_key")]
	[TestCase("truncated_mac")]
	[TestCase("truncated_body")]
	[TestCase("appended_byte")]
	[TestCase("appended_block")]
	public void TryDecrypt_MalformedFraming_FailsWithoutReturningPlaintext(string mutation) {
		var scheme = CreateScheme(ECDSAKeyType.SECP256K1, out var privateKey);
		var plaintext = new byte[17];
		plaintext[0] = 0x41;
		var ciphertext = scheme.IES.Encrypt(plaintext, scheme.DerivePublicKey(privateKey));

		switch (mutation) {
			case "key_zero":
				ciphertext[0] = 0;
				break;
			case "key_short":
				ciphertext[0]--;
				break;
			case "key_long":
				ciphertext[0] = byte.MaxValue;
				break;
			case "key_invalid_encoding":
				ciphertext[_headerSize] = 0;
				break;
			case "mac_zero":
				ciphertext[1] = 0;
				break;
			case "mac_short":
				ciphertext[1]--;
				break;
			case "mac_long":
				ciphertext[1]++;
				break;
			case "body_zero":
				EndianBitConverter.Little.GetBytes((ushort)0).CopyTo(ciphertext, 4);
				break;
			case "body_unaligned":
				EndianBitConverter.Little.GetBytes((ushort)31).CopyTo(ciphertext, 4);
				break;
			case "body_short":
				EndianBitConverter.Little.GetBytes((ushort)16).CopyTo(ciphertext, 4);
				break;
			case "body_long":
				EndianBitConverter.Little.GetBytes((ushort)48).CopyTo(ciphertext, 4);
				break;
			case "original_exceeds_body":
				EndianBitConverter.Little.GetBytes((ushort)33).CopyTo(ciphertext, 2);
				break;
			case "truncated_header":
				ciphertext = ciphertext.AsSpan(0, _headerSize - 1).ToArray();
				break;
			case "truncated_key":
				ciphertext = ciphertext.AsSpan(0, _headerSize + ciphertext[0] - 1).ToArray();
				break;
			case "truncated_mac":
				ciphertext = ciphertext.AsSpan(0, _headerSize + ciphertext[0] + _macSize - 1).ToArray();
				break;
			case "truncated_body":
				ciphertext = ciphertext.AsSpan(0, ciphertext.Length - 1).ToArray();
				break;
			case "appended_byte":
				ciphertext = Tools.Array.Concat<byte>(ciphertext, new byte[1]);
				break;
			case "appended_block":
				ciphertext = Tools.Array.Concat<byte>(ciphertext, new byte[_cipherBlockSize]);
				break;
		}

		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.False);
		Assert.That(decrypted, Is.Null, "Malformed ciphertext must never expose partial plaintext.");
	}

	[TestCase("mac")]
	[TestCase("body")]
	public void TryDecrypt_ChangedAuthenticatedBytes_Fails(string mutation) {
		var scheme = CreateScheme(ECDSAKeyType.SECP256K1, out var privateKey);
		var ciphertext = scheme.IES.Encrypt(new byte[] { 0x41, 0 }, scheme.DerivePublicKey(privateKey));
		var offset = _headerSize + ciphertext[0] + (mutation == "body" ? _macSize : 0);
		ciphertext[offset] ^= 1;

		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.False);
		Assert.That(decrypted, Is.Null);
	}

	[Test]
	public void TryDecrypt_EmptyBody_StillRequiresAuthentication([Values] ECDSAKeyType curve, [Values("mac", "recipient")] string mutation) {
		var scheme = CreateScheme(curve, out var privateKey);
		var ciphertext = scheme.IES.Encrypt(Array.Empty<byte>(), scheme.DerivePublicKey(privateKey));
		Assert.That(EndianBitConverter.Little.ToUInt16(ciphertext, 4), Is.Zero, "Exercise native empty-body encoding.");
		if (mutation == "mac") {
			ciphertext[_headerSize + ciphertext[0]] ^= 1;
		} else {
			var otherPrivateBytes = new byte[scheme.KeySize];
			otherPrivateBytes[^1] = 2;
			privateKey = (ECDSA.PrivateKey)scheme.ParsePrivateKey(otherPrivateBytes);
		}

		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.False, "Empty plaintext must not bypass MAC or recipient-key verification.");
		Assert.That(decrypted, Is.Null, "Failed authentication must not be reported as successfully decrypted empty plaintext.");
	}

	[Test]
	public void TryDecrypt_OriginalLengthExcludesNonzeroPlaintext_Fails() {
		var scheme = CreateScheme(ECDSAKeyType.SECP256K1, out var privateKey);
		var ciphertext = scheme.IES.Encrypt(new byte[] { 0x41, 1 }, scheme.DerivePublicKey(privateKey));
		EndianBitConverter.Little.GetBytes((ushort)1).CopyTo(ciphertext, 2);

		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.False, "Nonzero data cannot be accepted as zero padding.");
		Assert.That(decrypted, Is.Null);
	}

	[Test]
	public void TryDecrypt_OriginalLengthRemainsUnauthenticated([Values] ECDSAKeyType curve) {
		var scheme = CreateScheme(curve, out var privateKey);
		var ciphertext = scheme.IES.Encrypt(new byte[] { 0x41 }, scheme.DerivePublicKey(privateKey));
		EndianBitConverter.Little.GetBytes((ushort)2).CopyTo(ciphertext, 2);

		// Legacy compatibility cannot authenticate this header: the cipher body and MAC are unchanged.
		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.True);
		Assert.That(decrypted, Is.EqualTo(new byte[] { 0x41, 0 }), "Length recovery is not a repair of the legacy format's metadata integrity.");
	}

	private static ECDSA CreateScheme(ECDSAKeyType curve, out ECDSA.PrivateKey privateKey, string privateScalar = "01") {
		var scheme = new ECDSA(curve);
		var privateBytes = new byte[scheme.KeySize];
		var scalarBytes = privateScalar.ToHexByteArray();
		scalarBytes.CopyTo(privateBytes, privateBytes.Length - scalarBytes.Length);
		privateKey = (ECDSA.PrivateKey)scheme.ParsePrivateKey(privateBytes);
		return scheme;
	}
}
