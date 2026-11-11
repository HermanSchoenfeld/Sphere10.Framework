// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A class that is serialized instead of a <see cref="BlockSelectorInfo"/>, alongside the other explorer bar theme surrogates.
/// A theme without this section, or without one of its entries, keeps the defaults for what is missing.
/// </summary>
[Obfuscation(Exclude = true)]
[Serializable]
public class BlockSelectorInfoSurrogate : ISerializable {

	/// <summary>See <see cref="BlockSelectorInfo.Shape"/>. This member is not intended to be used directly from your code.</summary>
	public BlockSelectorShape Shape;

	/// <summary>See <see cref="BlockSelectorInfo.ColorStyle"/>. This member is not intended to be used directly from your code.</summary>
	public BlockSelectorColorStyle ColorStyle;

	/// <summary>See <see cref="BlockSelectorInfo.ButtonHeight"/>. This member is not intended to be used directly from your code.</summary>
	public int? ButtonHeight;

	/// <summary>See <see cref="BlockSelectorInfo.ButtonSpacing"/>. This member is not intended to be used directly from your code.</summary>
	public int? ButtonSpacing;

	/// <summary>See <see cref="BlockSelectorInfo.CornerRadius"/>. This member is not intended to be used directly from your code.</summary>
	public int? CornerRadius;

	/// <summary>See <see cref="BlockSelectorInfo.IconSize"/>. This member is not intended to be used directly from your code.</summary>
	public int? IconSize;

	/// <summary>See <see cref="BlockSelectorInfo.GlossAlpha"/>. This member is not intended to be used directly from your code.</summary>
	public int GlossAlpha;

	/// <summary>See <see cref="BlockSelectorInfo.NormalGradientStartColor"/>. This member is not intended to be used directly from your code.</summary>
	public string NormalGradientStartColor;

	/// <summary>See <see cref="BlockSelectorInfo.NormalGradientEndColor"/>. This member is not intended to be used directly from your code.</summary>
	public string NormalGradientEndColor;

	/// <summary>See <see cref="BlockSelectorInfo.HotGradientStartColor"/>. This member is not intended to be used directly from your code.</summary>
	public string HotGradientStartColor;

	/// <summary>See <see cref="BlockSelectorInfo.HotGradientEndColor"/>. This member is not intended to be used directly from your code.</summary>
	public string HotGradientEndColor;

	/// <summary>See <see cref="BlockSelectorInfo.SelectedGradientStartColor"/>. This member is not intended to be used directly from your code.</summary>
	public string SelectedGradientStartColor;

	/// <summary>See <see cref="BlockSelectorInfo.SelectedGradientEndColor"/>. This member is not intended to be used directly from your code.</summary>
	public string SelectedGradientEndColor;

	/// <summary>See <see cref="BlockSelectorInfo.BorderColor"/>. This member is not intended to be used directly from your code.</summary>
	public string BorderColor;

	/// <summary>See <see cref="BlockSelectorInfo.NormalTextColor"/>. This member is not intended to be used directly from your code.</summary>
	public string NormalTextColor;

	/// <summary>See <see cref="BlockSelectorInfo.HotTextColor"/>. This member is not intended to be used directly from your code.</summary>
	public string HotTextColor;

	/// <summary>See <see cref="BlockSelectorInfo.SelectedTextColor"/>. This member is not intended to be used directly from your code.</summary>
	public string SelectedTextColor;

	/// <summary>Version number of the surrogate. This member is not intended to be used directly from your code.</summary>
	public int Version = 3300;

	public BlockSelectorInfoSurrogate() => Load(new BlockSelectorInfo());

	protected BlockSelectorInfoSurrogate(SerializationInfo info, StreamingContext context)
		: this() {
		// Read whichever entries the stream holds, so themes written by earlier versions keep the defaults for the rest.
		foreach (var entry in info) {
			switch (entry.Name) {
				case nameof(Version): Version = (int)entry.Value; break;
				case nameof(Shape): Shape = (BlockSelectorShape)entry.Value; break;
				case nameof(ColorStyle): ColorStyle = (BlockSelectorColorStyle)entry.Value; break;
				case nameof(ButtonHeight): ButtonHeight = (int?)entry.Value; break;
				case nameof(ButtonSpacing): ButtonSpacing = (int?)entry.Value; break;
				case nameof(CornerRadius): CornerRadius = (int?)entry.Value; break;
				case nameof(IconSize): IconSize = (int?)entry.Value; break;
				case nameof(GlossAlpha): GlossAlpha = (int)entry.Value; break;
				case nameof(NormalGradientStartColor): NormalGradientStartColor = (string)entry.Value; break;
				case nameof(NormalGradientEndColor): NormalGradientEndColor = (string)entry.Value; break;
				case nameof(HotGradientStartColor): HotGradientStartColor = (string)entry.Value; break;
				case nameof(HotGradientEndColor): HotGradientEndColor = (string)entry.Value; break;
				case nameof(SelectedGradientStartColor): SelectedGradientStartColor = (string)entry.Value; break;
				case nameof(SelectedGradientEndColor): SelectedGradientEndColor = (string)entry.Value; break;
				case nameof(BorderColor): BorderColor = (string)entry.Value; break;
				case nameof(NormalTextColor): NormalTextColor = (string)entry.Value; break;
				case nameof(HotTextColor): HotTextColor = (string)entry.Value; break;
				case nameof(SelectedTextColor): SelectedTextColor = (string)entry.Value; break;
			}
		}
	}

	/// <summary>Populates the surrogate from the specified <see cref="BlockSelectorInfo"/>.</summary>
	public void Load(BlockSelectorInfo blockSelectorInfo) {
		Guard.ArgumentNotNull(blockSelectorInfo, nameof(blockSelectorInfo));
		Shape = blockSelectorInfo.Shape;
		ColorStyle = blockSelectorInfo.ColorStyle;
		ButtonHeight = blockSelectorInfo.ButtonHeight;
		ButtonSpacing = blockSelectorInfo.ButtonSpacing;
		CornerRadius = blockSelectorInfo.CornerRadius;
		IconSize = blockSelectorInfo.IconSize;
		GlossAlpha = blockSelectorInfo.GlossAlpha;
		NormalGradientStartColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.NormalGradientStartColor);
		NormalGradientEndColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.NormalGradientEndColor);
		HotGradientStartColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.HotGradientStartColor);
		HotGradientEndColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.HotGradientEndColor);
		SelectedGradientStartColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.SelectedGradientStartColor);
		SelectedGradientEndColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.SelectedGradientEndColor);
		BorderColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.BorderColor);
		NormalTextColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.NormalTextColor);
		HotTextColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.HotTextColor);
		SelectedTextColor = Tools.Drawing.ConvertColorToString(blockSelectorInfo.SelectedTextColor);
	}

	/// <summary>Returns a <see cref="BlockSelectorInfo"/> holding the surrogate's values.</summary>
	public BlockSelectorInfo Save() {
		return new BlockSelectorInfo {
			Shape = Shape,
			ColorStyle = ColorStyle,
			ButtonHeight = ButtonHeight,
			ButtonSpacing = ButtonSpacing,
			CornerRadius = CornerRadius,
			IconSize = IconSize,
			GlossAlpha = GlossAlpha,
			NormalGradientStartColor = Tools.Drawing.ConvertStringToColor(NormalGradientStartColor),
			NormalGradientEndColor = Tools.Drawing.ConvertStringToColor(NormalGradientEndColor),
			HotGradientStartColor = Tools.Drawing.ConvertStringToColor(HotGradientStartColor),
			HotGradientEndColor = Tools.Drawing.ConvertStringToColor(HotGradientEndColor),
			SelectedGradientStartColor = Tools.Drawing.ConvertStringToColor(SelectedGradientStartColor),
			SelectedGradientEndColor = Tools.Drawing.ConvertStringToColor(SelectedGradientEndColor),
			BorderColor = Tools.Drawing.ConvertStringToColor(BorderColor),
			NormalTextColor = Tools.Drawing.ConvertStringToColor(NormalTextColor),
			HotTextColor = Tools.Drawing.ConvertStringToColor(HotTextColor),
			SelectedTextColor = Tools.Drawing.ConvertStringToColor(SelectedTextColor)
		};
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context) {
		info.AddValue(nameof(Version), Version);
		info.AddValue(nameof(Shape), Shape);
		info.AddValue(nameof(ColorStyle), ColorStyle);
		info.AddValue(nameof(ButtonHeight), ButtonHeight);
		info.AddValue(nameof(ButtonSpacing), ButtonSpacing);
		info.AddValue(nameof(CornerRadius), CornerRadius);
		info.AddValue(nameof(IconSize), IconSize);
		info.AddValue(nameof(GlossAlpha), GlossAlpha);
		info.AddValue(nameof(NormalGradientStartColor), NormalGradientStartColor);
		info.AddValue(nameof(NormalGradientEndColor), NormalGradientEndColor);
		info.AddValue(nameof(HotGradientStartColor), HotGradientStartColor);
		info.AddValue(nameof(HotGradientEndColor), HotGradientEndColor);
		info.AddValue(nameof(SelectedGradientStartColor), SelectedGradientStartColor);
		info.AddValue(nameof(SelectedGradientEndColor), SelectedGradientEndColor);
		info.AddValue(nameof(BorderColor), BorderColor);
		info.AddValue(nameof(NormalTextColor), NormalTextColor);
		info.AddValue(nameof(HotTextColor), HotTextColor);
		info.AddValue(nameof(SelectedTextColor), SelectedTextColor);
	}

	// XmlSerializer omits a metric that is not set, so the shape's default applies when the theme is read back.
	public bool ShouldSerializeButtonHeight() => ButtonHeight.HasValue;

	public bool ShouldSerializeButtonSpacing() => ButtonSpacing.HasValue;

	public bool ShouldSerializeCornerRadius() => CornerRadius.HasValue;

	public bool ShouldSerializeIconSize() => IconSize.HasValue;
}
