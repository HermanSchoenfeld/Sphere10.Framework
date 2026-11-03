// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// Interface for informations about an image.
/// </summary>
public interface IImage : IModel {
	/// <summary>
	/// Get the image of the specified cell.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	System.Drawing.Image GetImage(CellContext cellContext);
}
