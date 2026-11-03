// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns a resizable popup while borrowing the control displayed inside it.</summary>
public class PopupControl : IDisposable {
	/// <summary>Occurs when the native dropdown is about to close.</summary>
	public event ToolStripDropDownClosingEventHandler Closing {
		add {
			_dropDown.Closing += value;
		}
		remove {
			if (_dropDown != null)
				_dropDown.Closing -= value;
		}
	}

	private ToolStripControlHost _host;
	private PopupDropDown _dropDown;
	private Padding _padding = Padding.Empty;
	private Padding _margin = new(1, 1, 1, 1);

	/// <summary>Creates the owned native dropdown.</summary>
	public PopupControl() {
		InitializeDropDown();
	}

	/// <summary>Gets whether the native dropdown is visible.</summary>
	public bool Visible => _dropDown != null && _dropDown.Visible;

	/// <summary>Gets the borrowed content currently hosted by the popup.</summary>
	public Control Control { get; private set; }

	/// <summary>Gets or sets the padding applied to the content host.</summary>
	public Padding Padding {
		get => _padding;
		set => _padding = value;
	}

	/// <summary>Gets or sets the margin applied to the content host.</summary>
	public Padding Margin {
		get => _margin;
		set => _margin = value;
	}

	/// <summary>Gets or sets whether native close releases the current content host.</summary>
	public bool AutoResetWhenClosed { get; set; }

	/// <summary>Gets or sets the popup control host, this is used to hide/show popup.</summary>
	public IPopupControlHost PopupControlHost {
		get; set;
	}

	/// <summary>Displays borrowed content at the supplied screen coordinates.</summary>
	public void Show(Control control, int x, int y) {
		Show(control, x, y, PopupResizeMode.None);
	}

	/// <summary>Displays borrowed content with the requested resize edges.</summary>
	public void Show(Control control, int x, int y, PopupResizeMode resizeMode) {
		Show(control, x, y, -1, -1, resizeMode);
	}

	/// <summary>Displays borrowed content relative to its owner's supplied screen bounds.</summary>
	public void Show(Control control, int x, int y, int width, int height, PopupResizeMode resizeMode) {
		InitializeHost(control);
		_dropDown.ResizeMode = resizeMode;
		_dropDown.Show(x, y, width, height);
		control.Focus();
	}

	/// <summary>Hides the dropdown and releases its current host, retaining borrowed content.</summary>
	public void Hide() {
		try {
			if (_dropDown != null && _dropDown.Visible)
				_dropDown.Hide();
		} finally {
			// Native dismissal can already have hidden the window while a host still needs cleanup.
			DisposeHost();
		}
	}

	/// <summary>Releases the current content host without disposing its content.</summary>
	public void Reset() {
		DisposeHost();
	}

	/// <summary>Releases the owned popup and host, leaving the displayed control with its caller.</summary>
	public void Dispose() {
		var dropDown = _dropDown;
		if (dropDown == null)
			return;

		// Avoid callbacks into the combo while tearing down its native popup.
		_dropDown = null;
		dropDown.Closed -= DropDownClosed;
		try {
			DisposeHost();
		} finally {
			dropDown.Dispose();
		}
	}

	// Create an owned surface so disposing ToolStripControlHost never disposes borrowed content.
	protected void InitializeHost(Control control) {
		InitializeDropDown();

		// If control is not yet being hosted then initialize host.
		if (control != Control)
			DisposeHost();

		// Create a new host?
		if (_host == null) {
			var surface = new Panel {
				Size = control.Size,
				MinimumSize = control.MinimumSize,
				MaximumSize = control.MaximumSize,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			Control = control;
			surface.Controls.Add(control);
			control.Bounds = surface.ClientRectangle;
			_host = new ToolStripControlHost(surface) {
				AutoSize = false,
				Padding = Padding,
				Margin = Margin
			};
			surface.SizeChanged += SurfaceSizeChanged;
			control.SizeChanged += ContentSizeChanged;
		}

		// Add control to drop-down.
		_dropDown.Items.Clear();
		_dropDown.Padding = _dropDown.Margin = Padding.Empty;
		_dropDown.Items.Add(_host);
	}

	// Retain the existing dropdown sizing and resize-grip implementation.
	protected void InitializeDropDown() {
		// Does a drop down exist?
		if (_dropDown == null) {
			_dropDown = new PopupDropDown(false);
			_dropDown.Closed += DropDownClosed;
		}
	}

	// Make sure the host is removed from the drop down without transferring ownership of its content.
	protected void DisposeHost() {
		var host = _host;
		var control = Control;
		_host = null;
		Control = null;
		PopupControlHost = null;
		if (host == null)
			return;

		// The ToolStrip host owns its panel; the caller still owns the panel's displayed control.
		var surface = host.Control;
		surface.SizeChanged -= SurfaceSizeChanged;
		control.SizeChanged -= ContentSizeChanged;
		try {
			surface.Controls.Remove(control);
		} finally {
			// ToolStripItem.Dispose removes itself from its owner and records its disposed state.
			// Keep that ownership intact until disposal; the panel no longer contains borrowed content.
			host.Dispose();
		}
	}

	// Synchronize the borrowed control without changing the caller's Dock or Anchor values.
	private void SurfaceSizeChanged(object sender, EventArgs args) {
		if (Control != null)
			Control.Bounds = ((Control)sender).ClientRectangle;
	}

	// Content-driven sizing remains available to controls that resize themselves while displayed.
	private void ContentSizeChanged(object sender, EventArgs args) {
		if (_host != null && _host.Control.ClientSize != Control.Size)
			_host.Control.ClientSize = Control.Size;
	}

	// Hide drop down within popup control and release reset-on-close hosts.
	private void DropDownClosed(object sender, ToolStripDropDownClosedEventArgs args) {
		var popupControlHost = PopupControlHost;
		if (AutoResetWhenClosed)
			DisposeHost();
		popupControlHost?.HideDropDown();
	}
}
