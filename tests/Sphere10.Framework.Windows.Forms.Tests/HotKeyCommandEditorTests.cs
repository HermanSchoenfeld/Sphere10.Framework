// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class HotKeyCommandEditorTests {
	private const Keys Toggle = GlobalHotKey.WindowsKey | Keys.Alt | Keys.L;
	private const Keys Next = GlobalHotKey.WindowsKey | Keys.Alt | Keys.K;

	private static HotKeyCommand[] Commands { get; } = {
		new HotKeyCommand("toggle", "Turn on or off", Toggle, () => { }) { Group = "General" },
		new HotKeyCommand("next", "Next effect", Next, () => { }) { Group = "General" },
		new HotKeyCommand("effect:Sepia", "Sepia", Keys.None, () => { }) { Group = "Effects" }
	};

	[Test]
	public void AssignmentsKeepOnlyChoicesThatDifferFromTheDefault() {
		var assignments = HotKeyAssignment.Assign(null, Commands[2], Keys.Control | Keys.Alt | Keys.S);
		assignments = HotKeyAssignment.Assign(assignments, Commands[0], Keys.None);
		assignments = HotKeyAssignment.Assign(assignments, Commands[2], Keys.None);

		Assert.That(assignments.Select(assignment => (assignment.Command, assignment.Shortcut)), Is.EqualTo(new[] { ("toggle", Keys.None) }));
		Assert.That(HotKeyAssignment.Resolve(assignments, Commands[0]), Is.EqualTo(Keys.None));
		Assert.That(HotKeyAssignment.Resolve(assignments, Commands[1]), Is.EqualTo(Next));
	}

	[Test]
	public void SharedShortcutsAreReportedAsConflictsOnBothCommands() {
		var assignments = HotKeyAssignment.Assign(null, Commands[2], Toggle);

		var conflicts = HotKeyAssignment.FindConflicts(Commands, assignments);

		Assert.That(conflicts.Keys, Is.EquivalentTo(new[] { "toggle", "effect:Sepia" }));
		Assert.That(conflicts["toggle"], Is.EqualTo(new[] { "Sepia" }));
	}

	[Test]
	public void ListsCommandsByGroupAndEditsTheSelectedShortcut() {
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-30000, -30000), ClientSize = new Size(700, 420) };
		using var editor = new HotKeyCommandEditor { Dock = DockStyle.Fill, Commands = Commands };
		form.Controls.Add(editor);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		var changes = 0;
		editor.AssignmentsChanged += (_, _) => changes++;
		var list = Field<ListView>(editor, "_commandsListView");

		Assert.That(list.Groups.Cast<ListViewGroup>().Select(group => group.Header), Is.EqualTo(new[] { "General", "Effects" }));
		Assert.That(list.Items.Cast<ListViewItem>().Select(item => item.SubItems[1].Text), Is.EqualTo(new[] { "Win+Alt+L", "Win+Alt+K", string.Empty }));
		Assert.That(Field<HotKeyEditor>(editor, "_shortcutEditor").Enabled, Is.False, "Nothing is selected yet.");

		editor.SelectCommand("effect:sepia");
		System.Windows.Forms.Application.DoEvents();
		Assert.That(editor.SelectedCommand, Is.SameAs(Commands[2]));
		Field<Button>(editor, "_defaultButton").PerformClick();
		Assert.That(changes, Is.Zero, "The default is already in force.");

		// The shortcut editor applies at once; Toggle's shortcut now conflicts.
		var shortcutEditor = Field<HotKeyEditor>(editor, "_shortcutEditor");
		shortcutEditor.Shortcut = Toggle;
		typeof(HotKeyCommandEditor).GetMethod("_shortcutEditor_ShortcutChanged", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { shortcutEditor, System.EventArgs.Empty });
		Assert.That(changes, Is.EqualTo(1));
		Assert.That(editor.Assignments.Single().Shortcut, Is.EqualTo(Toggle));
		Assert.That(list.Items[0].SubItems[2].Text, Does.Contain("Sepia"));
		Assert.That(Field<Label>(editor, "_summaryLabel").Text, Does.Contain("2 shortcuts"));

		Field<Button>(editor, "_clearButton").PerformClick();
		Assert.That(changes, Is.EqualTo(2));
		Assert.That(editor.Assignments, Is.Empty, "Clearing returns Sepia to its default of no shortcut.");

		editor.ShowRegistrationErrors(new Dictionary<string, string> { ["next"] = "Next effect (Win+Alt+K): the shortcut is in use by another program." });
		Assert.That(list.Items[1].SubItems[2].Text, Is.EqualTo("the shortcut is in use by another program."));
		Assert.That(Field<Label>(editor, "_summaryLabel").Text, Does.StartWith("One shortcut"));
	}

	private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
}
