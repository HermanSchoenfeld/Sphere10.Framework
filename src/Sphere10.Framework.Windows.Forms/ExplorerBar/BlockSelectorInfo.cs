// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Theme settings for the block selector buttons that an <see cref="ApplicationBar"/> stacks beneath its <see cref="TaskPane"/>.
/// <see cref="Shape"/> chooses the layout and <see cref="ColorStyle"/> the colour scheme. Every other entry is an optional override:
/// a null metric takes the shape's default and an empty colour takes the colour scheme's, which the
/// <see cref="BlockSelectorColorStyle.ExpandoHeader"/> scheme derives from the theme's header, expando and task pane settings.
/// Metrics are logical pixels at 96 DPI. The caption always uses the header's title font.
/// </summary>
public class BlockSelectorInfo {

	public BlockSelectorInfo() => SetDefaultValues();

	/// <summary>How the buttons are laid out and filled.</summary>
	[DefaultValue(BlockSelectorShape.Buttons), Description("How the block buttons are laid out and filled")]
	public BlockSelectorShape Shape { get; set; }

	/// <summary>The colour scheme that supplies every colour left empty here.</summary>
	[DefaultValue(BlockSelectorColorStyle.ExpandoHeader), Description("The colour scheme of the block buttons")]
	public BlockSelectorColorStyle ColorStyle { get; set; }

	/// <summary>The height of each button, or null for the shape's default.</summary>
	[DefaultValue(null), Description("The height of each block button in logical pixels; empty uses the shape's default")]
	public int? ButtonHeight {
		get;
		set {
			if (value.HasValue)
				Guard.ArgumentGT(value.Value, 0, nameof(value));
			field = value;
		}
	}

	/// <summary>The vertical gap between adjacent buttons, or null for the shape's default.</summary>
	[DefaultValue(null), Description("The vertical gap between block buttons in logical pixels; empty uses the shape's default")]
	public int? ButtonSpacing {
		get;
		set {
			if (value.HasValue)
				Guard.ArgumentGTE(value.Value, 0, nameof(value));
			field = value;
		}
	}

	/// <summary>The radius of every corner of a button, or null for the shape's default.</summary>
	[DefaultValue(null), Description("The corner radius of block buttons in logical pixels; empty uses the shape's default")]
	public int? CornerRadius {
		get;
		set {
			if (value.HasValue)
				Guard.ArgumentGTE(value.Value, 0, nameof(value));
			field = value;
		}
	}

	/// <summary>The width and height a block's image is drawn at, or null for the shape's default.</summary>
	[DefaultValue(null), Description("The size of a block button's image in logical pixels; empty uses the shape's default")]
	public int? IconSize {
		get;
		set {
			if (value.HasValue)
				Guard.ArgumentGT(value.Value, 0, nameof(value));
			field = value;
		}
	}

	/// <summary>The opacity, from 0 to 255, of the white sheen over the upper half of each button. Zero draws a plain gradient.</summary>
	[DefaultValue(0), Description("The opacity of the sheen over the upper half of block buttons, from 0 to 255")]
	public int GlossAlpha {
		get;
		set {
			Guard.ArgumentInRange(value, 0, 255, nameof(value));
			field = value;
		}
	}

	/// <summary>The first colour of an idle button's gradient, at the top of a button or the left of a tag.</summary>
	[Description("The first gradient colour of an idle block button; empty uses the colour style")]
	public Color NormalGradientStartColor { get; set; }

	/// <summary>The last colour of an idle button's gradient.</summary>
	[Description("The last gradient colour of an idle block button; empty uses the colour style")]
	public Color NormalGradientEndColor { get; set; }

	/// <summary>The first colour of a button under the mouse. A pressed button draws the hot gradient reversed.</summary>
	[Description("The first gradient colour of a block button under the mouse; empty uses the colour style")]
	public Color HotGradientStartColor { get; set; }

	/// <summary>The last colour of a button under the mouse.</summary>
	[Description("The last gradient colour of a block button under the mouse; empty uses the colour style")]
	public Color HotGradientEndColor { get; set; }

	/// <summary>The first colour of the selected block's button.</summary>
	[Description("The first gradient colour of the selected block button; empty uses the colour style")]
	public Color SelectedGradientStartColor { get; set; }

	/// <summary>The last colour of the selected block's button.</summary>
	[Description("The last gradient colour of the selected block button; empty uses the colour style")]
	public Color SelectedGradientEndColor { get; set; }

	/// <summary>The colour of the lines that separate buttons or frame tags.</summary>
	[Description("The colour of the lines between block buttons or around tags; empty uses the colour style")]
	public Color BorderColor { get; set; }

	/// <summary>The caption colour of an idle button.</summary>
	[Description("The caption colour of an idle block button; empty uses the colour style")]
	public Color NormalTextColor { get; set; }

	/// <summary>The caption colour of a button under the mouse.</summary>
	[Description("The caption colour of a block button under the mouse; empty uses the colour style")]
	public Color HotTextColor { get; set; }

	/// <summary>The caption colour of the selected block's button.</summary>
	[Description("The caption colour of the selected block button; empty uses the colour style")]
	public Color SelectedTextColor { get; set; }

	/// <summary>Restores full-width buttons in the expando header colours, with no overrides.</summary>
	public void SetDefaultValues() {
		Shape = BlockSelectorShape.Buttons;
		ColorStyle = BlockSelectorColorStyle.ExpandoHeader;
		ButtonHeight = null;
		ButtonSpacing = null;
		CornerRadius = null;
		IconSize = null;
		GlossAlpha = 0;
		NormalGradientStartColor = Color.Empty;
		NormalGradientEndColor = Color.Empty;
		HotGradientStartColor = Color.Empty;
		HotGradientEndColor = Color.Empty;
		SelectedGradientStartColor = Color.Empty;
		SelectedGradientEndColor = Color.Empty;
		BorderColor = Color.Empty;
		NormalTextColor = Color.Empty;
		HotTextColor = Color.Empty;
		SelectedTextColor = Color.Empty;
	}
}
