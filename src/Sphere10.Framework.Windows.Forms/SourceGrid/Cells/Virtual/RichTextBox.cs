// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Virtual;

/// <summary>
/// A Cell with a RichTextBox. This Cell is of type RichText.
/// Abstract, you must override GetValue and SetValue.
/// </summary>
public class RichTextBox : CellVirtual {
	/// <summary>
	/// Constructor of a RichTextBox style cell. You must st a valid Model to use this type of cell with this constructor.
	/// </summary>
	public RichTextBox() {
		View = Views.RichTextBox.Default;
		Model.AddModel(new Models.RichTextBox());
		AddController(Controllers.RichTextBox.Default);
		Editor = new Editors.RichTextBox();
	}
}
