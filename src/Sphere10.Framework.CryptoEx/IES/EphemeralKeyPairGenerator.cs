// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Ugochukwu Mmaduekwe
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Org.BouncyCastle.Crypto;

namespace Sphere10.Framework.CryptoEx.IES;

public class EphemeralKeyPairGenerator {
	private readonly IAsymmetricCipherKeyPairGenerator _gen;
	private readonly KeyEncoder _keyEncoder;

	public EphemeralKeyPairGenerator(IAsymmetricCipherKeyPairGenerator gen, KeyEncoder keyEncoder) {
		_gen = gen;
		_keyEncoder = keyEncoder;
	}

	public EphemeralKeyPair Generate() {
		AsymmetricCipherKeyPair eph = _gen.GenerateKeyPair();

		// Encode the ephemeral public key
		return new EphemeralKeyPair(eph, _keyEncoder);
	}
}

