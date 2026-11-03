// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

//Note about the new scrolling code:
//This algorithm still has some bugs.
//Right now the list is not long, but it seems you will have to deal with this, Davide:

//(FIXED) 1. [MICK(22)] GetVisibleRows returns partially visible row, though it shouldnt.
//  In sample 3, when focus is just above "boolean checkbox" and "boolean checkbox" is partially visible,
//  GetVisibleRows returns also this row and it should not. This causes CustomScrollPageDown() not to scroll down.
//DAVIDE: This is not a bug in the GetVisibleRows but in the GridVirtual.CustomScrollPageDown (now fixed) that didn't support Rows/Column span

//2. [MICK(23)] [MICK(24)]
//If there are no focusable cells below, the algorithm does not know what to do.
//Same thing when one cell is spread over a few cells in other row (sample 3, "editors and types", all blue titles are examples of these). If the focus is on the cell "Single Image", there is no scrolling or acrolling is inapropriate (because Selection.MoveActiveCell(-1, 0); is done instead scrolling).

/// <summary>
/// A control with a custom implementation of a scrollable area
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public abstract class CustomScrollControl : System.Windows.Forms.Panel {
	/// <summary>
	/// Fired when the scroll vertical posizion change
	/// </summary>
	public event ScrollPositionChangedEventHandler VScrollPositionChanged;

	/// <summary>
	/// Fired when the scroll horizontal posizion change
	/// </summary>
	public event ScrollPositionChangedEventHandler HScrollPositionChanged;

	// ScrollBars and Panels
	private HScrollBar _hScrollBar = new HScrollBar();
	private VScrollBar _vScrollBar = new VScrollBar();

	/// <summary>
	/// Panel showed on the bottom right of the grid when both scrollbars are visible
	/// </summary>
	private Panel _bottomRightPanel = new Panel();

	/// <summary>
	/// Internal panel that contains hScrollBar and mBottomRightPanel
	/// </summary>
	private System.Windows.Forms.Panel _panelDockBottom = new System.Windows.Forms.Panel();

	// ScrollArea, ScrollPosition, DisplayRectangle
	private int _horizontalPage = 0;
	private int _verticalPage = 0;
	private int _suspendedCount = 0;

	// ScrollChangeEvent
	private int _oldVScrollValue = 0;
	private int _oldHScrollValue = 0;

	// Constructor
	public CustomScrollControl() {
		SuspendLayout();

		Name = "CustomScrollControl";
		base.AutoScroll = false;
		//to remove flicker and use custom draw
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, true);

		CreateDockControls();

		ResumeLayout(false);
	}

	// override AutoScroll
	/// <summary>
	/// I disabled the default AutoScroll property because I have a custom implementation
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), DefaultValue(false)]
	public override bool AutoScroll {
		get {
			return false;
		}
		set {
			if (value)
				throw new SourceGridException("Auto Scroll not supported in this control");
			base.AutoScroll = false;
		}
	}

	/// <summary>
	/// Gets the vertical scrollbar. Can be visible or unvisible.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VScrollBar VScrollBar {
		get {
			return _vScrollBar;
		}
	}

	/// <summary>
	/// Gets the horizontal scrollbar. Can be visible or unvisible.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HScrollBar HScrollBar {
		get {
			return _hScrollBar;
		}
	}

	/// <summary>
	/// Gets the panel at the bottom right of the control. This panel is valid only if HScrollBar and VScrollBar are valid. Otherwise is null.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Panel BottomRightPanel {
		get {
			return _bottomRightPanel;
		}
	}

	[Browsable(false)]
	public bool HScrollBarVisible {
		get {
			return _hScrollBar.Height != 0;
		}
	}

	[Browsable(false)]
	public bool VScrollBarVisible {
		get {
			return _vScrollBar.Width != 0;
		}
	}

	/// <summary>
	/// Gets or sets the current scroll position relative to the CustomScrollArea.
	/// The value must be always between 0 and CustomScrollArea (0 or positive).
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual Point CustomScrollPosition {
		get {
			var x = 0;
			if (HScrollBarVisible)
				x = _hScrollBar.Value;

			var y = 0;
			if (VScrollBarVisible)
				y = _vScrollBar.Value;

			return new Point(x, y);
		}
		set {
			if (HScrollBarVisible) {
				if (value.X > 0)
					_hScrollBar.Value = value.X;
				else
					_hScrollBar.Value = 0;
			}

			if (VScrollBarVisible) {
				if (_vScrollBar.Maximum < value.Y)
					return;
				if (value.Y > 0)
					_vScrollBar.Value = value.Y;
				else
					_vScrollBar.Value = 0;
			}
		}
	}

	/// <summary>
	/// Display rectangle of the control, without ScrollBars.
	/// Note: I don't override this method because I use some dock feature that require the real display rectangle.
	/// </summary>
	public new Rectangle DisplayRectangle {
		get {
			var scrollH = 0;
			if (HScrollBarVisible)
				scrollH = _hScrollBar.Height;

			var scrollV = 0;
			if (VScrollBarVisible)
				scrollV = _vScrollBar.Width;

			var baseDisplay = base.DisplayRectangle;
			return new Rectangle(baseDisplay.X, baseDisplay.Y, baseDisplay.Width - scrollV, baseDisplay.Height - scrollH);
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected int HorizontalPage {
		get {
			return _horizontalPage;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected int VerticalPage {
		get {
			return _verticalPage;
		}
	}

	// Maximum/Minimum Scroll Position
	/// <summary>
	/// Return the maximum position that can be scrolled
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected int MaximumVScroll {
		get {
			if (VScrollBarVisible)
				return _vScrollBar.Maximum - (_vScrollBar.LargeChange - 1);
			return 0;
		}
	}

	/// <summary>
	/// Return the minimum position that can be scrolled
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected int MinimumVScroll {
		get {
			return 0;
		}
	}

	/// <summary>
	/// Return the minimum position that can be scrolled
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected int MinimumHScroll {
		get {
			return 0;
		}
	}

	/// <summary>
	/// Return the maximum position that can be scrolled
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected int MaximumHScroll {
		get {
			if (HScrollBarVisible)
				return _hScrollBar.Maximum - (_hScrollBar.LargeChange - 1);
			return 0;
		}
	}

	public new void PerformLayout() {
		PerformLayout(this, null);
	}

	public new void PerformLayout(Control affectedControl, string affectedProperty) {
		base.PerformLayout(affectedControl, affectedProperty);
	}

	public bool IsSuspended() {
		return _suspendedCount > 0;
	}

	// Scroll PageDown/Up/Right/Left/LineUp/Down/Right/Left
	/// <summary>
	/// Scroll the page down
	/// </summary>
	public virtual void CustomScrollPageDown() {
		if (VScrollBarVisible)
			_vScrollBar.Value = Math.Min(_vScrollBar.Value + _vScrollBar.LargeChange, _vScrollBar.Maximum - _vScrollBar.LargeChange);
	}

	//MICK(11)
	/// <summary>
	/// Scroll the page down to line
	/// </summary>
	public virtual void CustomScrollPageToLine(int line) {
		if (VScrollBarVisible)
			if (_vScrollBar.Value != line)
				_vScrollBar.Value = Math.Min(line, _vScrollBar.Maximum);
	}

	//MICK(12)
	/// <summary>
	/// Scroll the page down to line
	/// </summary>
	public virtual bool IsMaxPage() {
		return (_vScrollBar.Value == _vScrollBar.Maximum);
	}

	/// <summary>
	/// Scroll the page up
	/// </summary>
	public virtual void CustomScrollPageUp() {
		if (VScrollBarVisible)
			_vScrollBar.Value = Math.Max(_vScrollBar.Value - _vScrollBar.LargeChange, _vScrollBar.Minimum);
	}

	/// <summary>
	/// Scroll the page right
	/// </summary>
	public virtual void CustomScrollPageRight() {
		if (HScrollBarVisible)
			_hScrollBar.Value = Math.Min(_hScrollBar.Value + _hScrollBar.LargeChange, _hScrollBar.Maximum - _hScrollBar.LargeChange);
	}

	/// <summary>
	/// Scroll the page left
	/// </summary>
	public virtual void CustomScrollPageLeft() {
		if (HScrollBarVisible)
			_hScrollBar.Value = Math.Max(_hScrollBar.Value - _hScrollBar.LargeChange, _hScrollBar.Minimum);
	}

	public virtual void CustomScrollWheel(int rotationDelta) {
		if (rotationDelta >= 120 || rotationDelta <= -120) {
			if (VScrollBarVisible) {
				var current = CustomScrollPosition;
				var newY = current.Y +
							SystemInformation.MouseWheelScrollLines * VScrollBar.SmallChange * -Math.Sign(rotationDelta);

				//check that the value is between max and min
				if (newY < 0)
					newY = 0;
				if (newY > MaximumVScroll)
					newY = MaximumVScroll;

				CustomScrollPosition = new Point(current.X, newY);
			}
		}
	}

	/// <summary>
	/// Scroll the page down one line
	/// </summary>
	public virtual void CustomScrollLineDown() {
		if (VScrollBarVisible)
			_vScrollBar.Value = Math.Min(_vScrollBar.Value + _vScrollBar.SmallChange, _vScrollBar.Maximum);
	}

	/// <summary>
	/// Scroll the page up one line
	/// </summary>
	public virtual void CustomScrollLineUp() {
		if (VScrollBarVisible)
			_vScrollBar.Value = Math.Max(_vScrollBar.Value - _vScrollBar.SmallChange, _vScrollBar.Minimum);
	}

	/// <summary>
	/// Scroll the page right one line
	/// </summary>
	public virtual void CustomScrollLineRight() {
		if (HScrollBarVisible)
			_hScrollBar.Value = Math.Min(_hScrollBar.Value + _hScrollBar.SmallChange, _hScrollBar.Maximum);
	}

	/// <summary>
	/// Scroll the page left one line
	/// </summary>
	public virtual void CustomScrollLineLeft() {
		if (HScrollBarVisible)
			_hScrollBar.Value = Math.Max(_hScrollBar.Value - _hScrollBar.SmallChange, _hScrollBar.Minimum);
	}

	// Layout Events
	/// <summary>
	/// OnLayout Method
	/// </summary>
	/// <param name="levent"></param>
	protected override void OnLayout(LayoutEventArgs levent) {
		if (levent.AffectedControl != this) {
			base.OnLayout(levent);
			return;
		}

		RecalcCustomScrollBars();

		base.OnLayout(levent);
	}

	protected virtual void CreateDockControls() {
		_panelDockBottom.SuspendLayout();
		_panelDockBottom.Controls.Add(_hScrollBar);
		_panelDockBottom.Controls.Add(_bottomRightPanel);
		_panelDockBottom.Dock = System.Windows.Forms.DockStyle.Bottom;

		_bottomRightPanel.Dock = System.Windows.Forms.DockStyle.Right;

		_hScrollBar.Dock = System.Windows.Forms.DockStyle.Fill;

		_vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;

		Controls.Add(_vScrollBar);
		Controls.Add(_panelDockBottom);

		_hScrollBar.ValueChanged += new EventHandler(HScrollChange);
		_vScrollBar.ValueChanged += new EventHandler(VScrollChange);

		_vScrollBar.TabStop = false;
		_hScrollBar.TabStop = false;
		_bottomRightPanel.TabStop = false;
		_panelDockBottom.TabStop = false;
		_panelDockBottom.TabIndex = 1;
		_bottomRightPanel.TabIndex = 2;
		_vScrollBar.TabIndex = 3;
		_hScrollBar.TabIndex = 4;

		_bottomRightPanel.BackColor = Color.FromKnownColor(KnownColor.Control);

		PrepareScrollBars(false, false);

		_panelDockBottom.ResumeLayout(false);
	}

	/// <summary>
	/// Load the scrollable area that will affect the maximum scroll values.
	/// </summary>
	/// <param name="verticalPage"></param>
	/// <param name="horizontalPage"></param>
	protected void LoadScrollArea(int verticalPage, int horizontalPage) {
		if (verticalPage < 0)
			throw new ArgumentOutOfRangeException();
		_verticalPage = verticalPage;

		if (horizontalPage < 0)
			throw new ArgumentOutOfRangeException();
		_horizontalPage = horizontalPage;

		RecalcCustomScrollBars();
	}

	/// <summary>
	/// Recalculate the scrollbars position and size.
	/// Use this to refresh scroll bars
	/// </summary>
	public void RecalcCustomScrollBars() {
		SuspendLayout();

		// Start with the full client area so obsolete scrollbars can disappear.
		var scrollRows = GetScrollRows(base.DisplayRectangle.Height);
		var scrollCols = GetScrollColumns(base.DisplayRectangle.Width);

		PrepareScrollBars(scrollCols > 0, scrollRows > 0);

		// Either scrollbar can consume enough space to require the other axis too.
		scrollRows = GetScrollRows(DisplayRectangle.Height);
		scrollCols = GetScrollColumns(DisplayRectangle.Width);
		if (scrollRows > 0 && !VScrollBarVisible || scrollCols > 0 && !HScrollBarVisible) {
			PrepareScrollBars(scrollCols > 0, scrollRows > 0);

			// Both bars are now settled; calculate their ranges from the remaining viewport.
			scrollRows = GetScrollRows(DisplayRectangle.Height);
			scrollCols = GetScrollColumns(DisplayRectangle.Width);
		}

		//MICK(6)
		scrollRows = scrollRows - GetActualFixedRows();

		RecalcVScrollBar(scrollRows);
		RecalcHScrollBar(scrollCols);

		//forzo un ridisegno
		InvalidateScrollableArea();

		ResumeLayout(true);
	}

	public new void SuspendLayout() {
		base.SuspendLayout();

		_suspendedCount++;
	}

	public new void ResumeLayout() {
		base.ResumeLayout();

		if (_suspendedCount > 0)
			_suspendedCount--;
	}

	public new void ResumeLayout(bool performLayout) {
		base.ResumeLayout(performLayout);

		if (_suspendedCount > 0)
			_suspendedCount--;
	}

	/// <summary>
	/// Prepare the scrollbars with the specified dock option.
	/// </summary>
	/// <param name="showHScroll"></param>
	/// <param name="showVScroll"></param>
	protected virtual void PrepareScrollBars(bool showHScroll, bool showVScroll) {
		if (showHScroll != HScrollBarVisible) {
			if (showHScroll) {
				_panelDockBottom.Height = System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				//panelDockBottom.Enabled = true;
				//panelDockBottom.Visible = true;
				//mHScrollBar.Visible = true;
				SetHScrollBarVisible(true);
			} else {
				_panelDockBottom.Height = 0;
				//panelDockBottom.Enabled = false;
				//panelDockBottom.Visible = false;
				//mHScrollBar.Visible = false;
				SetHScrollBarVisible(false);
			}
		}

		if (showVScroll != VScrollBarVisible) {
			if (showVScroll) {
				//mVScrollBar.Width = System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				//mBottomRightPanel.Enabled = true;
				//mBottomRightPanel.Visible = true;
				//mVScrollBar.Enabled = true;
				//mVScrollBar.Visible = true;
				SetVScrollBarVisible(true);
				_bottomRightPanel.Width = _vScrollBar.Width;
			} else {
				//mVScrollBar.Width = 0;
				//mVScrollBar.Enabled = false;
				//mVScrollBar.Visible = false;
				SetVScrollBarVisible(false);
				_bottomRightPanel.Width = 0;
				//mBottomRightPanel.Enabled = false;
				//mBottomRightPanel.Visible = false;
			}
		}
	}

	protected void SetHScrollBarVisible(bool value) {
		if (value)
			_hScrollBar.Height = System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
		else
			_hScrollBar.Height = 0;
	}

	protected void SetVScrollBarVisible(bool value) {
		if (value)
			_vScrollBar.Width = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
		else
			_vScrollBar.Width = 0;
	}

	/// <summary>
	/// Calculate the number of rows to scroll. 0 to disable the scrollbar.
	/// The returned value is independent from the current scrolling position, must be a fixed value
	/// calculated based on the total number of rows and the available area.
	/// </summary>
	protected abstract int GetScrollRows(int displayHeight);

	//MICK(7)
	protected abstract int GetActualFixedRows();

	/// <summary>
	/// Calculate the number of columns to scroll. 0 to disable the scrollbar.
	/// The returned value is independent from the current scrolling position, must be a fixed value
	/// calculated based on the total number of columns and the available area.
	/// </summary>
	protected abstract int GetScrollColumns(int displayWidth);

	// Add/Remove ScrollBars
	/// <summary>
	/// recalculate the position of the horizontal scrollbar
	/// </summary>
	private void RecalcHScrollBar(int cols) {
		if (HScrollBarVisible == false)
			return;

		_hScrollBar.Minimum = 0;
		_hScrollBar.Maximum = cols + HorizontalPage - 1;

		if (HorizontalPage > 1)
			_hScrollBar.LargeChange = HorizontalPage;
		else
			_hScrollBar.LargeChange = 1;

		//mHScrollBar.SmallChange = 1;

		if (_hScrollBar.Value > MaximumHScroll)
			_hScrollBar.Value = MaximumHScroll;
	}

	/// <summary>
	/// Recalculate the position of the vertical scrollbar
	/// </summary>
	private void RecalcVScrollBar(int rows) {
		if (VScrollBarVisible == false)
			return;
		_vScrollBar.Minimum = 0;
		//MICK(4): Now (rows + VerticalPage - 1) equals the count of all rows minus fixed rows...
		// But I do not change it here because I do not know how to do that properly
		_vScrollBar.Maximum = rows + VerticalPage - 1;
		//MICK(5): this is changed too
		if (VerticalPage > 1)
			_vScrollBar.LargeChange = VerticalPage;
		else
			_vScrollBar.LargeChange = 1;

		//mVScrollBar.SmallChange = 1;

		if (_vScrollBar.Value > MaximumVScroll)
			_vScrollBar.Value = MaximumVScroll;
	}

	private void VScrollChange(object sender, EventArgs args) {
		OnVScrollPositionChanged(new ScrollPositionChangedEventArgs(-_vScrollBar.Value, -_oldVScrollValue));

		InvalidateScrollableArea();

		_oldVScrollValue = _vScrollBar.Value;
	}

	/// <summary>
	/// Fired when the scroll vertical posizion change
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnVScrollPositionChanged(ScrollPositionChangedEventArgs e) {
		if (VScrollPositionChanged != null)
			VScrollPositionChanged(this, e);
	}

	private void HScrollChange(object sender, EventArgs args) {
		OnHScrollPositionChanged(new ScrollPositionChangedEventArgs(-_hScrollBar.Value, -_oldHScrollValue));

		InvalidateScrollableArea();

		_oldHScrollValue = _hScrollBar.Value;
	}

	/// <summary>
	/// Invalidate the scrollable area
	/// </summary>
	protected abstract void InvalidateScrollableArea();

	/// <summary>
	/// Fired when the scroll horizontal posizion change
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnHScrollPositionChanged(ScrollPositionChangedEventArgs e) {
		if (HScrollPositionChanged != null)
			HScrollPositionChanged(this, e);
	}
}
