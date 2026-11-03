// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

public class Image : IImage {
	private System.Drawing.Image _image;

	public Image() {
	}

	public Image(System.Drawing.Image image) {
		_image = image;
	}

	public System.Drawing.Image ImageValue {
		get {
			return _image;
		}
		set {
			_image = value;
		}
	}

	// IImage Members
	/// <summary>
	/// Get the image of the specified cell.
	/// </summary>
	/// <returns></returns>
	public System.Drawing.Image GetImage(CellContext cellContext) {
		return _image;
	}
}
