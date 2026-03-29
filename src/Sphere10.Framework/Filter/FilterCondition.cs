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
	/// The primary operand value (used by most operators).
	/// </summary>
	public object Value { get; set; }

	/// <summary>
	/// The secondary operand value (used by <see cref="FilterOperator.Between"/>).
	/// </summary>
	public object ValueTo { get; set; }

	/// <summary>
	/// Multiple operand values (used by <see cref="FilterOperator.In"/>).
	/// </summary>
	public object[] Values { get; set; }

	public FilterCondition() {
	}

	public FilterCondition(string property, FilterOperator op, object value = null, object valueTo = null, object[] values = null) {
		Property = property;
		Operator = op;
		Value = value;
		ValueTo = valueTo;
		Values = values;
	}
}
