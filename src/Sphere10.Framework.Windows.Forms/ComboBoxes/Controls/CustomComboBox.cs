// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Forms.Design;


namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// <c>CustomComboBox</c> is an extension of <c>ComboBox</c> which provides drop-down customization.
/// </summary>
[Designer(typeof(CustomComboBoxDesigner))]
public class CustomComboBox : ComboBoxEx, IPopupControlHost {
	// Events
	public new event EventHandler DropDown;

	public new event EventHandler DropDownClosed;

	public new event OldNewEventHandler<object> SelectedValueChanged;

	private static DateTime _sShowTime = DateTime.Now;

	#region Attributes

	/// <summary>
	/// Popup control.
	/// </summary>
	private readonly PopupControl _popupCtrl = new PopupControl();

	/// <summary>
	/// Actual drop-down control itself.
	/// </summary>
	Control _dropDownCtrl;

	/// <summary>
	/// Indicates if drop-down is currently shown.
	/// </summary>
	bool _droppedDown = false;

	/// <summary>
	/// Indicates current sizing mode.
	/// </summary>
	SizeMode _sizeMode = SizeMode.UseComboSize;

	/// <summary>
	/// Time drop-down was last hidden.
	/// </summary>
	DateTime _lastHideTime = DateTime.Now;

	/// <summary>
	/// Automatic focus timer helps make sure drop-down control is focused for user
	/// input upon drop-down.
	/// </summary>
	Timer _timerAutoFocus;

	/// <summary>
	/// Original size of control dimensions when first assigned.
	/// </summary>
	Size _sizeOriginal = new Size(1, 1);

	/// <summary>
	/// Original size of combo box dropdown when first assigned.
	/// </summary>
	Size _sizeCombo;

	/// <summary>
	/// Indicates if drop-down is resizable.
	/// </summary>
	bool _isResizable = true;

	#endregion

	#region Construction and destruction

	public CustomComboBox()
		: base() {
		base.DropDownStyle = ComboBoxStyle.DropDownList;
		_sizeCombo = new Size(base.DropDownWidth, base.DropDownHeight);
		_popupCtrl.Closing += new ToolStripDropDownClosingEventHandler(DropDown_Closing);
	}

	public CustomComboBox(Control dropControl)
		: this() {
		DropDownControl = dropControl;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Actual drop-down control itself.
	/// </summary>
	[Browsable(false)]
	public Control DropDownControl {
		get => _dropDownCtrl;
		set => AssignControl(value);
	}

	/// <summary>
	/// Indicates if drop-down is currently shown.
	/// </summary>
	[Browsable(false)]
	public bool IsDroppedDown {
		get {
			return _droppedDown /*&& m_popupCtrl.Visible*/;
		}
	}

	/// <summary>
	/// Indicates if drop-down is resizable.
	/// </summary>
	[Category("Custom Drop-Down"), Description("Indicates if drop-down is resizable.")]
	public bool AllowResizeDropDown {
		get => _isResizable;
		set => _isResizable = value;
	}

	/// <summary>
	/// Indicates current sizing mode.
	/// </summary>
	[Category("Custom Drop-Down"), Description("Indicates current sizing mode."), DefaultValue(SizeMode.UseComboSize)]
	public SizeMode DropDownSizeMode {
		get => _sizeMode;
		set {
			if (value != _sizeMode) {
				_sizeMode = value;
				AutoSizeDropDown();
			}
		}
	}

	[Category("Custom Drop-Down")]
	public Size DropSize {
		get => _sizeCombo;
		set {
			_sizeCombo = value;
			if (DropDownSizeMode == SizeMode.UseDropDownSize)
				AutoSizeDropDown();
		}
	}

	[Category("Custom Drop-Down"), Browsable(false)]
	public Size ControlSize {
		get => _sizeOriginal;
		set {
			_sizeOriginal = value;
			if (DropDownSizeMode == SizeMode.UseInitialControlSize)
				AutoSizeDropDown();
		}
	}

	#endregion

	#region Hide some unwanted properties

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new ComboBoxStyle DropDownStyle {
		get => base.DropDownStyle;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new ObjectCollection Items {
		get => base.Items;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new int ItemHeight {
		get => base.ItemHeight;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new int MaxDropDownItems {
		get => base.MaxDropDownItems;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new string DisplayMember {
		get => base.DisplayMember;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new string ValueMember {
		get => base.ValueMember;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new int DropDownWidth {
		get => base.DropDownWidth;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new int DropDownHeight {
		get => base.DropDownHeight;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new bool IntegralHeight {
		get => base.IntegralHeight;
		set {
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new bool Sorted {
		get => base.Sorted;
		set {
		}
	}

	#endregion

	public void RaiseSelectedValueChangedEvent(object oldValue, object newValue) {
		var eventHandler = this.SelectedValueChanged;
		if (eventHandler != null)
			this.SelectedValueChanged(this, new OldNewEventArgs<object>(oldValue, newValue));
	}

	#region Win32 message handlers

	public override bool PreProcessMessage(ref Message m) {
		if (m.Msg == (WinAPI.USER32.WM_REFLECT + WinAPI.USER32.WM_COMMAND)) {
			if (Tools.Windows.Win32.HIWORD(m.WParam) == WinAPI.USER32.CBN_DROPDOWN)
				return false;
		}
		return base.PreProcessMessage(ref m);
	}

	#region IPopupControlHost Members

	/// <summary>
	/// Displays drop-down area of combo box, if not already shown.
	/// </summary>
	public virtual void ShowDropDown() {
		if (_popupCtrl != null && !IsDroppedDown) {
			// Raise drop-down event.
			RaiseDropDownEvent();

			// Restore original control size.
			AutoSizeDropDown();

			var location = PointToScreen(new Point(0, Height));

			// Actually show popup.
			var resizeMode = (_isResizable ? PopupResizeMode.BottomRight : PopupResizeMode.None);
			_popupCtrl.Show(this.DropDownControl, location.X, location.Y, Width, Height, resizeMode);
			_droppedDown = true;

			_popupCtrl.PopupControlHost = this;

			// Initialize automatic focus timer?
			if (_timerAutoFocus == null) {
				_timerAutoFocus = new Timer();
				_timerAutoFocus.Interval = 10;
				_timerAutoFocus.Tick += new EventHandler(TimerAutoFocus_Tick);
			}
			// Enable the timer!
			_timerAutoFocus.Enabled = true;
			_sShowTime = DateTime.Now;
		}
	}

	public void RaiseDropDownEvent() {
		var eventHandler = this.DropDown;
		if (eventHandler != null)
			this.DropDown(this, EventArgs.Empty);
	}

	/// <summary>
	/// Hides drop-down area of combo box, if shown.
	/// </summary>
	public virtual void HideDropDown() {
		var wasDroppedDown = IsDroppedDown;
		_droppedDown = false;

		// Clear state before native close callbacks can re-enter this method.
		if (_timerAutoFocus != null && _timerAutoFocus.Enabled)
			_timerAutoFocus.Enabled = false;

		// A native dismissal may already have reset the flag while its borrowed-content host remains.
		_popupCtrl?.Hide();
		if (wasDroppedDown)
			RaiseDropDownClosedEvent();
	}

	#endregion

	public void RaiseDropDownClosedEvent() {
		var eventHandler = this.DropDownClosed;
		if (eventHandler != null)
			this.DropDownClosed(this, EventArgs.Empty);
	}

	#region ComboBox overrides

	protected override void Dispose(bool disposing) {
		try {
			if (disposing) {
				try {
					if (_timerAutoFocus != null) {
						_timerAutoFocus.Dispose();
						_timerAutoFocus = null;
					}
				} finally {
					// The popup owns its native host; the assigned dropdown content remains borrowed.
					_popupCtrl.Closing -= DropDown_Closing;
					_popupCtrl.Dispose();
				}
			}
		} finally {
			// Consumer callbacks must not prevent the combo's normal control disposal.
			base.Dispose(disposing);
		}
	}

	#endregion

	protected override void WndProc(ref Message m) {
		if (m.Msg == WinAPI.USER32.WM_LBUTTONDOWN) {
			AutoDropDown();
			return;
		}

		if (m.Msg == (WinAPI.USER32.WM_REFLECT + WinAPI.USER32.WM_COMMAND)) {
			switch (Tools.Windows.Win32.HIWORD(m.WParam)) {
				case WinAPI.USER32.CBN_DROPDOWN:
					AutoDropDown();
					return;

				case WinAPI.USER32.CBN_CLOSEUP:
					if ((DateTime.Now - _sShowTime).TotalSeconds > 1)
						HideDropDown();
					return;
			}
		}

		base.WndProc(ref m);
	}

	#endregion

	/// <summary>
	/// Assigns control to custom drop-down area of combo box.
	/// </summary>
	/// <param name="control">Control to be used as drop-down. Please note that this control must not be contained elsewhere.</param>
	protected virtual void AssignControl(Control control) {
		// If specified control is different then...
		if (control != DropDownControl) {
			// Preserve original objectStream size.
			_sizeOriginal = control.Size;

			// Reference the user-specified drop down control.
			_dropDownCtrl = control;
		}
	}

	void DropDown_Closing(object sender, ToolStripDropDownClosingEventArgs args) => _lastHideTime = DateTime.Now;

	private void DropDown_LostFocus(object sender, EventArgs args) => _lastHideTime = DateTime.Now;

	private void AutoDropDown() => AutoDropDown(DateTime.Now);

	// A click within 50 ms of the drop-down closing is the click that closed it; only a later click shows the drop-down again.
	private void AutoDropDown(DateTime now) {
		if (_popupCtrl != null && _popupCtrl.Visible)
			HideDropDown();
		else if ((now - _lastHideTime).TotalMilliseconds > 50)
			ShowDropDown();
	}

	#region Methods

	/// <summary>
	/// Automatically resize drop-down from properties.
	/// </summary>
	protected void AutoSizeDropDown() {
		if (DropDownControl != null) {
			switch (DropDownSizeMode) {
				case SizeMode.UseComboSize:
					DropDownControl.Size = new Size(Width, _sizeCombo.Height);
					break;

				case SizeMode.UseInitialControlSize:
					DropDownControl.Size = new Size(_sizeOriginal.Width, _sizeOriginal.Height);
					break;

				case SizeMode.UseCurrentControlSize:
					DropDownControl.Size = this.DropDownControl.Size;
					break;

				case SizeMode.UseDropDownSize:
					DropDownControl.Size = _sizeCombo;
					break;
			}
		}
	}

	#endregion

	#region Event handlers

	private void TimerAutoFocus_Tick(object sender, EventArgs args) {
		if (_popupCtrl.Visible && !DropDownControl.Focused) {
			DropDownControl.Focus();
			_timerAutoFocus.Enabled = false;
		}

		if (base.DroppedDown)
			base.DroppedDown = false;
	}

	#endregion

	#region Enumerations

	public enum SizeMode {
		UseComboSize,
		UseInitialControlSize,
		UseCurrentControlSize,
		UseDropDownSize,
	}

	#endregion
}
