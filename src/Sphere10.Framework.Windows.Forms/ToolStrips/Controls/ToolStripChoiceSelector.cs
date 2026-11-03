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
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A non-editable toolbar choice displayed as a button and a checked native menu.</summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class ToolStripChoiceSelector : ToolStripDropDownButton {
	private object[] _items = Array.Empty<object>();
	private int _selectedIndex = -1;
	private string _emptyText = "(None)";
	private Func<object, string> _itemTextProvider;

	public ToolStripChoiceSelector() {
		DisplayStyle = ToolStripItemDisplayStyle.Text;
		Text = _emptyText;
	}

	public event EventHandler SelectedIndexChanged;

	/// <summary>Returns a copy of the choices while retaining each original data object.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object[] Items => (object[])_items.Clone();

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedItem {
		get => _selectedIndex < 0 ? null : _items[_selectedIndex];
		set => SelectedIndex = FindItem(value);
	}

	[DefaultValue(-1)]
	public int SelectedIndex {
		get => _selectedIndex;
		set {
			Guard.ArgumentInRange(value, -1, _items.Length - 1, nameof(value));
			if (_selectedIndex == value) return;
			_selectedIndex = value;
			UpdateSelection();
			OnSelectedIndexChanged(EventArgs.Empty);
		}
	}

	[DefaultValue("(None)")]
	public string EmptyText {
		get => _emptyText;
		set { _emptyText = value ?? string.Empty; UpdateSelection(); }
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, string> ItemTextProvider {
		get => _itemTextProvider;
		set { _itemTextProvider = value; UpdateSelection(); }
	}

	/// <summary>Replaces the choices and preserves the selected object when it remains present.</summary>
	public void SetItems(IEnumerable<object> items) {
		Guard.ArgumentNotNull(items, nameof(items));
		var choices = items.ToArray();
		Guard.Argument(choices.All(item => item != null), nameof(items), "Choices cannot contain null.");
		var previous = SelectedItem;
		var previousIndex = _selectedIndex;
		foreach (var item in DropDownItems.Cast<ToolStripItem>().ToArray()) {
			item.Tag = null;
			item.Dispose();
		}
		DropDownItems.Clear();
		_items = choices;
		_selectedIndex = FindItem(previous);
		for (var index = 0; index < _items.Length; index++) {
			var choiceIndex = index;
			var option = new ToolStripMenuItem { Tag = _items[index], CheckOnClick = false };
			option.Click += (_, _) => SelectedIndex = choiceIndex;
			DropDownItems.Add(option);
		}
		UpdateSelection();
		if (previousIndex != _selectedIndex || !ReferenceEquals(previous, SelectedItem))
			OnSelectedIndexChanged(EventArgs.Empty);
	}

	protected virtual void OnSelectedIndexChanged(EventArgs args) => SelectedIndexChanged?.Invoke(this, args);

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (ToolStripItem item in DropDownItems) item.Tag = null;
			_items = Array.Empty<object>();
			_selectedIndex = -1;
			_itemTextProvider = null;
			SelectedIndexChanged = null;
		}
		base.Dispose(disposing);
	}

	private int FindItem(object value) {
		if (value == null) return -1;
		var index = Array.FindIndex(_items, item => ReferenceEquals(item, value));
		return index >= 0 ? index : Array.IndexOf(_items, value);
	}

	private void UpdateSelection() {
		Text = _selectedIndex < 0 ? _emptyText : GetItemText(SelectedItem);
		for (var index = 0; index < DropDownItems.Count; index++) {
			var item = (ToolStripMenuItem)DropDownItems[index];
			item.Text = GetItemText(_items[index]);
			item.Checked = index == _selectedIndex;
		}
	}

	private string GetItemText(object item) => _itemTextProvider?.Invoke(item) ?? Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty;
}
