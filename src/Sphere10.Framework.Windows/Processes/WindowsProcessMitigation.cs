// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Native policy flags and per-policy errors. Unsupported does not mean disabled.</summary>
public sealed record WindowsProcessMitigation(string Name, uint? Flags, int? Error) {
	/// <summary>DEP's separate permanence field; absent for other policies or unsuccessful reads.</summary>
	public bool? Permanent { get; init; }
}
