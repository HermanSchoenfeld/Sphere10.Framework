// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Sphere10.Framework;

public interface IEncrypted<TObject> {
	byte[] EncryptedBytes { get; }

	bool TryDecrypt(byte[] secret, out TObject decryptedObject);

	public bool TryDecrypt(string secret, out TObject decryptedObject) {
		Guard.ArgumentNotNull(secret, nameof(secret));
		return TryDecrypt(Encoding.UTF8.GetBytes(secret), out decryptedObject);
	}
}

public class Encrypted {
	public const int DefaultKeyDerivationIterations = 600000;

	public static Encrypted<T> For<T>(T item, byte[] secret, int keyDerivationIterations = DefaultKeyDerivationIterations)
		=> new(item, secret, keyDerivationIterations);
}

/// <summary>Stores serialized ciphertext with separate salt, counter and authentication metadata.</summary>
/// <remarks>EncryptedBytes has exactly the serialized item's length. The instance retains metadata needed for decryption;
/// the ciphertext alone is not a portable envelope. Wrong secrets and modified ciphertext are rejected before deserialization.</remarks>
public sealed class Encrypted<TObject> : Encrypted, IEncrypted<TObject> {
	private readonly BinarySerializer _serializer = new();
	private byte[] _salt;
	private byte[] _initialCounter;
	private byte[] _authenticationTag;

	public Encrypted(TObject @object, byte[] secret, int keyDerivationIterations = DefaultKeyDerivationIterations) {
		Guard.ArgumentGT(keyDerivationIterations, 0, nameof(keyDerivationIterations));
		KeyDerivationIterations = keyDerivationIterations;
		SetItem(@object, secret);
	}

	public byte[] EncryptedBytes { get; private set; }

	public int KeyDerivationIterations { get; }

	public byte[] Salt => (byte[])_salt.Clone();

	public byte[] InitialCounter => (byte[])_initialCounter.Clone();

	public byte[] AuthenticationTag => (byte[])_authenticationTag.Clone();

	public void SetItem(TObject item, byte[] password) {
		Guard.ArgumentNotNull(password, nameof(password));
		Guard.ArgumentGT(password.Length, 0, nameof(password));
		var salt = Tools.Crypto.GenerateCryptographicallyRandomBytes(16);
		var initialCounter = Tools.Crypto.GenerateCryptographicallyRandomBytes(16);
		var keys = PBKDF2.DeriveKey(password, salt, KeyDerivationIterations, 64, HashAlgorithmName.SHA256);
		using var clearKeys = Tools.Scope.ExecuteOnDispose(() => CryptographicOperations.ZeroMemory(keys));
		var encryptionKey = keys.AsSpan(0, 32).ToArray();
		using var clearEncryptionKey = Tools.Scope.ExecuteOnDispose(() => CryptographicOperations.ZeroMemory(encryptionKey));
		using var storage = new MemoryStream();
		using (var stream = new EncryptedStream(storage, encryptionKey, initialCounter, leaveOpen: true))
			_serializer.Serialize(stream, item);
		var ciphertext = storage.ToArray();
		var authenticationTag = Authenticate(keys, salt, initialCounter, ciphertext);

		// Publish the new item only after serialization and encryption both succeed.
		_salt = salt;
		_initialCounter = initialCounter;
		_authenticationTag = authenticationTag;
		EncryptedBytes = ciphertext;
	}

	public bool TryDecrypt(string secret, out TObject decryptedObject) {
		Guard.ArgumentNotNull(secret, nameof(secret));
		var secretBytes = Encoding.UTF8.GetBytes(secret);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => CryptographicOperations.ZeroMemory(secretBytes));
		return TryDecrypt(secretBytes, out decryptedObject);
	}

	public bool TryDecrypt(byte[] secret, out TObject decryptedObject) {
		Guard.ArgumentNotNull(secret, nameof(secret));
		decryptedObject = default;
		if (secret.Length == 0)
			return false;
		// Verify and decrypt the same snapshot even if a caller modifies the exposed byte array.
		var ciphertext = (byte[])EncryptedBytes.Clone();
		var keys = PBKDF2.DeriveKey(secret, _salt, KeyDerivationIterations, 64, HashAlgorithmName.SHA256);
		using var clearKeys = Tools.Scope.ExecuteOnDispose(() => CryptographicOperations.ZeroMemory(keys));
		var authenticationTag = Authenticate(keys, _salt, _initialCounter, ciphertext);
		if (!CryptographicOperations.FixedTimeEquals(authenticationTag, _authenticationTag))
			return false;
		var encryptionKey = keys.AsSpan(0, 32).ToArray();
		using var clearEncryptionKey = Tools.Scope.ExecuteOnDispose(() => CryptographicOperations.ZeroMemory(encryptionKey));
		using var storage = new MemoryStream(ciphertext, writable: false);
		using var stream = new EncryptedStream(storage, encryptionKey, _initialCounter);
		decryptedObject = (TObject)_serializer.Deserialize(stream);
		return true;
	}

	private byte[] Authenticate(byte[] keys, byte[] salt, byte[] initialCounter, byte[] ciphertext) {
		// Standard HMAC with an independent key; framework hash helpers do not expose a standard HMAC primitive.
		using var authentication = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, keys.AsSpan(32));
		authentication.AppendData("Sphere10.Encrypted.v1"u8);
		authentication.AppendData(EndianBitConverter.Little.GetBytes(KeyDerivationIterations));
		authentication.AppendData(salt);
		authentication.AppendData(initialCounter);
		authentication.AppendData(ciphertext);
		return authentication.GetHashAndReset();
	}
}
