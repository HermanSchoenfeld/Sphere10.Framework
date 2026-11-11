// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Draws the desktop's display arrangement as selectable tiles, in the style of Windows display settings.</summary>
/// <remarks>
/// The application supplies <see cref="Items"/> with captions and accent colors; the control owns scaling, hover and selection. It can also host
/// controls beneath the tiles (<see cref="HostedControlMode"/>): one per tile through <see cref="SetTileControl"/>, or <see cref="SharedControl"/>
/// beneath the whole arrangement. Hosted controls are children whose bounds, visibility and tab order the layout owns.
/// </remarks>
public partial class MonitorLayoutControl : UserControlEx {
	private readonly record struct TileLayout(MonitorLayoutItem Item, Rectangle Bounds);

	/// <summary>Raised after the selected tile changes through the UI or <see cref="SelectedKey"/>.</summary>
	public event EventHandler SelectedItemChanged;

	private const int LogicalTileGap = 4;
	private const int LogicalTileControlInset = 4;
	private const int LogicalMinimumSharedControlWidth = 120;
	private static readonly PropertyDescriptor _visibleProperty = TypeDescriptor.GetProperties(typeof(Control))[nameof(Visible)];
	private readonly Dictionary<string, Control> _tileControls = new(StringComparer.Ordinal);
	private MonitorLayoutItem[] _items = Array.Empty<MonitorLayoutItem>();
	private TileLayout[] _tileLayouts;
	private string _selectedKey;
	private string _hoveredKey;

	public MonitorLayoutControl() {
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
	}

	/// <summary>The displayed tiles; the array is copied on assignment.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public MonitorLayoutItem[] Items {
		get => (MonitorLayoutItem[])_items.Clone();
		set {
			_items = value == null ? Array.Empty<MonitorLayoutItem>() : (MonitorLayoutItem[])value.Clone();
			if (_selectedKey != null && _items.All(item => item.Key != _selectedKey))
				_selectedKey = null;
			InvalidateTileLayout();
		}
	}

	/// <summary>The key of the selected tile, or null when nothing is selected.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string SelectedKey {
		get => _selectedKey;
		set {
			if (_selectedKey == value)
				return;
			_selectedKey = value;
			Invalidate();
			SelectedItemChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public MonitorLayoutItem SelectedItem => _items.FirstOrDefault(item => item.Key == _selectedKey);

	/// <summary>Whether clicking a tile selects it.</summary>
	[Category("Behavior")]
	[DefaultValue(true)]
	public bool AllowSelection { get; set; } = true;

	/// <summary>Space kept between the tiles and the control edge, in logical pixels.</summary>
	[Category("Layout")]
	[DefaultValue(12)]
	public int TilePadding {
		get => field;
		set {
			if (field == value)
				return;
			field = value;
			InvalidateTileLayout();
		}
	} = 12;

	/// <summary>Where hosted controls are shown: none, beneath each tile, or one shared control beneath the arrangement.</summary>
	[Category("Layout")]
	[DefaultValue(MonitorLayoutHostedControlMode.None)]
	public MonitorLayoutHostedControlMode HostedControlMode {
		get => field;
		set {
			if (field == value)
				return;
			field = value;
			InvalidateTileLayout();
		}
	}

	/// <summary>Space between a tile and the control hosted beneath it in <see cref="MonitorLayoutHostedControlMode.PerTile"/> mode, in logical pixels.</summary>
	[Category("Layout")]
	[DefaultValue(4)]
	public int HostedControlSpacing {
		get => field;
		set {
			Guard.ArgumentGTE(value, 0, nameof(value));
			if (field == value)
				return;
			field = value;
			InvalidateTileLayout();
		}
	} = 4;

	/// <summary>
	/// Space between the arrangement and <see cref="SharedControl"/> in <see cref="MonitorLayoutHostedControlMode.Shared"/> mode, in logical pixels;
	/// wider than <see cref="HostedControlSpacing"/> by default because the control belongs to the whole arrangement rather than the tile above it.
	/// </summary>
	[Category("Layout")]
	[DefaultValue(12)]
	public int SharedControlSpacing {
		get => field;
		set {
			Guard.ArgumentGTE(value, 0, nameof(value));
			if (field == value)
				return;
			field = value;
			InvalidateTileLayout();
		}
	} = 12;

	/// <summary>The control centred beneath the whole arrangement in <see cref="MonitorLayoutHostedControlMode.Shared"/> mode.</summary>
	/// <remarks>It becomes a child, disposed with the layout unless replaced first; a replaced control is un-parented, not disposed.</remarks>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Control SharedControl {
		get => field;
		set {
			if (ReferenceEquals(field, value))
				return;
			Guard.Argument(value == null || !_tileControls.ContainsValue(value), nameof(value), "The control is already hosted beneath a tile.");
			var previous = field;
			field = value;
			if (previous != null && ReferenceEquals(previous.Parent, this))
				Controls.Remove(previous);
			if (value != null && !ReferenceEquals(value.Parent, this))
				Controls.Add(value);
			InvalidateTileLayout();
		}
	}

	/// <summary>Returns the tile under a client point, or null.</summary>
	public MonitorLayoutItem HitTest(Point clientPoint) {
		foreach (var tile in GetTileLayouts())
			if (tile.Bounds.Contains(clientPoint))
				return tile.Item;
		return null;
	}

	/// <summary>Returns the client bounds of the tile with <paramref name="key"/>, or <see cref="Rectangle.Empty"/> when no tile has it.</summary>
	public Rectangle GetTileBounds(string key) {
		foreach (var tile in GetTileLayouts())
			if (tile.Item.Key == key)
				return tile.Bounds;
		return Rectangle.Empty;
	}

	/// <summary>Hosts <paramref name="control"/> beneath the tile with <paramref name="key"/> in <see cref="MonitorLayoutHostedControlMode.PerTile"/> mode.</summary>
	/// <remarks>
	/// The control becomes a child, shown only while a tile has the key, and is disposed with the layout unless removed first. Null removes and
	/// un-parents the current control without disposing it.
	/// </remarks>
	public void SetTileControl(string key, Control control) {
		Guard.ArgumentNotNull(key, nameof(key));
		_tileControls.TryGetValue(key, out var current);
		if (ReferenceEquals(current, control))
			return;
		Guard.Argument(control == null || !IsHostedControl(control), nameof(control), "The control is already hosted by this layout.");
		if (current != null) {
			_tileControls.Remove(key);
			if (ReferenceEquals(current.Parent, this))
				Controls.Remove(current);
		}
		if (control != null) {
			_tileControls.Add(key, control);
			if (!ReferenceEquals(control.Parent, this))
				Controls.Add(control);
		}
		InvalidateTileLayout();
	}

	/// <summary>Returns the control hosted beneath the tile with <paramref name="key"/>, or null.</summary>
	public Control GetTileControl(string key) => key != null && _tileControls.TryGetValue(key, out var control) ? control : null;

	/// <summary>
	/// The height that shows the arrangement at the full proposed width (or the current width) with no empty band above or below it,
	/// including the hosted controls. It reserves the larger band of the two hosted modes, so switching between them keeps the size.
	/// </summary>
	public override Size GetPreferredSize(Size proposedSize) {
		var width = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue ? proposedSize.Width : ClientSize.Width;
		if (_items.Length == 0 || width <= 0)
			return base.GetPreferredSize(proposedSize);
		var union = _items.Select(item => item.Bounds).Aggregate(Rectangle.Union);
		if (union.Width <= 0 || union.Height <= 0)
			return base.GetPreferredSize(proposedSize);
		var padding = LogicalToDeviceUnits(TilePadding);
		var scale = (float)Math.Max(1, width - 2 * padding) / union.Width;
		return new Size(width, 2 * padding + (int)Math.Ceiling(union.Height * scale) + GetLayoutReservedHeight());
	}

	/// <summary>Computes the tile rectangles once per layout and positions the hosted controls beneath them.</summary>
	protected override void OnLayout(LayoutEventArgs e) {
		base.OnLayout(e);
		var previous = _tileLayouts;
		_tileLayouts = CalculateTileLayouts();
		PositionHostedControls(_tileLayouts);
		if (previous == null || !previous.SequenceEqual(_tileLayouts))
			Invalidate();
	}

	/// <summary>Forgets the tile rectangles of the previous size; the layout a resize performs, or the next paint, computes them again.</summary>
	protected override void OnResize(EventArgs e) {
		_tileLayouts = null;
		base.OnResize(e);
	}

	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);
		var graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
		if (_items.Length == 0) {
			TextRenderer.DrawText(graphics, "No displays", Font, ClientRectangle, SystemColors.GrayText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			return;
		}
		foreach (var tile in GetTileLayouts())
			PaintTile(graphics, tile.Item, tile.Bounds);
	}

	protected override void OnMouseMove(MouseEventArgs e) {
		base.OnMouseMove(e);
		var hovered = HitTest(e.Location)?.Key;
		if (hovered == _hoveredKey)
			return;
		_hoveredKey = hovered;
		Cursor = hovered != null && AllowSelection ? Cursors.Hand : Cursors.Default;
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e) {
		base.OnMouseLeave(e);
		if (_hoveredKey == null)
			return;
		_hoveredKey = null;
		Cursor = Cursors.Default;
		Invalidate();
	}

	protected override void OnMouseClick(MouseEventArgs e) {
		base.OnMouseClick(e);
		if (!AllowSelection || e.Button != MouseButtons.Left)
			return;
		var item = HitTest(e.Location);
		if (item != null)
			SelectedKey = item.Key;
	}

	/// <summary>Forgets a hosted control that the application removed or disposed directly.</summary>
	protected override void OnControlRemoved(ControlEventArgs e) {
		base.OnControlRemoved(e);
		if (Disposing || IsDisposed)
			return;
		var keys = _tileControls.Where(pair => ReferenceEquals(pair.Value, e.Control)).Select(pair => pair.Key).ToArray();
		foreach (var key in keys)
			_tileControls.Remove(key);
		if (ReferenceEquals(SharedControl, e.Control))
			SharedControl = null;
		else if (keys.Length > 0)
			InvalidateTileLayout();
	}

	/// <summary>Lays the tiles out again for the new font, which hosted controls usually follow.</summary>
	protected override void OnFontChanged(EventArgs e) {
		base.OnFontChanged(e);
		InvalidateTileLayout();
	}

	/// <summary>Lays the tiles out again at the new DPI.</summary>
	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		InvalidateTileLayout();
	}

	// Items, padding, hosted controls and their mode change the tile rectangles: recompute them in a new layout and repaint.
	private void InvalidateTileLayout() {
		_tileLayouts = null;
		PerformLayout();
		Invalidate();
	}

	// Paint and hit testing read the layout's tiles; a read between an invalidation and the next layout computes them.
	private TileLayout[] GetTileLayouts() => _tileLayouts ??= CalculateTileLayouts();

	// Scale the union of all display bounds into the padded client area, preserving aspect ratio and relative placement.
	// Shown hosted controls reserve a band beneath each row of tiles (per tile) or beneath the arrangement (shared). The scale and
	// position use the larger reservation of the two hosted modes, so switching between them leaves the tiles where they are.
	private TileLayout[] CalculateTileLayouts() {
		if (_items.Length == 0)
			return Array.Empty<TileLayout>();
		var union = _items[0].Bounds;
		foreach (var item in _items.Skip(1))
			union = Rectangle.Union(union, item.Bounds);
		var padding = LogicalToDeviceUnits(TilePadding);
		var gap = LogicalToDeviceUnits(LogicalTileGap);
		var available = new Rectangle(padding, padding, Math.Max(1, ClientSize.Width - 2 * padding), Math.Max(1, ClientSize.Height - 2 * padding));
		if (union.Width <= 0 || union.Height <= 0)
			return _items.Select(item => new TileLayout(item, available)).ToArray();

		// A tile moves down by one band for each row of tiles entirely above it, so stacked displays never cover a control.
		var band = GetHostedControlBand();
		var depths = band > 0 && HostedControlMode == MonitorLayoutHostedControlMode.PerTile ? CalculateTileDepths() : new int[_items.Length];
		var reserved = GetLayoutReservedHeight();
		var scale = Math.Min((float)available.Width / union.Width, (float)Math.Max(1, available.Height - reserved) / union.Height);
		var offsetX = available.Left + (available.Width - union.Width * scale) / 2f;
		var offsetY = available.Top + (available.Height - (union.Height * scale + reserved)) / 2f;
		if (reserved > 0)
			offsetY = Math.Max(available.Top, offsetY);
		return _items.Select((item, index) => {
			var shift = depths[index] * band;
			var left = (int)Math.Round(offsetX + (item.Bounds.Left - union.Left) * scale);
			var top = (int)Math.Round(offsetY + (item.Bounds.Top - union.Top) * scale) + shift;
			var right = (int)Math.Round(offsetX + (item.Bounds.Right - union.Left) * scale);
			var bottom = (int)Math.Round(offsetY + (item.Bounds.Bottom - union.Top) * scale) + shift;
			var tile = Rectangle.FromLTRB(left + gap / 2, top + gap / 2, right - gap / 2, bottom - gap / 2);
			return new TileLayout(item, tile.Width > 0 && tile.Height > 0 ? tile : Rectangle.FromLTRB(left, top, right, bottom));
		}).ToArray();
	}

	// The band holds the tallest hosted control the layout will show plus the spacing above it; nothing shown reserves nothing.
	private int GetHostedControlBand() => GetHostedControlBand(HostedControlMode);

	private int GetHostedControlBand(MonitorLayoutHostedControlMode mode) {
		var (tallest, spacing) = mode switch {
			MonitorLayoutHostedControlMode.PerTile => (_items.Select(item => GetTileControl(item.Key)?.Height ?? 0).DefaultIfEmpty(0).Max(), HostedControlSpacing),
			MonitorLayoutHostedControlMode.Shared => (SharedControl?.Height ?? 0, SharedControlSpacing),
			_ => (0, 0)
		};
		return tallest > 0 ? tallest + LogicalToDeviceUnits(spacing) : 0;
	}

	// The space the layout keeps beneath the tiles while controls are hosted: the larger of the two modes' reservations, so the tiles
	// keep their scale and position, and the window its size, when the mode changes.
	private int GetLayoutReservedHeight() {
		if (HostedControlMode == MonitorLayoutHostedControlMode.None)
			return 0;
		return Math.Max(GetReservedHeight(MonitorLayoutHostedControlMode.PerTile), GetReservedHeight(MonitorLayoutHostedControlMode.Shared));
	}

	// The space hosted controls reserve beneath the tiles: one band per row of stacked tiles per tile, or one band beneath the arrangement.
	private int GetReservedHeight(MonitorLayoutHostedControlMode mode) {
		var band = GetHostedControlBand(mode);
		if (band == 0)
			return 0;
		return mode == MonitorLayoutHostedControlMode.PerTile ? (CalculateTileDepths().DefaultIfEmpty(0).Max() + 1) * band : band;
	}

	// A tile's depth is one more than the depth of the deepest tile entirely above it, in desktop coordinates.
	private int[] CalculateTileDepths() {
		var depths = new int[_items.Length];
		var order = Enumerable.Range(0, _items.Length).OrderBy(index => _items[index].Bounds.Top).ThenBy(index => _items[index].Bounds.Bottom).ToArray();
		for (var position = 0; position < order.Length; position++) {
			var tile = order[position];
			for (var earlier = 0; earlier < position; earlier++) {
				var above = order[earlier];
				if (_items[above].Bounds.Bottom <= _items[tile].Bounds.Top)
					depths[tile] = Math.Max(depths[tile], depths[above] + 1);
			}
		}
		return depths;
	}

	// The layout owns the hosted controls' bounds, visibility and tab order, and writes only the values that changed.
	private void PositionHostedControls(TileLayout[] tiles) {
		if (_tileControls.Count == 0 && SharedControl == null)
			return;
		SuspendLayout();
		using var resumeLayout = Tools.Scope.ExecuteOnDispose(static layout => layout.ResumeLayout(false), this);
		var spacing = LogicalToDeviceUnits(HostedControlSpacing);
		var sharedSpacing = LogicalToDeviceUnits(SharedControlSpacing);
		var inset = LogicalToDeviceUnits(LogicalTileControlInset);

		// Tile controls show only in PerTile mode, centred beneath a tile with their key, in item order.
		var shown = new HashSet<Control>();
		if (HostedControlMode == MonitorLayoutHostedControlMode.PerTile)
			for (var index = 0; index < tiles.Length; index++) {
				var control = GetTileControl(tiles[index].Item.Key);
				if (control == null || !shown.Add(control))
					continue;
				var tile = tiles[index].Bounds;
				var width = FitWidth(control, tile.Width - 2 * inset);
				PlaceHostedControl(control, new Rectangle(tile.Left + (tile.Width - width) / 2, tile.Bottom + spacing, width, control.Height), index);
			}
		foreach (var control in _tileControls.Values)
			if (!shown.Contains(control))
				SetHostedControlVisible(control, false);

		// The shared control shows only in Shared mode, centred beneath the arrangement and kept within the client area.
		var shared = SharedControl;
		if (shared == null)
			return;
		if (HostedControlMode != MonitorLayoutHostedControlMode.Shared || tiles.Length == 0) {
			SetHostedControlVisible(shared, false);
			return;
		}
		var arrangement = tiles.Select(tile => tile.Bounds).Aggregate(Rectangle.Union);
		var sharedWidth = FitWidth(shared, Math.Min(ClientSize.Width, Math.Max(arrangement.Width - 2 * inset, LogicalToDeviceUnits(LogicalMinimumSharedControlWidth))));
		var left = Math.Max(0, Math.Min(arrangement.Left + (arrangement.Width - sharedWidth) / 2, ClientSize.Width - sharedWidth));
		PlaceHostedControl(shared, new Rectangle(left, arrangement.Bottom + sharedSpacing, sharedWidth, shared.Height), tiles.Length);
	}

	private bool IsHostedControl(Control control) => ReferenceEquals(control, SharedControl) || _tileControls.ContainsValue(control);

	// A hosted control never becomes narrower than its minimum width or wider than its maximum.
	private static int FitWidth(Control control, int width) {
		if (control.MaximumSize.Width > 0)
			width = Math.Min(width, control.MaximumSize.Width);
		return Math.Max(Math.Max(1, width), control.MinimumSize.Width);
	}

	private static void PlaceHostedControl(Control control, Rectangle bounds, int tabIndex) {
		if (control.Bounds != bounds)
			control.Bounds = bounds;
		if (control.TabIndex != tabIndex)
			control.TabIndex = tabIndex;
		SetHostedControlVisible(control, true);
	}

	// The control's own requested visibility is compared: its effective Visible also reflects hidden ancestors.
	private static void SetHostedControlVisible(Control control, bool visible) {
		var requested = !_visibleProperty.ShouldSerializeValue(control);
		if (requested != visible)
			control.Visible = visible;
	}

	private void PaintTile(Graphics graphics, MonitorLayoutItem item, Rectangle tile) {
		var selected = item.Key == _selectedKey;
		var hovered = item.Key == _hoveredKey;
		var accent = item.Enabled ? item.Accent : Color.FromArgb(120, 120, 128);
		var fillStart = Lighten(accent, hovered ? 0.35f : 0.2f);
		var fillEnd = hovered ? Lighten(accent, 0.1f) : accent;
		var radius = LogicalToDeviceUnits(8);
		using var path = Tools.Drawing.CreateRoundedRectanglePath(tile, radius);
		using (var fill = new LinearGradientBrush(tile, fillStart, fillEnd, LinearGradientMode.Vertical))
			graphics.FillPath(fill, path);
		// A thin bezel suggests the physical display and the selection ring follows the system highlight color.
		using (var bezel = new Pen(Color.FromArgb(90, Color.Black), LogicalToDeviceUnits(1)))
			graphics.DrawPath(bezel, path);
		if (selected) {
			var ring = Rectangle.Inflate(tile, -LogicalToDeviceUnits(2), -LogicalToDeviceUnits(2));
			using var ringPath = Tools.Drawing.CreateRoundedRectanglePath(ring, Math.Max(1, radius - LogicalToDeviceUnits(2)));
			using var ringPen = new Pen(SystemColors.Highlight, LogicalToDeviceUnits(3));
			graphics.DrawPath(ringPen, ringPath);
		}
		var textColor = item.Enabled ? Color.White : Color.FromArgb(235, 235, 235);
		var labelHeight = Math.Max(10, Math.Min(tile.Height / 2, LogicalToDeviceUnits(56)));
		using (var labelFont = new Font(Font.FontFamily, labelHeight * 0.6f, FontStyle.Bold, GraphicsUnit.Pixel)) {
			var labelBounds = new Rectangle(tile.Left, tile.Top, tile.Width, tile.Height - LogicalToDeviceUnits(18));
			TextRenderer.DrawText(graphics, item.Label, labelFont, labelBounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		var captionBounds = new Rectangle(tile.Left + LogicalToDeviceUnits(4), tile.Bottom - LogicalToDeviceUnits(22), tile.Width - LogicalToDeviceUnits(8), LogicalToDeviceUnits(18));
		var caption = item.IsPrimary && !string.IsNullOrEmpty(item.Caption) ? item.Caption + " ★" : item.IsPrimary ? "★" : item.Caption;
		TextRenderer.DrawText(graphics, caption ?? string.Empty, Font, captionBounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
	}


	private static Color Lighten(Color color, float amount)
		=> Color.FromArgb(color.A, (int)(color.R + (255 - color.R) * amount), (int)(color.G + (255 - color.G) * amount), (int)(color.B + (255 - color.B) * amount));
}
