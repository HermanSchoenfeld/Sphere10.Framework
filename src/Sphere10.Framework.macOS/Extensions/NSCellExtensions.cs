// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Sphere10.Framework {
	public static class NSCellExtensions {

		public static int DetermineFitWidth(this NSCell cell, NSObject objectValueAtCell, int minWidth = 10) {
			int width;
			if (cell is NSTextFieldCell) {
				var font = cell.Font ?? NSFont.ControlContentFontOfSize(-1);
				var attrs = NSDictionary.FromObjectAndKey(font, NSAttributedString.FontAttributeName);
				
				// Determine the text on the cell
				NSString cellText;
				if (objectValueAtCell is NSString) {
					cellText = (NSString)objectValueAtCell;
				} else if (cell.Formatter != null) {
					cellText = cell.Formatter.StringFor(objectValueAtCell).ToNSString();
				} else {
					cellText = objectValueAtCell.Description.ToNSString();
				}
				
				width = (int)cellText.StringSize(attrs).Width + minWidth;
				
			} else if (cell.Image != null) {
				// if cell has an image, use that images width
				width = (int)Math.Max(minWidth, (int)cell.Image.Size.Width);
			}  else {
				// cell is something else, just use its width
				width = (int)Math.Max(minWidth, (int)cell.CellSize.Width);
			}

			return width;

		}
	
	}
}


