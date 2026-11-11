// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// RichText conversion methods
/// </summary>
public static class RichTextConversion {
	/// <summary>
	/// Convert plain text to rtf
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static Sphere10.Framework.Windows.Forms.RichText StringToRichText(string text) => StringToRichText(text, FontStyle.Regular);

	/// <summary>
	/// Convert plain text to rtf with font style
	/// </summary>
	/// <returns></returns>
	public static Sphere10.Framework.Windows.Forms.RichText StringToRichText(string text, FontStyle fontStyle) {
		// Use a local native surface: repeated assignments to a shared rich-text control retain state.
		// Dispose the editor before the font it borrows.
		using var font = fontStyle == FontStyle.Regular ? null : new Font(Control.DefaultFont, fontStyle);
		using var richTextBox = new RichTextBox();
		try {
			richTextBox.Text = text;
			if (font != null)
				richTextBox.Font = font;
			return new Sphere10.Framework.Windows.Forms.RichText(richTextBox.Rtf);
		} catch (Exception) {
			// Preserve the conversion API's empty-text fallback.
			richTextBox.Text = string.Empty;
			return new Sphere10.Framework.Windows.Forms.RichText(richTextBox.Rtf);
		}
	}

	/// <summary>
	/// Convert rtf to plain text
	/// </summary>
	/// <returns></returns>
	public static string RichTextToString(Sphere10.Framework.Windows.Forms.RichText rtf) {

		using var richTextBox = new RichTextBox();
		try {
			richTextBox.Rtf = rtf.Rtf;
			return richTextBox.Text;
		} catch (Exception) {
			// Preserve the conversion API's empty-text fallback.
			return string.Empty;
		}
	}

	public static string RichTextToStringStripWhitespaces(Sphere10.Framework.Windows.Forms.RichText rtf)
		=> System.Text.RegularExpressions.Regex.Replace(RichTextToString(rtf), @"[\t\n\r\f\v]", string.Empty);
}
