// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Virtual;

/// <summary>
/// A Cell with an Image. Write and read byte[] values.
/// </summary>
public class Image : CellVirtual {
	/// <summary>
	/// Constructor using a ValueImage model to read he image directly from the value of the cell.
	/// </summary>
	public Image() {
		Model.AddModel(Models.ValueImage.Default);
		Editor = Editors.ImagePicker.Default;
	}
}
