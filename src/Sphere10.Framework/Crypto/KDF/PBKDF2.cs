// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Security.Cryptography;

namespace Sphere10.Framework;

public static class PBKDF2 {
	public static byte[] DeriveKey(string secret, byte[] salt, int iterations, int keyLength) {
		Guard.ArgumentGT(keyLength, 0, nameof(keyLength));
		// Preserve the original SHA-1 PRF so existing encrypted data remains readable.
		return Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, HashAlgorithmName.SHA1, keyLength);
	}

	/// <summary>Derives key material from a binary secret using an explicitly selected PRF.</summary>
	public static byte[] DeriveKey(byte[] secret, byte[] salt, int iterations, int keyLength, HashAlgorithmName hashAlgorithm) {
		Guard.ArgumentNotNull(secret, nameof(secret));
		Guard.ArgumentNotNull(salt, nameof(salt));
		Guard.ArgumentGT(keyLength, 0, nameof(keyLength));
		Guard.ArgumentGT(iterations, 0, nameof(iterations));
		return Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, hashAlgorithm, keyLength);
	}
}

