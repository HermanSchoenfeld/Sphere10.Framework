// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Sphere10.Framework.CryptoEx.EC;
using Sphere10.Framework.CryptoEx.IES;
using Sphere10.Framework.CryptoEx.PascalCoin;
using EphemeralKeyPairGenerator = Sphere10.Framework.CryptoEx.IES.EphemeralKeyPairGenerator;

namespace Sphere10.Framework.CryptoEx.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PascalCoinEciesEncryptionVectorTests {
	public static IEnumerable<TestCaseData> NativeVerifiedEncoderVectors() {
		var resourceName = typeof(PascalCoinEciesEncryptionVectorTests).Namespace + ".Resources.pascalCoinEciesEncryptionVectors.json";
		using var resource = typeof(PascalCoinEciesEncryptionVectorTests).Assembly.GetManifestResourceStream(resourceName);
		Assert.That(resource, Is.Not.Null, "The OpenSSL-verified encoder known answers must be embedded in the test assembly.");
		using var document = JsonDocument.Parse(resource);
		var recipientScalar = document.RootElement.GetProperty("RecipientPrivateScalar").GetString();
		var ephemeralScalar = document.RootElement.GetProperty("EphemeralPrivateScalar").GetString();

		foreach (var vector in document.RootElement.GetProperty("Vectors").EnumerateArray()) {
			var curve = Tools.Enums.ParseEnum<ECDSAKeyType>(vector.GetProperty("Curve").GetString(), false);
			var caseName = vector.GetProperty("Case").GetString();
			var plaintext = vector.GetProperty("Plaintext").GetString().ToHexByteArray();
			var ciphertext = vector.GetProperty("Ciphertext").GetString().ToHexByteArray();
			yield return new TestCaseData(curve, recipientScalar, ephemeralScalar, plaintext, ciphertext)
				.SetName($"Encrypt_OpenSslVerifiedKnownAnswer_{curve}_{caseName}");
		}
	}

	[TestCaseSource(nameof(NativeVerifiedEncoderVectors))]
	public void Encrypt_FixedEphemeralKey_MatchesOpenSslVerifiedCiphertext(
		ECDSAKeyType curve,
		string recipientScalar,
		string ephemeralScalar,
		byte[] plaintext,
		byte[] expectedCiphertext
	) {
		// These exact outputs were independently decrypted by unchanged native PascalCoin OpenSSL code.
		// The permanent regression requires no native executable and never regenerates its expected answers.
		var scheme = new ECDSA(curve);
		var recipientKey = ParsePrivateScalar(scheme, recipientScalar);
		var ephemeralKey = ParsePrivateScalar(scheme, ephemeralScalar);
		var ephemeralPair = new AsymmetricCipherKeyPair(scheme.DerivePublicKey(ephemeralKey).Parameters, ephemeralKey.Parameters);
		var engine = new PascalCoinIesEngine(new ECDHBasicAgreement(),
			new PascalCoinEciesKdfBytesGenerator(DigestUtilities.GetDigest("SHA-512")), MacUtilities.GetMac("HMAC-MD5"),
			new BufferedBlockCipher(new CbcBlockCipher(new AesEngine())));
		var parameters = new ParametersWithIV(new IesWithCipherParameters(null, null, 256, 256), new byte[16]);
		var generator = new EphemeralKeyPairGenerator(new FixedKeyPairGenerator(ephemeralPair), new KeyEncoder(true));
		engine.Init(scheme.DerivePublicKey(recipientKey).Parameters, parameters, generator);

		var ciphertext = engine.ProcessBlock(plaintext, 0, plaintext.Length);

		Assert.That(ciphertext, Is.EqualTo(expectedCiphertext), "Header, ephemeral key, MAC and ciphertext must match the independently verified wire encoding.");
	}

	private static ECDSA.PrivateKey ParsePrivateScalar(ECDSA scheme, string scalar) {
		var keyBytes = new byte[scheme.KeySize];
		var scalarBytes = scalar.ToHexByteArray();
		scalarBytes.CopyTo(keyBytes, keyBytes.Length - scalarBytes.Length);
		Assert.That(scheme.TryParsePrivateKey(keyBytes, out var privateKey), Is.True);
		return privateKey;
	}

	private sealed class FixedKeyPairGenerator : IAsymmetricCipherKeyPairGenerator {
		private readonly AsymmetricCipherKeyPair _keyPair;

		public FixedKeyPairGenerator(AsymmetricCipherKeyPair keyPair) {
			_keyPair = keyPair;
		}

		public void Init(KeyGenerationParameters parameters) {
		}

		public AsymmetricCipherKeyPair GenerateKeyPair() => _keyPair;
	}
}
