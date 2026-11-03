// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Application.UI;

/// <summary>A schema-owned column identity with optional width and stretch preferences.</summary>
public sealed class ApplicationGridColumnPreferences {
	/// <summary>The stable key supplied by the column schema.</summary>
	[ReadOnly(true)]
	[DisplayName("Column key")]
	[Browsable(false)]
	[Description("The stable identity supplied by the grid's column schema.")]
	public string Key { get; set; }

	/// <summary>The readable column name supplied by the schema.</summary>
	[ReadOnly(true)]
	[DisplayName("Column")]
	[Browsable(false)]
	[Description("The readable column name supplied by the grid's schema.")]
	public string Title { get; set; }

	/// <summary>Null restores natural sizing; native values use 96-DPI logical pixels and web values use CSS pixels.</summary>
	[DisplayName("Manual width")]
	[Description("An optional logical pixel width; leave empty to use natural sizing.")]
	public double? Width { get; set; }

	/// <summary>Null retains the schema's stretch setting; manual width takes precedence.</summary>
	[DisplayName("Stretch to fit")]
	[Description("Uses spare grid width for this column. Leave empty to use its default. Manual width takes precedence.")]
	public bool? ExpandsToFit { get; set; }

	/// <summary>Shows the readable schema name in ordinary collection editors without using it as persistent identity.</summary>
	public override string ToString() {
		return Title ?? Key ?? string.Empty;
	}
}
