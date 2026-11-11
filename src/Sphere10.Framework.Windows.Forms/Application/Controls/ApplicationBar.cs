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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public delegate void ButtonPressedHandler(ApplicationBar source, ApplicationBar.Item button);


/// <summary>
/// Shows the selected item's menu control above a stack of block selector buttons. The buttons and the strip behind them are styled
/// by an explorer bar theme: the displayed <see cref="TaskPane"/>'s theme when the menu control is one, otherwise the default theme.
/// Applying another theme to that pane, or changing its custom settings, restyles the buttons with it. <see cref="Shape"/> and
/// <see cref="ColorStyle"/> override the theme's block selector shape and colour style for this bar.
/// </summary>
public partial class ApplicationBar : ApplicationControl {
	public event ButtonPressedHandler ButtonPressed;

	private readonly List<Item> _items;
	private ExplorerBarInfo _defaultTheme;
	private TaskPane _themePane;
	private BlockSelectorAppearance _appearance;
	private int _buttonsToShow = 0;

	public ApplicationBar() {
		using (this.EnterUpdateScope()) {
			InitializeComponent();
			_items = new List<Item>();
			Disposables.Add(() => ObserveThemePane(null));
			ApplyTheme();
		}
	}

	/// <summary>The menu control of the selected item, shown above the buttons.</summary>
	public Control ApplicationBarControl {
		get;
		set {
			field = value;
			ObserveThemePane(value as TaskPane);
			ApplyTheme();
		}
	}

	/// <summary>
	/// The explorer bar theme that styles the buttons: the displayed task pane's theme, or the default theme when the menu control
	/// is not a task pane. The theme is borrowed from its pane.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ExplorerBarInfo Theme => _themePane?.SystemSettings ?? DefaultTheme;

	/// <summary>The colour style of the buttons, overriding the theme's <see cref="BlockSelectorInfo.ColorStyle"/>; null follows the theme.</summary>
	[DefaultValue(null), Category("Appearance"), Description("The colour style of the block buttons; empty follows the explorer bar theme")]
	public BlockSelectorColorStyle? ColorStyle {
		get;
		set {
			if (field == value)
				return;
			field = value;
			ApplyTheme();
		}
	}

	/// <summary>The shape of the buttons, overriding the theme's <see cref="BlockSelectorInfo.Shape"/>; null follows the theme.</summary>
	[DefaultValue(null), Category("Appearance"), Description("The shape of the block buttons; empty follows the explorer bar theme")]
	public BlockSelectorShape? Shape {
		get;
		set {
			if (field == value)
				return;
			field = value;
			ApplyTheme();
		}
	}

	/// <summary>The height of each button in device pixels, from the resolved block selector metrics.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int ButtonHeight => LogicalToDeviceUnits(Appearance.ButtonHeight);

	public Item[] Items => _items.ToArray();

	public Item SelectedItem {
		get {
			foreach (Item item in _items) {
				if (item.Button.Pressed) {
					return item;
				}
			}
			return null;
		}
		set {
			Guard.Argument(value == null || _items.Contains(value), nameof(value), "Item is not registered.");
			foreach (var item in _items)
				item.Button.Pressed = ReferenceEquals(item, value);
			ShowMenuControl(value?.MenuControl);
		}
	}

	/// <summary>The colour behind the buttons, which continues the task pane's gradient below its expandos.</summary>
	internal Color StripColor => _themePane?.GradientEndColor ?? Theme.TaskPane.GradientEndColor;

	/// <summary>The buttons' resolved look, recomputed whenever the theme or an override changes.</summary>
	internal BlockSelectorAppearance Appearance => _appearance ??= BlockSelectorAppearance.Resolve(Theme, ColorStyle, Shape);

	private ExplorerBarInfo DefaultTheme {
		get {
			if (_defaultTheme == null) {
				_defaultTheme = ExplorerBarInfo.Default;
				Disposables.Add(_defaultTheme);
			}
			return _defaultTheme;
		}
	}

	// The gap between buttons in device pixels.
	private int ButtonSpacing => LogicalToDeviceUnits(Appearance.ButtonSpacing);

	// Tags sit inside the task pane's padding, in line with its expandos; buttons span the bar.
	private Padding StripPadding {
		get {
			if (!Appearance.Inset)
				return Padding.Empty;
			var padding = _themePane?.Padding ?? Theme.TaskPane.Padding;
			return new Padding(LogicalToDeviceUnits(padding.Left), LogicalToDeviceUnits(padding.Top), LogicalToDeviceUnits(padding.Right), LogicalToDeviceUnits(padding.Bottom));
		}
	}

	public void AddItem(Item item) {
		Guard.ArgumentNotNull(item, nameof(item));
		// set up the button
		item.Button ??= new ApplicationBarButton();
		item.Button.Bar = this;
		item.Button.Text = item.Text;
		item.Button.Image = item.Image;
		item.Button.ButtonStateChanged += Button_ButtonStateChanged;

		// add to the button objectStream
		_items.Add(item);
		_splitContainer.Panel2.Controls.Add(item.Button);
		_buttonsToShow++;

		// if first button, then auto select and hide button panel
		if (_items.Count == 1) {
			_splitContainer.Panel2Collapsed = true;
			SelectedItem = item;
		} else {
			item.Button.Pressed = false;
			_splitContainer.Panel2Collapsed = false;
		}
		PerformCustomLayout();
	}

	public void RemoveItem(Item item) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.Argument(_items.Contains(item), nameof(item), "Item is not registered");
		var wasSelected = SelectedItem == item;
		_items.Remove(item);
		_buttonsToShow--;
		item.Button.ButtonStateChanged -= Button_ButtonStateChanged;
		item.Button.Dispose();
		if (wasSelected) {
			item.MenuControl?.Parent?.Controls.Remove(item.MenuControl);
			ApplicationBarControl = null;
			if (_items.Count > 0)
				SelectedItem = _items[0];
		}
		_splitContainer.Panel2Collapsed = _items.Count <= 1;
		PerformCustomLayout();
	}

	public bool ContainsItem(Item item) => _items.Contains(item);

	public virtual void PerformCustomLayout() {
		using (this.EnterUpdateScope()) {
			if (_items == null)
				return;

			// A collapsed button panel fills the bar. Its old splitter minimum must not block the native panels from following resize.
			if (_splitContainer.Panel2Collapsed || _items.Count == 0) {
				_splitContainer.Panel1MinSize = 0;
				return;
			}

			// Stack the visible buttons across the bar, or inside the task pane's padding for tags.
			var padding = StripPadding;
			var buttonHeight = ButtonHeight;
			var pitch = buttonHeight + ButtonSpacing;
			var buttonWidth = Math.Max(0, _splitContainer.Panel2.Width - padding.Horizontal);
			for (var i = 0; i < _items.Count && i < _buttonsToShow; i++)
				_items[i].Button.Bounds = new Rectangle(padding.Left, padding.Top + pitch * i, buttonWidth, buttonHeight);

			// Fit the button panel to the visible buttons; the splitter can hide trailing buttons but never more than all of them.
			_splitContainer.SplitterIncrement = pitch;
			_splitContainer.Panel1MinSize = 0;
			_splitContainer.Panel1MinSize = Tools.Values.ClipValue(_splitContainer.Height - _splitContainer.SplitterWidth - GetStripHeight(_items.Count), 0, _splitContainer.Height);
			if (_splitContainer.Height > buttonHeight) {
				_splitContainer.SplitterDistance = Tools.Values.ClipValue(
					_splitContainer.Height - GetStripHeight(_buttonsToShow) - _splitContainer.SplitterWidth,
					_splitContainer.Panel1MinSize + 1,
					_splitContainer.Height
				);
			}
		}
	}

	protected virtual void OnButtonPressed(ApplicationBar source, ApplicationBar.Item button) {
	}

	protected void FireButtonPressedEvent(ApplicationBar source, ApplicationBar.Item button) {
		OnButtonPressed(source, button);
		ButtonPressed?.Invoke(source, button);
	}

	protected override void OnResize(EventArgs e) {
		base.OnResize(e);
		PerformCustomLayout();
	}

	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		ApplyTheme();
	}

	protected override void OnRightToLeftChanged(EventArgs e) {
		base.OnRightToLeftChanged(e);
		InvalidateButtons();
	}

	// Restyles the strip and buttons from the current theme.
	private void ApplyTheme() {
		if (IsDisposed || Disposing)
			return;
		_appearance = null;
		var stripColor = StripColor;
		// The splitter takes the container's colour, so the divider disappears into the strip.
		_splitContainer.BackColor = stripColor;
		_splitContainer.Panel2.BackColor = stripColor;
		PerformCustomLayout();
		InvalidateButtons();
	}

	private void InvalidateButtons() {
		foreach (var item in _items ?? [])
			item.Button.Invalidate();
	}

	// Follows only the displayed task pane, whose theme and custom settings style the buttons.
	private void ObserveThemePane(TaskPane pane) {
		if (ReferenceEquals(_themePane, pane))
			return;
		if (_themePane != null) {
			_themePane.SystemSettingsChanged -= ThemePane_Changed;
			_themePane.CustomSettingsChanged -= ThemePane_Changed;
			_themePane.Disposed -= ThemePane_Disposed;
		}
		_themePane = pane;
		if (_themePane != null) {
			_themePane.SystemSettingsChanged += ThemePane_Changed;
			_themePane.CustomSettingsChanged += ThemePane_Changed;
			_themePane.Disposed += ThemePane_Disposed;
		}
	}

	// The height of a strip holding the given number of buttons.
	private int GetStripHeight(int buttonCount) {
		if (buttonCount <= 0)
			return 0;
		return StripPadding.Vertical + buttonCount * ButtonHeight + (buttonCount - 1) * ButtonSpacing;
	}

	private void ShowMenuControl(Control control) {
		// get the current menu control
		Control currentControl = null;
		if (_splitContainer.Panel1.Controls.Count == 1) {
			currentControl = _splitContainer.Panel1.Controls[0];
		} else {
			// invalid state so just clear all menu controls in there
			_splitContainer.Panel1.Controls.Clear();
		}
		if (ReferenceEquals(currentControl, control)) {
			ApplicationBarControl = control;
			return;
		}

		// add the new control
		if (control != null) {
			control.Dock = DockStyle.Fill;
			_splitContainer.Panel1.Controls.Add(control);
		}

		// remove the old control
		if (currentControl != null) {
			_splitContainer.Panel1.Controls.Remove(currentControl);
		}

		ApplicationBarControl = control;
	}

	#region Handlers

	private void Button_ButtonStateChanged(object sender, EventArgs e) {
		Item source = null;
		// find the source button
		foreach (Item item in _items) {
			if (item.Button == sender) {
				source = item;
				break;
			}
		}
		if (source != null) {
			if (source.Button.Pressed) {
				SelectedItem = source;
				FireButtonPressedEvent(this, source);
			}
		}
	}

	private void SplitterMoved(object sender, SplitterEventArgs e) {
		if (!Updating) {
			// Count the buttons the user's divider reveals, including one it only partly reveals.
			var available = _splitContainer.Panel2.Height - StripPadding.Vertical + ButtonSpacing;
			_buttonsToShow = Tools.Values.ClipValue((int)Math.Ceiling(available / (float)(ButtonHeight + ButtonSpacing)), 0, _items.Count);
			PerformCustomLayout();
		}
	}

	private void ThemePane_Changed(object sender, EventArgs e) => ApplyTheme();

	private void ThemePane_Disposed(object sender, EventArgs e) {
		if (ReferenceEquals(sender, _themePane)) {
			ObserveThemePane(null);
			ApplyTheme();
		}
	}

	#endregion

	#region Inner Types

	public class Item {

		public Item(Control menuControl, Image image, string text)
			: this(null, menuControl, image, text) {
		}

		public Item(ApplicationBarButton button, Control menuControl, Image image, string text) {
			Button = button;
			MenuControl = menuControl;
			Image = image;
			Text = text;
		}

		public ApplicationBarButton Button { get; set; }

		public Control MenuControl { get; set; }

		public Image Image { get; set; }

		public string Text { get; set; }
	}

	#endregion

}
