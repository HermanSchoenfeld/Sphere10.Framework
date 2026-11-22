// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Covers the menu that mirrors toolbar commands as they are when it opens.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ToolStripCommandMenuTests {

	[Test]
	public void EntriesMirrorCurrentCommandsAndClickTheirOwners() {
		using var toolbar = new ToolStrip();
		var clicks = new List<string>();
		var refresh = new ToolStripButton("Refresh") { Name = "refresh" };
		refresh.Click += (_, _) => clicks.Add("refresh");
		var pause = new ToolStripButton("Pause Display") { CheckOnClick = true };
		var hidden = new ToolStripButton("Hidden") { Visible = false };
		var disabled = new ToolStripButton("Disabled") { Enabled = false };
		var rules = new ToolStripDropDownButton("Rules");
		var first = new ToolStripMenuItem("First Rule") { Checked = true };
		first.Click += (_, _) => clicks.Add("first rule");
		rules.DropDownItems.Add(first);
		var mode = new ToolStripChoiceSelector();
		mode.SetItems(new object[] { "Call Tree", "All Nodes" });
		mode.SelectedIndex = 0;
		toolbar.Items.AddRange(new ToolStripItem[] { new ToolStripSeparator(), refresh, pause, new ToolStripSeparator(), new ToolStripSeparator(), hidden, disabled,
			new ToolStripLabel("Group By"), rules, mode, new ToolStripSeparator() });
		using var menu = new ToolStripCommandMenu("View");
		Assert.That(menu.DropDownItems.Cast<ToolStripItem>().Single().Enabled, Is.False, "An unopened menu holds one disabled entry so it can open.");
		foreach (var command in toolbar.Items.Cast<ToolStripItem>().Where(item => !ReferenceEquals(item, mode)))
			menu.AddCommand(command);
		menu.AddCommand(mode, "CPU Sample View");
		menu.RefreshCommands();
		string[] Captions() => menu.DropDownItems.Cast<ToolStripItem>().Select(entry => entry is ToolStripSeparator ? "-" : entry.Text).ToArray();
		Assert.That(Captions(), Is.EqualTo(new[] { "Refresh", "Pause Display", "-", "Disabled", "Rules", "-", "CPU Sample View" }),
			"Hidden commands, labels and leading or repeated separators have no entry.");
		var entries = menu.DropDownItems.OfType<ToolStripMenuItem>().ToDictionary(entry => entry.Text);
		Assert.That(entries["Disabled"].Enabled, Is.False);
		Assert.That(entries["Pause Display"].Checked, Is.False);

		entries["Refresh"].PerformClick();
		entries["Pause Display"].PerformClick();
		Assert.That(clicks, Is.EqualTo(new[] { "refresh" }));
		Assert.That(pause.Checked, Is.True, "Choosing an entry clicks the command, which keeps its own state.");

		var rule = entries["Rules"].DropDownItems.OfType<ToolStripMenuItem>().Single();
		Assert.That(rule.Text == "First Rule" && rule.Checked, Is.True);
		rule.PerformClick();
		Assert.That(clicks, Is.EqualTo(new[] { "refresh", "first rule" }));
		var choices = entries["CPU Sample View"].DropDownItems.OfType<ToolStripMenuItem>().ToArray();
		Assert.That(choices.Select(choice => (choice.Text, choice.Checked)), Is.EqualTo(new[] { ("Call Tree", true), ("All Nodes", false) }));
		choices[1].PerformClick();
		Assert.That(mode.SelectedIndex, Is.EqualTo(1), "A mirrored choice selects through its owner.");

		// The next opening shows the commands as they are then.
		refresh.Text = "Refresh Now";
		disabled.Enabled = true;
		hidden.Visible = true;
		toolbar.Items.Remove(hidden);
		menu.RefreshCommands();
		Assert.That(Captions(), Is.EqualTo(new[] { "Refresh Now", "Pause Display", "-", "Hidden", "Disabled", "Rules", "-", "CPU Sample View" }));
		Assert.That(menu.DropDownItems.OfType<ToolStripMenuItem>().Single(entry => entry.Text == "Pause Display").Checked, Is.True);
		hidden.Dispose();
	}

	[Test]
	public void AMenuWithoutAvailableCommandsSaysSoAndOwnsNoCommand() {
		var command = new ToolStripButton("Stop") { Visible = false };
		var menu = new ToolStripCommandMenu("Capture") { EmptyText = "Nothing To Do" };
		menu.AddSeparator();
		menu.AddCommand(command);
		menu.AddSeparator();
		menu.RefreshCommands();
		var empty = menu.DropDownItems.Cast<ToolStripItem>().Single();
		Assert.That(empty.Text == "Nothing To Do" && !empty.Enabled, Is.True);
		Assert.That(menu.Commands, Is.EqualTo(new[] { command }));
		menu.Dispose();
		Assert.That(command.IsDisposed, Is.False, "Commands are borrowed.");
		command.Dispose();
	}
}
