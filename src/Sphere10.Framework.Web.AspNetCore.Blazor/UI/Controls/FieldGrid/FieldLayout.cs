// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

/// <summary>How a <see cref="FieldGrid"/> places each <see cref="FieldRow"/>'s label relative to its control.</summary>
public enum FieldLayout {

	/// <summary>The label above its control; the fields flow across <see cref="FieldGrid.Columns"/> columns, as on a record form.</summary>
	Stacked,

	/// <summary>"Label: [Control]": the label in an aligned left column and the control beside it, one row per field.</summary>
	Inline
}
