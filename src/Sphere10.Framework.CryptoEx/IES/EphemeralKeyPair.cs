// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Ugochukwu Mmaduekwe
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Org.BouncyCastle.Crypto;

namespace Sphere10.Framework.CryptoEx.IES;

public class EphemeralKeyPair {
	private readonly AsymmetricCipherKeyPair _keyPair;
	private readonly KeyEncoder _publicKeyEncoder;

	public EphemeralKeyPair(AsymmetricCipherKeyPair keyPair, KeyEncoder publicKeyEncoder) {
		_keyPair = keyPair;
		_publicKeyEncoder = publicKeyEncoder;
	}

	public AsymmetricCipherKeyPair GetKeyPair() {
		return _keyPair;
	}

	public byte[] GetEncodedPublicKey() {
		return _publicKeyEncoder.GetEncoded(_keyPair.Public);
	}
}

