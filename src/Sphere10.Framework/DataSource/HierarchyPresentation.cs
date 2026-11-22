// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>How a source that arranges its rows as a tree serves them.</summary>
public enum HierarchyPresentation {
	/// <summary>Top-level rows, each with its children as a child view to drill into.</summary>
	Tree,

	/// <summary>Every shown row in tree order, with its depth and expansion through <see cref="IHierarchyOutline"/>; no child views.</summary>
	Outline,

	/// <summary>The rows as they are read, with no hierarchy.</summary>
	Flat
}
