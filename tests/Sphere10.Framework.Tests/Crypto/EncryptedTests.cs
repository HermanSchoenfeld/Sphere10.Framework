// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EncryptedTests {
	[TestCase(0)]
	[TestCase(42)]
	[TestCase(-100)]
	public void RoundTripsPrimitiveWithSameCiphertextLength(int value) {
		var secret = new byte[] { 0, 1, 127, 128, 255 };
		var encrypted = new Encrypted<int>(value, secret, keyDerivationIterations: 1000);
		Assert.That(encrypted.EncryptedBytes.Length, Is.EqualTo(new BinarySerializer().SerializeBytesLE(value).Length));
		Assert.That(encrypted.TryDecrypt(secret, out var result), Is.True);
		Assert.That(result, Is.EqualTo(value));
		Assert.That(encrypted.TryDecrypt(new byte[] { 1, 2, 3 }, out result), Is.False);
		Assert.That(result, Is.Zero);
	}

	[TestCase("")]
	[TestCase("text")]
	[TestCase("Unicode \u00a3 \u03b1 \u4e16\u754c")]
	public void StringSecretUsesUtf8(string value) {
		const string secret = "secret \u03b1 \u4e16\u754c";
		var encrypted = new Encrypted<string>(value, Encoding.UTF8.GetBytes(secret), keyDerivationIterations: 1000);
		Assert.That(encrypted.EncryptedBytes.Length, Is.EqualTo(new BinarySerializer().SerializeBytesLE(value).Length));
		Assert.That(encrypted.TryDecrypt(secret, out var result), Is.True);
		Assert.That(result, Is.EqualTo(value));
		IEncrypted<string> abstraction = encrypted;
		Assert.That(abstraction.TryDecrypt(secret, out result), Is.True);
		Assert.That(result, Is.EqualTo(value));
	}

	[Test]
	public void RejectsTamperingBeforeDeserialization() {
		var secret = new byte[] { 1, 2, 3 };
		var encrypted = new Encrypted<int>(42, secret, keyDerivationIterations: 1000);
		encrypted.EncryptedBytes[0] ^= 1;
		Assert.That(encrypted.TryDecrypt(secret, out var result), Is.False);
		Assert.That(result, Is.Zero);
	}

	[Test]
	public void SetItemRefreshesCiphertextAndRejectsPreviousSecret() {
		var firstSecret = new byte[] { 1, 2, 3 };
		var nextSecret = new byte[] { 4, 5, 6 };
		var encrypted = new Encrypted<int>(42, firstSecret, keyDerivationIterations: 1000);
		var oldSalt = encrypted.Salt;
		var oldCounter = encrypted.InitialCounter;
		var oldCiphertext = (byte[])encrypted.EncryptedBytes.Clone();
		encrypted.SetItem(42, firstSecret);
		Assert.That(encrypted.Salt, Is.Not.EqualTo(oldSalt));
		Assert.That(encrypted.InitialCounter, Is.Not.EqualTo(oldCounter));
		Assert.That(encrypted.EncryptedBytes, Is.Not.EqualTo(oldCiphertext));
		encrypted.SetItem(123, nextSecret);
		Assert.That(encrypted.TryDecrypt(firstSecret, out _), Is.False);
		Assert.That(encrypted.TryDecrypt(nextSecret, out var result), Is.True);
		Assert.That(result, Is.EqualTo(123));
	}

	[Test]
	public void FailedSetPreservesPreviousValueAndMetadataIsDefensive() {
		var secret = new byte[] { 1, 2, 3 };
		var encrypted = new Encrypted<int>(42, secret, keyDerivationIterations: 1000);
		Array.Clear(encrypted.Salt);
		Array.Clear(encrypted.InitialCounter);
		Array.Clear(encrypted.AuthenticationTag);
		Assert.That(() => encrypted.SetItem(123, Array.Empty<byte>()), Throws.InstanceOf<ArgumentException>());
		Assert.That(encrypted.TryDecrypt(secret, out var result), Is.True);
		Assert.That(result, Is.EqualTo(42));
		Assert.That(encrypted.TryDecrypt(Array.Empty<byte>(), out result), Is.False);
		Assert.That(() => encrypted.TryDecrypt((byte[])null, out _), Throws.ArgumentNullException);
	}

	[Test]
	public void FactoryUsesBinarySerializer() {
		var encrypted = Encrypted.For("factory value", new byte[] { 1, 2, 3 }, keyDerivationIterations: 1000);
		Assert.That(encrypted.TryDecrypt(new byte[] { 1, 2, 3 }, out var result), Is.True);
		Assert.That(result, Is.EqualTo("factory value"));
	}

	[Test]
	public void BinarySerializerPreservesNull() {
		var encrypted = Encrypted.For<string>(null, new byte[] { 1, 2, 3 }, keyDerivationIterations: 1000);
		Assert.That(encrypted.TryDecrypt(new byte[] { 1, 2, 3 }, out var result), Is.True);
		Assert.That(result, Is.Null);
	}

	[Test]
	public void BinarySerializerPreservesPolymorphicRuntimeType() {
		var item = new Sample { Name = "object graph", Value = 123 };
		var encrypted = Encrypted.For<object>(item, new byte[] { 1, 2, 3 }, keyDerivationIterations: 1000);
		Assert.That(encrypted.TryDecrypt(new byte[] { 1, 2, 3 }, out var result), Is.True);
		Assert.That(result, Is.TypeOf<Sample>());
		Assert.That(((Sample)result).Name, Is.EqualTo(item.Name));
		Assert.That(((Sample)result).Value, Is.EqualTo(item.Value));
	}

	public class Sample {
		public string Name { get; set; }
		public int Value { get; set; }
	}
}
