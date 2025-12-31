// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Ugochukwu Mmaduekwe, Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;

namespace Sphere10.Framework.CryptoEx;

public class KeyTypePrimeFieldAttribute : Attribute {
	public KeyTypePrimeFieldAttribute(string bigIntHexValue) {
		Value = new BigInteger(1, Hex.DecodeStrict(bigIntHexValue));
	}

	public BigInteger Value { get; }

}

