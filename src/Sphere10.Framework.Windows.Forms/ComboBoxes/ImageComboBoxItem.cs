// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>One choice of an <see cref="ImageComboBox"/>: a caption, an image and the application value it stands for.</summary>
/// <remarks>The image is borrowed: the combo draws it and never disposes it. Call <see cref="ImageComboBox.SetChoices"/> again after changing a choice.</remarks>
public sealed class ImageComboBoxItem {
	/// <summary>Creates an empty choice.</summary>
	public ImageComboBoxItem() {
	}

	/// <summary>Creates a choice with its caption, borrowed image and optional value.</summary>
	public ImageComboBoxItem(string text, Image image, object value = null) {
		Text = text;
		Image = image;
		Value = value;
	}

	/// <summary>The caption drawn beside the image in the closed box and beneath it on the choice's tile.</summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>The borrowed image, scaled to fit; the combo never disposes it.</summary>
	public Image Image { get; set; }

	/// <summary>The application value the choice stands for; <see cref="ImageComboBox.SetChoices"/> keeps a selection whose value is still offered.</summary>
	public object Value { get; set; }

	/// <summary>The tool tip shown over the choice's tile.</summary>
	public string ToolTipText { get; set; } = string.Empty;

	/// <summary>An optional caption grouping the choice's tile with others of the same group in the drop-down.</summary>
	public string Group { get; set; }

	/// <summary>Returns <see cref="Text"/>, which the native combo items and accessibility use as the choice's name.</summary>
	public override string ToString() => Text;
}
