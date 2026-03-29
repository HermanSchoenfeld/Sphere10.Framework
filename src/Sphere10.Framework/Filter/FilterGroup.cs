// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A composite node in a <see cref="FilterExpression"/> tree.
/// Combines child expressions using a logical <see cref="FilterConjunction"/> (AND / OR).
/// Groups can be nested to arbitrary depth, forming Notion-style compound filters.
/// </summary>
public class FilterGroup : FilterExpression {

	/// <summary>
	/// The logical conjunction that combines the <see cref="Expressions"/>.
	/// </summary>
	public FilterConjunction Conjunction { get; set; }

	/// <summary>
	/// The child filter expressions (conditions or nested groups).
	/// </summary>
	public List<FilterExpression> Expressions { get; set; } = new();

	public FilterGroup() {
	}

	public FilterGroup(FilterConjunction conjunction, params FilterExpression[] expressions) {
		Conjunction = conjunction;
		Expressions.AddRange(expressions);
	}
}
