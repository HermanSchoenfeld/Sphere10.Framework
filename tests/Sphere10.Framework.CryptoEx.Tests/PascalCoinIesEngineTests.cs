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
using System.Text.Json;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Sphere10.Framework.CryptoEx.EC;
using Sphere10.Framework.CryptoEx.IES;
using Sphere10.Framework.CryptoEx.PascalCoin;
using EphemeralKeyPairGenerator = Sphere10.Framework.CryptoEx.IES.EphemeralKeyPairGenerator;

namespace Sphere10.Framework.CryptoEx.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PascalCoinIesEngineTests {
	private const int _inputOffset = 7;
	private const int _suffixLength = 11;
	private const int _blockSize = 16;
	private const int _headerSize = 6;

	[Test]
	public void Constructor_UnpaddedCipher_IsAccepted() {
		var cipher = new BufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
		var engine = CreateEngine(cipher);

		Assert.That(engine.GetCipher(), Is.SameAs(cipher), "The engine must retain the supplied unpadded cipher.");
	}

	[Test]
	public void Constructor_AutomaticallyPaddedCipher_IsRejected() {
		var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()), new ZeroBytePadding());

		Assert.That(() => CreateEngine(cipher), Throws.InstanceOf<ArgumentException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo("cipher"));
	}

	[Test]
	public void ProcessBlock_Encrypt_NonzeroOffsetUsesOnlySelectedPlaintext([Values] ECDSAKeyType curve, [Values(0, 16, 17)] int length) {
		var scheme = CreateScheme(curve, out var privateKey);
		var engine = CreateEncryptor(scheme.DerivePublicKey(privateKey));
		var plaintext = new byte[length];
		for (var index = 0; index < plaintext.Length; index++)
			plaintext[index] = (byte)(index + 1);
		if (plaintext.Length > 0)
			plaintext[^1] = 0;
		var input = SurroundWithSentinels(plaintext);

		var ciphertext = engine.ProcessBlock(input, _inputOffset, plaintext.Length);

		Assert.That(EndianBitConverter.Little.ToUInt16(ciphertext, 2), Is.EqualTo(plaintext.Length), "Original length is a count, independent of input offset.");
		var expectedBodyLength = ((plaintext.Length + _blockSize - 1) / _blockSize) * _blockSize;
		Assert.That(EndianBitConverter.Little.ToUInt16(ciphertext, 4), Is.EqualTo(expectedBodyLength));
		Assert.That(scheme.IES.TryDecrypt(ciphertext, out var decrypted, privateKey), Is.True);
		Assert.That(decrypted, Is.EqualTo(plaintext), "Prefix and suffix bytes must not enter the encrypted message.");
	}

	[Test]
	public void ProcessBlock_Decrypt_NonzeroOffsetRecoversCapturedNativeCiphertext(
		[Values] ECDSAKeyType curve,
		[Values("empty", "sixteen_ending_zero", "seventeen_ending_zero")] string caseName
	) {
		var (plaintext, ciphertext) = ReadNativeVector(curve, caseName);
		CreateScheme(curve, out var privateKey);
		var parser = new CompressedPointParser(privateKey.Parameters.Parameters);
		var engine = CreateEngine(new BufferedBlockCipher(new CbcBlockCipher(new AesEngine())));
		engine.Init(privateKey.Parameters, CreateParameters(), parser);
		var input = SurroundWithSentinels(ciphertext);

		var decrypted = engine.ProcessBlock(input, _inputOffset, ciphertext.Length);

		Assert.That(decrypted, Is.EqualTo(plaintext), "Expected bytes come from the original OpenSSL implementation, including genuine trailing zeros.");
		Assert.That(parser.InitialPosition, Is.Zero, "The key parser must see positions relative to its bounded stream.");
		Assert.That(parser.InputLength, Is.EqualTo(ciphertext[0]), "The key parser must receive only the declared ephemeral key bytes.");
	}

	[Test]
	public void ProcessBlock_VerifiesMacBeforeCipherUse(
		[Values] ECDSAKeyType curve,
		[Values("empty", "seventeen_ending_zero")] string caseName,
		[Values] bool tamperMac
	) {
		var (plaintext, ciphertext) = ReadNativeVector(curve, caseName);
		CreateScheme(curve, out var privateKey);
		var cipher = new RecordingCipher();
		var engine = CreateEngine(cipher);
		engine.Init(privateKey.Parameters, CreateParameters(), new CompressedPointParser(privateKey.Parameters.Parameters));
		if (tamperMac)
			ciphertext[_headerSize + ciphertext[0]] ^= 1;

		if (tamperMac) {
			Assert.That(() => engine.ProcessBlock(ciphertext, 0, ciphertext.Length), Throws.TypeOf<InvalidCipherTextException>());
			Assert.That(cipher.InitializationCount, Is.Zero, "Reject the MAC before initializing the payload cipher.");
			Assert.That(cipher.FinalizationCount, Is.Zero, "An unauthenticated payload must never reach cipher decryption.");
		} else {
			Assert.That(engine.ProcessBlock(ciphertext, 0, ciphertext.Length), Is.EqualTo(plaintext));
			Assert.That(cipher.InitializationCount, Is.EqualTo(1), "The valid-message control must exercise the supplied cipher.");
			Assert.That(cipher.FinalizationCount, Is.EqualTo(1));
		}
	}

	[TestCase(-1, 1)]
	[TestCase(0, -1)]
	[TestCase(4, 1)]
	[TestCase(3, 2)]
	[TestCase(5, 0)]
	[TestCase(int.MaxValue, 1)]
	[TestCase(1, int.MaxValue)]
	public void ProcessBlock_InvalidRange_IsRejectedBeforeReadingInput(int offset, int length) {
		var scheme = CreateScheme(ECDSAKeyType.SECP256K1, out var privateKey);
		var engine = CreateEncryptor(scheme.DerivePublicKey(privateKey));

		Assert.That(() => engine.ProcessBlock(new byte[4], offset, length),
			Throws.InstanceOf<ArgumentException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo("inOff"));
	}

	[Test]
	public void ProcessBlock_NullInput_IsRejected() {
		var scheme = CreateScheme(ECDSAKeyType.SECP256K1, out var privateKey);
		var engine = CreateEncryptor(scheme.DerivePublicKey(privateKey));

		Assert.That(() => engine.ProcessBlock(null, 0, 0),
			Throws.InstanceOf<ArgumentNullException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo("in"));
	}

	private static PascalCoinIesEngine CreateEngine(BufferedBlockCipher cipher)
		=> new(new ECDHBasicAgreement(), new PascalCoinEciesKdfBytesGenerator(DigestUtilities.GetDigest("SHA-512")), MacUtilities.GetMac("HMAC-MD5"), cipher);

	private static PascalCoinIesEngine CreateEncryptor(ECDSA.PublicKey publicKey) {
		var engine = CreateEngine(new BufferedBlockCipher(new CbcBlockCipher(new AesEngine())));
		var keyPairGenerator = new ECKeyPairGenerator();
		keyPairGenerator.Init(new ECKeyGenerationParameters(publicKey.Parameters.Parameters, new SecureRandom()));
		engine.Init(publicKey.Parameters, CreateParameters(), new EphemeralKeyPairGenerator(keyPairGenerator, new KeyEncoder(true)));
		return engine;
	}

	private static ICipherParameters CreateParameters()
		=> new ParametersWithIV(new IesWithCipherParameters(null, null, 256, 256), new byte[_blockSize]);

	private static ECDSA CreateScheme(ECDSAKeyType curve, out ECDSA.PrivateKey privateKey) {
		var scheme = new ECDSA(curve);
		var privateKeyBytes = new byte[scheme.KeySize];
		privateKeyBytes[^1] = 1; // The captured native vectors use the public test scalar 01.
		Assert.That(scheme.TryParsePrivateKey(privateKeyBytes, out privateKey), Is.True);
		return scheme;
	}

	private static byte[] SurroundWithSentinels(byte[] bytes) {
		var input = new byte[_inputOffset + bytes.Length + _suffixLength];
		input.AsSpan().Fill(0xD3);
		bytes.CopyTo(input, _inputOffset);
		return input;
	}

	private static (byte[] Plaintext, byte[] Ciphertext) ReadNativeVector(ECDSAKeyType curve, string caseName) {
		var resourceName = typeof(PascalCoinIesEngineTests).Namespace + ".Resources.pascalCoinEciesCompatibilityVectors.json";
		using var resource = typeof(PascalCoinIesEngineTests).Assembly.GetManifestResourceStream(resourceName);
		Assert.That(resource, Is.Not.Null, "The captured native compatibility vectors must be embedded.");
		using var document = JsonDocument.Parse(resource);
		var vector = document.RootElement.GetProperty("Vectors").EnumerateArray().Single(candidate =>
			candidate.GetProperty("Source").GetString() == "OpenSsl" &&
			candidate.GetProperty("Curve").GetString() == curve.ToString() &&
			candidate.GetProperty("Case").GetString() == caseName);
		return (vector.GetProperty("Plaintext").GetString().ToHexByteArray(), vector.GetProperty("Ciphertext").GetString().ToHexByteArray());
	}

	private sealed class RecordingCipher : BufferedBlockCipher {
		public RecordingCipher()
			: base(new CbcBlockCipher(new AesEngine())) {
		}

		public int InitializationCount { get; private set; }

		public int FinalizationCount { get; private set; }

		public override void Init(bool forEncryption, ICipherParameters parameters) {
			InitializationCount++;
			base.Init(forEncryption, parameters);
		}

		public override byte[] DoFinal(byte[] input, int inOff, int inLen) {
			FinalizationCount++;
			return base.DoFinal(input, inOff, inLen);
		}
	}

	private sealed class CompressedPointParser : IKeyParser {
		private readonly ECDomainParameters _domainParameters;

		public CompressedPointParser(ECDomainParameters domainParameters) {
			_domainParameters = domainParameters;
		}

		public long InitialPosition { get; private set; }

		public long InputLength { get; private set; }

		public AsymmetricKeyParameter ReadKey(Stream stream) {
			InitialPosition = stream.Position;
			InputLength = stream.Length;
			var encodedPoint = new byte[1 + (_domainParameters.Curve.FieldSize + 7) / 8];
			stream.ReadExactly(encodedPoint);
			return new ECPublicKeyParameters(_domainParameters.Curve.DecodePoint(encodedPoint), _domainParameters);
		}
	}
}
