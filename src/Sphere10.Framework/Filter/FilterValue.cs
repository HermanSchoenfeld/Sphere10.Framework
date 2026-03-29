// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Abstract base for the operand value(s) of a <see cref="FilterCondition"/>.
/// Concrete subclasses carry the actual data:
/// <see cref="FilterValueNone"/>, <see cref="FilterValueSingle"/>, <see cref="FilterValueMultiple"/>.
/// </summary>
public abstract class FilterValue {

	/// <summary>
	/// The cardinality of this value.
	/// </summary>
	public abstract FilterValueType Type { get; }

	/// <summary>
	/// A hint describing the intended data type of the value(s).
	/// When <see cref="FilterValueDataType.Auto"/>, the compiler infers the type from the target property.
	/// </summary>
	public FilterValueDataType DataType { get; set; }

	protected FilterValue(FilterValueDataType dataType = FilterValueDataType.Auto) {
		DataType = dataType;
	}

	/// <summary>Creates a <see cref="FilterValueNone"/> (no operand).</summary>
	public static FilterValueNone None(FilterValueDataType dataType = FilterValueDataType.Auto)
		=> new(dataType);

	/// <summary>Creates a <see cref="FilterValueSingle"/>.</summary>
	public static FilterValueSingle Single(object operand, FilterValueDataType dataType = FilterValueDataType.Auto)
		=> new(operand, dataType);

	/// <summary>Creates a <see cref="FilterValueMultiple"/>.</summary>
	public static FilterValueMultiple Multiple(object[] operands, FilterValueDataType dataType = FilterValueDataType.Auto)
		=> new(operands, dataType);
}

/// <summary>
/// A filter value with no operand (used by unary operators such as <see cref="FilterOperator.IsEmpty"/>).
/// </summary>
public class FilterValueNone : FilterValue {

	public override FilterValueType Type => FilterValueType.None;

	public FilterValueNone(FilterValueDataType dataType = FilterValueDataType.Auto) : base(dataType) {
	}
}

/// <summary>
/// A filter value carrying a single operand (used by most comparison operators).
/// </summary>
public class FilterValueSingle : FilterValue {

	public override FilterValueType Type => FilterValueType.Single;

	/// <summary>
	/// The operand value.
	/// </summary>
	public object Operand { get; set; }

	public FilterValueSingle(object operand, FilterValueDataType dataType = FilterValueDataType.Auto) : base(dataType) {
		Operand = operand;
	}
}

/// <summary>
/// A filter value carrying multiple operands
/// (e.g. [from, to] for <see cref="FilterOperator.Between"/>,
/// or an arbitrary set for <see cref="FilterOperator.In"/>).
/// </summary>
public class FilterValueMultiple : FilterValue {

	public override FilterValueType Type => FilterValueType.Multiple;

	/// <summary>
	/// The operand values.
	/// </summary>
	public object[] Operands { get; set; }

	public FilterValueMultiple(object[] operands, FilterValueDataType dataType = FilterValueDataType.Auto) : base(dataType) {
		Operands = operands ?? [];
	}
}
