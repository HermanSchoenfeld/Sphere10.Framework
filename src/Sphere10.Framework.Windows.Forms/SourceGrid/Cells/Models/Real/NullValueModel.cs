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

public class NullValueModel : IValueModel {
	public readonly static NullValueModel Default = new NullValueModel();

	/// <summary>
	/// Constructor
	/// </summary>
	public NullValueModel() {
	}

	// IModel Members
	public object GetValue(CellContext cellContext) => null;

	public void SetValue(CellContext cellContext, object value) => throw new ApplicationException("This model doesn't support editing");

	public string GetDisplayText(CellContext cellContext) => null;
}
