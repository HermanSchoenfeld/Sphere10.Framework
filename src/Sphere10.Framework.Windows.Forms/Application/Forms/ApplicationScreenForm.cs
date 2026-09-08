// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Detached screen window. The originating host retains ownership of the screen.</summary>
public class ApplicationScreenForm : Form {
	private static readonly Size LogicalDefaultClientSize = new(900, 650);
	private static readonly Size LogicalMinimumSize = new(240, 160);
	private const int LogicalTopBorderHeight = 1;
	private readonly IApplicationScreenHost _host;
	private readonly MenuStrip? _menuStrip;
	private readonly MenuStrip? _screenMenuStrip;
	private readonly bool _screenMenuVisible;
	private readonly ScreenCaption _caption;
	private readonly ToolStrip? _screenToolBar;
	private readonly Control? _toolBarParent;
	private readonly int _toolBarIndex;
	private readonly DockStyle _toolBarDock;
	private readonly AnchorStyles _toolBarAnchor;
	private readonly Rectangle _toolBarBounds;
	private readonly bool _toolBarVisible;
	private readonly bool _movedToolBar;
	private bool _released;
	private bool _inMoveSize;
	private bool _moving;
	private bool _resizing;
	private Point _moveStartLocation;
	private int _nativeTopResizeHeight;
	private Rectangle? _nativeRestoreBounds;
	private FormWindowState _lastNativeWindowState;
	private Size? _requestedClientSize;

	public ApplicationScreenForm(IApplicationScreenHost Host, ApplicationScreen Screen) {
		Guard.ArgumentNotNull(Host, nameof(Host));
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		SuspendLayout();
		using var Layout = Tools.Scope.ExecuteOnDispose(() => ResumeLayout(true));
		_host = Host;
		this.Screen = Screen;
		Text = Screen.Title;
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.Sizable;
		MinimizeBox = MaximizeBox = true;
		AutoScaleMode = AutoScaleMode.Dpi;
		// The screen is already scaled by its previous host; initialize new window geometry at the current DPI.
		AutoScaleDimensions = new SizeF(DeviceDpi, DeviceDpi);
		ClientSize = LogicalToDeviceUnits(LogicalDefaultClientSize);
		MinimumSize = LogicalToDeviceUnits(LogicalMinimumSize);
		_caption = new ScreenCaption(this) { Name = "_screenCaption", Dock = DockStyle.Top };
		var MenuItems = Screen.MenuItems;
		if (MenuItems.Length > 0) {
			_menuStrip = new MenuStrip();
			_menuStrip.Items.AddRange(MenuItems);
			_menuStrip.ItemAdded += UpdateStripVisibility;
			_menuStrip.ItemRemoved += UpdateStripVisibility;
		}
		_screenMenuStrip = _menuStrip == null ? FindScreenMenuStrip(Screen) : null;
		_screenMenuVisible = _screenMenuStrip?.Visible ?? false;
		if (_screenMenuStrip != null) {
			_screenMenuStrip.ItemAdded += UpdateStripVisibility;
			_screenMenuStrip.ItemRemoved += UpdateStripVisibility;
			_screenMenuStrip.Visible = _screenMenuStrip.Items.Count > 0;
		}
		MainMenuStrip = _menuStrip ?? _screenMenuStrip;
		_screenToolBar = Screen.ToolBar;
		_toolBarVisible = _screenToolBar?.Visible ?? false;
		Screen.Dock = DockStyle.Fill;
		Controls.Add(Screen);
		if (_screenToolBar != null) {
			// Designer screens already reserve space for their toolbar; preserve that original layout and control.
			_movedToolBar = !Screen.Contains(_screenToolBar);
			if (_movedToolBar) {
				_toolBarParent = _screenToolBar.Parent;
				_toolBarIndex = _toolBarParent?.Controls.GetChildIndex(_screenToolBar) ?? -1;
				_toolBarDock = _screenToolBar.Dock;
				_toolBarAnchor = _screenToolBar.Anchor;
				_toolBarBounds = _screenToolBar.Bounds;
				_screenToolBar.Dock = DockStyle.Top;
				Controls.Add(_screenToolBar);
			}
			_screenToolBar.ItemAdded += UpdateStripVisibility;
			_screenToolBar.ItemRemoved += UpdateStripVisibility;
			_screenToolBar.Visible = _screenToolBar.Items.Count > 0;
		}
		if (_menuStrip != null)
			Controls.Add(_menuStrip);
		Controls.Add(_caption);
		Screen.TextChanged += ScreenTextChanged;
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ApplicationScreen Screen { get; }

	/// <summary>The actual title area in desktop coordinates, measured at the window's current DPI.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Rectangle CaptionBounds => _caption.RectangleToScreen(_caption.ClientRectangle);

	protected override CreateParams CreateParams {
		get {
			var Parameters = base.CreateParams;
			// Retain Windows sizing, system commands and restore bounds while supplying a compact caption with all four actions.
			Parameters.Style &= ~0x00C00000; // WS_CAPTION
			return Parameters;
		}
	}

	protected override Size SizeFromClientSize(Size ClientSize) {
		var WindowSize = base.SizeFromClientSize(ClientSize);
		// Without a native caption the top and bottom sizing borders have equal height.
		var NativeTopBorderHeight = (WindowSize.Height - ClientSize.Height) / 2;
		WindowSize.Height -= Math.Max(0, NativeTopBorderHeight - LogicalToDeviceUnits(LogicalTopBorderHeight));
		return WindowSize;
	}

	protected override void SetClientSizeCore(int Width, int Height) {
		_requestedClientSize = IsHandleCreated ? null : new Size(Width, Height);
		base.SetClientSizeCore(Width, Height);
		if (IsHandleCreated && WindowState == FormWindowState.Normal) {
			// WinForms also uses its internal conversion here; reconcile the HWND with our actual non-client geometry.
			var WindowSize = SizeFromClientSize(new Size(Width, Height));
			SetBounds(Left, Top, WindowSize.Width, WindowSize.Height, BoundsSpecified.Size);
		}
	}

	protected override void OnHandleCreated(EventArgs Args) {
		base.OnHandleCreated(Args);
		var RequestedSize = _requestedClientSize;
		_requestedClientSize = null;
		if (RequestedSize is { } Requested && WindowState == FormWindowState.Normal)
			SetClientSizeCore(Requested.Width, Requested.Height);
	}

	protected override void SetBoundsCore(int X, int Y, int Width, int Height, BoundsSpecified Specified) {
		// Form's internal restore path bypasses SizeFromClientSize and adds the native top border to its cached client height.
		// Preserve the placement Windows supplied when that path expands it by exactly the removed non-client inset.
		var RemovedInset = Math.Max(0, _nativeTopResizeHeight - LogicalToDeviceUnits(LogicalTopBorderHeight));
		if (_nativeRestoreBounds is { } Restored && Width == Restored.Width && Height == Restored.Height + RemovedInset)
			Height = Restored.Height;
		base.SetBoundsCore(X, Y, Width, Height, Specified);
	}

	internal void ReleaseScreen() {
		if (_released)
			return;
		_released = true;
		(_host as ApplicationScreenHost)?.ClearDockPreview();
		Screen.TextChanged -= ScreenTextChanged;
		MainMenuStrip = null;
		Controls.Remove(Screen);
		// Registered menus and their children belong to the screen, including when this window is disposed directly.
		if (_menuStrip != null) {
			_menuStrip.ItemAdded -= UpdateStripVisibility;
			_menuStrip.ItemRemoved -= UpdateStripVisibility;
			_menuStrip.Items.Clear();
		}
		if (_screenMenuStrip is { IsDisposed: false }) {
			_screenMenuStrip.ItemAdded -= UpdateStripVisibility;
			_screenMenuStrip.ItemRemoved -= UpdateStripVisibility;
			_screenMenuStrip.Visible = _screenMenuVisible;
		}
		if (_screenToolBar is { IsDisposed: false }) {
			_screenToolBar.ItemAdded -= UpdateStripVisibility;
			_screenToolBar.ItemRemoved -= UpdateStripVisibility;
			if (_movedToolBar) {
				Controls.Remove(_screenToolBar);
				_screenToolBar.Dock = DockStyle.None;
				_screenToolBar.Anchor = _toolBarAnchor;
				_screenToolBar.Bounds = _toolBarBounds;
				if (_toolBarParent is { IsDisposed: false }) {
					_toolBarParent.Controls.Add(_screenToolBar);
					_toolBarParent.Controls.SetChildIndex(_screenToolBar, Math.Min(_toolBarIndex, _toolBarParent.Controls.Count - 1));
				}
				_screenToolBar.Dock = _toolBarDock;
			}
			_screenToolBar.Visible = _toolBarVisible;
		}
	}

	protected override bool ProcessCmdKey(ref Message Message, Keys KeyData) {
		if (KeyData == (Keys.Control | Keys.Shift | Keys.D)) {
			_host.DockScreen(Screen);
			return true;
		}
		return base.ProcessCmdKey(ref Message, KeyData);
	}

	protected override void OnResize(EventArgs Args) {
		base.OnResize(Args);
		_caption?.UpdateWindowState();
	}

	protected override void OnFormClosing(FormClosingEventArgs Args) {
		base.OnFormClosing(Args);
		if (_released || Args.Cancel || Args.CloseReason == CloseReason.FormOwnerClosing || Args.CloseReason == CloseReason.ApplicationExitCall)
			return;
		Args.Cancel = !_host.CloseScreen(Screen);
	}

	protected override void OnResizeBegin(EventArgs Args) {
		base.OnResizeBegin(Args);
		_moveStartLocation = Location;
		_inMoveSize = true;
		_moving = false;
		_resizing = false;
	}

	protected override void OnMove(EventArgs Args) {
		base.OnMove(Args);
		if (_inMoveSize && !_released && _host is ApplicationScreenHost Host) {
			if (_moving && !_resizing && WindowState == FormWindowState.Normal && Location != _moveStartLocation)
				Host.UpdateDockPreview(Screen, Cursor.Position, CaptionBounds);
			else
				Host.ClearDockPreview();
		}
	}

	protected override void OnResizeEnd(EventArgs Args) {
		base.OnResizeEnd(Args);
		var Dock = _moving && !_resizing && WindowState == FormWindowState.Normal && Location != _moveStartLocation;
		_inMoveSize = false;
		_moving = false;
		if (_released || _host is not ApplicationScreenHost Host)
			return;
		if (Dock)
			Host.CompleteScreenDock(Screen, Cursor.Position, CaptionBounds);
		Host.ClearDockPreview();
	}

	protected override void WndProc(ref Message message) {
		if (message.Msg == 0x0047) { // WM_WINDOWPOSCHANGED
			var style = WinAPI.USER32.GetWindowLong(message.HWnd, -16).ToInt64();
			var nativeState = (style & 0x20000000) != 0 ? FormWindowState.Minimized : (style & 0x01000000) != 0 ? FormWindowState.Maximized : FormWindowState.Normal;
			var restoring = _lastNativeWindowState != FormWindowState.Normal && nativeState == FormWindowState.Normal;
			_lastNativeWindowState = nativeState;
			if (restoring) {
				var position = Tools.Windows.Win32.ReadStructure<WinAPI.WINDOWPOS>(message.LParam);
				var previousRestoreBounds = _nativeRestoreBounds;
				using var restorePlacement = Tools.Scope.ExecuteOnDispose(() => _nativeRestoreBounds = previousRestoreBounds);
				_nativeRestoreBounds = new Rectangle(position.x, position.y, position.cx, position.cy);
				base.WndProc(ref message);
				return;
			}
		}
		var windowBounds = message.Msg == WinAPI.USER32.WM_NCCALCSIZE ? Tools.Windows.Win32.ReadStructure<WinAPI.RECT>(message.LParam) : default;
		if (_inMoveSize) {
			if (message.Msg == WinAPI.USER32.WM_MOVING)
				_moving = true;
			if (message.Msg == WinAPI.USER32.WM_SIZING) {
				_resizing = true;
				(_host as ApplicationScreenHost)?.ClearDockPreview();
			}
		}
		base.WndProc(ref message);
		if (message.Msg == WinAPI.USER32.WM_NCCALCSIZE) {
			// RECT is also the first field of NCCALCSIZE_PARAMS, so both native message variants use the same layout here.
			var clientBounds = Tools.Windows.Win32.ReadStructure<WinAPI.RECT>(message.LParam);
			_nativeTopResizeHeight = Math.Max(0, clientBounds.Top - windowBounds.Top);
			clientBounds.Top = Math.Min(clientBounds.Bottom, windowBounds.Top + LogicalToDeviceUnits(LogicalTopBorderHeight));
			Tools.Windows.Win32.WriteStructure(message.LParam, clientBounds);
			message.Result = IntPtr.Zero;
		}
		if (message.Msg == 0x0084 && _caption != null) { // WM_NCHITTEST
			var coordinates = message.LParam.ToInt64();
			var position = new Point(unchecked((short)coordinates), unchecked((short)(coordinates >> 16)));
			if (IsTopResizeArea(position)) {
				var clientOrigin = PointToScreen(Point.Empty);
				message.Result = (IntPtr)(position.X < clientOrigin.X ? 13 : position.X >= clientOrigin.X + ClientSize.Width ? 14 : 12); // HTTOPLEFT, HTTOPRIGHT, HTTOP
			} else if (message.Result == (IntPtr)1 && _caption.IsDragArea(position))
				message.Result = (IntPtr)2; // HTCAPTION: Windows handles moving, double-click, system menu and cancellation.
		}
		if (message.Msg == 0x0024) { // WM_GETMINMAXINFO
			var monitor = System.Windows.Forms.Screen.FromHandle(message.HWnd);
			var limits = Tools.Windows.Win32.ReadStructure<WinAPI.USER32.MINMAXINFO>(message.LParam);
			limits.MaxPosition = new Point(monitor.WorkingArea.Left - monitor.Bounds.Left, monitor.WorkingArea.Top - monitor.Bounds.Top);
			limits.MaxSize = new Size(monitor.WorkingArea.Width, monitor.WorkingArea.Height);
			Tools.Windows.Win32.WriteStructure(message.LParam, limits);
		}
	}

	protected override void Dispose(bool Disposing) {
		if (Disposing)
			ReleaseScreen();
		base.Dispose(Disposing);
	}

	private void ScreenTextChanged(object? Sender, EventArgs Args) {
		Text = Screen.Title;
		_caption.AccessibleName = Text;
		_caption.Invalidate();
	}

	private bool IsTopResizeArea(Point Position) => WindowState == FormWindowState.Normal && Position.X >= Left && Position.X < Right
		&& Position.Y >= Top && Position.Y < Top + _nativeTopResizeHeight;

	private void UpdateStripVisibility(object? Sender, ToolStripItemEventArgs Args) {
		if (!_released && Sender is ToolStrip Strip)
			Strip.Visible = Strip.Items.Count > 0;
	}

	private static MenuStrip? FindScreenMenuStrip(Control Parent) {
		foreach (Control Child in Parent.Controls) {
			if (Child is MenuStrip Menu)
				return Menu;
			if (FindScreenMenuStrip(Child) is { } NestedMenu)
				return NestedMenu;
		}
		return null;
	}

	private enum CaptionAction { Dock, Minimize, Maximize, Restore, Close }

	private sealed class ScreenCaption : Panel {
		private const int LogicalHeight = 28;
		private const int LogicalButtonWidth = 32;
		private const int LogicalPadding = 8;
		private const int BorderThickness = 1;
		private readonly ApplicationScreenForm _window;
		private readonly ToolTip _toolTip;
		private readonly CaptionActionButton _maximizeButton;

		public ScreenCaption(ApplicationScreenForm Window) {
			_window = Window;
			_toolTip = new ToolTip();
			Font = SystemFonts.SmallCaptionFont;
			BackColor = SystemColors.Control;
			ForeColor = SystemColors.ControlText;
			AccessibleRole = AccessibleRole.TitleBar;
			AccessibleName = Window.Text;
			DoubleBuffered = true;
			AddAction(CaptionAction.Dock, "_redockButton", "Re-dock", "Re-dock (Ctrl+Shift+D)", () => Window._host.DockScreen(Window.Screen));
			AddAction(CaptionAction.Minimize, "_minimizeButton", "Minimize", "Minimize", () => Window.WindowState = FormWindowState.Minimized);
			_maximizeButton = AddAction(CaptionAction.Maximize, "_maximizeButton", "Maximize", "Maximize",
				() => Window.WindowState = Window.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized);
			AddAction(CaptionAction.Close, "_closeButton", "Close", "Close", Window.Close);
		}

		public bool IsDragArea(Point Position) => RectangleToScreen(ClientRectangle).Contains(Position)
			&& !Controls.Cast<Control>().Any(Button => Button.RectangleToScreen(Button.ClientRectangle).Contains(Position));

		public void UpdateWindowState() {
			var Maximized = _window.WindowState == FormWindowState.Maximized;
			_maximizeButton.Action = Maximized ? CaptionAction.Restore : CaptionAction.Maximize;
			_maximizeButton.AccessibleName = Maximized ? "Restore" : "Maximize";
			_maximizeButton.AccessibleDescription = _maximizeButton.AccessibleName;
			_toolTip.SetToolTip(_maximizeButton, _maximizeButton.AccessibleName);
			_maximizeButton.Invalidate();
		}

		protected override void OnLayout(LayoutEventArgs Args) {
			base.OnLayout(Args);
			Height = Math.Max(LogicalToDeviceUnits(LogicalHeight), Font.Height + LogicalToDeviceUnits(LogicalPadding));
			var ButtonWidth = LogicalToDeviceUnits(LogicalButtonWidth);
			for (var Index = 0; Index < Controls.Count; Index++)
				Controls[Index].Bounds = new Rectangle(Width - ButtonWidth * (Controls.Count - Index), 0, ButtonWidth, Height - BorderThickness);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs Args) {
			base.OnPaint(Args);
			var Padding = LogicalToDeviceUnits(LogicalPadding);
			var TitleWidth = Math.Max(0, (Controls.Count == 0 ? Width : Controls[0].Left) - Padding * 2);
			TextRenderer.DrawText(Args.Graphics, _window.Text, Font, new Rectangle(Padding, 0, TitleWidth, Height), ForeColor,
				TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
			Args.Graphics.DrawLine(SystemPens.ControlDark, 0, Height - BorderThickness, Width, Height - BorderThickness);
		}

		protected override void WndProc(ref Message Message) {
			if (Message.Msg == 0x0084) { // WM_NCHITTEST
				Message.Result = (IntPtr)(-1); // HTTRANSPARENT: let the form supply native caption hit testing.
				return;
			}
			base.WndProc(ref Message);
		}

		protected override void Dispose(bool Disposing) {
			if (Disposing)
				_toolTip.Dispose();
			base.Dispose(Disposing);
		}

		private CaptionActionButton AddAction(CaptionAction Action, string Name, string AccessibleName, string Hint, Action Execute) {
			var Button = new CaptionActionButton(Action) { Name = Name, AccessibleName = AccessibleName, AccessibleDescription = Hint, TabIndex = Controls.Count };
			Button.Click += (_, _) => Execute();
			_toolTip.SetToolTip(Button, Hint);
			Controls.Add(Button);
			return Button;
		}
	}

	private sealed class CaptionActionButton : Button {
		private const int LogicalGlyphSize = 10;
		private const int LogicalGlyphOffset = 3;
		private const int LogicalStrokeWidth = 1;

		public CaptionActionButton(CaptionAction Action) {
			this.Action = Action;
			FlatStyle = FlatStyle.Flat;
			FlatAppearance.BorderSize = 0;
			FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
			AccessibleRole = AccessibleRole.PushButton;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CaptionAction Action { get; set; }

		protected override void OnPaint(PaintEventArgs Args) {
			base.OnPaint(Args);
			var Size = LogicalToDeviceUnits(LogicalGlyphSize);
			var Offset = LogicalToDeviceUnits(LogicalGlyphOffset);
			var Bounds = new Rectangle((Width - Size) / 2, (Height - Size) / 2, Size, Size);
			using var Pen = new Pen(Enabled ? ForeColor : SystemColors.GrayText, Math.Max(1, LogicalToDeviceUnits(LogicalStrokeWidth)));
			switch (Action) {
				case CaptionAction.Dock:
					Args.Graphics.DrawRectangle(Pen, Bounds.Left, Bounds.Top + Offset, Bounds.Width, Bounds.Height - Offset);
					Args.Graphics.DrawLine(Pen, Bounds.Right, Bounds.Top - Offset, Bounds.Left + Offset, Bounds.Bottom - Offset);
					Args.Graphics.DrawLines(Pen, new[] {
						new Point(Bounds.Left + Offset, Bounds.Top + Offset), new Point(Bounds.Left + Offset, Bounds.Bottom - Offset),
						new Point(Bounds.Right - Offset, Bounds.Bottom - Offset)
					});
					break;
				case CaptionAction.Minimize:
					Args.Graphics.DrawLine(Pen, Bounds.Left, Bounds.Bottom, Bounds.Right, Bounds.Bottom);
					break;
				case CaptionAction.Maximize:
					Args.Graphics.DrawRectangle(Pen, Bounds);
					break;
				case CaptionAction.Restore:
					Args.Graphics.DrawLines(Pen, new[] {
						new Point(Bounds.Left + Offset, Bounds.Top + Offset), new Point(Bounds.Left + Offset, Bounds.Top),
						new Point(Bounds.Right, Bounds.Top), new Point(Bounds.Right, Bounds.Bottom - Offset), new Point(Bounds.Right - Offset, Bounds.Bottom - Offset)
					});
					Args.Graphics.DrawRectangle(Pen, Bounds.Left, Bounds.Top + Offset, Bounds.Width - Offset, Bounds.Height - Offset);
					break;
				case CaptionAction.Close:
					Args.Graphics.DrawLine(Pen, Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Bottom);
					Args.Graphics.DrawLine(Pen, Bounds.Right, Bounds.Top, Bounds.Left, Bounds.Bottom);
					break;
			}
		}

		protected override void WndProc(ref Message Message) {
			if (Message.Msg == 0x0084 && FindForm() is ApplicationScreenForm Window) { // WM_NCHITTEST
				var Coordinates = Message.LParam.ToInt64();
				var Position = new Point(unchecked((short)Coordinates), unchecked((short)(Coordinates >> 16)));
				if (Window.IsTopResizeArea(Position)) {
					Message.Result = (IntPtr)(-1); // Let the native resize edge continue across caption buttons.
					return;
				}
			}
			base.WndProc(ref Message);
		}
	}
}
