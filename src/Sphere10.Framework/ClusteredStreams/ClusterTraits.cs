// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>
/// Flags describing where a cluster sits within a chain.
/// </summary>
[Flags]
public enum ClusterTraits : byte {
	/// <summary>
	/// Interior cluster within a chain.
	/// </summary>
	None = 0,
	/// <summary>
	/// First cluster in a chain.
	/// </summary>
	Start = 1 << 0,
	/// <summary>
	/// Last cluster in a chain.
	/// </summary>
	End = 1 << 1,
}

