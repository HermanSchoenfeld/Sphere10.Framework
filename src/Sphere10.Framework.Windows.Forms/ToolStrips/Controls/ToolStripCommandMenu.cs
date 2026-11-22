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
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A menu whose entries mirror toolbar commands as they are when the menu opens. Each command keeps one owner for its caption,
/// image, state and handler: choosing an entry clicks the command it mirrors.</summary>
/// <remarks>The commands are borrowed. Buttons become entries, drop-down buttons become submenus of their own items, separators stay
/// separators, and labels and hosted controls are left out. A hidden command has no entry and a disabled command has a disabled one.</remarks>
public class ToolStripCommandMenu : ToolStripMenuItem {
	private readonly List<(ToolStripItem Command, string Text)> _commands = new();
	private string _emptyText = "No Commands Available";

	public ToolStripCommandMenu()
		: this(string.Empty) {
	}

	public ToolStripCommandMenu(string text)
		: base(text) {
		// A menu opens only when it has an entry; the mirrored entries replace this one on opening.
		DropDownItems.Add(CreateEmptyEntry());
	}

	/// <summary>The borrowed commands this menu mirrors, in display order.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ToolStripItem[] Commands => _commands.Where(entry => entry.Command != null).Select(entry => entry.Command).ToArray();

	/// <summary>The disabled entry shown while no mirrored command is available.</summary>
	[DefaultValue("No Commands Available")]
	public string EmptyText {
		get => _emptyText;
		set => _emptyText = value ?? string.Empty;
	}

	/// <summary>Adds borrowed commands after those already mirrored.</summary>
	public void AddCommands(params ToolStripItem[] commands) {
		Guard.ArgumentNotNull(commands, nameof(commands));
		foreach (var command in commands)
			AddCommand(command);
	}

	/// <summary>Adds one borrowed command, optionally under a caption of its own, for a command whose text shows a value instead of naming itself.</summary>
	public void AddCommand(ToolStripItem command, string text = null) {
		Guard.ArgumentNotNull(command, nameof(command));
		Guard.Argument(!ReferenceEquals(command, this), nameof(command), "A command menu cannot mirror itself.");
		_commands.Add((command, text));
	}

	/// <summary>Separates the commands added so far from those added next.</summary>
	public void AddSeparator() => _commands.Add((null, null));

	/// <summary>Rebuilds the entries from the commands' current captions, images and states. Opening the menu does this.</summary>
	public void RefreshCommands() {
		foreach (var entry in DropDownItems.Cast<ToolStripItem>().ToArray())
			ReleaseEntry(entry);
		DropDownItems.Clear();
		DropDownItems.AddRange(CreateEntries(_commands).ToArray());
		if (DropDownItems.Count == 0)
			DropDownItems.Add(CreateEmptyEntry());
	}

	protected override void OnDropDownShow(EventArgs args) {
		RefreshCommands();
		base.OnDropDownShow(args);
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var entry in DropDownItems.Cast<ToolStripItem>().ToArray())
				ReleaseEntry(entry);
			_commands.Clear();
		}
		base.Dispose(disposing);
	}

	// Separators group what is available: none leads, trails or repeats.
	private static List<ToolStripItem> CreateEntries(IEnumerable<(ToolStripItem Command, string Text)> commands) {
		var entries = new List<ToolStripItem>();
		foreach (var (command, text) in commands) {
			if (command is { Available: false })
				continue;
			if (command is null or ToolStripSeparator) {
				if (entries.Count > 0 && entries[^1] is not ToolStripSeparator)
					entries.Add(new ToolStripSeparator());
				continue;
			}
			if (CreateEntry(command, text) is { } entry)
				entries.Add(entry);
		}
		if (entries.Count > 0 && entries[^1] is ToolStripSeparator trailing) {
			entries.RemoveAt(entries.Count - 1);
			trailing.Dispose();
		}
		return entries;
	}

	private static ToolStripMenuItem CreateEntry(ToolStripItem command, string text) {
		if (command is not (ToolStripButton or ToolStripDropDownItem))
			return null;
		var entry = new ToolStripMenuItem(Caption(command, text)) { Image = command.Image, Enabled = command.Enabled, ToolTipText = command.ToolTipText, Tag = command };
		if (command is ToolStripDropDownItem menu) {
			// The owner prepares a drop-down as it would for its own opening, so the submenu shows current items.
			ToolStripVisualInheritanceFixer.NotifyDropDownOpening(menu);
			var children = CreateEntries(menu.DropDownItems.Cast<ToolStripItem>().Select(child => (child, (string)null)));
			if (command is ToolStripSplitButton split) {
				// A split button's own action leads its alternatives.
				var primary = new ToolStripMenuItem(Caption(command, text)) { Image = command.Image, Enabled = command.Enabled, Tag = command };
				primary.Click += (_, _) => split.PerformButtonClick();
				if (children.Count > 0)
					children.Insert(0, new ToolStripSeparator());
				children.Insert(0, primary);
			}
			if (children.Count > 0)
				entry.DropDownItems.AddRange(children.ToArray());
			else if (command is ToolStripMenuItem)
				entry.Click += (_, _) => command.PerformClick();
			else
				entry.Enabled = false;
			if (command is ToolStripMenuItem checkable)
				entry.Checked = checkable.Checked;
			return entry;
		}
		entry.Checked = ((ToolStripButton)command).Checked;
		entry.Click += (_, _) => command.PerformClick();
		return entry;
	}

	// An image-only command names itself through its accessible name, the first line of its tool tip or its component name.
	private static string Caption(ToolStripItem command, string text) {
		foreach (var candidate in new[] { text, command.Text, command.AccessibleName, command.ToolTipText?.Split('\n')[0].Trim(), command.Name })
			if (!string.IsNullOrWhiteSpace(candidate))
				return candidate;
		return command.GetType().Name;
	}

	private ToolStripMenuItem CreateEmptyEntry() => new(_emptyText) { Enabled = false };

	// Images and handlers belong to the mirrored commands; the entries only reference them.
	private static void ReleaseEntry(ToolStripItem entry) {
		if (entry is ToolStripDropDownItem menu)
			foreach (var child in menu.DropDownItems.Cast<ToolStripItem>().ToArray())
				ReleaseEntry(child);
		entry.Image = null;
		entry.Tag = null;
		entry.Dispose();
	}
}
