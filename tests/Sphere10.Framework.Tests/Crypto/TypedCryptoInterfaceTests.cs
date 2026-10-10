// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TypedCryptoInterfaceTests {
	[Test]
	public void SignatureScheme_BaseContractUsesTypedKeysAndNonce() {
		var implementation = new TypedScheme();
		IDigitalSignatureScheme scheme = implementation;
		var seed = new byte[] { 1, 2, 3 };
		var privateKey = scheme.CreatePrivateKey(seed);
		var publicKey = scheme.DerivePublicKey(privateKey, 42);

		Assert.That(privateKey, Is.TypeOf<PrivateKey>());
		Assert.That(publicKey, Is.TypeOf<PublicKey>());
		Assert.That(publicKey.RawBytes, Is.EqualTo(seed));
		Assert.That(implementation.LastNonce, Is.EqualTo(42));
		Assert.That(scheme.IsPublicKey(privateKey, publicKey.RawBytes), Is.True);
		Assert.That(scheme.SignDigest(privateKey, seed, 43), Is.EqualTo(seed));
		Assert.That(implementation.LastNonce, Is.EqualTo(43));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void SignatureScheme_ParseAdaptersPreserveSuccessAndBoxedOutKeys(bool success) {
		IDigitalSignatureScheme scheme = new TypedScheme();
		var bytes = success ? new byte[] { 9 } : Array.Empty<byte>();

		Assert.That(scheme.TryParsePrivateKey(bytes, out var privateKey), Is.EqualTo(success));
		Assert.That(scheme.TryParsePublicKey(bytes, out var publicKey), Is.EqualTo(success));
		Assert.That(privateKey, Is.TypeOf<PrivateKey>());
		Assert.That(publicKey, Is.TypeOf<PublicKey>());
		Assert.That(privateKey.RawBytes, Is.EqualTo(bytes));
		Assert.That(publicKey.RawBytes, Is.EqualTo(bytes));
	}

	[Test]
	public void SignatureScheme_TypedContractParsesInferredKeyTypes() {
		IDigitalSignatureScheme<PrivateKey, PublicKey> scheme = new TypedScheme();
		var bytes = new byte[] { 9 };

		Assert.That(scheme.TryParsePrivateKey(bytes, out var privateKey), Is.True);
		Assert.That(scheme.TryParsePublicKey(bytes, out var publicKey), Is.True);
		PrivateKey typedPrivateKey = privateKey;
		PublicKey typedPublicKey = publicKey;
		Assert.That(typedPrivateKey.RawBytes, Is.EqualTo(bytes));
		Assert.That(typedPublicKey.RawBytes, Is.EqualTo(bytes));
	}

	[Test]
	public void SignatureScheme_AdaptersRetainInvalidKeyCastBehavior() {
		IDigitalSignatureScheme scheme = new TypedScheme();
		var foreignKey = new ForeignPrivateKey();

		Assert.That(() => scheme.DerivePublicKey(foreignKey, 0), Throws.TypeOf<InvalidCastException>());
		Assert.That(() => scheme.IsPublicKey(foreignKey, Array.Empty<byte>()), Throws.TypeOf<InvalidCastException>());
		Assert.That(() => scheme.SignDigest(foreignKey, Array.Empty<byte>(), 0), Throws.TypeOf<InvalidCastException>());
	}

	[Test]
	public void Wots_BaseAndTypedContractsExposeOwnedConfiguration() {
		var input = new WOTS.Configuration(4, CHF.SHA2_256, false);
		var algorithm = new WOTS(input);
		IOTSAlgorithm<WOTS.Configuration> typed = algorithm;
		IOTSAlgorithm untyped = algorithm;

		Assert.That(typed.Config, Is.SameAs(algorithm.Config));
		Assert.That(untyped.Config, Is.SameAs(algorithm.Config));
		Assert.That(untyped.Config, Is.Not.SameAs(input));
	}

	[Test]
	public void BlakeConfiguration_BaseCloneRetainsDeepCopyBehavior() {
		var original = new Blake2b.Config(32) { Key = new byte[] { 1, 2 }, Salt = new byte[] { 3, 4 }, Personalization = new byte[] { 5, 6 } };
		var clone = (Blake2b.Config)((System.ICloneable)original).Clone();

		Assert.That(clone, Is.Not.SameAs(original));
		Assert.That(clone.OutputSizeInBytes, Is.EqualTo(32));
		Assert.That(clone.Key, Is.EqualTo(original.Key).And.Not.SameAs(original.Key));
		Assert.That(clone.Salt, Is.EqualTo(original.Salt).And.Not.SameAs(original.Salt));
		Assert.That(clone.Personalization, Is.EqualTo(original.Personalization).And.Not.SameAs(original.Personalization));
	}

	[Test]
	public void TreeAndAmsConfigurations_RetainTypedAndBaseCloneContracts() {
		ICloneable<Blake2b.TreeConfig> tree = new Blake2b.TreeConfig { FanOut = 4, MaxHeight = 2, LeafSize = 16 };
		var treeClone = (Blake2b.TreeConfig)((System.ICloneable)tree).Clone();
		Assert.That(treeClone.FanOut, Is.EqualTo(4));
		Assert.That(treeClone.LeafSize, Is.EqualTo(16));

		var original = new AMS.Configuration(new WOTS.Configuration(4, CHF.SHA2_256, false), 2);
		ICloneable<AMS.Configuration> configuration = original;
		var clone = (AMS.Configuration)((System.ICloneable)configuration).Clone();
		Assert.That(clone.H, Is.EqualTo(2));
		Assert.That(clone.OTS, Is.Not.SameAs(original.OTS));
	}

	private readonly struct PrivateKey : IPrivateKey {
		public PrivateKey(byte[] bytes) => RawBytes = bytes;
		public byte[] RawBytes { get; }
	}

	private readonly struct PublicKey : IPublicKey {
		public PublicKey(byte[] bytes) => RawBytes = bytes;
		public byte[] RawBytes { get; }
	}

	private sealed class ForeignPrivateKey : IPrivateKey {
		public byte[] RawBytes => Array.Empty<byte>();
	}

	private sealed class TypedScheme : IDigitalSignatureScheme<PrivateKey, PublicKey> {
		public DigitalSignatureSchemeTraits Traits => DigitalSignatureSchemeTraits.None;
		public IIESAlgorithm IES => null;
		public int MessageDigestLength => 3;
		public ulong LastNonce { get; private set; }

		public bool TryParsePublicKey(ReadOnlySpan<byte> bytes, out PublicKey publicKey) {
			publicKey = new PublicKey(bytes.ToArray());
			return !bytes.IsEmpty;
		}

		public bool TryParsePrivateKey(ReadOnlySpan<byte> bytes, out PrivateKey privateKey) {
			privateKey = new PrivateKey(bytes.ToArray());
			return !bytes.IsEmpty;
		}

		public PrivateKey GeneratePrivateKey(ReadOnlySpan<byte> seed) => new(seed.ToArray());

		public PublicKey DerivePublicKey(PrivateKey privateKey, ulong signerNonce) {
			LastNonce = signerNonce;
			return new PublicKey(privateKey.RawBytes);
		}

		public bool IsPublicKey(PrivateKey privateKey, ReadOnlySpan<byte> publicKeyBytes) => publicKeyBytes.SequenceEqual(privateKey.RawBytes);

		public byte[] CalculateMessageDigest(ReadOnlySpan<byte> message) => message.ToArray();

		public byte[] SignDigest(PrivateKey privateKey, ReadOnlySpan<byte> messageDigest, ulong signerNonce) {
			LastNonce = signerNonce;
			return messageDigest.ToArray();
		}

		public bool VerifyDigest(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> messageDigest, ReadOnlySpan<byte> publicKey) => signature.SequenceEqual(messageDigest);
	}
}
