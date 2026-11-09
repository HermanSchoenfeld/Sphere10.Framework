// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;

namespace Sphere10.Framework.Consensus;

public static class CryptoTool {

	/// <summary>
	/// Derives a checksum for a secret without revealing any information about that secret.
	/// </summary>
	/// <param name="secret">Secret to checksum.</param>
	/// <returns>A 32-bit checksum (secure).</returns>
	public static uint DeriveSecureChecksum(byte[] secret) {
		Guard.ArgumentNotNull(secret, nameof(secret));

		// Checksum's are public and used for fast lookups of secret, yet do not reveal any
		// information about secret.
		// Checksum = CastToUInt32( BytesLE[27..30]( SHA2-256( SHA2-256( secret || secret ) ) ) )
		// The read starts at offset 27, one byte before the last four, and existing checksums depend on that selection.
		var concatenated = Tools.Array.Concat(secret, secret);
		var firstHash = Hashers.Hash(CHF.SHA2_256, concatenated);
		var secondHash = Hashers.Hash(CHF.SHA2_256, firstHash);
		return EndianBitConverter.Little.ToUInt32(secondHash, 32 - 4 - 1);
	}

	/// <summary>
	/// Derives a child digest from a parent digest and an index.
	/// </summary>
	/// <param name="digest">Parent digest.</param>
	/// <param name="index">Child index.</param>
	/// <returns>A derived child digest.</returns>
	public static byte[] DeriveChildDigest(byte[] digest, ulong index) {
		Guard.ArgumentNotNull(digest, nameof(digest));

		// DerivedKey_i = H(H(i || seed))
		// Knowing the set DerivedKey_0..DerivedKey_i reveals no info about seed, double hashing prevents
		// length extension attacks.
		var indexBytes = EndianBitConverter.Little.GetBytes(index);
		var concatenated = Tools.Array.Concat(indexBytes, digest);
		var firstHash = Hashers.Hash(CHF.SHA2_256, concatenated);
		return Hashers.Hash(CHF.SHA2_256, firstHash);
	}
}




