// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Hosts popup content with working-area placement and optional resize grips.</summary>
public class PopupDropDown : ToolStripDropDown {
	// Attributes
	private PopupResizeMode _resizeMode = PopupResizeMode.None;
	private Rectangle _gripBounds = Rectangle.Empty;
	private bool _lockedHostedControlSize = false;
	private bool _lockedThisSize = false;
	private bool _refreshSize = false;
	private Rectangle _anchorBounds;

	// Construction and destruction
	public PopupDropDown(bool autoSize) {
		AutoSize = autoSize;
		Padding = Margin = Padding.Empty;
	}

	// Properties
	/// <summary>
	/// Type of resize mode, grips are automatically drawn at bottom-left and bottom-right corners.
	/// </summary>
	public PopupResizeMode ResizeMode {
		get => _resizeMode;
		set {
			if (value != _resizeMode) {
				_resizeMode = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Bounds of active grip box position.
	/// </summary>
	protected Rectangle GripBounds {
		get => _gripBounds;
		set => _gripBounds = value;
	}

	/// <summary>
	/// Indicates when a grip box is shown.
	/// </summary>
	protected bool IsGripShown {
		get {
			return (ResizeMode == PopupResizeMode.TopLeft || ResizeMode == PopupResizeMode.TopRight ||
					ResizeMode == PopupResizeMode.BottomLeft || ResizeMode == PopupResizeMode.BottomRight);
		}
	}

	// Methods
	public new void Show(int x, int y) => Show(x, y, -1, -1);

	public void Show(int x, int y, int width, int height) {
		// If no hosted control is associated, this procedure is pointless!
		var hostedControl = GetHostedControl();
		if (hostedControl == null)
			return;
		_anchorBounds = new Rectangle(x, y - Math.Max(0, height), Math.Max(0, width), Math.Max(0, height));

		// Initially hosted control should be displayed within a drop down of 1x1, however
		// its size should exceed the dimensions of the drop-down.
		{
			_lockedHostedControlSize = true;
			_lockedThisSize = true;

			// Display actual popup and occupy just 1x1 pixel to avoid automatic reposition.
			Size = new Size(1, 1);
			base.Show(x, y);

			_lockedHostedControlSize = false;
			_lockedThisSize = false;
		}

		// Resize drop-down to fit its contents.
		ResizeFromContent(width);

		// If client area was enlarged using the minimum width paramater, then the hosted
		// control must also be enlarged.
		if (_refreshSize)
			RecalculateHostedControlLayout();

		// Assign event handler to control.
		hostedControl.SizeChanged += hostedControl_SizeChanged;
	}

	/// <summary>
	/// Processes the resizing messages.
	/// </summary>
	/// <param name="m">The message.</param>
	/// <returns>true, if the WndProc method from the base class shouldn't be invoked.</returns>
	public bool ProcessGrip(ref Message m) => ProcessGrip(ref m, true);

	public Control GetHostedControl() {
		if (Items.Count > 0) {
			var host = Items[0] as ToolStripControlHost;
			if (host != null)
				return host.Control;
		}
		return null;
	}

	public bool CompareResizeMode(PopupResizeMode resizeMode) => (ResizeMode & resizeMode) == resizeMode;

	// ToolStripDropDown overrides
	protected override void OnClosing(ToolStripDropDownClosingEventArgs e) {
		var hostedControl = GetHostedControl();
		if (hostedControl != null)
			hostedControl.SizeChanged -= hostedControl_SizeChanged;
		base.OnClosing(e);
	}

	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);

		GripBounds = Rectangle.Empty;

		if (CompareResizeMode(PopupResizeMode.BottomLeft)) {
			// Draw grip area at bottom-left of popup.
			e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, Height - 16, Width - 2, 14);
			GripBounds = new Rectangle(1, Height - 16, 16, 16);
			GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.BottomLeft);
		} else if (CompareResizeMode(PopupResizeMode.BottomRight)) {
			// Draw grip area at bottom-right of popup.
			e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, Height - 16, Width - 2, 14);
			GripBounds = new Rectangle(Width - 17, Height - 16, 16, 16);
			GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.BottomRight);
		} else if (CompareResizeMode(PopupResizeMode.TopLeft)) {
			// Draw grip area at top-left of popup.
			e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, 1, Width - 2, 14);
			GripBounds = new Rectangle(1, 0, 16, 16);
			GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.TopLeft);
		} else if (CompareResizeMode(PopupResizeMode.TopRight)) {
			// Draw grip area at top-right of popup.
			e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, 1, Width - 2, 14);
			GripBounds = new Rectangle(Width - 17, 0, 16, 16);
			GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.TopRight);
		}
	}

	protected override void OnSizeChanged(EventArgs e) {
		base.OnSizeChanged(e);

		// When drop-down window is being resized by the user (i.e. not locked),
		// update size of hosted control.
		if (!_lockedThisSize)
			RecalculateHostedControlLayout();
	}

	// Win32 message processing
	protected override void WndProc(ref Message m) {
		if (!ProcessGrip(ref m, false))
			base.WndProc(ref m);
	}

	protected void hostedControl_SizeChanged(object sender, EventArgs e) {
		// Only update size of this objectStream when it is not locked.
		if (!_lockedHostedControlSize)
			ResizeFromContent(-1);
	}

	protected void ResizeFromContent(int width) {
		if (_lockedThisSize)
			return;
		var layoutChanged = false;
		_lockedHostedControlSize = true;
		using (Tools.Scope.ExecuteOnDispose(() => _lockedHostedControlSize = false)) {
			var workingArea = Screen.FromRectangle(_anchorBounds).WorkingArea;
			var contentSize = SizeFromContent(width);
			var spaceAbove = Tools.Values.ClipValue(_anchorBounds.Top - workingArea.Top, 0, workingArea.Height);
			var spaceBelow = Tools.Values.ClipValue(workingArea.Bottom - _anchorBounds.Bottom, 0, workingArea.Height);
			var popupSize = new Size(Math.Min(contentSize.Width, workingArea.Width), Math.Min(contentSize.Height, Math.Max(1, Math.Max(spaceAbove, spaceBelow))));
			var showAbove = popupSize.Height > spaceBelow;
			var left = (ResizeMode & PopupResizeMode.Left) != 0 && _anchorBounds.Width > 0 ? _anchorBounds.Right - popupSize.Width : _anchorBounds.Left;
			var top = showAbove ? _anchorBounds.Top - popupSize.Height : _anchorBounds.Bottom;
			var location = new Point(Tools.Values.ClipValue(left, workingArea.Left, workingArea.Right - popupSize.Width),
				Tools.Values.ClipValue(top, workingArea.Top, workingArea.Bottom - popupSize.Height));
			var previousResizeMode = ResizeMode;
			if (IsGripShown)
				ResizeMode = (ResizeMode & (PopupResizeMode.Left | PopupResizeMode.Right)) | (showAbove ? PopupResizeMode.Top : PopupResizeMode.Bottom);
			Bounds = new Rectangle(location, popupSize);
			layoutChanged = contentSize != popupSize || ResizeMode != previousResizeMode;
		}
		if (layoutChanged)
			RecalculateHostedControlLayout();
	}

	protected void RecalculateHostedControlLayout() {
		if (_lockedHostedControlSize)
			return;

		_lockedThisSize = true;

		// Update size of hosted control.
		var hostedControl = GetHostedControl();
		if (hostedControl != null) {
			// Fetch control bounds and adjust as necessary.
			var bounds = hostedControl.Bounds;
			if (CompareResizeMode(PopupResizeMode.TopLeft) || CompareResizeMode(PopupResizeMode.TopRight))
				bounds.Location = new Point(1, 16);
			else
				bounds.Location = new Point(1, 1);

			bounds.Width = ClientRectangle.Width - 2;
			bounds.Height = ClientRectangle.Height - 2;
			if (IsGripShown)
				bounds.Height -= 16;

			if (bounds.Size != hostedControl.Size)
				hostedControl.Size = bounds.Size;
			if (bounds.Location != hostedControl.Location)
				hostedControl.Location = bounds.Location;
		}

		_lockedThisSize = false;
	}

	protected Size SizeFromContent(int width) {
		var contentSize = Size.Empty;

		_refreshSize = false;

		// Fetch hosted control.
		var hostedControl = GetHostedControl();
		if (hostedControl != null) {
			if (CompareResizeMode(PopupResizeMode.TopLeft) || CompareResizeMode(PopupResizeMode.TopRight))
				hostedControl.Location = new Point(1, 16);
			else
				hostedControl.Location = new Point(1, 1);
			contentSize = SizeFromClientSize(hostedControl.Size);

			// Use minimum width (if specified).
			if (hostedControl.MaximumSize.Width > 0)
				width = Math.Min(width, hostedControl.MaximumSize.Width);
			if (width > 0 && contentSize.Width < width) {
				contentSize.Width = width;
				_refreshSize = true;
			}
		}

		// If a grip box is shown then add it into the drop down height.
		if (IsGripShown)
			contentSize.Height += 16;

		// Add some additional space to allow for borders.
		contentSize.Width += 2;
		contentSize.Height += 2;

		return contentSize;
	}

	private bool ProcessGrip(ref Message m, bool contentControl) {
		if (ResizeMode != PopupResizeMode.None) {
			switch (m.Msg) {
				case WinAPI.USER32.WM_NCHITTEST:
					return OnNcHitTest(ref m, contentControl);

				case WinAPI.USER32.WM_GETMINMAXINFO:
					return OnGetMinMaxInfo(ref m);
			}
		}
		return false;
	}

	private bool OnGetMinMaxInfo(ref Message m) {
		var hostedControl = GetHostedControl();
		if (hostedControl != null) {
			var minmax = Tools.Windows.Win32.ReadStructure<WinAPI.USER32.MINMAXINFO>(m.LParam);
			var workingArea = Screen.FromRectangle(_anchorBounds).WorkingArea;
			var borderAndGrip = new Size(2, IsGripShown ? 18 : 2);

			// Maximum size.
			minmax.MaxTrackSize.Width = hostedControl.MaximumSize.Width > 0 ? Math.Min(workingArea.Width, hostedControl.MaximumSize.Width + borderAndGrip.Width) : workingArea.Width;
			minmax.MaxTrackSize.Height = hostedControl.MaximumSize.Height > 0 ? Math.Min(workingArea.Height, hostedControl.MaximumSize.Height + borderAndGrip.Height) : workingArea.Height;

			// Minimum size.
			minmax.MinTrackSize = new Size(32, 32);
			minmax.MinTrackSize.Width = Math.Min(minmax.MaxTrackSize.Width, Math.Max(32, hostedControl.MinimumSize.Width + borderAndGrip.Width));
			minmax.MinTrackSize.Height = Math.Min(minmax.MaxTrackSize.Height, Math.Max(32, hostedControl.MinimumSize.Height + borderAndGrip.Height));

			Tools.Windows.Win32.WriteStructure(m.LParam, minmax);
		}
		return true;
	}

	private bool OnNcHitTest(ref Message m, bool contentControl) {
		var location = PointToClient(Tools.Windows.Win32.GetMessagePosition(m.LParam));
		var transparent = new IntPtr(WinAPI.USER32.HTTRANSPARENT);

		// Check for simple gripper dragging.
		if (GripBounds.Contains(location)) {
			if (CompareResizeMode(PopupResizeMode.BottomLeft)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTBOTTOMLEFT;
				return true;
			} else if (CompareResizeMode(PopupResizeMode.BottomRight)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTBOTTOMRIGHT;
				return true;
			} else if (CompareResizeMode(PopupResizeMode.TopLeft)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTTOPLEFT;
				return true;
			} else if (CompareResizeMode(PopupResizeMode.TopRight)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTTOPRIGHT;
				return true;
			}
		} else // Check for edge based dragging.
		{
			var rectClient = ClientRectangle;
			if (location.X > rectClient.Right - 3 && location.X <= rectClient.Right && CompareResizeMode(PopupResizeMode.Right)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTRIGHT;
				return true;
			} else if (location.Y > rectClient.Bottom - 3 && location.Y <= rectClient.Bottom && CompareResizeMode(PopupResizeMode.Bottom)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTBOTTOM;
				return true;
			} else if (location.X > -1 && location.X < 3 && CompareResizeMode(PopupResizeMode.Left)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTLEFT;
				return true;
			} else if (location.Y > -1 && location.Y < 3 && CompareResizeMode(PopupResizeMode.Top)) {
				m.Result = contentControl ? transparent : (IntPtr)WinAPI.USER32.HTTOP;
				return true;
			}
		}
		return false;
	}
}
