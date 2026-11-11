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
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Paints a <see cref="HexEditorViewport"/> as an offset column, grouped hexadecimal bytes and decoded text, and
/// translates mouse and keyboard input into viewport operations. It owns no document state: bind a viewport with
/// <see cref="Viewport"/>. Rendering is double-buffered, DPI-aware and uses ordinary WinForms colors.
/// </summary>
public class HexEditorView : Control {
	private const TextFormatFlags TextFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping;
	private const int GroupSize = 8;
	private readonly VScrollBar _scrollBar;
	private readonly Timer _blinkTimer;
	private readonly Font _ownedFont;
	private HexEditorViewport _viewport;
	private byte[] _rowBuffer;
	private bool _fontMetricsValid;
	private int _charWidth;
	private int _rowHeight;
	private bool _syncingScrollBar;
	private bool _dragging;
	private bool _caretVisible;
	private bool _showHeader;
	private Color _offsetForeColor;
	private Color _modifiedForeColor;
	private Color _selectionBackColor;
	private Color _selectionForeColor;
	private Color _inactiveSelectionBackColor;
	private Color _separatorColor;
	private Color _nonPrintableForeColor;
	private Color _headerBackColor;

	public HexEditorView() {
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
		SetStyle(ControlStyles.SupportsTransparentBackColor, false);
		DoubleBuffered = true;
		TabStop = true;
		Cursor = Cursors.IBeam;
		BackColor = SystemColors.Window;
		ForeColor = SystemColors.WindowText;
		_offsetForeColor = SystemColors.GrayText;
		_modifiedForeColor = Color.OrangeRed;
		_selectionBackColor = SystemColors.Highlight;
		_selectionForeColor = SystemColors.HighlightText;
		_inactiveSelectionBackColor = SystemColors.ControlLight;
		_separatorColor = SystemColors.ControlDark;
		_nonPrintableForeColor = SystemColors.GrayText;
		_headerBackColor = SystemColors.Control;
		_showHeader = true;
		_rowBuffer = new byte[HexEditorViewport.MaxBytesPerRow];
		_ownedFont = CreateMonospaceFont();
		Font = _ownedFont;
		_scrollBar = new VScrollBar { Dock = DockStyle.Right, Enabled = false, TabStop = false };
		_scrollBar.Scroll += ScrollBarScrolled;
		Controls.Add(_scrollBar);
		var blinkTime = SystemInformation.CaretBlinkTime;
		_blinkTimer = new Timer { Interval = blinkTime > 0 ? blinkTime : 530 };
		_blinkTimer.Tick += BlinkTimerTicked;
	}

	/// <summary>The bound view model. The view repaints on its <see cref="HexEditorViewport.Changed"/> event and keeps its visible row count current.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorViewport Viewport {
		get => _viewport;
		set {
			if (ReferenceEquals(_viewport, value))
				return;
			if (_viewport != null)
				_viewport.Changed -= ViewportChanged;
			_viewport = value;
			_dragging = false;
			if (_viewport != null) {
				_viewport.Changed += ViewportChanged;
				UpdateVisibleRows();
			}
			UpdateScrollBar();
			Invalidate();
		}
	}

	/// <summary>Whether the column header row is drawn above the bytes.</summary>
	[Category("Appearance"), DefaultValue(true), Description("Whether the column header row is drawn above the bytes.")]
	public bool ShowHeader {
		get => _showHeader;
		set {
			if (_showHeader == value)
				return;
			_showHeader = value;
			UpdateVisibleRows();
			Invalidate();
		}
	}

	[Category("Appearance"), Description("The color of the offset column and header captions.")]
	public Color OffsetForeColor {
		get => _offsetForeColor;
		set { _offsetForeColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The color of bytes with pending, unsaved edits.")]
	public Color ModifiedForeColor {
		get => _modifiedForeColor;
		set { _modifiedForeColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The selection background in the active pane.")]
	public Color SelectionBackColor {
		get => _selectionBackColor;
		set { _selectionBackColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The selection text color in the active pane.")]
	public Color SelectionForeColor {
		get => _selectionForeColor;
		set { _selectionForeColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The selection background in the pane without the caret.")]
	public Color InactiveSelectionBackColor {
		get => _inactiveSelectionBackColor;
		set { _inactiveSelectionBackColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The color of the lines separating the offset, hex and text columns.")]
	public Color SeparatorColor {
		get => _separatorColor;
		set { _separatorColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The color of the '.' placeholder for non-printable bytes in the text pane.")]
	public Color NonPrintableForeColor {
		get => _nonPrintableForeColor;
		set { _nonPrintableForeColor = value; Invalidate(); }
	}

	[Category("Appearance"), Description("The background of the column header row.")]
	public Color HeaderBackColor {
		get => _headerBackColor;
		set { _headerBackColor = value; Invalidate(); }
	}

	/// <summary>The pixel height of one byte row at the current font and DPI.</summary>
	[Browsable(false)]
	public int RowHeight {
		get {
			EnsureFontMetrics();
			return _rowHeight;
		}
	}

	/// <summary>The pixel height of the header row, or zero when hidden.</summary>
	[Browsable(false)]
	public int HeaderHeight {
		get {
			EnsureFontMetrics();
			return _showHeader ? _rowHeight + LogicalToDeviceUnits(2) : 0;
		}
	}

	protected override Size DefaultSize => new Size(640, 320);

	/// <summary>Resolves the byte, pane and nibble under a client point, clamping rows and columns to the document. Returns null above the header.</summary>
	public HexEditorHitTestResult HitTest(Point location) {
		if (_viewport == null)
			return null;
		var layout = ComputeLayout();
		if (location.Y < layout.HeaderHeight)
			return null;
		var row = Math.Clamp(_viewport.FirstVisibleRow + (location.Y - layout.HeaderHeight) / _rowHeight, 0, _viewport.RowCount - 1);
		var rowStart = row * _viewport.BytesPerRow;
		var bytesPerRow = _viewport.BytesPerRow;
		if (location.X < layout.TextLeft - _charWidth) {
			// Anything left of the text pane resolves into the hex pane, including the offset column.
			var column = bytesPerRow;
			var nibble = 0;
			for (var index = 0; index < bytesPerRow; index++) {
				var cellLeft = layout.HexCellLeft(index);
				var cellRight = cellLeft + 3 * _charWidth + (index % GroupSize == GroupSize - 1 ? _charWidth : 0);
				if (location.X < cellRight) {
					column = index;
					nibble = location.X >= cellLeft + _charWidth ? 1 : 0;
					break;
				}
			}
			return new HexEditorHitTestResult(HexEditorPane.Hex, Math.Min(_viewport.Document.Length, rowStart + column), nibble);
		}
		var textColumn = Math.Clamp((location.X - layout.TextLeft) / _charWidth, 0, bytesPerRow);
		return new HexEditorHitTestResult(HexEditorPane.Text, Math.Min(_viewport.Document.Length, rowStart + textColumn), 0);
	}

	/// <summary>The client rectangle of a byte cell in a pane, or empty when the offset is not visible.</summary>
	public Rectangle GetCellBounds(long offset, HexEditorPane pane) {
		if (_viewport == null)
			return Rectangle.Empty;
		var layout = ComputeLayout();
		var row = _viewport.RowOf(offset);
		if (row < _viewport.FirstVisibleRow || row >= _viewport.FirstVisibleRow + _viewport.VisibleRows)
			return Rectangle.Empty;
		var column = (int)(offset - row * _viewport.BytesPerRow);
		var top = layout.HeaderHeight + (int)(row - _viewport.FirstVisibleRow) * _rowHeight;
		return pane == HexEditorPane.Hex
			? new Rectangle(layout.HexCellLeft(column), top, 2 * _charWidth, _rowHeight)
			: new Rectangle(layout.TextLeft + column * _charWidth, top, _charWidth, _rowHeight);
	}

	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);
		var graphics = e.Graphics;
		graphics.Clear(BackColor);
		if (_viewport == null)
			return;
		var layout = ComputeLayout();
		var document = _viewport.Document;
		var bytesPerRow = _viewport.BytesPerRow;
		using var separatorPen = new Pen(_separatorColor);
		if (layout.HeaderHeight > 0)
			PaintHeader(graphics, layout, separatorPen);
		// Only rows whose bytes need individual colors are painted per cell; plain rows are drawn as one string per pane.
		var hexLine = new StringBuilder(bytesPerRow * 4);
		var textLine = new char[bytesPerRow];
		for (var index = 0; index < _viewport.VisibleRows; index++) {
			var row = _viewport.FirstVisibleRow + index;
			if (row >= _viewport.RowCount)
				break;
			var top = layout.HeaderHeight + index * _rowHeight;
			if (top >= ClientSize.Height)
				break;
			var rowStart = row * bytesPerRow;
			var count = _viewport.ReadRow(row, _rowBuffer);
			TextRenderer.DrawText(graphics, _viewport.FormatOffset(rowStart), Font, new Point(layout.OffsetLeft, top + layout.TextTop), _offsetForeColor, TextFlags);
			var plain = !RowNeedsCellPainting(rowStart, count);
			if (plain) {
				hexLine.Clear();
				for (var column = 0; column < count; column++) {
					if (column > 0)
						hexLine.Append(column % GroupSize == 0 ? "  " : " ");
					hexLine.Append(_rowBuffer[column].ToString("X2", CultureInfo.InvariantCulture));
					textLine[column] = HexEditorViewport.ToDisplayCharacter(_rowBuffer[column]);
				}
				TextRenderer.DrawText(graphics, hexLine.ToString(), Font, new Point(layout.HexLeft, top + layout.TextTop), ForeColor, TextFlags);
				TextRenderer.DrawText(graphics, new string(textLine, 0, count), Font, new Point(layout.TextLeft, top + layout.TextTop), ForeColor, TextFlags);
			} else {
				for (var column = 0; column < count; column++)
					PaintCell(graphics, layout, rowStart + column, column, _rowBuffer[column], top);
			}
		}
		// Column separators span the whole client height so the layout reads as a table.
		graphics.DrawLine(separatorPen, layout.HexLeft - _charWidth, layout.HeaderHeight, layout.HexLeft - _charWidth, ClientSize.Height);
		graphics.DrawLine(separatorPen, layout.TextLeft - _charWidth, layout.HeaderHeight, layout.TextLeft - _charWidth, ClientSize.Height);
		PaintCaret(graphics, layout);
	}

	protected override bool IsInputKey(Keys keyData) {
		switch (keyData & Keys.KeyCode) {
			case Keys.Left:
			case Keys.Right:
			case Keys.Up:
			case Keys.Down:
			case Keys.Home:
			case Keys.End:
			case Keys.PageUp:
			case Keys.PageDown:
				return true;
			case Keys.Tab:
				return keyData == Keys.Tab;
			default:
				return base.IsInputKey(keyData);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e) {
		base.OnKeyDown(e);
		if (_viewport == null || e.Handled)
			return;
		var extend = e.Shift;
		switch (e.KeyCode) {
			case Keys.Left:
				_viewport.MoveCaret(-1, extend);
				break;
			case Keys.Right:
				_viewport.MoveCaret(1, extend);
				break;
			case Keys.Up:
				_viewport.MoveRows(-1, extend);
				break;
			case Keys.Down:
				_viewport.MoveRows(1, extend);
				break;
			case Keys.PageUp:
				_viewport.MovePages(-1, extend);
				break;
			case Keys.PageDown:
				_viewport.MovePages(1, extend);
				break;
			case Keys.Home:
				if (e.Control)
					_viewport.MoveToDocumentStart(extend);
				else
					_viewport.MoveToRowStart(extend);
				break;
			case Keys.End:
				if (e.Control)
					_viewport.MoveToDocumentEnd(extend);
				else
					_viewport.MoveToRowEnd(extend);
				break;
			case Keys.Tab when !e.Control && !e.Shift:
				_viewport.ActivePane = _viewport.ActivePane == HexEditorPane.Hex ? HexEditorPane.Text : HexEditorPane.Hex;
				break;
			case Keys.Back:
				// Overwrite mode keeps Backspace as a caret move; insert mode (or a selection) removes bytes.
				if (_viewport.InsertMode || _viewport.HasSelection)
					_viewport.DeleteBackward();
				else
					_viewport.MoveCaret(-1, false);
				break;
			case Keys.Delete when !e.Shift:
				_viewport.DeleteForward();
				break;
			case Keys.Insert when !e.Control && !e.Shift:
				_viewport.InsertMode = !_viewport.InsertMode;
				break;
			case Keys.Escape:
				_viewport.ClearSelection();
				break;
			case Keys.A when e.Control:
				_viewport.SelectAll();
				break;
			default:
				return;
		}
		e.Handled = true;
		e.SuppressKeyPress = true;
	}

	protected override void OnKeyPress(KeyPressEventArgs e) {
		base.OnKeyPress(e);
		if (_viewport == null || e.Handled || char.IsControl(e.KeyChar))
			return;
		// Hex digits type nibbles, text characters type whole bytes; the viewport's mode decides between insert and overwrite.
		if (_viewport.ActivePane == HexEditorPane.Hex)
			_viewport.TypeHexDigit(e.KeyChar);
		else
			_viewport.TypeCharacter(e.KeyChar);
		e.Handled = true;
	}

	protected override void OnMouseDown(MouseEventArgs e) {
		base.OnMouseDown(e);
		Focus();
		if (_viewport == null || e.Button != MouseButtons.Left)
			return;
		var hit = HitTest(e.Location);
		if (hit == null)
			return;
		_viewport.BeginPointerSelection(hit.Pane, hit.Offset, hit.Nibble, (ModifierKeys & Keys.Shift) == Keys.Shift);
		_dragging = true;
		Capture = true;
	}

	protected override void OnMouseMove(MouseEventArgs e) {
		base.OnMouseMove(e);
		if (!_dragging || _viewport == null)
			return;
		// Dragging past the top or bottom edge scrolls one row per move so selections can grow beyond the window.
		var layout = ComputeLayout();
		var location = e.Location;
		if (location.Y < layout.HeaderHeight) {
			_viewport.ScrollBy(-1);
			location.Y = layout.HeaderHeight;
		} else if (location.Y >= ClientSize.Height) {
			_viewport.ScrollBy(1);
			location.Y = ClientSize.Height - 1;
		}
		var hit = HitTest(location);
		if (hit != null)
			_viewport.ContinuePointerSelection(hit.Offset);
	}

	protected override void OnMouseUp(MouseEventArgs e) {
		base.OnMouseUp(e);
		if (!_dragging)
			return;
		_dragging = false;
		Capture = false;
		_viewport?.EndPointerSelection();
	}

	protected override void OnMouseWheel(MouseEventArgs e) {
		base.OnMouseWheel(e);
		if (_viewport == null)
			return;
		var lines = SystemInformation.MouseWheelScrollLines;
		var rows = lines < 0 ? _viewport.VisibleRows : lines;
		_viewport.ScrollBy(-(long)e.Delta / SystemInformation.MouseWheelScrollDelta * rows);
		if (e is HandledMouseEventArgs handled)
			handled.Handled = true;
	}

	protected override void OnGotFocus(EventArgs e) {
		base.OnGotFocus(e);
		RestartCaretBlink();
		Invalidate();
	}

	protected override void OnLostFocus(EventArgs e) {
		base.OnLostFocus(e);
		_blinkTimer.Stop();
		_caretVisible = false;
		Invalidate();
	}

	protected override void OnResize(EventArgs e) {
		base.OnResize(e);
		UpdateVisibleRows();
	}

	protected override void OnFontChanged(EventArgs e) {
		base.OnFontChanged(e);
		_fontMetricsValid = false;
		UpdateVisibleRows();
		Invalidate();
	}

	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		_fontMetricsValid = false;
		UpdateVisibleRows();
		Invalidate();
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_viewport != null)
				_viewport.Changed -= ViewportChanged;
			_blinkTimer.Dispose();
			_ownedFont.Dispose();
		}
		base.Dispose(disposing);
	}

	// Designer serialization: only colors differing from the system defaults are persisted.
	private bool ShouldSerializeOffsetForeColor() => _offsetForeColor != SystemColors.GrayText;

	private void ResetOffsetForeColor() => OffsetForeColor = SystemColors.GrayText;

	private bool ShouldSerializeModifiedForeColor() => _modifiedForeColor != Color.OrangeRed;

	private void ResetModifiedForeColor() => ModifiedForeColor = Color.OrangeRed;

	private bool ShouldSerializeSelectionBackColor() => _selectionBackColor != SystemColors.Highlight;

	private void ResetSelectionBackColor() => SelectionBackColor = SystemColors.Highlight;

	private bool ShouldSerializeSelectionForeColor() => _selectionForeColor != SystemColors.HighlightText;

	private void ResetSelectionForeColor() => SelectionForeColor = SystemColors.HighlightText;

	private bool ShouldSerializeInactiveSelectionBackColor() => _inactiveSelectionBackColor != SystemColors.ControlLight;

	private void ResetInactiveSelectionBackColor() => InactiveSelectionBackColor = SystemColors.ControlLight;

	private bool ShouldSerializeSeparatorColor() => _separatorColor != SystemColors.ControlDark;

	private void ResetSeparatorColor() => SeparatorColor = SystemColors.ControlDark;

	private bool ShouldSerializeNonPrintableForeColor() => _nonPrintableForeColor != SystemColors.GrayText;

	private void ResetNonPrintableForeColor() => NonPrintableForeColor = SystemColors.GrayText;

	private bool ShouldSerializeHeaderBackColor() => _headerBackColor != SystemColors.Control;

	private void ResetHeaderBackColor() => HeaderBackColor = SystemColors.Control;

	private void PaintHeader(Graphics graphics, ColumnLayout layout, Pen separatorPen) {
		using var headerBrush = new SolidBrush(_headerBackColor);
		graphics.FillRectangle(headerBrush, 0, 0, ClientSize.Width, layout.HeaderHeight);
		TextRenderer.DrawText(graphics, "Offset(h)", Font, new Point(layout.OffsetLeft, layout.TextTop), _offsetForeColor, TextFlags);
		for (var column = 0; column < _viewport.BytesPerRow; column++)
			TextRenderer.DrawText(graphics, column.ToString("X2", CultureInfo.InvariantCulture), Font, new Point(layout.HexCellLeft(column), layout.TextTop), _offsetForeColor, TextFlags);
		TextRenderer.DrawText(graphics, "Decoded text", Font, new Point(layout.TextLeft, layout.TextTop), _offsetForeColor, TextFlags);
		graphics.DrawLine(separatorPen, 0, layout.HeaderHeight - 1, ClientSize.Width, layout.HeaderHeight - 1);
	}

	// A row is painted cell by cell when any byte is selected or modified.
	private bool RowNeedsCellPainting(long rowStart, int count) {
		if (count == 0)
			return false;
		if (_viewport.HasSelection && rowStart < _viewport.SelectionEnd && rowStart + count > _viewport.SelectionStart)
			return true;
		if (!_viewport.Document.HasChanges)
			return false;
		for (var column = 0; column < count; column++)
			if (_viewport.Document.IsModified(rowStart + column))
				return true;
		return false;
	}

	private void PaintCell(Graphics graphics, ColumnLayout layout, long offset, int column, byte value, int top) {
		var selected = _viewport.IsSelected(offset);
		var modified = _viewport.Document.IsModified(offset);
		var hexLeft = layout.HexCellLeft(column);
		var textLeft = layout.TextLeft + column * _charWidth;
		if (selected) {
			// The gap after a selected byte is filled when the next byte is selected too, so runs read as one block.
			var continues = column + 1 < _viewport.BytesPerRow && _viewport.IsSelected(offset + 1);
			var hexWidth = continues ? layout.HexCellLeft(column + 1) - hexLeft : 2 * _charWidth;
			using var activeBrush = new SolidBrush(_selectionBackColor);
			using var inactiveBrush = new SolidBrush(_inactiveSelectionBackColor);
			var hexActive = _viewport.ActivePane == HexEditorPane.Hex;
			graphics.FillRectangle(hexActive ? activeBrush : inactiveBrush, hexLeft, top, hexWidth, _rowHeight);
			graphics.FillRectangle(hexActive ? inactiveBrush : activeBrush, textLeft, top, _charWidth, _rowHeight);
		}
		var hexColor = selected && _viewport.ActivePane == HexEditorPane.Hex ? _selectionForeColor : modified ? _modifiedForeColor : ForeColor;
		var character = HexEditorViewport.ToDisplayCharacter(value);
		var printable = character != '.' || value == (byte)'.';
		var textColor = selected && _viewport.ActivePane == HexEditorPane.Text ? _selectionForeColor : modified ? _modifiedForeColor : printable ? ForeColor : _nonPrintableForeColor;
		TextRenderer.DrawText(graphics, value.ToString("X2", CultureInfo.InvariantCulture), Font, new Point(hexLeft, top + layout.TextTop), hexColor, TextFlags);
		TextRenderer.DrawText(graphics, character.ToString(), Font, new Point(textLeft, top + layout.TextTop), textColor, TextFlags);
	}

	// The active pane shows a blinking bar at the caret nibble; the other pane frames the same byte.
	private void PaintCaret(Graphics graphics, ColumnLayout layout) {
		var row = _viewport.RowOf(_viewport.CaretOffset);
		if (row < _viewport.FirstVisibleRow || row >= _viewport.FirstVisibleRow + _viewport.VisibleRows)
			return;
		var column = (int)(_viewport.CaretOffset - row * _viewport.BytesPerRow);
		var top = layout.HeaderHeight + (int)(row - _viewport.FirstVisibleRow) * _rowHeight;
		var hexLeft = layout.HexCellLeft(column) + _viewport.CaretNibble * _charWidth;
		var textLeft = layout.TextLeft + column * _charWidth;
		var atEnd = _viewport.CaretOffset >= _viewport.Document.Length;
		var hexActive = _viewport.ActivePane == HexEditorPane.Hex;
		var barWidth = Math.Max(1, LogicalToDeviceUnits(2));
		using var framePen = new Pen(ForeColor);
		if (!atEnd && !_viewport.HasSelection) {
			var inactiveLeft = hexActive ? textLeft : layout.HexCellLeft(column);
			var inactiveWidth = hexActive ? _charWidth : 2 * _charWidth;
			graphics.DrawRectangle(framePen, inactiveLeft, top, inactiveWidth - 1, _rowHeight - 1);
		}
		if (!Focused || !_caretVisible)
			return;
		using var caretBrush = new SolidBrush(ForeColor);
		graphics.FillRectangle(caretBrush, hexActive ? hexLeft : textLeft, top, barWidth, _rowHeight);
	}

	private void EnsureFontMetrics() {
		if (_fontMetricsValid)
			return;
		using var graphics = CreateGraphics();
		var size = TextRenderer.MeasureText(graphics, new string('0', 64), Font, new Size(int.MaxValue, int.MaxValue), TextFlags);
		_charWidth = Math.Max(1, (int)Math.Round(size.Width / 64.0));
		_rowHeight = size.Height + LogicalToDeviceUnits(2);
		_fontMetricsValid = true;
	}

	private ColumnLayout ComputeLayout() {
		EnsureFontMetrics();
		var digits = _viewport?.EffectiveAddressDigits ?? HexEditorViewport.MinAddressDigits;
		var padding = LogicalToDeviceUnits(6);
		var offsetLeft = padding;
		var hexLeft = offsetLeft + digits * _charWidth + 2 * _charWidth;
		var bytesPerRow = _viewport?.BytesPerRow ?? HexEditorViewport.DefaultBytesPerRow;
		var groups = (bytesPerRow - 1) / GroupSize;
		var hexWidth = bytesPerRow * 3 * _charWidth - _charWidth + groups * _charWidth;
		var textLeft = hexLeft + hexWidth + 2 * _charWidth;
		return new ColumnLayout(HeaderHeight, offsetLeft, hexLeft, textLeft, _charWidth, LogicalToDeviceUnits(1));
	}

	private void UpdateVisibleRows() {
		if (_viewport == null)
			return;
		EnsureFontMetrics();
		var rows = Math.Max(1, (ClientSize.Height - HeaderHeight) / _rowHeight);
		if (_viewport.VisibleRows != rows)
			_viewport.VisibleRows = rows;
		else
			UpdateScrollBar();
	}

	// 64-bit row counts are mapped onto the 32-bit scrollbar range with a proportional scale.
	private void UpdateScrollBar() {
		if (_syncingScrollBar || _scrollBar.IsDisposed)
			return;
		_syncingScrollBar = true;
		try {
			if (_viewport == null) {
				_scrollBar.Enabled = false;
				_scrollBar.Maximum = 0;
				_scrollBar.Value = 0;
				return;
			}
			var scale = ScrollScale();
			var maxValue = (int)(_viewport.MaxFirstVisibleRow / scale);
			var largeChange = Math.Max(1, (int)Math.Min(int.MaxValue, _viewport.VisibleRows / scale));
			_scrollBar.Minimum = 0;
			_scrollBar.SmallChange = 1;
			_scrollBar.LargeChange = largeChange;
			_scrollBar.Maximum = Math.Max(0, maxValue + largeChange - 1);
			_scrollBar.Value = Math.Clamp((int)(_viewport.FirstVisibleRow / scale), 0, maxValue);
			_scrollBar.Enabled = maxValue > 0;
		} finally {
			_syncingScrollBar = false;
		}
	}

	private long ScrollScale() {
		var limit = int.MaxValue / 2L;
		return Math.Max(1, (_viewport.RowCount + limit - 1) / limit);
	}

	private void ScrollBarScrolled(object sender, ScrollEventArgs e) {
		if (_syncingScrollBar || _viewport == null)
			return;
		_viewport.FirstVisibleRow = Math.Min(_viewport.MaxFirstVisibleRow, e.NewValue * ScrollScale());
	}

	private void ViewportChanged() {
		if (IsDisposed || Disposing)
			return;
		UpdateScrollBar();
		if (Focused)
			RestartCaretBlink();
		Invalidate();
	}

	private void RestartCaretBlink() {
		_caretVisible = true;
		_blinkTimer.Stop();
		_blinkTimer.Start();
	}

	private void BlinkTimerTicked(object sender, EventArgs e) {
		if (_viewport == null || !Focused)
			return;
		_caretVisible = !_caretVisible;
		var hexBounds = GetCellBounds(_viewport.CaretOffset, HexEditorPane.Hex);
		var textBounds = GetCellBounds(_viewport.CaretOffset, HexEditorPane.Text);
		if (hexBounds.IsEmpty)
			return;
		Invalidate(Rectangle.Inflate(hexBounds, _charWidth, 0));
		Invalidate(Rectangle.Inflate(textBounds, _charWidth, 0));
	}

	// Consolas is preferred; GDI+ silently substitutes a proportional face when it is missing, so verify the name.
	private static Font CreateMonospaceFont() {
		var consolas = new Font("Consolas", 10F);
		if (string.Equals(consolas.Name, "Consolas", StringComparison.OrdinalIgnoreCase))
			return consolas;
		consolas.Dispose();
		return new Font(FontFamily.GenericMonospace, 10F);
	}

	#region Inner Types

	/// <summary>The horizontal column positions for the current font, address width and bytes per row.</summary>
	private readonly struct ColumnLayout {
		public ColumnLayout(int headerHeight, int offsetLeft, int hexLeft, int textLeft, int charWidth, int textTop) {
			HeaderHeight = headerHeight;
			OffsetLeft = offsetLeft;
			HexLeft = hexLeft;
			TextLeft = textLeft;
			CharWidth = charWidth;
			TextTop = textTop;
		}

		public int HeaderHeight { get; }

		public int OffsetLeft { get; }

		public int HexLeft { get; }

		public int TextLeft { get; }

		public int CharWidth { get; }

		/// <summary>The vertical text inset inside a row.</summary>
		public int TextTop { get; }

		/// <summary>The left edge of a hex byte cell, including one extra character of spacing after each eight-byte group.</summary>
		public int HexCellLeft(int column) => HexLeft + column * 3 * CharWidth + column / GroupSize * CharWidth;
	}

	#endregion
}
