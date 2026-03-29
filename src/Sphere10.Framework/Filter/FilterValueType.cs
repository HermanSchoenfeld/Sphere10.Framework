// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Describes the cardinality of a <see cref="FilterValue"/>.
/// </summary>
public enum FilterValueType {
	/// <summary>No value (used by unary operators such as <see cref="FilterOperator.IsEmpty"/>).</summary>
	None,

	/// <summary>A single value (used by most comparison operators).</summary>
	Single,

	/// <summary>Multiple values (used by <see cref="FilterOperator.In"/>, <see cref="FilterOperator.Between"/>, etc.).</summary>
	Multiple
}
