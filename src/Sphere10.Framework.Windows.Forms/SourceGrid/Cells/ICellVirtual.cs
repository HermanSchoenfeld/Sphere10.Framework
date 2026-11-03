// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// Interface to represents a cell virtual (without position or value information).
/// </summary>
public interface ICellVirtual {
	// Editor
	/// <summary>
	/// Editor of this cell and others cells. If null no edit is supported.
	///  You can share the same model between many cells to optimize memory size. Warning Changing this property can affect many cells
	/// </summary>
	Editors.EditorBase Editor {
		get; set;
	}

	// Controller
	/// <summary>
	/// Controller of the cell. Represents the actions of a cell.
	/// </summary>
	Controllers.ControllerContainer Controller {
		get;
	}

	// View
	/// <summary>
	/// Visual properties of this cell and other cell. You can share the VisualProperties between many cell to optimize memory size.
	/// Warning Changing this property can affect many cells
	/// </summary>
	Views.IView View {
		get; set;
	}

	// Model
	/// <summary>
	/// Model that contains the data of the cells. Cannot be null.
	/// </summary>
	Models.ModelContainer Model {
		get; set;
	}

	/// <summary>
	/// Add the specified controller.
	/// </summary>
	/// <param name="controller"></param>
	void AddController(Controllers.IController controller);

	/// <summary>
	/// Remove the specifed controller
	/// </summary>
	/// <param name="controller"></param>
	void RemoveController(Controllers.IController controller);

	/// <summary>
	/// Find the specified controller. Returns null if not found.
	/// </summary>
	/// <param name="controllerType"></param>
	/// <returns></returns>
	Controllers.IController FindController(Type controllerType);

	/// <summary>
	/// Find the specified controller. Returns null if not found.
	/// </summary>
	/// <returns></returns>
	T FindController<T>()
		where T : class, IController;

	// Copy
	/// <summary>
	/// Create a shallow copy of the current object. Note that this is not a deep clone, all the reference are the same.
	/// Use internally MemberwiseClone method.
	/// </summary>
	/// <returns></returns>
	ICellVirtual Copy();
}
