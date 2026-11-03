// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Tools;

public static partial class WinForms {
	/// <summary>Converts a drawing alignment to the corresponding native horizontal alignment.</summary>
	public static HorizontalAlignment ContentToHorizontalAlignment(ContentAlignment alignment) {
		if (Tools.Drawing.IsLeft(alignment))
			return HorizontalAlignment.Left;
		if (Tools.Drawing.IsRight(alignment))
			return HorizontalAlignment.Right;
		return HorizontalAlignment.Center;
	}

	/// <summary>Converts drawing alignment into native text-renderer positioning flags.</summary>
	public static TextFormatFlags ContentAlignmentToTextFormatFlags(ContentAlignment alignment) {
		var flags = Tools.Drawing.IsBottom(alignment)
			? TextFormatFlags.Bottom
			: Tools.Drawing.IsTop(alignment) ? TextFormatFlags.Top : TextFormatFlags.VerticalCenter;

		flags |= Tools.Drawing.IsLeft(alignment)
			? TextFormatFlags.Left
			: Tools.Drawing.IsRight(alignment) ? TextFormatFlags.Right : TextFormatFlags.HorizontalCenter;
		return flags;
	}

	public static Cursor CreateCursor(Bitmap bitmap, int xHotSpot, int yHotSpot) {
		return new Cursor(Tools.Windows.Win32.CreateCursorHandle(bitmap, xHotSpot, yHotSpot));
	}

	public static Cursor LoadRawCursor(byte[] bytes) {
		return new Cursor(new MemoryStream(bytes));
	}
}
