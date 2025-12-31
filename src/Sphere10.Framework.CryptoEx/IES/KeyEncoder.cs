// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Ugochukwu Mmaduekwe
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Sphere10.Framework.CryptoEx.IES;

public class KeyEncoder {
	private readonly bool _usePointCompression;

	public KeyEncoder(bool usePointCompression) {
		_usePointCompression = usePointCompression;
	}

	public byte[] GetEncoded(AsymmetricKeyParameter keyParameter) {
		return (keyParameter as ECPublicKeyParameters)?.Q.GetEncoded(_usePointCompression);
	}
}

