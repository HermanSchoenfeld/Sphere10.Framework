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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Searchable, lazy examples for every public control and form in the referenced native UI libraries.</summary>
public partial class ControlCatalogScreen : ApplicationScreen {
	private ControlCatalogEntry[] _entries;
	private Control _example;
	private PopupDropDown _openPopup;
	private bool _openingWindow;

	public ControlCatalogScreen() {
		InitializeComponent();
		Disposables.Add(_className.Font);
		Title = "Control catalog";
	}

	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (Tools.Runtime.IsDesignMode || _entries != null)
			return;
		_entries = ControlCatalogSamples.CreateEntries().OrderBy(entry => entry.ControlType.Name, StringComparer.Ordinal).ToArray();
		FilterChanged(this, EventArgs.Empty);
	}

	protected override void OnDestroyScreen() {
		_openPopup?.Close();
		ReleaseExample();
		base.OnDestroyScreen();
	}

	protected override void OnHide(ref bool cancelHide) {
		base.OnHide(ref cancelHide);
		cancelHide |= _openingWindow;
	}

	private void FilterChanged(object sender, EventArgs args) {
		if (_entries == null)
			return;
		var selected = _catalog.SelectedItem;
		var filter = _filter.Text.Trim();
		_catalog.BeginUpdate();
		using var update = Tools.Scope.ExecuteOnDispose(_catalog.EndUpdate);
		_catalog.Items.Clear();
		_catalog.Items.AddRange(_entries.Where(entry => (entry.ClassName + " " + entry.Category).Contains(filter, StringComparison.OrdinalIgnoreCase)).Cast<object>().ToArray());
		_count.Text = $"{_catalog.Items.Count} of {_entries.Length} classes";
		if (selected != null && _catalog.Items.Contains(selected))
			_catalog.SelectedItem = selected;
		else if (_catalog.Items.Count > 0)
			_catalog.SelectedIndex = 0;
		else {
			ReleaseExample();
			_className.Text = "No matching classes";
			_instructions.Text = "Search a class name, namespace or category.";
			_openWindow.Visible = false;
			_showProperties.Visible = false;
		}
	}

	private async void SelectionChanged(object sender, EventArgs args) {
		if (_catalog.SelectedItem is not ControlCatalogEntry entry || _openingWindow)
			return;
		ReleaseExample();
		_className.Text = entry.ClassName;
		_instructions.Text = entry.Instructions;
		_openWindow.Visible = entry.OpensWindow;
		_showProperties.Visible = !entry.OpensWindow;
		_status.Text = entry.ControlType.IsAbstract ? "Abstract base: the example uses a concrete implementation." : "Edit the example and inspect its live properties.";
		if (entry.OpensWindow)
			return;
		try {
			_example = entry.CreateExample();
			if (_example is ConnectionBarBase)
				_example.Height = Math.Max(_example.Height, _example.Font.Height * 3 + 12);
			_showProperties.Checked = _example is not ApplicationScreen;
			_example.Name = "catalogExample";
			_example.Dock = _example is TextBoxBase or ComboBox or ButtonBase or NumericUpDown or ProgressBar or ToolStrip or SeparatorLine or PathSelectorControl or ConnectionBarBase
				? DockStyle.Top : DockStyle.Fill;
			_preview.Controls.Add(_example);
			var subject = Subjects(_example).FirstOrDefault(control => Matches(entry.ControlType, control.GetType()));
			if (subject is Control nativeControl)
				SelectContainingTabs(nativeControl);
			_properties.SelectedObject = subject ?? _example;
			_status.Text = subject == null ? "Use the example's named actions to open this native class." : "Live instance: " + subject.GetType().FullName;
			_example.Click += (_, _) => _status.Text = "Clicked " + entry.ClassName;
		} catch (Exception error) {
			_status.Text = "The example could not be opened: " + error.Message;
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void OpenWindowClicked(object sender, EventArgs args) {
		if (_catalog.SelectedItem is not ControlCatalogEntry entry || _openingWindow)
			return;
		_openingWindow = true;
		_openWindow.Enabled = _catalog.Enabled = _filter.Enabled = false;
		using var state = Tools.Scope.ExecuteOnDispose(() => {
			_openingWindow = false;
			if (!IsDisposed)
				_openWindow.Enabled = _catalog.Enabled = _filter.Enabled = true;
		});
		try {
			if (entry.ShowWindow != null) {
				await entry.ShowWindow(this);
				if (!IsDisposed)
					_status.Text = "Example closed.";
				return;
			}
			using var example = entry.CreateExample();
			if (example is PopupDropDown popup) {
				_openPopup = popup;
				using var popupState = Tools.Scope.ExecuteOnDispose(() => _openPopup = null);
				var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				popup.Closed += (_, _) => closed.TrySetResult();
				popup.Show(_openWindow, new Point(0, _openWindow.Height));
				await closed.Task;
				if (!IsDisposed)
					_status.Text = "Popup closed.";
			} else {
				var form = (Form)example;
				form.Text = entry.ControlType.Name + " — " + form.Text;
				form.StartPosition = FormStartPosition.CenterParent;
				var result = await form.ShowDialogAsync(this);
				if (!IsDisposed)
					_status.Text = "Dialog result: " + result;
			}
		} catch (Exception error) {
			if (!IsDisposed)
				await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private void ReleaseExample() {
		_properties.SelectedObject = null;
		if (_example == null)
			return;
		_preview.Controls.Remove(_example);
		_example.Dispose();
		_example = null;
	}

	private void ShowPropertiesChanged(object sender, EventArgs args) {
		if (_exampleLayout != null)
			_exampleLayout.Panel2Collapsed = !_showProperties.Checked;
	}

	private static IEnumerable<object> Subjects(Control control) {
		yield return control;
		if (control.Tag is Component component)
			yield return component;
		if (control is ToolStrip strip)
			foreach (var item in strip.Items.Cast<ToolStripItem>().SelectMany(StripItems))
				yield return item;
		foreach (var subject in control.Controls.Cast<Control>().SelectMany(Subjects))
			yield return subject;
	}

	private static void SelectContainingTabs(Control control) {
		for (var current = control; current != null; current = current.Parent)
			if (current is TabPage { Parent: TabControl tabs } page)
				tabs.SelectedTab = page;
	}

	private static IEnumerable<ToolStripItem> StripItems(ToolStripItem item) {
		yield return item;
		if (item is ToolStripDropDownItem dropdown)
			foreach (var child in dropdown.DropDownItems.Cast<ToolStripItem>().SelectMany(StripItems))
				yield return child;
	}

	private static bool Matches(Type expected, Type actual) {
		if (!expected.IsGenericTypeDefinition)
			return expected.IsAssignableFrom(actual);
		for (var type = actual; type != null; type = type.BaseType)
			if (type.IsGenericType && type.GetGenericTypeDefinition() == expected)
				return true;
		return false;
	}
}
