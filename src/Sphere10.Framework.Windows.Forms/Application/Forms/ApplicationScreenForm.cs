// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Detached screen window. The originating host retains ownership of the screen.</summary>
public class ApplicationScreenForm : Form {
	private readonly record struct StripLocation(ToolStrip Strip, Control Parent, int Index);

	private static readonly Size LogicalDefaultClientSize = new(900, 650);
	private static readonly Size LogicalMinimumSize = new(240, 160);
	private const int LogicalTopBorderHeight = 1;
	private readonly IApplicationScreenHost _host;
	private readonly IApplicationScreenHost _screenOwner;
	private MenuStrip _contextMenuStrip;
	private ToolStrip _contextToolBar;
	private MenuStrip? _screenMenuStrip;
	private bool _screenMenuVisible;
	private bool _restoreRegisteredMenuSource;
	private Control? _menuParent;
	private int _menuIndex;
	private DockStyle _menuDock;
	private AnchorStyles _menuAnchor;
	private Rectangle _menuBounds;
	private bool _movedMenuStrip;
	private readonly ScreenCaption _caption;
	private ToolStrip? _screenToolBar;
	private Control? _toolBarParent;
	private int _toolBarIndex;
	private DockStyle _toolBarDock;
	private AnchorStyles _toolBarAnchor;
	private Rectangle _toolBarBounds;
	private bool _toolBarVisible;
	private bool _movedToolBar;
	private bool _released;
	private bool _closing;
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
		Guard.Argument(Screen.Host == null || ReferenceEquals(Screen.Host, Host), nameof(Screen), "The screen belongs to another host.");
		SuspendLayout();
		using var Layout = Tools.Scope.ExecuteOnDispose(() => ResumeLayout(true));
		_host = Host;
		_screenOwner = Screen.Host;
		this.Screen = Screen;
		ScreenHost = new DetachedApplicationScreenHost(Host, this);
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
		Screen.Dock = DockStyle.Fill;
		Controls.Add(Screen);
		Controls.Add(_caption);
		Screen.TextChanged += ScreenTextChanged;
		Screen.NativeCommandsChanged += ScreenNativeCommandsChanged;
		AttachNativeStrips();

		// The application retains ownership; this window supplies only its own active-screen context.
		Screen.Host = ScreenHost;
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ApplicationScreen Screen { get; }

	/// <summary>The local active-screen view over the originating application's shared host.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IApplicationScreenHost ScreenHost { get; }

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
		Screen.NativeCommandsChanged -= ScreenNativeCommandsChanged;
		if (ReferenceEquals(Screen.Host, ScreenHost))
			Screen.Host = _screenOwner;
		RestoreNativeStrips();
		ScreenHost.Dispose();
		Controls.Remove(Screen);
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

	// Cancel the native attempt while the existing host validates and saves; successful removal releases this window.
	protected override async void OnFormClosing(FormClosingEventArgs Args) {
		base.OnFormClosing(Args);
		if (_released || Args.Cancel || Args.CloseReason == CloseReason.FormOwnerClosing || Args.CloseReason == CloseReason.ApplicationExitCall)
			return;
		Args.Cancel = true;
		if (_closing)
			return;
		_closing = true;
		try {
			await _host.CloseScreenAsync(Screen);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		} finally {
			_closing = false;
		}
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

	// Property replacement while detached returns the old views before borrowing the newly assigned controls.
	private void ScreenNativeCommandsChanged() {
		if (_released)
			return;
		SuspendLayout();
		using var Layout = Tools.Scope.ExecuteOnDispose(() => ResumeLayout(true));
		RestoreNativeStrips();
		AttachNativeStrips();
	}

	// Designer-owned strips keep their screen layout; externally parented strips are borrowed by this window.
	private void AttachNativeStrips() {
		_restoreRegisteredMenuSource = Screen.ActivateDetachedCommands();
		_screenMenuStrip = Screen.ScreenMenuStrip ?? FindScreenMenuStrip(Screen);
		_screenToolBar = Screen.ScreenToolBar;
		// Capture both original positions before moving either control out of a shared parent.
		if (_screenMenuStrip != null) {
			_screenMenuVisible = !TypeDescriptor.GetProperties(typeof(Control))[nameof(Control.Visible)].ShouldSerializeValue(_screenMenuStrip);
			_movedMenuStrip = !Screen.Contains(_screenMenuStrip);
			if (_movedMenuStrip) {
				_menuParent = _screenMenuStrip.Parent;
				_menuIndex = _menuParent?.Controls.GetChildIndex(_screenMenuStrip) ?? -1;
				_menuDock = _screenMenuStrip.Dock;
				_menuAnchor = _screenMenuStrip.Anchor;
				_menuBounds = _screenMenuStrip.Bounds;
			}
		}
		if (_screenToolBar != null) {
			_toolBarVisible = !TypeDescriptor.GetProperties(typeof(Control))[nameof(Control.Visible)].ShouldSerializeValue(_screenToolBar);
			_movedToolBar = !Screen.Contains(_screenToolBar);
			if (_movedToolBar) {
				_toolBarParent = _screenToolBar.Parent;
				_toolBarIndex = _toolBarParent?.Controls.GetChildIndex(_screenToolBar) ?? -1;
				_toolBarDock = _screenToolBar.Dock;
				_toolBarAnchor = _screenToolBar.Anchor;
				_toolBarBounds = _screenToolBar.Bounds;
			}
		}

		if (_screenMenuStrip != null) {
			if (_movedMenuStrip) {
				_screenMenuStrip.Dock = DockStyle.Top;
				Controls.Add(_screenMenuStrip);
			}
			_screenMenuStrip.ItemAdded += UpdateStripVisibility;
			_screenMenuStrip.ItemRemoved += UpdateStripVisibility;
			_screenMenuStrip.Visible = _screenMenuVisible && _screenMenuStrip.Items.Count > 0;
		}
		if (_screenToolBar != null) {
			if (_movedToolBar) {
				_screenToolBar.Dock = DockStyle.Top;
				Controls.Add(_screenToolBar);
			}
			_screenToolBar.ItemAdded += UpdateStripVisibility;
			_screenToolBar.ItemRemoved += UpdateStripVisibility;
			_screenToolBar.Visible = _toolBarVisible && _screenToolBar.Items.Count > 0;
		}

		MergeContextCommands();
		MainMenuStrip = _screenMenuVisible && _screenMenuStrip != null ? _screenMenuStrip
			: _contextMenuStrip?.Items.Count > 0 ? _contextMenuStrip : null;
		// Docking walks reverse child order: allocate caption, menu and toolbar before the fill screen.
		Screen.BringToFront();
		if (_movedToolBar)
			_screenToolBar?.SendToBack();
		_contextToolBar?.SendToBack();
		if (_movedMenuStrip)
			_screenMenuStrip?.SendToBack();
		_contextMenuStrip?.SendToBack();
		_caption.SendToBack();
	}

	// Shared owners retain command definitions; this window owns the generated views merged into its local strips.
	// A detached window presents its screen's own commands and those of its plugin and block. Application-wide
	// commands (File, Help, Options, Shutdown and the application toolbar) stay with the main window.
	private void MergeContextCommands() {
		var userInterface = ScreenHost.UserInterfaceServices;
		var owners = new IApplicationCommandProvider[] { Screen.Block?.Source, Screen.Block }.Where(owner => owner != null).ToArray();

		// Legacy registrations are already native commands; a detached window displays their original top-level items.
		var menu = _screenMenuVisible ? _screenMenuStrip : null;
		var registeredItems = Screen.ScreenMenuStrip == null ? Screen.MenuItems : Array.Empty<ToolStripItem>();
		var hasMenuCommands = registeredItems.Length > 0
			|| owners.Any(owner => (owner is not IApplicationBlock block || block.ShowInMenuStrip)
				&& owner.Menus.Cast<IApplicationMenu>().Any(item => item.ShowInMenuStrip));
		if (menu == null && hasMenuCommands) {
			_contextMenuStrip ??= new MenuStrip { Name = "_contextMenuStrip", Dock = DockStyle.Top };
			if (_contextMenuStrip.Parent == null)
				Controls.Add(_contextMenuStrip);
			menu = _contextMenuStrip;
			menu.Items.AddRange(registeredItems);
		}

		// Create a contextual row only when an active owner actually contributes commands to it.
		var toolBar = _toolBarVisible ? _screenToolBar : null;
		var hasToolBarCommands = owners.Any(owner => owner.ToolBarItems.Length > 0 || owner is IApplicationBlock block && block.ShowInToolStrip
				&& block.Menus.SelectMany(item => item.Items).OfType<IWinFormsLinkMenuItem>().Any(item => item.ShowOnToolStrip));
		if (toolBar == null && hasToolBarCommands) {
			_contextToolBar ??= new ToolStrip { Name = "_contextToolBar", Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
			if (_contextToolBar.Parent == null)
				Controls.Add(_contextToolBar);
			toolBar = _contextToolBar;
		}

		WinFormsCommandControls.MergeSources(this, menu, toolBar, owners, userInterface);
		if (_contextMenuStrip != null)
			_contextMenuStrip.Visible = _contextMenuStrip.Items.Count > 0;
		if (_contextToolBar != null)
			_contextToolBar.Visible = _contextToolBar.Items.Count > 0;
	}

	// Return the currently attached controls before replacing them or releasing the borrowed screen.
	private void RestoreNativeStrips() {
		Screen.UnmergeCommands();
		WinFormsCommandControls.ClearSources(this);
		if (_contextMenuStrip != null)
			Screen.RestoreRegisteredMenuItems(_contextMenuStrip, _restoreRegisteredMenuSource);
		_contextMenuStrip?.Dispose();
		_contextToolBar?.Dispose();
		_contextMenuStrip = null;
		_contextToolBar = null;
		MainMenuStrip = null;
		if (_screenMenuStrip is { IsDisposed: false }) {
			_screenMenuStrip.ItemAdded -= UpdateStripVisibility;
			_screenMenuStrip.ItemRemoved -= UpdateStripVisibility;
			if (_movedMenuStrip) {
				Controls.Remove(_screenMenuStrip);
				_screenMenuStrip.Dock = DockStyle.None;
				_screenMenuStrip.Anchor = _menuAnchor;
				_screenMenuStrip.Bounds = _menuBounds;
				if (_menuParent is { IsDisposed: false })
					_menuParent.Controls.Add(_screenMenuStrip);
				_screenMenuStrip.Dock = _menuDock;
			}
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
				if (_toolBarParent is { IsDisposed: false })
					_toolBarParent.Controls.Add(_screenToolBar);
				_screenToolBar.Dock = _toolBarDock;
			}
			_screenToolBar.Visible = _toolBarVisible;
		}

		RestoreStripOrder(_menuParent);
		if (!ReferenceEquals(_menuParent, _toolBarParent))
			RestoreStripOrder(_toolBarParent);

		_screenMenuStrip = null;
		_screenToolBar = null;
		_menuParent = null;
		_toolBarParent = null;
		_movedMenuStrip = false;
		_movedToolBar = false;
		_restoreRegisteredMenuSource = false;
	}

	// Native z-order ignores siblings without HWNDs; replay the desired order at the front without creating hidden controls.
	private void RestoreStripOrder(Control parent) {
		if (parent is not { IsDisposed: false })
			return;
		var returnedStrips = new[] {
			new StripLocation(_screenMenuStrip, _menuParent, _menuIndex),
			new StripLocation(_screenToolBar, _toolBarParent, _toolBarIndex)
		}.Where(strip => strip.Strip is { IsDisposed: false } && ReferenceEquals(strip.Parent, parent)
			&& ReferenceEquals(strip.Strip.Parent, parent) && strip.Index >= 0).OrderBy(strip => strip.Index).ToArray();
		if (returnedStrips.Length == 0)
			return;

		var orderedControls = parent.Controls.Cast<Control>()
			.Where(control => returnedStrips.All(strip => !ReferenceEquals(strip.Strip, control))).ToList();
		foreach (var strip in returnedStrips)
			orderedControls.Insert(Math.Min(strip.Index, orderedControls.Count), strip.Strip);

		for (var index = orderedControls.Count - 1; index >= 0; index--)
			parent.Controls.SetChildIndex(orderedControls[index], 0);
	}

	private void ScreenTextChanged(object? Sender, EventArgs Args) {
		Text = Screen.Title;
		_caption.AccessibleName = Text;
		_caption.Invalidate();
	}

	private bool IsTopResizeArea(Point position) {
		return WindowState == FormWindowState.Normal && position.X >= Left && position.X < Right
			&& position.Y >= Top && position.Y < Top + _nativeTopResizeHeight;
	}

	private void UpdateStripVisibility(object? Sender, ToolStripItemEventArgs Args) {
		if (!_released && Sender is ToolStrip Strip) {
			var RequestedVisible = ReferenceEquals(Strip, _screenMenuStrip) ? _screenMenuVisible
				: !ReferenceEquals(Strip, _screenToolBar) || _toolBarVisible;
			Strip.Visible = RequestedVisible && Strip.Items.Count > 0;
		}
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

	#region Inner Types

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

		public bool IsDragArea(Point position) {
			return RectangleToScreen(ClientRectangle).Contains(position)
				&& !Controls.Cast<Control>().Any(button => button.RectangleToScreen(button.ClientRectangle).Contains(position));
		}

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

	#endregion
}
