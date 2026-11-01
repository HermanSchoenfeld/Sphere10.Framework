// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using static Sphere10.Framework.Windows.WinAPI.USER32;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public enum PopupResizeMode {
	None = 0,

	// Individual styles.
	Left = 1,
	Top = 2,
	Right = 4,
	Bottom = 8,

	// Combined styles.
	All = (Top | Left | Bottom | Right),
	TopLeft = (Top | Left),
	TopRight = (Top | Right),
	BottomLeft = (Bottom | Left),
	BottomRight = (Bottom | Right),
}


public enum GripAlignMode {
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight,
}


public sealed class GripRenderer {

	#region Construction and destruction

	private GripRenderer() {
	}

	#endregion

	#region Methods

	private static void InitializeGripBitmap(Graphics g, Size size, bool forceRefresh) {
		if (m_sGripBitmap == null || forceRefresh || size != m_sGripBitmap.Size) {
			// Draw size grip into a bitmap image.
			m_sGripBitmap = new Bitmap(size.Width, size.Height, g);
			using (Graphics gripG = Graphics.FromImage(m_sGripBitmap))
				ControlPaint.DrawSizeGrip(gripG, SystemColors.ButtonFace, 0, 0, size.Width, size.Height);
		}
	}

	public static void RefreshSystemColors(Graphics g, Size size) {
		InitializeGripBitmap(g, size, true);
	}

	public static void Render(Graphics g, Point location, Size size, GripAlignMode mode) {
		InitializeGripBitmap(g, size, false);

		// Calculate display size and position of grip.
		switch (mode) {
			case GripAlignMode.TopLeft:
				size.Height = -size.Height;
				size.Width = -size.Width;
				break;

			case GripAlignMode.TopRight:
				size.Height = -size.Height;
				break;

			case GripAlignMode.BottomLeft:
				size.Width = -size.Height;
				break;
		}

		// Reverse size grip for left-aligned.
		if (size.Width < 0)
			location.X -= size.Width;
		if (size.Height < 0)
			location.Y -= size.Height;

		g.DrawImage(GripBitmap, location.X, location.Y, size.Width, size.Height);
	}

	public static void Render(Graphics g, Point location, GripAlignMode mode) {
		Render(g, location, new Size(16, 16), mode);
	}

	#endregion

	#region Properties

	private static Bitmap GripBitmap {
		get { return m_sGripBitmap; }
	}

	#endregion

	#region Attributes

	private static Bitmap m_sGripBitmap;

	#endregion

}


public class PopupDropDown : ToolStripDropDown {

	#region Construction and destruction

	public PopupDropDown(bool autoSize) {
		AutoSize = autoSize;
		Padding = Margin = Padding.Empty;
	}

	#endregion

	#region ToolStripDropDown overrides

	protected override void OnClosing(ToolStripDropDownClosingEventArgs e) {
		Control hostedControl = GetHostedControl();
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
		if (!m_lockedThisSize)
			RecalculateHostedControlLayout();
	}

	protected void hostedControl_SizeChanged(object sender, EventArgs e) {
		// Only update size of this objectStream when it is not locked.
		if (!m_lockedHostedControlSize)
			ResizeFromContent(-1);
	}

	#endregion

	#region Methods

	public new void Show(int x, int y) {
		Show(x, y, -1, -1);
	}

	public void Show(int x, int y, int width, int height) {
		// If no hosted control is associated, this procedure is pointless!
		Control hostedControl = GetHostedControl();
		if (hostedControl == null)
			return;
		_anchorBounds = new Rectangle(x, y - Math.Max(0, height), Math.Max(0, width), Math.Max(0, height));

		// Initially hosted control should be displayed within a drop down of 1x1, however
		// its size should exceed the dimensions of the drop-down.
		{
			m_lockedHostedControlSize = true;
			m_lockedThisSize = true;

			// Display actual popup and occupy just 1x1 pixel to avoid automatic reposition.
			Size = new Size(1, 1);
			base.Show(x, y);

			m_lockedHostedControlSize = false;
			m_lockedThisSize = false;
		}

		// Resize drop-down to fit its contents.
		ResizeFromContent(width);

		// If client area was enlarged using the minimum width paramater, then the hosted
		// control must also be enlarged.
		if (m_refreshSize)
			RecalculateHostedControlLayout();

		// Assign event handler to control.
		hostedControl.SizeChanged += hostedControl_SizeChanged;
	}

	protected void ResizeFromContent(int width) {
		if (m_lockedThisSize)
			return;
		var LayoutChanged = false;
		m_lockedHostedControlSize = true;
		using (Tools.Scope.ExecuteOnDispose(() => m_lockedHostedControlSize = false)) {
			var WorkingArea = Screen.FromRectangle(_anchorBounds).WorkingArea;
			var ContentSize = SizeFromContent(width);
			var SpaceAbove = Tools.Values.ClipValue(_anchorBounds.Top - WorkingArea.Top, 0, WorkingArea.Height);
			var SpaceBelow = Tools.Values.ClipValue(WorkingArea.Bottom - _anchorBounds.Bottom, 0, WorkingArea.Height);
			var PopupSize = new Size(Math.Min(ContentSize.Width, WorkingArea.Width), Math.Min(ContentSize.Height, Math.Max(1, Math.Max(SpaceAbove, SpaceBelow))));
			var ShowAbove = PopupSize.Height > SpaceBelow;
			var Left = (ResizeMode & PopupResizeMode.Left) != 0 && _anchorBounds.Width > 0 ? _anchorBounds.Right - PopupSize.Width : _anchorBounds.Left;
			var Top = ShowAbove ? _anchorBounds.Top - PopupSize.Height : _anchorBounds.Bottom;
			var Location = new Point(Tools.Values.ClipValue(Left, WorkingArea.Left, WorkingArea.Right - PopupSize.Width),
				Tools.Values.ClipValue(Top, WorkingArea.Top, WorkingArea.Bottom - PopupSize.Height));
			var PreviousResizeMode = ResizeMode;
			if (IsGripShown)
				ResizeMode = (ResizeMode & (PopupResizeMode.Left | PopupResizeMode.Right)) | (ShowAbove ? PopupResizeMode.Top : PopupResizeMode.Bottom);
			Bounds = new Rectangle(Location, PopupSize);
			LayoutChanged = ContentSize != PopupSize || ResizeMode != PreviousResizeMode;
		}
		if (LayoutChanged)
			RecalculateHostedControlLayout();
	}

	protected void RecalculateHostedControlLayout() {
		if (m_lockedHostedControlSize)
			return;

		m_lockedThisSize = true;

		// Update size of hosted control.
		Control hostedControl = GetHostedControl();
		if (hostedControl != null) {
			// Fetch control bounds and adjust as necessary.
			Rectangle bounds = hostedControl.Bounds;
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

		m_lockedThisSize = false;
	}

	public Control GetHostedControl() {
		if (Items.Count > 0) {
			ToolStripControlHost host = Items[0] as ToolStripControlHost;
			if (host != null)
				return host.Control;
		}
		return null;
	}

	public bool CompareResizeMode(PopupResizeMode resizeMode) {
		return (ResizeMode & resizeMode) == resizeMode;
	}

	protected Size SizeFromContent(int width) {
		Size contentSize = Size.Empty;

		m_refreshSize = false;

		// Fetch hosted control.
		Control hostedControl = GetHostedControl();
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
				m_refreshSize = true;
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

	#endregion

	#region Win32 message processing

	protected override void WndProc(ref Message m) {
		if (!ProcessGrip(ref m, false))
			base.WndProc(ref m);
	}

	/// <summary>
	/// Processes the resizing messages.
	/// </summary>
	/// <param name="m">The message.</param>
	/// <returns>true, if the WndProc method from the base class shouldn't be invoked.</returns>
	public bool ProcessGrip(ref Message m) {
		return ProcessGrip(ref m, true);
	}
	private bool ProcessGrip(ref Message m, bool contentControl) {
		if (ResizeMode != PopupResizeMode.None) {
			switch (m.Msg) {
				case WM_NCHITTEST:
					return OnNcHitTest(ref m, contentControl);

				case WM_GETMINMAXINFO:
					return OnGetMinMaxInfo(ref m);
			}
		}
		return false;
	}
	private bool OnGetMinMaxInfo(ref Message m) {
		Control hostedControl = GetHostedControl();
		if (hostedControl != null) {
			var minmax = Tools.Windows.Win32.ReadStructure<MINMAXINFO>(m.LParam);
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
		Point location = PointToClient(Tools.Windows.Win32.GetMessagePosition(m.LParam));
		IntPtr transparent = new IntPtr(HTTRANSPARENT);

		// Check for simple gripper dragging.
		if (GripBounds.Contains(location)) {
			if (CompareResizeMode(PopupResizeMode.BottomLeft)) {
				m.Result = contentControl ? transparent : (IntPtr)HTBOTTOMLEFT;
				return true;
			} else if (CompareResizeMode(PopupResizeMode.BottomRight)) {
				m.Result = contentControl ? transparent : (IntPtr)HTBOTTOMRIGHT;
				return true;
			} else if (CompareResizeMode(PopupResizeMode.TopLeft)) {
				m.Result = contentControl ? transparent : (IntPtr)HTTOPLEFT;
				return true;
			} else if (CompareResizeMode(PopupResizeMode.TopRight)) {
				m.Result = contentControl ? transparent : (IntPtr)HTTOPRIGHT;
				return true;
			}
		} else // Check for edge based dragging.
		{
			Rectangle rectClient = ClientRectangle;
			if (location.X > rectClient.Right - 3 && location.X <= rectClient.Right && CompareResizeMode(PopupResizeMode.Right)) {
				m.Result = contentControl ? transparent : (IntPtr)HTRIGHT;
				return true;
			} else if (location.Y > rectClient.Bottom - 3 && location.Y <= rectClient.Bottom && CompareResizeMode(PopupResizeMode.Bottom)) {
				m.Result = contentControl ? transparent : (IntPtr)HTBOTTOM;
				return true;
			} else if (location.X > -1 && location.X < 3 && CompareResizeMode(PopupResizeMode.Left)) {
				m.Result = contentControl ? transparent : (IntPtr)HTLEFT;
				return true;
			} else if (location.Y > -1 && location.Y < 3 && CompareResizeMode(PopupResizeMode.Top)) {
				m.Result = contentControl ? transparent : (IntPtr)HTTOP;
				return true;
			}
		}
		return false;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Type of resize mode, grips are automatically drawn at bottom-left and bottom-right corners.
	/// </summary>
	public PopupResizeMode ResizeMode {
		get { return m_resizeMode; }
		set {
			if (value != m_resizeMode) {
				m_resizeMode = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Bounds of active grip box position.
	/// </summary>
	protected Rectangle GripBounds {
		get { return this.m_gripBounds; }
		set { this.m_gripBounds = value; }
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

	#endregion

	#region Attributes

	private PopupResizeMode m_resizeMode = PopupResizeMode.None;
	private Rectangle m_gripBounds = Rectangle.Empty;

	private bool m_lockedHostedControlSize = false;
	private bool m_lockedThisSize = false;
	private bool m_refreshSize = false;
	private Rectangle _anchorBounds;

	#endregion

}


public interface IPopupControlHost {

	#region Methods

	/// <summary>
	/// Displays drop-down area of combo box, if not already shown.
	/// </summary>
	void ShowDropDown();

	/// <summary>
	/// Hides drop-down area of combo box, if shown.
	/// </summary>
	void HideDropDown();

	#endregion

}


public class PopupControl {

	#region Construction and destruction

	public PopupControl() {
		InitializeDropDown();
	}

	#endregion

	#region Event handlers

	private void m_dropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e) {
		if (AutoResetWhenClosed)
			DisposeHost();

		// Hide drop down within popup control.
		if (PopupControlHost != null)
			PopupControlHost.HideDropDown();
	}

	#endregion

	#region Events

	public event ToolStripDropDownClosingEventHandler Closing {
		add { m_dropDown.Closing += value; }
		remove { m_dropDown.Closing -= value; }
	}

	#endregion

	#region Methods

	public void Show(Control control, int x, int y) {
		Show(control, x, y, PopupResizeMode.None);
	}

	public void Show(Control control, int x, int y, PopupResizeMode resizeMode) {
		Show(control, x, y, -1, -1, resizeMode);
	}

	public void Show(Control control, int x, int y, int width, int height, PopupResizeMode resizeMode) {
		Size controlSize = control.Size;

		InitializeHost(control);

		m_dropDown.ResizeMode = resizeMode;
		m_dropDown.Show(x, y, width, height);

		control.Focus();
	}

	public void Hide() {
		if (m_dropDown != null && m_dropDown.Visible) {
			m_dropDown.Hide();
			DisposeHost();
		}
	}

	public void Reset() {
		DisposeHost();
	}

	#endregion

	#region Internal methods

	protected void DisposeHost() {
		if (m_host != null) {
			// Make sure host is removed from drop down.
			if (m_dropDown != null)
				m_dropDown.Items.Clear();

			// Dispose of host.
			m_host = null;
		}

		PopupControlHost = null;
	}

	protected void InitializeHost(Control control) {
		InitializeDropDown();

		// If control is not yet being hosted then initialize host.
		if (control != Control)
			DisposeHost();

		// Create a new host?
		if (m_host == null) {
			m_host = new ToolStripControlHost(control);
			m_host.AutoSize = false;
			m_host.Padding = Padding;
			m_host.Margin = Margin;
		}

		// Add control to drop-down.
		m_dropDown.Items.Clear();
		m_dropDown.Padding = m_dropDown.Margin = Padding.Empty;
		m_dropDown.Items.Add(m_host);
	}

	protected void InitializeDropDown() {
		// Does a drop down exist?
		if (m_dropDown == null) {
			m_dropDown = new PopupDropDown(false);
			m_dropDown.Closed += new ToolStripDropDownClosedEventHandler(m_dropDown_Closed);
		}
	}

	#endregion

	#region Properties

	public bool Visible {
		get { return (this.m_dropDown != null && this.m_dropDown.Visible) ? true : false; }
	}

	public Control Control {
		get { return (this.m_host != null) ? this.m_host.Control : null; }
	}

	public Padding Padding {
		get { return this.m_padding; }
		set { this.m_padding = value; }
	}

	public Padding Margin {
		get { return this.m_margin; }
		set { this.m_margin = value; }
	}

	public bool AutoResetWhenClosed {
		get { return this.m_autoReset; }
		set { this.m_autoReset = value; }
	}

	/// <summary>
	/// Gets or sets the popup control host, this is used to hide/show popup.
	/// </summary>
	public IPopupControlHost PopupControlHost { get; set; }

	#endregion

	#region Attributes

	private ToolStripControlHost m_host;
	private PopupDropDown m_dropDown;

	private Padding m_padding = Padding.Empty;
	private Padding m_margin = new Padding(1, 1, 1, 1);

	private bool m_autoReset = false;

	#endregion

}

