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

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Hosts a borrowed editor control in a positioned native dropdown.</summary>
public partial class DropDownForm : System.Windows.Forms.Form {
	public event EventHandler DropDownOpen;

	public event EventHandler DropDownClosed;

	private bool _calculatingLocation;
	private Control _parentControl = null;
	private Control _innerControl = null;
	private DropDownFlags _flags = DropDownFlags.CloseOnEnter | DropDownFlags.CloseOnEscape;
	private bool _isOpen = false;

	public DropDownForm() {
		//
		// Required for Windows Form Designer support
		//
		InitializeComponent();
	}

	/// <summary>
	/// Constructor to create a dropdown form used to display the innerControl specified.
	/// It is responsability of the caller to dispose the innerControl.
	/// </summary>
	/// <param name="innerControl"></param>
	/// <param name="parentControl"></param>
	/// <param name="owner"></param>
	public DropDownForm(Control innerControl, Control parentControl, System.Windows.Forms.Form owner)
		: this() {
		Owner = owner;
		_innerControl = innerControl;
		_parentControl = parentControl;
	}

	public Control ParentControl {
		get => _parentControl;
		set => _parentControl = value;
	}

	public Control InnerControl {
		get => _innerControl;
		set => _innerControl = value;
	}

	public DropDownFlags DropDownFlags {
		get => _flags;
		set => _flags = value;
	}

	public void ShowDropDown() {
		if (_isOpen)
			return;
		_isOpen = true;

		if (InnerControl == null)
			throw new ApplicationException("InnerControl is null");
		if (ParentControl == null)
			throw new ApplicationException("ParentControl is null");
		if (Owner == null)
			throw new ApplicationException("Owner is null");

		OnDropDownOpen(EventArgs.Empty);
	}

	public void CloseDropDown() {
		if (_isOpen == false)
			return;

		OnDropDownClosed(EventArgs.Empty);

		_isOpen = false;
	}

	protected virtual void OnDropDownOpen(EventArgs args) {
		if (DropDownOpen != null)
			DropDownOpen(this, args);

		_containerPanel.Controls.Add(_innerControl);
		_innerControl.SizeChanged += InnerControlSizeChanged;
		using var contentScope = Tools.Scope.ExecuteOnDispose(() => {
			_innerControl.SizeChanged -= InnerControlSizeChanged;
			// The editor owns the control; removing it prevents disposal by this form.
			_containerPanel.Controls.Remove(_innerControl);
		});
		CalcLocation();

		Show();

		//This code simulate a ShowDialog. ShowDialog cannot be used because I need to receive the deactivate event to close the window.
		// This is not the best solution anyway because the parent for is deactivated and the user experience it is not the best.
		while (_isOpen) {
			System.Windows.Forms.Application.DoEvents();
			System.Threading.Thread.Sleep(1); //To prevent the CPU to work on 100%
		}
	}

	protected virtual void OnDropDownClosed(EventArgs args) {
		Owner.Activate();

		Hide();

		if (DropDownClosed != null)
			DropDownClosed(this, args);
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
		if ((_flags & DropDownFlags.CloseOnEscape) == DropDownFlags.CloseOnEscape) {
			if (keyData == Keys.Escape) {
				DialogResult = DialogResult.Cancel;
				CloseDropDown();
				//return true; altrimenti alcuni controlli che gestiscono i tasti non funzionano bene (ad esempio i controlli UITypeEditor)
			}
		}

		if ((_flags & DropDownFlags.CloseOnEnter) == DropDownFlags.CloseOnEnter) {
			if (keyData == Keys.Enter) {
				DialogResult = DialogResult.OK;
				CloseDropDown();
				//return true; altrimenti alcuni controlli che gestiscono i tasti non funzionano bene (ad esempio i controlli UITypeEditor)
			}
		}

		return base.ProcessCmdKey(ref msg, keyData);
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	void DropDownLayout(object sender, LayoutEventArgs args) {
		SuspendLayout();
		CalcLocation();
		ResumeLayout(false);
	}

	// Repositions the popup after its hosted control changes size.
	private void InnerControlSizeChanged(object sender, EventArgs args) => CalcLocation();

	// Fits the hosted content and popup to the available monitor work area.
	private void CalcLocation() {
		// Skip unavailable controls and suppress recursive size-change layout.
		if (_calculatingLocation || _innerControl == null || _parentControl == null || _innerControl.IsDisposed || _parentControl.IsDisposed)
			return;
		_calculatingLocation = true;
		using var locationScope = Tools.Scope.ExecuteOnDispose(() => _calculatingLocation = false);

		// Fit the hosted content within the monitor work area and available vertical space.
		var parentRectangle = _parentControl.RectangleToScreen(_parentControl.ClientRectangle);
		var workingArea = Screen.FromRectangle(parentRectangle).WorkingArea;
		var spaceAbove = Tools.Values.ClipValue(parentRectangle.Top - workingArea.Top, 0, workingArea.Height);
		var spaceBelow = Tools.Values.ClipValue(workingArea.Bottom - parentRectangle.Bottom, 0, workingArea.Height);
		var maximumHeight = Math.Max(1, Math.Max(spaceAbove, spaceBelow));
		var borderSize = _containerPanel.Size - _containerPanel.ClientSize;
		var contentSize = new Size(
			Math.Min(_innerControl.Width, Math.Max(1, workingArea.Width - borderSize.Width)),
			Math.Min(_innerControl.Height, Math.Max(1, maximumHeight - borderSize.Height))
		);
		_innerControl.Size = contentSize;
		ClientSize = contentSize + borderSize;

		// Align with the owner and clamp the popup to the visible monitor bounds.
		var left = parentRectangle.Left + Width <= workingArea.Right ? parentRectangle.Left : parentRectangle.Right - Width;
		var top = Height <= spaceBelow ? parentRectangle.Bottom : parentRectangle.Top - Height;
		Location = new Point(
			Tools.Values.ClipValue(left, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - Width)),
			Tools.Values.ClipValue(top, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - Height))
		);
	}

	// Ends the active popup session when its native window loses activation.
	private void DropDownDeactivate(object sender, System.EventArgs args) => CloseDropDown();
}
