// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// A leaf node in a <see cref="FilterExpression"/> tree.
/// Represents a single condition: a property compared to one or more values via an operator.
/// </summary>
public class FilterCondition : FilterExpression {

	/// <summary>
	/// The name of the property to filter on (e.g. "Name", "PID", "DateOfBirth").
	/// </summary>
	public string Property { get; set; }

	/// <summary>
	/// The comparison operator.
	/// </summary>
	public FilterOperator Operator { get; set; }

	/// <summary>
	/// The operand value(s) for this condition.
	/// </summary>
	public FilterValue Value { get; set; }

	public FilterCondition() {
		Value = FilterValue.None();
	}

	public FilterCondition(string property, FilterOperator op, FilterValue value = null) {
		Property = property;
		Operator = op;
		Value = value ?? FilterValue.None();
	}
}
