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
using System.Security.Cryptography;
using System.Text;

// ReSharper disable CheckNamespace
namespace Sphere10.Framework;

// TODO: Requires major refactoring and improvements
// - unify string/byte[]/stream encryption
// - unify IV/salt prepending
// - not backwards compatible
public static class Encryptor {
	/// <summary>Encodes text in the original DES format used by the extracted Dev Age helpers.</summary>
	public static string EncryptStringDES(string plainText, string key) {
		// Buffer the text without closing the stream needed by the DES operation.
		using var input = new MemoryStream();
		using (var writer = new StreamWriter(new NonClosingStream(input))) {
			writer.Write(plainText);
		}
		input.Position = 0;

		// Encode the ciphertext using the existing Base64 text format.
		using var output = new MemoryStream();
		EncryptStreamDES(input, output, key);
		return Convert.ToBase64String(output.ToArray());
	}

	/// <summary>Decodes text from the original DES format without closing its intermediate buffer.</summary>
	public static string DecryptStringDES(string cipherText, string key) {
		using var input = new MemoryStream(Convert.FromBase64String(cipherText));
		using var output = new MemoryStream();
		using var borrowedOutput = new NonClosingStream(output);
		DecryptStreamDES(input, borrowedOutput, key);
		using var reader = new StreamReader(output);
		return reader.ReadToEnd();
	}

	/// <summary>Writes the original DES ciphertext and closes output; input remains caller-owned.</summary>
	/// <remarks>Preserves the original full-Length allocation, including the zero tail of positioned input.</remarks>
	public static void EncryptStreamDES(Stream input, Stream output, string key) {
		// Adapted from Dev Age's MIT-licensed DES helper; retain its key/IV and byte framing.
		using var algorithm = DES.Create();
		algorithm.Key = Encoding.ASCII.GetBytes(key);
		algorithm.IV = Encoding.ASCII.GetBytes(key);
		using var transform = algorithm.CreateEncryptor();
		using var destination = new CryptoStream(output, transform, CryptoStreamMode.Write);
		var bytes = new byte[input.Length];
		_ = input.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false);
		destination.Write(bytes, 0, bytes.Length);
		destination.FlushFinalBlock();
	}

	/// <summary>Reads original DES ciphertext, closes output and rewinds a nonclosing output wrapper.</summary>
	public static void DecryptStreamDES(Stream input, Stream output, string key) {
		// Adapted from Dev Age's MIT-licensed helper; callers can retain output through NonClosingStream.
		using var algorithm = DES.Create();
		algorithm.Key = Encoding.ASCII.GetBytes(key);
		algorithm.IV = Encoding.ASCII.GetBytes(key);
		using var transform = algorithm.CreateDecryptor();
		using (var destination = new CryptoStream(output, transform, CryptoStreamMode.Write)) {
			var bytes = new byte[input.Length];
			_ = input.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false);
			destination.Write(bytes, 0, bytes.Length);
			destination.FlushFinalBlock();
		}
		output.Seek(0, SeekOrigin.Begin);
	}

	public static string EncryptStringAES(string plainText, string sharedSecret, string salt) => EncryptStringAES(plainText, sharedSecret, Encoding.UTF8.GetBytes(salt));

	/// <summary>
	/// Encrypt the given string using AES.  The string can be decrypted using
	/// DecryptStringAES().  The sharedSecret parameters must match.
	/// </summary>
	/// <param name="plainText">The text to encrypt.</param>
	/// <param name="sharedSecret">A password used to generate a key for encryption.</param>
	/// <param name="salt"></param>
	public static string EncryptStringAES(string plainText, string sharedSecret, byte[] salt) {
		if (String.IsNullOrEmpty(plainText))
			throw new ArgumentNullException(nameof(plainText));
		if (String.IsNullOrEmpty(sharedSecret))
			throw new ArgumentNullException(nameof(sharedSecret));

		string cipherText = null; // Encrypted string to return
		Aes algorithm = null; // RijndaelManaged object used to encrypt the data.

		try {
			// generate the key from the shared secret and the salt
			// Keep the original 1000 iterations and SHA-1 PRF for ciphertext compatibility.
			// Create a RijndaelManaged object
			algorithm = Aes.Create();
			algorithm.Key = PBKDF2.DeriveKey(sharedSecret, salt, 1000, algorithm.KeySize / 8);

			// Create a decryptor to perform the stream transform.
			var encryptor = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);

			// Create the streams used for encryption.
			using (var output = new MemoryStream()) {
				// prepend the IV (length + bytes)
				output.Write(EndianBitConverter.Little.GetBytes(algorithm.IV.Length), 0, sizeof(int));
				output.Write(algorithm.IV, 0, algorithm.IV.Length);

				using (var encryptedStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write)) {
					using (var encryptedWriter = new StreamWriter(encryptedStream)) {
						//Write all data to the stream.
						encryptedWriter.Write(plainText);
					}
				}
				cipherText = Convert.ToBase64String(output.ToArray());
			}
		} finally {
			// Clear the RijndaelManaged object.
			algorithm?.Clear();
		}

		// Return the encrypted bytes from the memory stream.
		return cipherText;
	}

	public static string DecryptStringAES(string cipherText, string sharedSecret, string salt)
		=> DecryptStringAES(cipherText, sharedSecret, Encoding.UTF8.GetBytes(salt));

	/// <summary>
	/// Decrypt the given string.  Assumes the string was encrypted using
	/// EncryptStringAES(), using an identical sharedSecret.
	/// </summary>
	/// <param name="cipherText">The text to decrypt.</param>
	/// <param name="sharedSecret">A password used to generate a key for decryption.</param>
	public static string DecryptStringAES(string cipherText, string sharedSecret, byte[] salt) {
		if (String.IsNullOrEmpty(cipherText))
			throw new ArgumentNullException("cipherText");
		if (String.IsNullOrEmpty(sharedSecret))
			throw new ArgumentNullException("sharedSecret");

		// Declare the RijndaelManaged object
		// used to decrypt the data.
		Aes algorithm = null;

		// Declare the string used to hold
		// the decrypted text.
		string plaintext = null;

		try {
			// generate the key from the shared secret and the salt
			// Keep the original 1000 iterations and SHA-1 PRF for ciphertext compatibility.
			// Create the streams used for decryption.
			var bytes = Convert.FromBase64String(cipherText);
			using (var input = new MemoryStream(bytes)) {
				// Create a RijndaelManaged object
				// with the specified key and IV.
				algorithm = Aes.Create();
				algorithm.Key = PBKDF2.DeriveKey(sharedSecret, salt, 1000, algorithm.KeySize / 8);
				// Get the initialization vector from the encrypted stream
				algorithm.IV = ReadByteArray(input);
				// Create a decrytor to perform the stream transform.
				var decryptor = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV);
				using (var decryptingStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read)) {
					// Read the decrypted bytes from the decrypting stream
					// and place them in a string.
					using (var reader = new StreamReader(decryptingStream))
						plaintext = reader.ReadToEnd();
				}
			}
		} finally {
			// Clear the RijndaelManaged object.
			algorithm?.Clear();
		}

		return plaintext;
	}

	public static void EncryptStream(Stream input, Stream output, SymmetricAlgorithm symmetricAlgorithm) {
		// prepend IV
		output.Write(symmetricAlgorithm.IV, 0, symmetricAlgorithm.IV.Length);
		using (var transform = symmetricAlgorithm.CreateEncryptor())
		using (var encryptor = new CryptoStream(output, transform, CryptoStreamMode.Write)) {
			Tools.Streams.RouteStream(input, encryptor);
		}
	}

	public static void DecryptStream(Stream input, Stream output, SymmetricAlgorithm symmetricAlgorithm) {
		// read IV
		var iv = new byte[symmetricAlgorithm.BlockSize / 8];
		var numRead = input.Read(iv, 0, iv.Length);
		if (numRead != iv.Length)
			throw new Exception("Unable to contiguously read intialization vector from input stream");
		symmetricAlgorithm.IV = iv;
		using (var transform = symmetricAlgorithm.CreateDecryptor())
		using (var decryptor = new CryptoStream(input, transform, CryptoStreamMode.Read))
			Tools.Streams.RouteStream(decryptor, output);
	}

	public static SymmetricAlgorithm PrepareSymmetricAlgorithm(string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC)
		=> PrepareSymmetricAlgorithm(Aes.Create(), password, salt, paddingMode, cipherMode);

	public static SymmetricAlgorithm PrepareSymmetricAlgorithm<TSymmetricAlgorithm>(string password, byte[] salt = null, PaddingMode paddingMode = PaddingMode.PKCS7, CipherMode cipherMode = CipherMode.CBC)
		where TSymmetricAlgorithm : SymmetricAlgorithm, new() {
		return PrepareSymmetricAlgorithm(new TSymmetricAlgorithm(), password, salt, paddingMode, cipherMode);
	}

	// Reads the length-prefixed initialization vector used by the existing AES string format.
	private static byte[] ReadByteArray(Stream stream) {
		var rawLength = new byte[sizeof(int)];
		if (stream.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
			throw new SystemException("Stream did not contain properly formatted byte array");

		var buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
		if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
			throw new SystemException("Did not read byte array properly");

		return buffer;
	}

	// Applies the existing password-derived key, padding, mode and generated IV to an owned algorithm.
	private static SymmetricAlgorithm PrepareSymmetricAlgorithm(SymmetricAlgorithm algorithm, string password, byte[] salt, PaddingMode paddingMode, CipherMode cipherMode) {
		salt ??= Array.Empty<byte>();
		algorithm.Padding = paddingMode;
		algorithm.Mode = cipherMode;

		// Preserve the existing key derivation and raw-IV stream format for stored ciphertext.
		algorithm.Key = PBKDF2.DeriveKey(password, salt, 100, algorithm.KeySize / 8);
		algorithm.IV = Tools.Crypto.GenerateCryptographicallyRandomBytes(algorithm.BlockSize / 8);
		return algorithm;
	}
}
