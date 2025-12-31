// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Ugochukwu Mmaduekwe, Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.CryptoEx.EC;

public sealed class SECP256k1 : ECDSA {
	public SECP256k1() : base(ECDSAKeyType.SECP256K1) {
	}
}

