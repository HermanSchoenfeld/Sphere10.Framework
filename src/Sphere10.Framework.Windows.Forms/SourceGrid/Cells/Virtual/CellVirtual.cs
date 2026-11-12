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

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Virtual;

/// <summary>
/// Represents a CellVirtual in a grid.
/// </summary>
public class CellVirtual : ICellVirtual {
	#region Model

	private Models.ModelContainer _model;

	// View
	private Views.IView _view;

	// Controller
	private Controllers.ControllerContainer _controller;

	// Editor
	private Editors.EditorBase _editor = null;

	#region Constructor

	/// <summary>
	/// Constructor. Create a CellVirtual using a default NullValueModel. You must provide your custom ValueModel to bind the cell to a value.
	/// </summary>
	public CellVirtual() {
		View = Views.Cell.Default;
		Editor = null;
		Model = new SourceGrid.Cells.Models.ModelContainer();
		Model.AddModel(Cells.Models.NullValueModel.Default);
	}

	/// <summary>
	/// Constructor. Create a CellVirtual using a default NullValueModel. You must provide your custom ValueModel to bind the cell to a value.
	/// </summary>
	public CellVirtual(Type type)
		: this() {
		Editor = Editors.Factory.Create(type);
	}

	#endregion

	/// <summary>
	/// Represents the model of the cell.
	/// </summary>
	public Models.ModelContainer Model {
		get => _model;
		set {
			if (value == null)
				throw new SourceGridException("Model cannot be null");
			_model = value;
		}
	}

	#endregion

	/// <summary>
	/// Visual properties of this cell and other cell. You can share the VisualProperties between many cell to optimize memory size.
	/// Warning Changing this property can affect many cells
	/// </summary>
	public virtual Views.IView View {
		get => _view;
		set {
			if (value == null)
				throw new ArgumentNullException("View");

			_view = value;
		}
	}

	/// <summary>
	/// Controller of the cell.
	/// </summary>
	public Controllers.ControllerContainer Controller => _controller;

	/// <summary>
	/// Editor of this cell and others cells. If null no edit is supported.
	///  You can share the same model between many cells to optimize memory size. Warning Changing this property can affect many cells
	/// </summary>
	public Editors.EditorBase Editor {
		get => _editor;
		set => _editor = value;
	}

	/// <summary>
	/// Add the specified controller
	/// </summary>
	/// <param name="controller"></param>
	public void AddController(Controllers.IController controller) {
		if (_controller == null)
			_controller = new SourceGrid.Cells.Controllers.ControllerContainer();
		_controller.AddController(controller);
	}

	/// <summary>
	/// Remove the specifed controller
	/// </summary>
	/// <param name="controller"></param>
	public void RemoveController(Controllers.IController controller) {
		if (_controller != null)
			_controller.RemoveController(controller);
	}

	/// <summary>
	/// Find the specified controller. Returns null if not found.
	/// </summary>
	/// <param name="controllerType"></param>
	/// <returns></returns>
	public Controllers.IController FindController(Type controllerType) {
		if (_controller != null)
			return _controller.FindController(controllerType);
		return null;
	}

	/// <summary>
	/// Find the specified controller. Returns null if not found.
	/// </summary>
	/// <returns></returns>
	public T FindController<T>()
		where T : class, IController {
		return this.FindController(typeof(T)) as T;
	}

	#region Copy

	/// <summary>
	/// Create a shallow copy of the current object. Note that this is not a deep clone, all the reference are the same.
	/// Use internally MemberwiseClone method.
	/// </summary>
	/// <returns></returns>
	public ICellVirtual Copy() => (ICellVirtual)MemberwiseClone();

	#endregion
}
