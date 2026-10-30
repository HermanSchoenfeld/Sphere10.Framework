// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Screen tabs with close buttons, a context menu, reordering and drag-out requests.</summary>
public partial class WinFormsApplicationScreenTabControl : TabControl {
	public event EventHandlerEx<WinFormsApplicationScreen>? ScreenCloseRequested;
	public event EventHandlerEx<WinFormsApplicationScreen>? ScreenUndockRequested;
	public event EventHandlerEx ScreenCloseAllRequested;
	public event EventHandlerEx<WinFormsApplicationScreen> ScreenCloseOthersRequested;
	public event EventHandlerEx TabOrderChanged;

	private const int LogicalDpi = 96;
	private const int LogicalHorizontalPadding = 20;
	private const int LogicalVerticalPadding = 5;
	private const int LogicalTextInset = 6;
	private const int LogicalCloseSize = 16;
	private const int LogicalIconSize = 16;
	private const int LogicalIconGap = 4;
	private const int LogicalGlyphInset = 4;
	private const int LogicalBorderWidth = 2;
	private const int LogicalDockMarkerWidth = 3;
	private const int LogicalDockPreviewMinimumWidth = 180;
	private const int LogicalDockPreviewCornerSize = 4;
	private const int LogicalDockToolTipGap = 4;
	private const int LogicalMinimumTabWidth = 64;
	private static readonly (Color BackColor, Color ForeColor) _defaultDockPreviewColors = GetDefaultDockPreviewColors();
	private Color _dockPreviewBackColor = _defaultDockPreviewColors.BackColor;
	private Color _dockPreviewForeColor = _defaultDockPreviewColors.ForeColor;
	private TabPage? _pressedTab;
	private TabPage? _contextTab;
	private int _dockPreviewIndex = -1;
	private string? _dockPreviewTitle;
	private Rectangle _dockMarkerBounds;
	private Rectangle _dockPreviewBounds;
	private Point _pressLocation;
	private bool _dragging;
	private bool _undockPreviewVisible;
	private bool _completingDrag;
	private bool _updatingCaptions;
	private bool _deferringInputRendering;
	private bool _refreshAfterRecreate;
	private int _originalIndex;
	private int _maximumTabWidth = 320;
	private int _metricsDpi = LogicalDpi;

	public WinFormsApplicationScreenTabControl() {
		InitializeComponent();
		_metricsDpi = DeviceDpi;
		ApplyMetrics();
	}

	private void TabMenuOpening(object sender, CancelEventArgs args) {
		_undockMenuItem.Enabled = _closeMenuItem.Enabled = CanCloseOrUndock(_contextTab);
		var screen = _contextTab?.Tag as WinFormsApplicationScreen;
		var openScreens = screen?.Host?.OpenScreens ?? Array.Empty<WinFormsApplicationScreen>();
		_closeAllMenuItem.Enabled = openScreens.Any(candidate => candidate.ActivationMode != ScreenActivationMode.PermanentSingleton);
		_closeOthersMenuItem.Enabled = openScreens.Any(candidate => !ReferenceEquals(candidate, screen) && candidate.ActivationMode != ScreenActivationMode.PermanentSingleton);
		var startupHost = screen?.Host as WinFormsApplicationScreenHost;
		_makeStartupMenuItem.Enabled = startupHost?.CanMakeStartupScreen(screen) == true;
		_makeStartupMenuItem.Checked = startupHost?.IsStartupScreen(screen) == true;
		_makeStartupMenuItem.ToolTipText = _makeStartupMenuItem.Enabled
			? "Open this screen when the application starts."
			: "This screen requires session-specific state or is not registered for startup restoration.";
	}

	private void UndockMenuClicked(object sender, EventArgs args) => RequestUndock(_contextTab);

	private void CloseMenuClicked(object sender, EventArgs args) => RequestClose(_contextTab);

	private void CloseAllMenuClicked(object sender, EventArgs args) => ScreenCloseAllRequested?.Invoke();

	private void CloseOthersMenuClicked(object sender, EventArgs args) {
		if (_contextTab?.Tag is WinFormsApplicationScreen screen)
			ScreenCloseOthersRequested?.Invoke(screen);
	}

	private async void MakeStartupMenuClicked(object sender, EventArgs args) {
		if (_contextTab?.Tag is not WinFormsApplicationScreen screen || screen.Host is not WinFormsApplicationScreenHost host)
			return;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await host.MakeStartupScreenAsync(screen);
		});
		if (error != null) {
			await ExceptionDialog.ShowAsync(this, "Make Startup Screen", error);
		}
	}

	internal bool Reordering { get; private set; }

	[DefaultValue(320), Category("Layout"), Description("Maximum width of a tab in logical pixels at 96 DPI; longer titles use an ellipsis")]
	public int MaximumTabWidth {
		get => _maximumTabWidth;
		set {
			Guard.ArgumentGTE(value, LogicalMinimumTabWidth, nameof(value), "The maximum width must leave room for the title and close button");
			if (_maximumTabWidth == value)
				return;
			_maximumTabWidth = value;
			RefreshTabCaptions();
		}
	}

	[Category("Appearance"), Description("Docking preview background, using the ExplorerBar theme blue by default")]
	public Color DockPreviewBackColor {
		get => _dockPreviewBackColor;
		set {
			if (_dockPreviewBackColor == value)
				return;
			_dockPreviewBackColor = value;
			if (DockPreviewVisible)
				InvalidatePreview(Rectangle.Union(_dockMarkerBounds, _dockPreviewBounds));
		}
	}

	[Category("Appearance"), Description("Docking preview caption color")]
	public Color DockPreviewForeColor {
		get => _dockPreviewForeColor;
		set {
			if (_dockPreviewForeColor == value)
				return;
			_dockPreviewForeColor = value;
			if (DockPreviewVisible)
				InvalidatePreview(Rectangle.Union(_dockMarkerBounds, _dockPreviewBounds));
		}
	}

	/// <summary>The header area in client coordinates, including an empty header that can accept a detached screen.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Rectangle TabStripBounds {
		get {
			var HeaderHeight = Math.Max(ItemSize.Height, Font.Height + Padding.Y * 2) + ScaleMetric(LogicalBorderWidth);
			if (TabCount > 0) {
				HeaderHeight = 0;
				for (var Index = 0; Index < TabCount; Index++)
					HeaderHeight = Math.Max(HeaderHeight, GetTabRect(Index).Bottom);
			}
			return new Rectangle(0, 0, ClientSize.Width, Math.Min(ClientSize.Height, HeaderHeight));
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool DockPreviewVisible => _dockPreviewIndex >= 0;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int DockPreviewIndex => _dockPreviewIndex;

	/// <summary>Whether releasing the current dragged tab at the last pointer position will request undocking.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool UndockPreviewVisible => _undockPreviewVisible;

	/// <summary>Shows a highlighted docking tab without adding pages or disturbing their layout and selection.</summary>
	public void ShowDockPreview(string Title, Point Location) {
		Guard.ArgumentNotNull(Title, nameof(Title));
		var Index = GetInsertionIndex(Location);
		var MarkerBounds = GetDockMarkerBounds(Index);
		var PreviewBounds = GetDockPreviewBounds(Title, MarkerBounds);
		if (_dockPreviewIndex == Index && _dockPreviewTitle == Title && _dockMarkerBounds == MarkerBounds && _dockPreviewBounds == PreviewBounds)
			return;
		var PreviousBounds = DockPreviewVisible ? Rectangle.Union(_dockMarkerBounds, _dockPreviewBounds) : Rectangle.Empty;
		_dockPreviewIndex = Index;
		_dockPreviewTitle = Title;
		_dockMarkerBounds = MarkerBounds;
		_dockPreviewBounds = PreviewBounds;
		InvalidatePreview(PreviousBounds);
		InvalidatePreview(Rectangle.Union(MarkerBounds, PreviewBounds));
		if (IsHandleCreated && Visible)
			_dockPreviewTip.Show($"Release to dock: {Title}", this, new Point(PreviewBounds.Left, TabStripBounds.Bottom + ScaleMetric(LogicalDockToolTipGap)));
	}

	public void HideDockPreview() {
		if (!DockPreviewVisible)
			return;
		var PreviousBounds = Rectangle.Union(_dockMarkerBounds, _dockPreviewBounds);
		_dockPreviewIndex = -1;
		_dockPreviewTitle = null;
		_dockMarkerBounds = Rectangle.Empty;
		_dockPreviewBounds = Rectangle.Empty;
		_dockPreviewTip.Hide(this);
		InvalidatePreview(PreviousBounds);
	}

	/// <summary>Moves a tab without changing the selected screen or its lifecycle.</summary>
	public void MoveTab(TabPage Page, int Index) {
		Guard.ArgumentNotNull(Page, nameof(Page));
		Guard.Argument(TabPages.Contains(Page), nameof(Page), "Tab does not belong to this control");
		Guard.ArgumentInRange(Index, 0, TabCount - 1, nameof(Index));
		if (TabPages.IndexOf(Page) == Index)
			return;
		using var Update = EnterReorderScope();
		var Selected = SelectedTab;
		TabPages.Remove(Page);
		TabPages.Insert(Index, Page);
		SelectedTab = Selected;
		Invalidate();
		TabOrderChanged?.Invoke();
	}

	protected override void OnDrawItem(DrawItemEventArgs Args) {
		// Native draw callbacks can still reference a tab removed during undocking or preview cleanup.
		if (Disposing || IsDisposed || Args.Index < 0 || Args.Index >= TabCount)
			return;
		var Page = TabPages[Args.Index];
		var Bounds = Args.Bounds;
		var undockPreview = _undockPreviewVisible && ReferenceEquals(Page, _pressedTab);
		using var Background = new SolidBrush(undockPreview ? SystemColors.ControlDark : Args.Index == SelectedIndex ? SystemColors.Window : SystemColors.Control);
		Args.Graphics.FillRectangle(Background, Bounds);
		var CloseBounds = GetCloseBounds(Bounds);
		var TextBounds = Rectangle.FromLTRB(Bounds.Left + ScaleMetric(LogicalTextInset), Bounds.Top, CloseBounds.Left - ScaleMetric(LogicalTextInset), Bounds.Bottom);
		var imageIndex = !string.IsNullOrEmpty(Page.ImageKey) ? ImageList?.Images.IndexOfKey(Page.ImageKey) ?? -1 : Page.ImageIndex;
		if (ImageList != null && imageIndex >= 0 && imageIndex < ImageList.Images.Count) {
			var imageSize = ScaleMetric(LogicalIconSize);
			ImageList.Draw(Args.Graphics, TextBounds.Left, Bounds.Top + (Bounds.Height - imageSize) / 2, imageSize, imageSize, imageIndex);
			TextBounds.X += imageSize + ScaleMetric(LogicalIconGap);
			TextBounds.Width = Math.Max(0, TextBounds.Width - imageSize - ScaleMetric(LogicalIconGap));
		}
		// The explicit insets provide text padding; match the native caption's glyph width when drawing.
		TextRenderer.DrawText(Args.Graphics, Page.Text, Font, TextBounds, SystemColors.ControlText,
			TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
		if (_dragging && Page == _pressedTab) {
			var BorderWidth = ScaleMetric(LogicalBorderWidth);
			using var Border = new Pen(undockPreview ? SystemColors.ControlDarkDark : DockPreviewBackColor, BorderWidth);
			Args.Graphics.DrawRectangle(Border, Rectangle.Inflate(Bounds, -BorderWidth / 2, -BorderWidth / 2));
		}
		if (CanCloseOrUndock(Page)) {
			var GlyphBounds = Rectangle.Inflate(CloseBounds, -ScaleMetric(LogicalGlyphInset), -ScaleMetric(LogicalGlyphInset));
			using var Pen = new Pen(SystemColors.ControlText, Math.Max(1, _metricsDpi / (float)LogicalDpi));
			Args.Graphics.DrawLine(Pen, GlyphBounds.Left, GlyphBounds.Top, GlyphBounds.Right, GlyphBounds.Bottom);
			Args.Graphics.DrawLine(Pen, GlyphBounds.Right, GlyphBounds.Top, GlyphBounds.Left, GlyphBounds.Bottom);
		}
		base.OnDrawItem(Args);
	}

	protected override void OnControlAdded(ControlEventArgs Args) {
		base.OnControlAdded(Args);
		if (Args.Control is TabPage Page)
			ApplyTabCaption(TabPages.IndexOf(Page));
	}

	protected override void OnControlRemoved(ControlEventArgs Args) {
		HideDockPreview();
		if (!Reordering && ReferenceEquals(Args.Control, _pressedTab))
			CancelDrag();
		base.OnControlRemoved(Args);
	}

	protected override void OnHandleCreated(EventArgs Args) {
		base.OnHandleCreated(Args);
		_refreshAfterRecreate = RecreatingHandle;
		RefreshTabCaptions();
	}

	protected override void OnSizeChanged(EventArgs Args) {
		base.OnSizeChanged(Args);
		// WinForms restores its native tabs after OnHandleCreated, then performs a final resize.
		if (_refreshAfterRecreate && TabCount > 0 && TabCount == Controls.Count) {
			_refreshAfterRecreate = false;
			RefreshTabCaptions();
		}
	}

	protected override void OnFontChanged(EventArgs Args) {
		base.OnFontChanged(Args);
		RefreshTabCaptions();
	}

	protected override void RescaleConstantsForDpi(int DeviceDpiOld, int DeviceDpiNew) {
		base.RescaleConstantsForDpi(DeviceDpiOld, DeviceDpiNew);
		_metricsDpi = DeviceDpiNew;
		ApplyMetrics();
	}

	protected override void OnDpiChangedAfterParent(EventArgs Args) {
		base.OnDpiChangedAfterParent(Args);
		RefreshTabCaptions();
	}

	protected override void OnMouseDown(MouseEventArgs Args) {
		_dragging = false;
		_undockPreviewVisible = false;
		_pressedTab = GetTabAt(Args.Location);
		_pressLocation = Args.Location;
		_originalIndex = _pressedTab == null ? -1 : TabPages.IndexOf(_pressedTab);
		if (_pressedTab != null && CanCloseOrUndock(_pressedTab) && (Args.Button == MouseButtons.Middle || Args.Button == MouseButtons.Left && GetCloseBounds(TabPages.IndexOf(_pressedTab)).Contains(Args.Location))) {
			RequestClose(_pressedTab);
			_pressedTab = null;
			return;
		}
		base.OnMouseDown(Args);
		if (Args.Button == MouseButtons.Right && _pressedTab != null) {
			_contextTab = _pressedTab;
			_tabMenu.Show(this, Args.Location);
		}
		if (Args.Button == MouseButtons.Left && _pressedTab != null)
			Capture = true;
	}

	protected override void OnMouseMove(MouseEventArgs Args) {
		base.OnMouseMove(Args);
		if (Args.Button != MouseButtons.Left || _pressedTab == null || _pressedTab != SelectedTab)
			return;
		if (!_dragging) {
			var DragBounds = new Rectangle(_pressLocation - new Size(SystemInformation.DragSize.Width / 2, SystemInformation.DragSize.Height / 2), SystemInformation.DragSize);
			if (DragBounds.Contains(Args.Location))
				return;
			_dragging = true;
			Invalidate();
		}
		UpdateUndockPreview(Args.Location);
		if (!IsOverTabBar(Args.Location))
			return;
		var Target = GetTabAt(Args.Location);
		if (Target == null) {
			if (Args.X >= GetTabRect(TabCount - 1).Right)
				MoveTab(_pressedTab, TabCount - 1);
			return;
		}
		if (Target == _pressedTab)
			return;
		var Index = TabPages.IndexOf(Target);
		var Bounds = GetTabRect(Index);
		var MovingRight = Index > TabPages.IndexOf(_pressedTab);
		if (MovingRight ? Args.X >= Bounds.Left + Bounds.Width / 2 : Args.X <= Bounds.Left + Bounds.Width / 2)
			MoveTab(_pressedTab, Index);
	}

	protected override void OnMouseUp(MouseEventArgs Args) {
		var WasDragging = _dragging;
		var UndockingTab = Args.Button == MouseButtons.Left ? GetUndockingTab(Args.Location) : null;
		_pressedTab = null;
		_dragging = false;
		_undockPreviewVisible = false;
		Capture = false;
		if (WasDragging)
			Invalidate();
		base.OnMouseUp(Args);
		if (UndockingTab != null && TabPages.Contains(UndockingTab))
			RequestUndock(UndockingTab);
	}

	protected override void OnMouseCaptureChanged(EventArgs Args) {
		if (!Capture && !_completingDrag)
			CancelDrag();
		base.OnMouseCaptureChanged(Args);
	}

	protected override void OnVisibleChanged(EventArgs args) {
		base.OnVisibleChanged(args);
		if (!Visible)
			CancelDrag();
	}

	protected override void OnEnabledChanged(EventArgs args) {
		base.OnEnabledChanged(args);
		if (!Enabled)
			CancelDrag();
	}

	protected override void OnSelectedIndexChanged(EventArgs args) {
		base.OnSelectedIndexChanged(args);
		if (_dragging && !Reordering && _pressedTab != SelectedTab)
			CancelDrag();
	}

	protected override bool ProcessCmdKey(ref Message Message, Keys KeyData) {
		if (KeyData == Keys.Escape && _dragging && _pressedTab != null) {
			CancelDrag();
			return true;
		}
		return base.ProcessCmdKey(ref Message, KeyData);
	}

	protected override void WndProc(ref Message message) {
		if (_deferringInputRendering && message.Msg == WinAPI.USER32.WM_PAINT) {
			// Native selection can request synchronous painting even with WM_SETREDRAW disabled.
			// Validate this intermediate frame; the input rendering scope invalidates the final selection afterward.
			WinAPI.USER32.ValidateRect(new HandleRef(this, Handle), IntPtr.Zero);
			message.Result = IntPtr.Zero;
			return;
		}
		if (_deferringInputRendering && message.Msg == WinAPI.USER32.WM_ERASEBKGND) {
			message.Result = (IntPtr)1;
			return;
		}
		if (message.Msg == WinAPI.USER32.WM_LBUTTONDOWN) {
			// The native tab control can paint while committing its selection after managed Selecting handlers return.
			// Keep its redraw suppressed until that input message has completed, then queue the final paint.
			var wasDeferring = _deferringInputRendering;
			_deferringInputRendering = true;
			using var input = Tools.Scope.ExecuteOnDispose(() => _deferringInputRendering = wasDeferring);
			using var rendering = this.EnterRenderingScope(repaintImmediately: false);
			base.WndProc(ref message);
			return;
		}
		if (message.Msg != WinAPI.USER32.WM_LBUTTONUP) {
			base.WndProc(ref message);
			if (message.Msg == WinAPI.COMCTL32.TCM_SETITEMW && !_updatingCaptions)
				ApplyTabCaption((int)message.WParam);
			if (DockPreviewVisible && IsHandleCreated && !Disposing && !IsDisposed) {
				if (message.Msg == WinAPI.USER32.WM_PAINT && _dockPreviewBounds.Width > 0 && _dockPreviewBounds.Height > 0) {
					using var surface = Graphics.FromHwnd(Handle);
					using var buffer = BufferedGraphicsManager.Current.Allocate(surface, Rectangle.Union(_dockMarkerBounds, _dockPreviewBounds));
					DrawDockPreview(buffer.Graphics);
					buffer.Render(surface);
				} else if ((message.Msg == WinAPI.USER32.WM_PRINT || message.Msg == WinAPI.USER32.WM_PRINTCLIENT) && message.WParam != IntPtr.Zero) {
					using var surface = Graphics.FromHdc(message.WParam);
					DrawDockPreview(surface);
				}
			}
			return;
		}
		// Native mouse-up handling can release capture before OnMouseUp commits the drag.
		var wasCompletingDrag = _completingDrag;
		_completingDrag = true;
		using var completion = Tools.Scope.ExecuteOnDispose(() => _completingDrag = wasCompletingDrag);
		base.WndProc(ref message);
	}

	protected override void OnSelecting(TabControlCancelEventArgs Args) {
		if (!Reordering)
			base.OnSelecting(Args);
	}

	protected override void Dispose(bool Disposing) {
		if (Disposing) {
			_pressedTab = null;
			_dragging = false;
			_undockPreviewVisible = false;
			Capture = false;
			HideDockPreview();
			components?.Dispose();
		}
		base.Dispose(Disposing);
	}

	private TabPage? GetTabAt(Point Location) {
		for (var Index = 0; Index < TabCount; Index++)
			if (GetTabRect(Index).Contains(Location))
				return TabPages[Index];
		return null;
	}

	private Rectangle GetCloseBounds(int Index) => GetCloseBounds(GetTabRect(Index));

	private Rectangle GetCloseBounds(Rectangle Bounds) {
		var CloseSize = ScaleMetric(LogicalCloseSize);
		return new Rectangle(Bounds.Right - CloseSize - ScaleMetric(LogicalTextInset), Bounds.Top + (Bounds.Height - CloseSize) / 2, CloseSize, CloseSize);
	}

	private bool IsOverTabBar(Point Location) => TabStripBounds.Contains(Location);

	private TabPage GetUndockingTab(Point location) => _dragging && _pressedTab == SelectedTab
		&& _pressedTab?.Tag is WinFormsApplicationScreen && CanCloseOrUndock(_pressedTab)
		&& TabPages.Contains(_pressedTab) && !IsOverTabBar(location) ? _pressedTab : null;

	private void UpdateUndockPreview(Point location) {
		var visible = GetUndockingTab(location) != null;
		if (_undockPreviewVisible == visible)
			return;
		_undockPreviewVisible = visible;
		if (_pressedTab != null && TabPages.Contains(_pressedTab))
			Invalidate(GetTabRect(TabPages.IndexOf(_pressedTab)));
	}

	private int GetInsertionIndex(Point Location) {
		var Index = 0;
		for (var TabIndex = 0; TabIndex < TabCount; TabIndex++) {
			var Bounds = GetTabRect(TabIndex);
			if (Location.X < Bounds.Left + Bounds.Width / 2)
				return Index;
			Index++;
		}
		return Index;
	}

	private Rectangle GetDockMarkerBounds(int Index) {
		var StripBounds = TabStripBounds;
		var MarkerWidth = ScaleMetric(LogicalDockMarkerWidth);
		var Position = TabCount == 0 ? ScaleMetric(LogicalBorderWidth) : Index == TabCount ? GetTabRect(TabCount - 1).Right : GetTabRect(Index).Left;
		Position = Math.Max(0, Math.Min(Position - MarkerWidth / 2, StripBounds.Right - MarkerWidth));
		return new Rectangle(Position, StripBounds.Top, MarkerWidth, StripBounds.Height);
	}

	private Rectangle GetDockPreviewBounds(string Title, Rectangle MarkerBounds) {
		var StripBounds = TabStripBounds;
		var CaptionWidth = TextRenderer.MeasureText($"Release to dock: {Title}", Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
		var MinimumWidth = ScaleMetric(LogicalDockPreviewMinimumWidth);
		var MaximumWidth = ScaleMetric(Math.Max(LogicalDockPreviewMinimumWidth, _maximumTabWidth));
		var Width = Math.Min(StripBounds.Width, Math.Clamp(CaptionWidth + 2 * ScaleMetric(LogicalTextInset), MinimumWidth, MaximumWidth));
		var Left = Math.Clamp(MarkerBounds.Left, StripBounds.Left, StripBounds.Right - Width);
		return new Rectangle(Left, StripBounds.Top, Width, StripBounds.Height);
	}

	protected virtual void DrawDockPreview(Graphics Surface) {
		if (_dockPreviewBounds.Width <= 0 || _dockPreviewBounds.Height <= 0)
			return;
		var Bounds = _dockPreviewBounds;
		var CornerSize = Math.Min(ScaleMetric(LogicalDockPreviewCornerSize), Math.Min(Bounds.Width, Bounds.Height) / 2);
		var Outline = new[] {
			new Point(Bounds.Left, Bounds.Bottom - 1),
			new Point(Bounds.Left, Bounds.Top + CornerSize),
			new Point(Bounds.Left + CornerSize, Bounds.Top),
			new Point(Bounds.Right - CornerSize - 1, Bounds.Top),
			new Point(Bounds.Right - 1, Bounds.Top + CornerSize),
			new Point(Bounds.Right - 1, Bounds.Bottom - 1)
		};
		using var Background = new SolidBrush(SystemColors.Control);
		using var Highlight = new SolidBrush(DockPreviewBackColor);
		using var Marker = new SolidBrush(ControlPaint.Dark(DockPreviewBackColor));
		using var Border = new Pen(ControlPaint.Dark(DockPreviewBackColor), Math.Max(1, _metricsDpi / (float)LogicalDpi));
		Surface.FillRectangle(Background, Rectangle.Union(_dockMarkerBounds, Bounds));
		Surface.FillPolygon(Highlight, Outline);
		Surface.DrawPolygon(Border, Outline);
		var TextBounds = Rectangle.Inflate(Bounds, -ScaleMetric(LogicalTextInset), 0);
		// The runtime buffer translates this header rectangle to its local origin; GDI text must preserve that translation like the GDI+ background does.
		TextRenderer.DrawText(Surface, $"Release to dock: {_dockPreviewTitle}", Font, TextBounds, DockPreviewForeColor,
			TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
			TextFormatFlags.PreserveGraphicsTranslateTransform | TextFormatFlags.PreserveGraphicsClipping);
		// Keep the exact insertion edge visible when the preview shifts left to fit the end of the strip.
		Surface.FillRectangle(Marker, _dockMarkerBounds);
	}

	private void InvalidatePreview(Rectangle Bounds) {
		if (!Bounds.IsEmpty && IsHandleCreated && !Disposing && !IsDisposed)
			Invalidate(Bounds, false);
	}

	private void ResetDockPreviewBackColor() => DockPreviewBackColor = _defaultDockPreviewColors.BackColor;

	private bool ShouldSerializeDockPreviewBackColor() => DockPreviewBackColor != _defaultDockPreviewColors.BackColor;

	private void ResetDockPreviewForeColor() => DockPreviewForeColor = _defaultDockPreviewColors.ForeColor;

	private bool ShouldSerializeDockPreviewForeColor() => DockPreviewForeColor != _defaultDockPreviewColors.ForeColor;

	private static (Color BackColor, Color ForeColor) GetDefaultDockPreviewColors() {
		using var Theme = ExplorerBarInfo.Default;
		return (Theme.TaskPane.GradientStartColor, ControlPaint.Dark(Theme.Header.NormalTitleColor));
	}

	private int ScaleMetric(int Value) => (int)Math.Round(Value * _metricsDpi / (double)LogicalDpi);

	private void ApplyMetrics() {
		UpdateTabPadding();
		RefreshTabCaptions();
	}

	private void UpdateTabPadding() {
		var reservedWidth = ScaleMetric(LogicalCloseSize) + 3 * ScaleMetric(LogicalTextInset);
		if (ImageList != null)
			reservedWidth += ScaleMetric(LogicalIconSize) + ScaleMetric(LogicalIconGap) - ImageList.ImageSize.Width;
		// Native tabs measure their caption and unscaled image list. Reserve the remaining owner-drawn icon, gap and close-button area separately.
		var padding = new Point(Math.Max(ScaleMetric(LogicalHorizontalPadding), (reservedWidth + 1) / 2), ScaleMetric(LogicalVerticalPadding));
		if (Padding != padding)
			Padding = padding;
	}

	private void RefreshTabCaptions() {
		if (!IsHandleCreated || Disposing || IsDisposed || _updatingCaptions)
			return;
		UpdateTabPadding();
		HideDockPreview();
		WinAPI.USER32.SendMessage(Handle, WinAPI.COMCTL32.TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)ScaleMetric(LogicalMinimumTabWidth));
		for (var index = 0; index < TabCount; index++)
			ApplyTabCaption(index);
		Invalidate(TabStripBounds, false);
	}

	private void ApplyTabCaption(int Index) {
		if (!IsHandleCreated || Disposing || IsDisposed || _updatingCaptions || Index < 0 || Index >= TabCount)
			return;
		_updatingCaptions = true;
		using var Update = Tools.Scope.ExecuteOnDispose(() => _updatingCaptions = false);
		var Page = TabPages[Index];
		var Title = Page.Text;
		// ImageKey set before attachment has no parent image list in WinForms' native item cache.
		// Rebind it after attachment so native width measurement reserves the icon that owner drawing already renders.
		if (!string.IsNullOrEmpty(Page.ImageKey))
			Page.ImageKey = Page.ImageKey;
		Page.ToolTipText = Title;
		SetNativeCaption(Index, Title);
		var MaximumWidth = ScaleMetric(_maximumTabWidth);
		if (GetTabRect(Index).Width <= MaximumWidth)
			return;

		// Native tabs size themselves from their caption. Shorten only that caption, preserving the managed title for painting and accessibility.
		var Characters = StringInfo.ParseCombiningCharacters(Title);
		var Minimum = 0;
		var Maximum = Characters.Length;
		while (Minimum < Maximum) {
			var Candidate = (Minimum + Maximum + 1) / 2;
			SetNativeCaption(Index, Ellipsize(Candidate));
			if (GetTabRect(Index).Width <= MaximumWidth)
				Minimum = Candidate;
			else
				Maximum = Candidate - 1;
		}
		SetNativeCaption(Index, Ellipsize(Minimum));
		if (GetTabRect(Index).Width > MaximumWidth)
			SetNativeCaption(Index, string.Empty);

		string Ellipsize(int CharacterCount) => Title[..(CharacterCount == Characters.Length ? Title.Length : Characters[CharacterCount])] + "\u2026";
	}

	private void SetNativeCaption(int index, string caption) {
		// Native captions interpret ampersands as mnemonics, while the owner-drawn title displays every ampersand literally.
		Tools.Windows.Win32.SetTabCaption(Handle, index, caption.Replace("&", "&&", StringComparison.Ordinal));
	}

	private ActionScope EnterReorderScope() {
		var WasReordering = Reordering;
		Reordering = true;
		return Tools.Scope.ExecuteOnDispose(() => Reordering = WasReordering);
	}

	private void CancelDrag() {
		var Page = _pressedTab;
		var WasDragging = _dragging;
		var RestoreOrder = _dragging && !Disposing && !IsDisposed && Page != null && !Page.IsDisposed && TabPages.Contains(Page);
		_pressedTab = null;
		_dragging = false;
		_undockPreviewVisible = false;
		if (RestoreOrder)
			MoveTab(Page!, Math.Min(_originalIndex, TabCount - 1));
		Capture = false;
		if (WasDragging)
			Invalidate();
	}

	private static bool CanCloseOrUndock(TabPage Page) => Page?.Tag is not WinFormsApplicationScreen screen || screen.ActivationMode != ScreenActivationMode.PermanentSingleton;

	private void RequestClose(TabPage? Page) {
		if (CanCloseOrUndock(Page) && Page?.Tag is WinFormsApplicationScreen Screen)
			ScreenCloseRequested?.Invoke(Screen);
	}

	private void RequestUndock(TabPage? Page) {
		if (CanCloseOrUndock(Page) && Page?.Tag is WinFormsApplicationScreen Screen)
			ScreenUndockRequested?.Invoke(Screen);
	}

}
