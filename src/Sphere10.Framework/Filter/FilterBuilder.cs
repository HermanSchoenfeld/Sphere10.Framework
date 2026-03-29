// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Fluent builder for constructing <see cref="FilterExpression"/> trees.
/// </summary>
/// <example>
/// <code>
/// var filter = FilterBuilder.And(
///     FilterBuilder.Condition("Name", FilterOperator.Contains, "chrome"),
///     FilterBuilder.Or(
///         FilterBuilder.Condition("PID", FilterOperator.GreaterThan, 1000),
///         FilterBuilder.Condition("Responding", FilterOperator.Equals, true)
///     )
/// );
/// </code>
/// </example>
public static class FilterBuilder {

	public static FilterCondition Condition(string property, FilterOperator op, object value = null) =>
		new(property, op, value);

	public static FilterCondition Between(string property, object from, object to) =>
		new(property, FilterOperator.Between, from, to);

	public static FilterCondition In(string property, params object[] values) =>
		new(property, FilterOperator.In, values: values);

	public static FilterGroup And(params FilterExpression[] expressions) =>
		new(FilterConjunction.And, expressions);

	public static FilterGroup Or(params FilterExpression[] expressions) =>
		new(FilterConjunction.Or, expressions);
}
