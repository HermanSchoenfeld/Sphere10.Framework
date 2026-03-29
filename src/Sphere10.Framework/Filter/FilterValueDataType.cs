// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Describes the intended data type of a <see cref="FilterValue"/>.
/// Used as a hint for value conversion and UI rendering.
/// </summary>
public enum FilterValueDataType {
	/// <summary>Automatically infer the data type from the target property.</summary>
	Auto,

	/// <summary>Date or date-time value.</summary>
	Date,

	/// <summary>Free-form text value.</summary>
	Text,

	/// <summary>List / enumeration value.</summary>
	List,

	/// <summary>Numeric value (integer or floating-point).</summary>
	Number,

	/// <summary>Boolean value.</summary>
	Boolean
}
