// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Sphere10.Framework;
using Sphere10.Framework.Maths;

// ReSharper disable CheckNamespace
namespace Tools;

public class Crypto {
	/// <summary>
	/// Coverup <see cref="bytes"/> with cryptographically random bytes. This stops any forensic analysis of the processes memory from retrieving sensitive <see cref="bytes"/>.
	/// </summary>
	/// <param name="bytes">The sensitive byte array to overwrite</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SecureErase(byte[] bytes) => RandomNumberGenerator.Fill(bytes.AsSpan(0, bytes.Length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Guid NewCryptographicGuid() => CryptographicallySecureGUIDGenerator.NewCryptographicGuid();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] GenerateCryptographicallyRandomBytes(int count) => new SystemCRNG().NextBytes(count);

	/// <summary>Compares equal-length secret values without branching on their contents. Length differences may return immediately.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => CryptographicOperations.FixedTimeEquals(left, right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string PasswordHash(string password) => PasswordHasher.CreateHash(password);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ValidatePassword(string password, string hash) => PasswordHasher.ValidatePassword(password, hash);

	/// <summary>Encodes text using the extracted legacy DES format.</summary>
	public static string EncryptStringDES(string plainText, string key) => Encryptor.EncryptStringDES(plainText, key);

	/// <summary>Decodes text using the extracted legacy DES format.</summary>
	public static string DecryptStringDES(string cipherText, string key) => Encryptor.DecryptStringDES(cipherText, key);

	/// <summary>Encrypts the legacy DES stream format with the original output ownership.</summary>
	public static void EncryptStreamDES(Stream input, Stream output, string key) => Encryptor.EncryptStreamDES(input, output, key);

	/// <summary>Decrypts the legacy DES stream format with the original output ownership.</summary>
	public static void DecryptStreamDES(Stream input, Stream output, string key) => Encryptor.DecryptStreamDES(input, output, key);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string EncryptStringAES(string plainText, string sharedSecret, string salt) => Encryptor.EncryptStringAES(plainText, sharedSecret, salt);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string EncryptStringAES(string plainText, string sharedSecret, byte[] salt) => Encryptor.EncryptStringAES(plainText, sharedSecret, salt);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string DecryptStringAES(string cipherText, string sharedSecret, string salt) => Encryptor.DecryptStringAES(cipherText, sharedSecret, salt);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string DecryptStringAES(string cipherText, string sharedSecret, byte[] salt) => Encryptor.DecryptStringAES(cipherText, sharedSecret, salt);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void EncryptStream(Stream input, Stream output, SymmetricAlgorithm symmetricAlgorithm) => Encryptor.EncryptStream(input, output, symmetricAlgorithm);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DecryptStream(Stream input, Stream output, SymmetricAlgorithm symmetricAlgorithm) => Encryptor.DecryptStream(input, output, symmetricAlgorithm);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SymmetricAlgorithm PrepareSymmetricAlgorithm(string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC)
		=> Encryptor.PrepareSymmetricAlgorithm(password, salt, paddingMode, cipherMode);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SymmetricAlgorithm PrepareSymmetricAlgorithm<TSymmetricAlgorithm>(string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC)
		where TSymmetricAlgorithm : SymmetricAlgorithm, new() {
		return Encryptor.PrepareSymmetricAlgorithm<TSymmetricAlgorithm>(password, salt, paddingMode, cipherMode);
	}
}
