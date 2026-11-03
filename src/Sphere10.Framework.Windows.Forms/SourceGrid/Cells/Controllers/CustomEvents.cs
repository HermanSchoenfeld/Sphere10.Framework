// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// In this Controller are defined all the events fired by the Controller. Each event has a sender object of type CellContext that you can use to read the cell informations.
/// </summary>
public class CustomEvents : IController {
	// IController Members
	public event MouseEventHandler MouseDown;

	public event MouseEventHandler MouseUp;

	public event MouseEventHandler MouseMove;

#if !MINI

	public event EventHandler MouseEnter;

	public event EventHandler MouseLeave;

#endif

	public event KeyEventHandler KeyUp;

	public event KeyEventHandler KeyDown;

	public event KeyPressEventHandler KeyPress;

#if !MINI

	public event EventHandler DoubleClick;

#endif

	public event EventHandler Click;

	public event System.ComponentModel.CancelEventHandler FocusLeaving;

	public event EventHandler FocusLeft;

	public event System.ComponentModel.CancelEventHandler FocusEntering;

	public event EventHandler FocusEntered;

	/// <summary>
	/// Fired before the value of the cell is changed.
	/// </summary>
	public event ValueChangeEventHandler ValueChanging;

	/// <summary>
	/// Fired after the value of the cell is changed.
	/// </summary>
	public event EventHandler ValueChanged;

	public event System.ComponentModel.CancelEventHandler EditStarting;

	public event System.EventHandler EditStarted;

	public event System.EventHandler EditEnded;

	public event DragEventHandler DragDrop;

	public event DragEventHandler DragEnter;

	public event EventHandler DragLeave;

	public event DragEventHandler DragOver;

	public event GiveFeedbackEventHandler GiveFeedback;

	public void OnMouseDown(CellContext sender, MouseEventArgs e) {
		if (MouseDown != null)
			MouseDown(sender, e);
	}

	public void OnMouseUp(CellContext sender, MouseEventArgs e) {
		if (MouseUp != null)
			MouseUp(sender, e);
	}

	public void OnMouseMove(CellContext sender, MouseEventArgs e) {
		if (MouseMove != null)
			MouseMove(sender, e);
	}

#if !MINI

	public void OnMouseEnter(CellContext sender, EventArgs e) {
		if (MouseEnter != null)
			MouseEnter(sender, e);
	}

	public void OnMouseLeave(CellContext sender, EventArgs e) {
		if (MouseLeave != null)
			MouseLeave(sender, e);
	}

#endif

	public void OnKeyUp(CellContext sender, KeyEventArgs e) {
		if (KeyUp != null)
			KeyUp(sender, e);
	}

	public void OnKeyDown(CellContext sender, KeyEventArgs e) {
		if (KeyDown != null)
			KeyDown(sender, e);
	}

	public void OnKeyPress(CellContext sender, KeyPressEventArgs e) {
		if (KeyPress != null)
			KeyPress(sender, e);
	}

#if !MINI

	public void OnDoubleClick(CellContext sender, EventArgs e) {
		if (DoubleClick != null)
			DoubleClick(sender, e);
	}

#endif

	public void OnClick(CellContext sender, EventArgs e) {
		if (Click != null)
			Click(sender, e);
	}

	public void OnFocusLeaving(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		if (FocusLeaving != null)
			FocusLeaving(sender, e);
	}

	public void OnFocusLeft(CellContext sender, EventArgs e) {
		if (FocusLeft != null)
			FocusLeft(sender, e);
	}

	public void OnFocusEntering(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		if (FocusEntering != null)
			FocusEntering(sender, e);
	}

	public void OnFocusEntered(CellContext sender, EventArgs e) {
		if (FocusEntered != null)
			FocusEntered(sender, e);
	}

	/// <summary>
	/// Fired before the value of the cell is changed.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnValueChanging(CellContext sender, ValueChangeEventArgs e) {
		if (ValueChanging != null)
			ValueChanging(sender, e);
	}

	/// <summary>
	/// Fired after the value of the cell is changed.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnValueChanged(CellContext sender, EventArgs e) {
		if (ValueChanged != null)
			ValueChanged(sender, e);
	}

	public virtual void OnEditStarting(CellContext sender, System.ComponentModel.CancelEventArgs e) {
		if (EditStarting != null)
			EditStarting(sender, e);
	}

	public virtual void OnEditStarted(CellContext sender, System.EventArgs e) {
		if (EditStarted != null)
			EditStarted(sender, e);
	}

	public virtual void OnEditEnded(CellContext sender, System.EventArgs e) {
		if (EditEnded != null)
			EditEnded(sender, e);
	}

	public virtual bool CanReceiveFocus(CellContext sender, EventArgs e) {
		return true;
	}

	public virtual void OnDragDrop(CellContext sender, DragEventArgs e) {
		if (DragDrop != null)
			DragDrop(sender, e);
	}

	public virtual void OnDragEnter(CellContext sender, DragEventArgs e) {
		if (DragEnter != null)
			DragEnter(sender, e);
	}

	public virtual void OnDragLeave(CellContext sender, EventArgs e) {
		if (DragLeave != null)
			DragLeave(sender, e);
	}

	public virtual void OnDragOver(CellContext sender, DragEventArgs e) {
		if (DragOver != null)
			DragOver(sender, e);
	}

	public virtual void OnGiveFeedback(CellContext sender, GiveFeedbackEventArgs e) {
		if (GiveFeedback != null)
			GiveFeedback(sender, e);
	}
}
