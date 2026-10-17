// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms;

public class PropertyGridEx : PropertyGrid {
	private static readonly Type _nativeGridType = typeof(PropertyGrid).Assembly.GetType("System.Windows.Forms.PropertyGridInternal.PropertyGridView");
	private static readonly PropertyInfo _nativeLabelWidth = _nativeGridType?.GetProperty("LabelWidth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly MethodInfo _nativeMoveSplitter = _nativeGridType?.GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
	private static readonly FieldInfo _nativeListBox = _nativeGridType?.GetField("_listBox", BindingFlags.Instance | BindingFlags.NonPublic);
	private readonly HashSet<Control> _keyboardControls = new();
	private bool _readonly;
	private bool _autoSizeLabelColumn;
	private int _maximumLabelColumnWidth = 320;
	private double _maximumLabelColumnRatio = 0.4;
	private bool _manualLabelColumn;
	private bool _sizingLabelColumn;
	private bool _labelSizingUnsupported;
	private bool _labelSizingDirty = true;
	private GridItem _lastSizedRoot;
	private int _lastSizedWidth;
	private int _lastAutomaticLabelWidth;
	private Control _nativeGrid;
	private LabelColumnInputWindow _inputWindow;

	public PropertyGridEx() {
//		this.ToolbarVisible = false; // categories need to be always visible
	}

	[Category("Behavior")]
	[Description("Legacy read-only marker; property descriptors and their ReadOnly attributes determine whether values can be edited.")]
	[DefaultValue(false)]
	public bool Readonly {
		get => _readonly;
		set { _readonly = value; }
	}

	[Category("Layout")]
	[Description("Sizes the label column to its visible captions until the user adjusts the native splitter.")]
	[DefaultValue(false)]
	public bool AutoSizeLabelColumn {
		get => _autoSizeLabelColumn;
		set {
			if (_autoSizeLabelColumn == value)
				return;
			_autoSizeLabelColumn = value;
			_labelSizingDirty = true;
			FitLabelColumn();
		}
	}

	[Category("Layout")]
	[Description("Maximum automatically measured label width, in logical pixels, subject to native splitter limits.")]
	[DefaultValue(320)]
	public int MaximumLabelColumnWidth {
		get => _maximumLabelColumnWidth;
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_maximumLabelColumnWidth = value;
			_labelSizingDirty = true;
			FitLabelColumn();
		}
	}

	[Category("Layout")]
	[Description("Maximum fraction of the available grid width used by automatically measured labels.")]
	[DefaultValue(0.4)]
	public double MaximumLabelColumnRatio {
		get => _maximumLabelColumnRatio;
		set {
			Guard.Argument(value > 0 && value < 1, nameof(value), "The label fraction must be greater than zero and less than one.");
			_maximumLabelColumnRatio = value;
			_labelSizingDirty = true;
			FitLabelColumn();
		}
	}

	/// <summary>Restores automatic caption measurement after a manual splitter adjustment.</summary>
	public void ResetLabelColumnWidth() {
		_manualLabelColumn = false;
		_labelSizingDirty = true;
		FitLabelColumn();
	}

	protected override void OnSelectedGridItemChanged(SelectedGridItemChangedEventArgs e) {
		base.OnSelectedGridItemChanged(e);
		FitLabelColumn();
	}

	protected override void OnSelectedObjectsChanged(EventArgs e) {
		base.OnSelectedObjectsChanged(e);
		_labelSizingDirty = true;
		FitLabelColumn();
	}

	protected override void OnLayout(LayoutEventArgs e) {
		base.OnLayout(e);
		FitLabelColumn();
	}

	protected override void OnFontChanged(EventArgs e) {
		base.OnFontChanged(e);
		_labelSizingDirty = true;
		FitLabelColumn();
	}

	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		_labelSizingDirty = true;
		FitLabelColumn();
	}

	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		FitLabelColumn();
	}

	protected override void Dispose(bool disposing) {
		if (disposing)
			DetachNativeGrid();
		base.Dispose(disposing);
	}

	private void FitLabelColumn() {
		if (!AutoSizeLabelColumn || _manualLabelColumn || _sizingLabelColumn || _labelSizingUnsupported || IsDisposed || Disposing
			|| !IsHandleCreated || !Visible || SelectedObject == null || Tools.Runtime.IsDesignMode)
			return;
		// .NET does not expose its PropertyGrid splitter publicly. Keep the optional runtime adapter here;
		// a future runtime without these members retains an ordinary usable PropertyGrid.
		if (_nativeLabelWidth?.PropertyType != typeof(int) || _nativeMoveSplitter == null) {
			_labelSizingUnsupported = true;
			return;
		}
		EnsureNativeGrid();
		if (_nativeGrid == null || !_nativeGrid.IsHandleCreated || _nativeGrid.ClientSize.Width < ScaleLabelSize(60))
			return;
		_sizingLabelColumn = true;
		using var restoreSizing = Tools.Scope.ExecuteOnDispose(() => _sizingLabelColumn = false);
		try {
			// SelectedObjectsChanged can run between disposal of the old entries and publication of the new root.
			// Traverse only inside the reentrancy/error boundary; a later native selection/layout retries stale entries.
			var root = SelectedGridItem;
			while (root?.Parent != null)
				root = root.Parent;
			if (root == null || !_labelSizingDirty && root == _lastSizedRoot && _nativeGrid.ClientSize.Width == _lastSizedWidth)
				return;
			using var bold = new Font(Font, FontStyle.Bold);
			var measured = MeasureLabels(root, 0, bold);
			var maximum = Math.Min(ScaleLabelSize(MaximumLabelColumnWidth), (int)Math.Floor(_nativeGrid.ClientSize.Width * MaximumLabelColumnRatio));
			var desired = Math.Max(ScaleLabelSize(24), Math.Min(measured, maximum));
			if ((int)_nativeLabelWidth.GetValue(_nativeGrid) != desired)
				_nativeMoveSplitter.Invoke(_nativeGrid, new object[] { desired });
			_lastSizedRoot = root;
			_lastSizedWidth = _nativeGrid.ClientSize.Width;
			_lastAutomaticLabelWidth = (int)_nativeLabelWidth.GetValue(_nativeGrid);
			_labelSizingDirty = false;
		} catch (ObjectDisposedException) {
			_labelSizingDirty = true;
		} catch (TargetInvocationException error) when (error.InnerException is ObjectDisposedException) {
			_labelSizingDirty = true;
		} catch (Exception error) when (error is MemberAccessException or TargetInvocationException or ArgumentException or NotSupportedException) {
			_labelSizingUnsupported = true;
		}
	}

	private int MeasureLabels(GridItem parent, int depth, Font bold) {
		var width = ScaleLabelSize(24);
		foreach (GridItem item in parent.GridItems) {
			var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
			var regularWidth = TextRenderer.MeasureText(item.Label ?? string.Empty, Font, Size.Empty, flags).Width;
			var boldWidth = TextRenderer.MeasureText(item.Label ?? string.Empty, bold, Size.Empty, flags).Width;
			width = Math.Max(width, Math.Max(regularWidth, boldWidth) + ScaleLabelSize(24 + depth * 12));
			if (item.Expanded)
				width = Math.Max(width, MeasureLabels(item, depth + 1, bold));
		}
		return width;
	}

	private int ScaleLabelSize(int logicalPixels) => (int)Math.Ceiling(logicalPixels * DeviceDpi / 96.0);

	private void EnsureNativeGrid() {
		foreach (Control child in Controls) {
			if (child.GetType() != _nativeGridType)
				continue;
			if (_nativeGrid != child) {
				DetachNativeGrid();
				_nativeGrid = child;
				_nativeGrid.HandleCreated += NativeGridHandleCreated;
				_nativeGrid.HandleDestroyed += NativeGridHandleDestroyed;
				_nativeGrid.SizeChanged += NativeGridSizeChanged;
				_nativeGrid.ControlAdded += NativeGridControlAdded;
				_nativeGrid.LostFocus += NativeGridLostFocus;
				_labelSizingDirty = true;
			}
			if (_nativeGrid.IsHandleCreated && _inputWindow == null)
				_inputWindow = new LabelColumnInputWindow(this, _nativeGrid);
			ObserveNativeKeyboardControls();
			return;
		}
	}

	private void DetachNativeGrid() {
		_inputWindow?.ReleaseHandle();
		_inputWindow = null;
		if (_nativeGrid == null)
			return;
		_nativeGrid.HandleCreated -= NativeGridHandleCreated;
		_nativeGrid.HandleDestroyed -= NativeGridHandleDestroyed;
		_nativeGrid.SizeChanged -= NativeGridSizeChanged;
		_nativeGrid.ControlAdded -= NativeGridControlAdded;
		_nativeGrid.LostFocus -= NativeGridLostFocus;
		foreach (var child in _keyboardControls) {
			child.KeyDown -= NativeChildKeyDown;
			child.LostFocus -= NativeGridLostFocus;
			child.Disposed -= NativeChildDisposed;
		}
		_keyboardControls.Clear();
		_nativeGrid = null;
	}

	private void NativeGridHandleCreated(object sender, EventArgs e) {
		EnsureNativeGrid();
		FitLabelColumn();
	}

	private void NativeGridHandleDestroyed(object sender, EventArgs e) {
		_inputWindow?.ReleaseHandle();
		_inputWindow = null;
		_labelSizingDirty = true;
	}

	private void NativeGridSizeChanged(object sender, EventArgs e) => FitLabelColumn();

	private void NativeGridControlAdded(object sender, ControlEventArgs e) => ObserveKeyboardControl(e.Control);

	private void NativeGridLostFocus(object sender, EventArgs e) => ObserveNativeKeyboardControls();

	private void ObserveNativeKeyboardControls() {
		if (_nativeGrid == null || _nativeGrid.IsDisposed)
			return;
		foreach (Control child in _nativeGrid.Controls)
			ObserveKeyboardControl(child);
		// The native list is reparented into a popup rather than added to the grid's Controls.
		// Inspect only the already-created owned field when focus transfers; never create a lazy editor here.
		try {
			if (_nativeListBox?.GetValue(_nativeGrid) is Control list)
				ObserveKeyboardControl(list);
		} catch (Exception error) when (error is MemberAccessException or ArgumentException or NotSupportedException) {
			// The optional child adapter is unavailable on this runtime; normal native editors remain usable.
		}
	}

	private void ObserveKeyboardControl(Control child) {
		if (child.IsDisposed || !_keyboardControls.Add(child))
			return;
		if (child is ButtonBase or ListBox)
			child.KeyDown += NativeChildKeyDown;
		child.LostFocus += NativeGridLostFocus;
		child.Disposed += NativeChildDisposed;
	}

	private void NativeChildDisposed(object sender, EventArgs e) {
		var child = (Control)sender;
		child.KeyDown -= NativeChildKeyDown;
		child.LostFocus -= NativeGridLostFocus;
		child.Disposed -= NativeChildDisposed;
		_keyboardControls.Remove(child);
	}

	private void NativeChildKeyDown(object sender, KeyEventArgs e) {
		// Native button/list handlers run first and forward Ctrl+Shift+Arrow directly, bypassing the view's HWND.
		if (AutoSizeLabelColumn && !_manualLabelColumn && e.Control && e.Shift && (e.KeyCode is Keys.Left or Keys.Right) && e.Handled
			&& _nativeGrid != null && TryGetNativeLabelWidth(_nativeGrid, out var width) && width != _lastAutomaticLabelWidth)
			_manualLabelColumn = true;
	}

	private bool TryGetNativeLabelWidth(Control view, out int width) {
		width = -1;
		if (_labelSizingUnsupported || view.IsDisposed || !view.IsHandleCreated)
			return false;
		try {
			width = (int)_nativeLabelWidth.GetValue(view);
			return true;
		} catch (Exception error) when (error is MemberAccessException or TargetInvocationException or ArgumentException or NotSupportedException) {
			_labelSizingUnsupported = true;
			return false;
		}
	}

	private sealed class LabelColumnInputWindow : NativeWindow {
		private readonly PropertyGridEx _owner;
		private readonly Control _view;

		public LabelColumnInputWindow(PropertyGridEx owner, Control view) {
			_owner = owner;
			_view = view;
			AssignHandle(view.Handle);
		}

		protected override void WndProc(ref Message message) {
			var mouseDown = message.Msg == WinAPI.USER32.WM_LBUTTONDOWN || message.Msg == (int)WinAPI.WindowMessageFlags.WM_LBUTTONDBLCLK;
			var drag = message.Msg == (int)WinAPI.WindowMessageFlags.WM_MOUSEMOVE && _view.Capture && (message.WParam.ToInt64() & 1) != 0;
			var keyboard = message.Msg == WinAPI.USER32.WM_KEYDOWN && ((Keys)message.WParam.ToInt32() is Keys.Left or Keys.Right)
				&& (Control.ModifierKeys & Keys.Control) != 0;
			var before = -1;
			if (mouseDown || drag || keyboard)
				_owner.TryGetNativeLabelWidth(_view, out before);
			var splitClick = mouseDown && before >= 0 && Math.Abs((short)(message.LParam.ToInt64() & 0xFFFF) - before) <= _owner.ScaleLabelSize(4);
			var refreshLabels = message.Msg == WinAPI.USER32.WM_LBUTTONUP || message.Msg == WinAPI.USER32.WM_KEYDOWN;
			base.WndProc(ref message);
			if ((splitClick || drag || keyboard) && before >= 0 && _owner.TryGetNativeLabelWidth(_view, out var after) && before != after)
				_owner._manualLabelColumn = true;
			if (refreshLabels && !_view.IsDisposed) {
				_owner._labelSizingDirty = true;
				_owner.FitLabelColumn();
			}
		}
	}
}

