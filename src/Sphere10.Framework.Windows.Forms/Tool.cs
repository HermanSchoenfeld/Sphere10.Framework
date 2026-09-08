// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
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
	public static Cursor CreateCursor(Bitmap bmp, int xHotSpot, int yHotSpot)
		=> new Cursor(Tools.Windows.Win32.CreateCursorHandle(bmp, xHotSpot, yHotSpot));

	public static Cursor LoadRawCursor(byte[] bytes) {
		return new Cursor(new MemoryStream(bytes));
	}

}

