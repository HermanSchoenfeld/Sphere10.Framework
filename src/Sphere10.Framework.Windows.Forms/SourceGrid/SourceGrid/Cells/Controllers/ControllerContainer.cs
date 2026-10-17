// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

public class ControllerContainer : IController {
	private ControllerList m_ControllerList = null;
	private IController[] _dispatchControllers;


	/// <summary>
	/// A collection of elements of type IController. Only one instance of the same controller is allowed.
	/// </summary>
	public class ControllerList : Sphere10.Framework.Windows.Forms.SourceGrid.Collections.ListByType<IController> {
	}


	/// <summary>
	/// Returns null if not exist
	/// </summary>
	/// <param name="modelType"></param>
	/// <returns></returns>
	public virtual IController FindController(Type modelType) {
		if (m_ControllerList == null)
			m_ControllerList = new ControllerList();

		return m_ControllerList.GetByType(modelType);
	}


	public virtual void AddController(IController model) {
		if (m_ControllerList == null)
			m_ControllerList = new ControllerList();

		m_ControllerList.Add(model);
		_dispatchControllers = null;
	}

	public virtual void RemoveController(IController model) {
		if (m_ControllerList != null && m_ControllerList.Remove(model))
			_dispatchControllers = null;
	}

	// A callback can rebuild cells and modify their controllers. Keep the admitted dispatch stable,
	// while nested/subsequent events see the new list; reuse the snapshot until membership changes.
	private IController[] GetDispatchControllers() => _dispatchControllers ??= m_ControllerList?.ToArray() ?? Array.Empty<IController>();

	#region IController Members

	public void OnMouseDown(CellContext sender, System.Windows.Forms.MouseEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnMouseDown(sender, e);
	}

	public void OnMouseUp(CellContext sender, System.Windows.Forms.MouseEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnMouseUp(sender, e);
	}

	public void OnMouseMove(CellContext sender, System.Windows.Forms.MouseEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnMouseMove(sender, e);
	}

	public void OnMouseEnter(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnMouseEnter(sender, e);
	}

	public void OnMouseLeave(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnMouseLeave(sender, e);
	}

	public void OnKeyUp(CellContext sender, System.Windows.Forms.KeyEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnKeyUp(sender, e);
	}

	public void OnKeyDown(CellContext sender, System.Windows.Forms.KeyEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnKeyDown(sender, e);
	}

	public void OnKeyPress(CellContext sender, System.Windows.Forms.KeyPressEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnKeyPress(sender, e);
	}

	public void OnDoubleClick(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnDoubleClick(sender, e);
	}

	public void OnClick(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnClick(sender, e);
	}

	public void OnFocusLeaving(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnFocusLeaving(sender, e);
	}

	public void OnFocusLeft(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnFocusLeft(sender, e);
	}

	public void OnFocusEntering(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnFocusEntering(sender, e);
	}

	public void OnFocusEntered(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnFocusEntered(sender, e);
	}

	/// <summary>
	/// Fired before the value of the cell is changed.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnValueChanging(CellContext sender, ValueChangeEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnValueChanging(sender, e);
	}

	/// <summary>
	/// Fired after the value of the cell is changed.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnValueChanged(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnValueChanged(sender, e);
	}

	/// <summary>
	/// Fired when the StartEdit is called and before the cell start the edit operation. You can set the Cancel = true to stop editing.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnEditStarting(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnEditStarting(sender, e);
	}
	/// <summary>
	/// Fired when the StartEdit is sucesfully called.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnEditStarted(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnEditStarted(sender, e);
	}
	/// <summary>
	/// Fired when the EndEdit is called. You can read the Cancel property to determine if the edit is completed. If you change the cancel property there is no effect.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnEditEnded(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnEditEnded(sender, e);
	}

	public bool CanReceiveFocus(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers()) {
			if (controller.CanReceiveFocus(sender, e) == false)
				return false;
		}

		return true;
	}

	public void OnDragDrop(CellContext sender, System.Windows.Forms.DragEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnDragDrop(sender, e);
	}

	public void OnDragEnter(CellContext sender, System.Windows.Forms.DragEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnDragEnter(sender, e);
	}

	public void OnDragLeave(CellContext sender, EventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnDragLeave(sender, e);
	}

	public void OnDragOver(CellContext sender, System.Windows.Forms.DragEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnDragOver(sender, e);
	}

	public void OnGiveFeedback(CellContext sender, System.Windows.Forms.GiveFeedbackEventArgs e) {
		foreach (var controller in GetDispatchControllers())
			controller.OnGiveFeedback(sender, e);
	}

	#endregion

}

