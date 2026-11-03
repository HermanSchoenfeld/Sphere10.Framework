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

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Class to manage the visual aspect of a cell. This class can be shared beetween multiple cells.
/// </summary>
[Serializable]
public class Link : Cell {
	/// <summary>
	/// Represents a model with a link style font and forecolor.
	/// </summary>
	public new readonly static Link Default;

	static Link() {
		Default = new Link();
	}

	// Constructors
	/// <summary>
	/// Use default setting and construct a read and write VisualProperties
	/// </summary>
	public Link() {
		Font = new Font(Control.DefaultFont, FontStyle.Underline);
		ForeColor = Color.Blue;
	}

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public Link(Link source)
		: base(source) {
	}

	// Clone
	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new Link(this);
	}
}
