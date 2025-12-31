// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework;
using System;

namespace AbstractProtocol.AnonymousPipeComplex;

[Serializable]
public class NotifyNewTransaction {
	public string Name { get; set; }
	public int Age { get; set; }

	internal static NotifyNewTransaction GenRandom() => new()
		{ Name = Guid.NewGuid().ToStrictAlphaString(), Age = Tools.Maths.RNG.Next(5, 75) };
}


