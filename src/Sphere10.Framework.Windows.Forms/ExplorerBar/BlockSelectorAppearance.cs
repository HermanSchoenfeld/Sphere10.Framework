// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The block selector's complete look, resolved from an explorer bar theme: the shape's metrics and gradient direction, then each
/// colour from the theme's explicit <see cref="BlockSelectorInfo"/> entry, else its colour style. Metrics are logical pixels.
/// </summary>
internal sealed record BlockSelectorAppearance {
	private const float TagGradientOffset = 0.5f;

	public BlockSelectorShape Shape { get; init; }

	public int ButtonHeight { get; init; }

	public int ButtonSpacing { get; init; }

	public int CornerRadius { get; init; }

	public int IconSize { get; init; }

	public int GlossAlpha { get; init; }

	/// <summary>Whether the buttons sit inside the task pane's padding, in line with its expandos, instead of spanning the bar.</summary>
	public bool Inset { get; init; }

	public LinearGradientMode GradientMode { get; init; }

	/// <summary>Where a two-colour gradient starts to blend, as a fraction of its length; zero blends along the whole length.</summary>
	public float GradientOffset { get; init; }

	public Color NormalStart { get; init; }

	public Color NormalEnd { get; init; }

	public Color HotStart { get; init; }

	public Color HotEnd { get; init; }

	public Color SelectedStart { get; init; }

	public Color SelectedEnd { get; init; }

	public Color Border { get; init; }

	public Color NormalText { get; init; }

	public Color HotText { get; init; }

	public Color SelectedText { get; init; }

	/// <summary>Whether adjacent buttons share separator lines instead of each drawing a frame.</summary>
	public bool Stacked => ButtonSpacing == 0 && CornerRadius == 0;

	/// <summary>Resolves the look for a theme, optionally overriding the theme's colour style and shape.</summary>
	public static BlockSelectorAppearance Resolve(ExplorerBarInfo theme, BlockSelectorColorStyle? colorStyle = null, BlockSelectorShape? shape = null) {
		Guard.ArgumentNotNull(theme, nameof(theme));
		var selector = theme.BlockSelector;
		var resolvedShape = shape ?? selector.Shape;
		var tags = resolvedShape == BlockSelectorShape.Tags;
		var scheme = (colorStyle ?? selector.ColorStyle) switch {
			BlockSelectorColorStyle.SoftOffice => SoftOfficeScheme,
			_ => ExpandoHeaderScheme(theme, tags)
		};
		return new BlockSelectorAppearance {
			Shape = resolvedShape,
			ButtonHeight = selector.ButtonHeight ?? (tags ? 28 : 32),
			ButtonSpacing = selector.ButtonSpacing ?? (tags ? 4 : 0),
			CornerRadius = selector.CornerRadius ?? (tags ? 4 : 0),
			IconSize = selector.IconSize ?? (tags ? 16 : 20),
			GlossAlpha = selector.GlossAlpha,
			Inset = tags,
			GradientMode = tags ? LinearGradientMode.Horizontal : LinearGradientMode.Vertical,
			GradientOffset = tags ? TagGradientOffset : 0f,
			NormalStart = Visible(selector.NormalGradientStartColor, scheme.NormalStart),
			NormalEnd = Visible(selector.NormalGradientEndColor, scheme.NormalEnd),
			HotStart = Visible(selector.HotGradientStartColor, scheme.HotStart),
			HotEnd = Visible(selector.HotGradientEndColor, scheme.HotEnd),
			SelectedStart = Visible(selector.SelectedGradientStartColor, scheme.SelectedStart),
			SelectedEnd = Visible(selector.SelectedGradientEndColor, scheme.SelectedEnd),
			Border = Visible(selector.BorderColor, scheme.Border),
			NormalText = Visible(selector.NormalTextColor, scheme.NormalText),
			HotText = Visible(selector.HotTextColor, scheme.HotText),
			SelectedText = Visible(selector.SelectedTextColor, scheme.SelectedText)
		};
	}

	private static Scheme SoftOfficeScheme => new(
		Color.FromArgb(227, 239, 255), Color.FromArgb(160, 190, 236),
		Color.FromArgb(255, 252, 230), Color.FromArgb(255, 222, 150),
		Color.FromArgb(255, 233, 168), Color.FromArgb(249, 190, 92),
		Color.FromArgb(101, 147, 207),
		Color.FromArgb(21, 66, 139), Color.FromArgb(21, 66, 139), Color.Black);

	// The theme's own title bar colours: its header gradient, else the end columns of its header images, else its header colours.
	private static Scheme ExpandoHeaderScheme(ExplorerBarInfo theme, bool tags) {
		var header = theme.Header;
		var normal = header.TitleGradient
			? (header.NormalGradientStartColor, header.NormalGradientEndColor)
			: SampleEnds(header.NormalBackImage) ?? Shade(Visible(header.NormalBackColor, Visible(theme.Expando.NormalBackColor, SystemColors.Control)));
		var special = header.TitleGradient
			? (header.SpecialGradientStartColor, header.SpecialGradientEndColor)
			: SampleEnds(header.SpecialBackImage) ?? Shade(Visible(header.SpecialBackColor, SystemColors.Highlight));
		// A top-to-bottom button reads as raised with its lighter colour on top; a tag keeps the header's left-to-right order.
		if (!tags) {
			normal = LighterFirst(normal);
			special = LighterFirst(special);
		}
		var hotEnd = Blend(normal.Item2, special.Item2, 0.2f);
		var border = tags
			? Visible(header.NormalBorderColor, Visible(theme.Expando.NormalBorderColor, SystemColors.ControlDark))
			: Visible(theme.TaskPane.GradientEndColor, SystemColors.ControlDark);
		return new Scheme(
			normal.Item1, normal.Item2,
			normal.Item1, hotEnd,
			special.Item1, special.Item2,
			border,
			Visible(header.NormalTitleColor, SystemColors.ControlText),
			Visible(header.NormalTitleHotColor, SystemColors.ControlText),
			Visible(header.SpecialTitleColor, SystemColors.HighlightText));
	}

	// The colours at the left and right of an expando header image, inside the corner columns Expando keeps unstretched.
	private static (Color, Color)? SampleEnds(Image image) {
		const int edge = 5;
		if (image is not Bitmap bitmap || bitmap.Width <= 2 * edge)
			return null;
		var y = bitmap.Height / 2;
		return (Opaque(bitmap.GetPixel(edge, y)), Opaque(bitmap.GetPixel(bitmap.Width - edge - 1, y)));
	}

	private static (Color, Color) Shade(Color color) => (ControlPaint.LightLight(color), color);

	private static (Color, Color) LighterFirst((Color, Color) colors) => colors.Item1.GetBrightness() >= colors.Item2.GetBrightness() ? colors : (colors.Item2, colors.Item1);

	private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
		(int)(from.R + (to.R - from.R) * amount),
		(int)(from.G + (to.G - from.G) * amount),
		(int)(from.B + (to.B - from.B) * amount));

	private static Color Opaque(Color color) => Color.FromArgb(255, color);

	private static Color Visible(Color color, Color fallback) => color.A > 0 ? color : fallback;

	#region Inner Types

	private readonly record struct Scheme(
		Color NormalStart, Color NormalEnd, Color HotStart, Color HotEnd, Color SelectedStart, Color SelectedEnd,
		Color Border, Color NormalText, Color HotText, Color SelectedText);

	#endregion
}
