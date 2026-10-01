// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Generators;
using Sphere10.Framework.NUnit;

namespace PackageConsumer.Libraries;

[AutoDirty(nameof(IsDirty))]
public partial class PackageUsage {
	public bool IsDirty { get; set; }

	public partial string Name { get; set; }

	public static Type AssertionHelpers => typeof(AssertEx);

	public static Type DatabaseHelpers => typeof(UnitTestDAC);
}
