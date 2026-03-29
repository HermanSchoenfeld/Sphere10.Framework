// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sphere10.Framework;

/// <summary>
/// Compiles a <see cref="FilterExpression"/> tree into an <see cref="Expression{TDelegate}"/> predicate
/// or a compiled <see cref="Func{T, Boolean}"/> that can be applied to in-memory sequences or IQueryable sources.
/// </summary>
/// <remarks>
/// Property resolution is case-insensitive and supports simple (non-nested) property names.
/// String comparisons default to <see cref="StringComparison.OrdinalIgnoreCase"/>.
/// </remarks>
public static class FilterCompiler<T> {

	/// <summary>
	/// Compiles the expression tree into a lambda expression.
	/// </summary>
	public static Expression<Func<T, bool>> Compile(FilterExpression filter) {
		var param = Expression.Parameter(typeof(T), "x");
		var body = Build(filter, param);
		return Expression.Lambda<Func<T, bool>>(body, param);
	}

	/// <summary>
	/// Compiles the expression tree into a ready-to-invoke delegate.
	/// </summary>
	public static Func<T, bool> CompileFunc(FilterExpression filter) => Compile(filter).Compile();

	private static Expression Build(FilterExpression filter, ParameterExpression param) {
		return filter switch {
			FilterCondition condition => BuildCondition(condition, param),
			FilterGroup group => BuildGroup(group, param),
			_ => throw new NotSupportedException($"Unknown FilterExpression type: {filter.GetType().Name}")
		};
	}

	private static Expression BuildGroup(FilterGroup group, ParameterExpression param) {
		if (group.Expressions == null || group.Expressions.Count == 0)
			return Expression.Constant(true);

		var expressions = group.Expressions.Select(e => Build(e, param)).ToList();
		var combined = expressions[0];
		for (var i = 1; i < expressions.Count; i++) {
			combined = group.Conjunction == FilterConjunction.And
				? Expression.AndAlso(combined, expressions[i])
				: Expression.OrElse(combined, expressions[i]);
		}
		return combined;
	}

	private static Expression BuildCondition(FilterCondition condition, ParameterExpression param) {
		var property = ResolveProperty(condition.Property);
		var member = Expression.Property(param, property);

		return condition.Operator switch {
			FilterOperator.Equals => BuildComparison(Expression.Equal, member, condition.Value, property.PropertyType),
			FilterOperator.NotEquals => BuildComparison(Expression.NotEqual, member, condition.Value, property.PropertyType),
			FilterOperator.GreaterThan => BuildComparison(Expression.GreaterThan, member, condition.Value, property.PropertyType),
			FilterOperator.GreaterThanOrEqual => BuildComparison(Expression.GreaterThanOrEqual, member, condition.Value, property.PropertyType),
			FilterOperator.LessThan => BuildComparison(Expression.LessThan, member, condition.Value, property.PropertyType),
			FilterOperator.LessThanOrEqual => BuildComparison(Expression.LessThanOrEqual, member, condition.Value, property.PropertyType),
			FilterOperator.Contains => BuildStringMethod("Contains", member, condition.Value, property.PropertyType),
			FilterOperator.NotContains => Expression.Not(BuildStringMethod("Contains", member, condition.Value, property.PropertyType)),
			FilterOperator.StartsWith => BuildStringMethod("StartsWith", member, condition.Value, property.PropertyType),
			FilterOperator.EndsWith => BuildStringMethod("EndsWith", member, condition.Value, property.PropertyType),
			FilterOperator.In => BuildIn(member, condition.Values, property.PropertyType),
			FilterOperator.Between => BuildBetween(member, condition.Value, condition.ValueTo, property.PropertyType),
			FilterOperator.IsEmpty => BuildIsEmpty(member, property.PropertyType),
			FilterOperator.IsNotEmpty => Expression.Not(BuildIsEmpty(member, property.PropertyType)),
			_ => throw new NotSupportedException($"Operator {condition.Operator} is not supported.")
		};
	}

	private static Expression BuildComparison(
		Func<Expression, Expression, BinaryExpression> factory,
		MemberExpression member,
		object value,
		Type propertyType) {
		var converted = ConvertValue(value, propertyType);
		var constant = Expression.Constant(converted, propertyType);

		// For string equality, use string.Equals(a, b, OrdinalIgnoreCase)
		if (propertyType == typeof(string) && (factory.Method.Name == nameof(Expression.Equal) || factory.Method.Name == nameof(Expression.NotEqual))) {
			var equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)])!;
			var call = Expression.Call(equalsMethod, member, constant, Expression.Constant(StringComparison.OrdinalIgnoreCase));
			return factory.Method.Name == nameof(Expression.NotEqual) ? Expression.Not(call) : (Expression)call;
		}

		return factory(member, constant);
	}

	private static Expression BuildStringMethod(string methodName, MemberExpression member, object value, Type propertyType) {
		var stringValue = value?.ToString() ?? string.Empty;
		var method = typeof(string).GetMethod(methodName, [typeof(string), typeof(StringComparison)])!;
		// Null-safe: (member != null && member.Method(value, OrdinalIgnoreCase))
		var call = Expression.Call(member, method, Expression.Constant(stringValue), Expression.Constant(StringComparison.OrdinalIgnoreCase));
		if (propertyType == typeof(string)) {
			return Expression.AndAlso(
				Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
				call);
		}
		return call;
	}

	private static Expression BuildIn(MemberExpression member, object[] values, Type propertyType) {
		if (values == null || values.Length == 0)
			return Expression.Constant(false);

		var convertedValues = values.Select(v => ConvertValue(v, propertyType)).ToArray();
		var typedArray = Array.CreateInstance(propertyType, convertedValues.Length);
		for (var i = 0; i < convertedValues.Length; i++)
			typedArray.SetValue(convertedValues[i], i);

		// Enumerable.Contains(array, member)
		var containsMethod = typeof(Enumerable)
			.GetMethods(BindingFlags.Static | BindingFlags.Public)
			.First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
			.MakeGenericMethod(propertyType);

		return Expression.Call(containsMethod, Expression.Constant(typedArray), member);
	}

	private static Expression BuildBetween(MemberExpression member, object from, object to, Type propertyType) {
		var fromConverted = Expression.Constant(ConvertValue(from, propertyType), propertyType);
		var toConverted = Expression.Constant(ConvertValue(to, propertyType), propertyType);
		return Expression.AndAlso(
			Expression.GreaterThanOrEqual(member, fromConverted),
			Expression.LessThanOrEqual(member, toConverted));
	}

	private static Expression BuildIsEmpty(MemberExpression member, Type propertyType) {
		if (propertyType == typeof(string)) {
			return Expression.Call(typeof(string), nameof(string.IsNullOrEmpty), Type.EmptyTypes, member);
		}
		if (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null) {
			return Expression.Equal(member, Expression.Constant(null, propertyType));
		}
		// Value types are never empty
		return Expression.Constant(false);
	}

	private static PropertyInfo ResolveProperty(string name) {
		var property = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (property == null)
			throw new InvalidOperationException($"Property '{name}' not found on type '{typeof(T).Name}'.");
		return property;
	}

	private static object ConvertValue(object value, Type targetType) {
		if (value == null)
			return null;

		var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

		if (underlying.IsInstanceOfType(value))
			return value;

		if (underlying.IsEnum && value is string s)
			return Enum.Parse(underlying, s, ignoreCase: true);

		return Tools.Object.ChangeType(value, underlying);
	}
}
