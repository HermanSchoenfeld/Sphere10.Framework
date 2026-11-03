// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

/// <summary>
/// Default interface for RichText implementations
/// </summary>
public interface IRichText : IVisualElement {
	/// <summary>
	/// Text of format RichText
	/// </summary>
	Sphere10.Framework.Windows.Forms.RichText Value {
		get; set;
	}

	/// <summary>
	/// ForeColor of text
	/// </summary>
	Color ForeColor {
		get; set;
	}

	/// <summary>
	/// Text Alignment.
	/// </summary>
	System.Drawing.ContentAlignment TextAlignment {
		get; set;
	}

	/// <summary>
	/// Text Font
	/// </summary>
	Font Font {
		get; set;
	}

	/// <summary>
	/// Rotate flip type
	/// </summary>
	RotateFlipType RotateFlipType {
		get; set;
	}
}
