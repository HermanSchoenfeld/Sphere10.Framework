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
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Draws the desktop's display arrangement as selectable tiles, in the style of Windows display settings.</summary>
/// <remarks>The application supplies <see cref="Items"/> with captions and accent colors; the control owns scaling, hover and selection.</remarks>
public partial class MonitorLayoutControl : UserControlEx {
	/// <summary>Raised after the selected tile changes through the UI or <see cref="SelectedKey"/>.</summary>
	public event EventHandler SelectedItemChanged;

	private MonitorLayoutItem[] _items = Array.Empty<MonitorLayoutItem>();
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
			Invalidate();
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
	public int TilePadding { get; set; } = 12;

	/// <summary>Returns the tile under a client point, or null.</summary>
	public MonitorLayoutItem HitTest(Point clientPoint) {
		foreach (var (item, tile) in LayoutTiles())
			if (tile.Contains(clientPoint))
				return item;
		return null;
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
		foreach (var (item, tile) in LayoutTiles())
			PaintTile(graphics, item, tile);
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

	// Scale the union of all display bounds into the padded client area, preserving aspect ratio and relative placement.
	private (MonitorLayoutItem Item, Rectangle Tile)[] LayoutTiles() {
		if (_items.Length == 0)
			return Array.Empty<(MonitorLayoutItem, Rectangle)>();
		var union = _items[0].Bounds;
		foreach (var item in _items.Skip(1))
			union = Rectangle.Union(union, item.Bounds);
		var padding = LogicalToDeviceUnits(TilePadding);
		var gap = LogicalToDeviceUnits(4);
		var available = new Rectangle(padding, padding, Math.Max(1, ClientSize.Width - 2 * padding), Math.Max(1, ClientSize.Height - 2 * padding));
		if (union.Width <= 0 || union.Height <= 0)
			return _items.Select(item => (item, available)).ToArray();
		var scale = Math.Min((float)available.Width / union.Width, (float)available.Height / union.Height);
		var offsetX = available.Left + (available.Width - union.Width * scale) / 2f;
		var offsetY = available.Top + (available.Height - union.Height * scale) / 2f;
		return _items.Select(item => {
			var left = (int)Math.Round(offsetX + (item.Bounds.Left - union.Left) * scale);
			var top = (int)Math.Round(offsetY + (item.Bounds.Top - union.Top) * scale);
			var right = (int)Math.Round(offsetX + (item.Bounds.Right - union.Left) * scale);
			var bottom = (int)Math.Round(offsetY + (item.Bounds.Bottom - union.Top) * scale);
			var tile = Rectangle.FromLTRB(left + gap / 2, top + gap / 2, right - gap / 2, bottom - gap / 2);
			return (item, tile.Width > 0 && tile.Height > 0 ? tile : Rectangle.FromLTRB(left, top, right, bottom));
		}).ToArray();
	}

	private void PaintTile(Graphics graphics, MonitorLayoutItem item, Rectangle tile) {
		var selected = item.Key == _selectedKey;
		var hovered = item.Key == _hoveredKey;
		var accent = item.Enabled ? item.Accent : Color.FromArgb(120, 120, 128);
		var fillStart = Lighten(accent, hovered ? 0.35f : 0.2f);
		var fillEnd = hovered ? Lighten(accent, 0.1f) : accent;
		var radius = LogicalToDeviceUnits(8);
		using var path = CreateRoundedRectangle(tile, radius);
		using (var fill = new LinearGradientBrush(tile, fillStart, fillEnd, LinearGradientMode.Vertical))
			graphics.FillPath(fill, path);
		// A thin bezel suggests the physical display and the selection ring follows the system highlight color.
		using (var bezel = new Pen(Color.FromArgb(90, Color.Black), LogicalToDeviceUnits(1)))
			graphics.DrawPath(bezel, path);
		if (selected) {
			var ring = Rectangle.Inflate(tile, -LogicalToDeviceUnits(2), -LogicalToDeviceUnits(2));
			using var ringPath = CreateRoundedRectangle(ring, Math.Max(1, radius - LogicalToDeviceUnits(2)));
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

	private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius) {
		var path = new GraphicsPath();
		var diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
		var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
		path.AddArc(arc, 180, 90);
		arc.X = bounds.Right - diameter;
		path.AddArc(arc, 270, 90);
		arc.Y = bounds.Bottom - diameter;
		path.AddArc(arc, 0, 90);
		arc.X = bounds.Left;
		path.AddArc(arc, 90, 90);
		path.CloseFigure();
		return path;
	}

	private static Color Lighten(Color color, float amount) {
		return Color.FromArgb(color.A, (int)(color.R + (255 - color.R) * amount), (int)(color.G + (255 - color.G) * amount), (int)(color.B + (255 - color.B) * amount));
	}
}
