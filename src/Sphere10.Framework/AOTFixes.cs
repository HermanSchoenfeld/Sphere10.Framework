// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Diagnostics.CodeAnalysis;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

/// <summary>Preserves the reflected metadata required by this project's factories.</summary>
internal static class AOTFixes {
	// Metadata reopening uses the Type overload even for dimensions first configured generically.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ObjectSpaceDimensionBuilder<>))]
	internal static void Retain() {
	}
}
