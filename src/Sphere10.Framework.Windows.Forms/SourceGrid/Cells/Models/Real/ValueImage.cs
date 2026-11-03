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

/// <summary>
/// Model that implements the IImage interface, used to read the Image directly from the Value of the cell.
/// </summary>
public class ValueImage : IImage {
	private Sphere10.Framework.ComponentModel.TypeConverterValidator _imageConverter = new Sphere10.Framework.ComponentModel.TypeConverterValidator(typeof(System.Drawing.Image));

	public static readonly ValueImage Default = new ValueImage();

	// IImage Members
	public System.Drawing.Image GetImage(CellContext cellContext) {
		var val = cellContext.Cell.Model.ValueModel.GetValue(cellContext);
		return (System.Drawing.Image)_imageConverter.ObjectToValue(val);
	}
}
