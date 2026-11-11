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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Lists an application's <see cref="HotKeyCommand"/>s by group with their shortcuts, and edits the selected command's shortcut
/// with a <see cref="HotKeyEditor"/>. Edits apply at once: <see cref="Assignments"/> changes and <see cref="AssignmentsChanged"/>
/// is raised, after which the application saves the assignments and registers them, for example with
/// <see cref="GlobalHotKeySet.Apply(IEnumerable{HotKeyCommand}, IEnumerable{HotKeyAssignment})"/>, and passes the set's
/// <see cref="GlobalHotKeySet.ErrorsByKey"/> to <see cref="ShowRegistrationErrors"/>. Shortcuts shared by two commands are
/// marked as conflicts.
/// </summary>
public partial class HotKeyCommandEditor : UserControlEx {
	/// <summary>Raised after the user changed a shortcut.</summary>
	public event EventHandler AssignmentsChanged;

	private static readonly Color ProblemColor = Color.Firebrick;
	private static readonly Color SuccessColor = Color.ForestGreen;
	private HotKeyCommand[] _commands = Array.Empty<HotKeyCommand>();
	private HotKeyAssignment[] _assignments = Array.Empty<HotKeyAssignment>();
	private IReadOnlyDictionary<string, string> _registrationErrors = new Dictionary<string, string>();
	// The selection is kept here as well as in the list, whose selection is unreliable until its handle exists (on a hidden tab).
	private string _selectedName;
	private bool _showingSelection;
	private bool _selectingRows;

	public HotKeyCommandEditor() {
		InitializeComponent();
		ShowSelection();
	}

	/// <summary>The commands to list, in display order within their groups.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HotKeyCommand[] Commands {
		get => (HotKeyCommand[])_commands.Clone();
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			Guard.Argument(value.All(command => command != null && !string.IsNullOrWhiteSpace(command.Name)), nameof(value), "Every command needs a name.");
			Guard.Argument(value.Select(command => command.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.Length, nameof(value), "Command names must be unique.");
			var selected = SelectedCommand?.Name;
			_commands = (HotKeyCommand[])value.Clone();
			RebuildRows(selected);
		}
	}

	/// <summary>The user's shortcut choices; only choices that differ from a command's default are kept.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HotKeyAssignment[] Assignments {
		get => _assignments.Select(assignment => new HotKeyAssignment { Command = assignment.Command, Shortcut = assignment.Shortcut }).ToArray();
		set {
			_assignments = (value ?? Array.Empty<HotKeyAssignment>()).Where(assignment => assignment != null).ToArray();
			UpdateRows();
			ShowSelection();
		}
	}

	/// <summary>The command whose row is selected, or null.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HotKeyCommand SelectedCommand => _selectedName == null ? null : _commands.FirstOrDefault(command => string.Equals(command.Name, _selectedName, StringComparison.OrdinalIgnoreCase));

	/// <summary>Shows registration problems by command name, such as <see cref="GlobalHotKeySet.ErrorsByKey"/>; empty or null clears them.</summary>
	public void ShowRegistrationErrors(IReadOnlyDictionary<string, string> errors) {
		_registrationErrors = errors == null ? new Dictionary<string, string>() : new Dictionary<string, string>(errors, StringComparer.OrdinalIgnoreCase);
		UpdateRows();
	}

	/// <summary>Selects a command's row, or clears the selection for null.</summary>
	public void SelectCommand(string name) {
		_selectedName = name != null && _commands.Any(command => string.Equals(command.Name, name, StringComparison.OrdinalIgnoreCase)) ? name : null;
		_selectingRows = true;
		try {
			foreach (ListViewItem item in _commandsListView.Items) {
				item.Selected = _selectedName != null && string.Equals(((HotKeyCommand)item.Tag).Name, _selectedName, StringComparison.OrdinalIgnoreCase);
				if (item.Selected)
					item.EnsureVisible();
			}
		} finally {
			_selectingRows = false;
		}
		ShowSelection();
	}

	protected override void OnLayout(LayoutEventArgs e) {
		base.OnLayout(e);
		FitStatusColumn();
	}

	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		SizeColumns();
	}

	private void RebuildRows(string selected) {
		_commandsListView.BeginUpdate();
		try {
			_commandsListView.Items.Clear();
			_commandsListView.Groups.Clear();
			var groups = new Dictionary<string, ListViewGroup>(StringComparer.OrdinalIgnoreCase);
			foreach (var command in _commands) {
				var groupName = command.Group ?? string.Empty;
				if (!groups.TryGetValue(groupName, out var group)) {
					group = new ListViewGroup(groupName, groupName.Length == 0 ? "Commands" : groupName);
					groups.Add(groupName, group);
					_commandsListView.Groups.Add(group);
				}
				_commandsListView.Items.Add(new ListViewItem(new[] { command.Caption ?? command.Name, string.Empty, string.Empty }, group) { Tag = command, UseItemStyleForSubItems = false });
			}
		} finally {
			_commandsListView.EndUpdate();
		}
		UpdateRows();
		SelectCommand(selected);
	}

	// Shortcut text and status for every row, plus the summary line.
	private void UpdateRows() {
		var conflicts = HotKeyAssignment.FindConflicts(_commands, _assignments);
		var problems = 0;
		_commandsListView.BeginUpdate();
		try {
			foreach (ListViewItem item in _commandsListView.Items) {
				var command = (HotKeyCommand)item.Tag;
				var shortcut = HotKeyAssignment.Resolve(_assignments, command);
				item.SubItems[1].Text = shortcut == Keys.None ? string.Empty : HotKeyEditor.Describe(shortcut);
				var status = conflicts.TryGetValue(command.Name, out var others)
					? "Also assigned to " + string.Join(", ", others)
					: _registrationErrors.TryGetValue(command.Name, out var error) ? ErrorReason(error) : string.Empty;
				item.SubItems[2].Text = status;
				item.SubItems[2].ForeColor = ProblemColor;
				if (status.Length > 0)
					problems++;
			}
		} finally {
			_commandsListView.EndUpdate();
		}
		_summaryLabel.Text = problems == 0 ? "All shortcuts are registered." : problems == 1 ? "One shortcut is unavailable; see its status." : $"{problems} shortcuts are unavailable; see their status.";
		_summaryLabel.ForeColor = problems == 0 ? SuccessColor : ProblemColor;
	}

	// GlobalHotKeySet errors read "Caption (Shortcut): reason"; the row already shows the caption and shortcut.
	private static string ErrorReason(string error) {
		var separator = error.IndexOf("): ", StringComparison.Ordinal);
		return separator >= 0 ? error[(separator + 3)..] : error;
	}

	private void ShowSelection() {
		var command = SelectedCommand;
		_showingSelection = true;
		try {
			_shortcutEditor.Shortcut = command == null ? Keys.None : HotKeyAssignment.Resolve(_assignments, command);
		} finally {
			_showingSelection = false;
		}
		_shortcutEditor.Enabled = _clearButton.Enabled = command != null;
		_defaultButton.Enabled = command != null && HotKeyAssignment.Resolve(_assignments, command) != command.DefaultShortcut;
		_shortcutLabel.Text = command == null ? "Select a command to change its shortcut" : (command.Caption ?? command.Name) + ":";
	}

	private void Assign(Keys shortcut) {
		var command = SelectedCommand;
		if (command == null || HotKeyAssignment.Resolve(_assignments, command) == shortcut)
			return;
		_assignments = HotKeyAssignment.Assign(_assignments, command, shortcut);
		UpdateRows();
		ShowSelection();
		AssignmentsChanged?.Invoke(this, EventArgs.Empty);
	}

	private void SizeColumns() {
		_commandColumn.Width = LogicalToDeviceUnits(230);
		_shortcutColumn.Width = LogicalToDeviceUnits(150);
		FitStatusColumn();
	}

	private void FitStatusColumn() {
		if (_commandsListView.IsHandleCreated)
			_statusColumn.Width = Math.Max(LogicalToDeviceUnits(120), _commandsListView.ClientSize.Width - _commandColumn.Width - _shortcutColumn.Width - SystemInformation.VerticalScrollBarWidth);
	}

	private void _commandsListView_SelectedIndexChanged(object sender, EventArgs e) {
		if (_selectingRows)
			return;
		_selectedName = _commandsListView.SelectedItems.Count == 1 ? ((HotKeyCommand)_commandsListView.SelectedItems[0].Tag).Name : null;
		ShowSelection();
	}

	private void _shortcutEditor_ShortcutChanged(object sender, EventArgs e) {
		if (!_showingSelection)
			Assign(_shortcutEditor.Shortcut);
	}

	private void _clearButton_Click(object sender, EventArgs e) => Assign(Keys.None);

	private void _defaultButton_Click(object sender, EventArgs e) {
		var command = SelectedCommand;
		if (command != null)
			Assign(command.DefaultShortcut);
	}
}
