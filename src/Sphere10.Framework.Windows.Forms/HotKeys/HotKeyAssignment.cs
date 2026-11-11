// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A user's choice of shortcut for one <see cref="HotKeyCommand"/>, stored in application settings. Only choices that differ from
/// the command's default are kept, so a changed default reaches users who never changed that shortcut; <see cref="Keys.None"/>
/// records that the user removed a default shortcut.
/// </summary>
[Obfuscation(Exclude = true)]
public class HotKeyAssignment {
	public string Command { get; set; }

	public Keys Shortcut { get; set; }

	/// <summary>The shortcut in force for a command: the user's assignment, or else the command's default.</summary>
	public static Keys Resolve(IEnumerable<HotKeyAssignment> assignments, HotKeyCommand command) {
		Guard.ArgumentNotNull(command, nameof(command));
		return Find(assignments, command.Name)?.Shortcut ?? command.DefaultShortcut;
	}

	/// <summary>
	/// Returns the assignments with <paramref name="command"/> set to <paramref name="shortcut"/>, dropping the entry when it equals
	/// the default. The input is left unchanged.
	/// </summary>
	public static HotKeyAssignment[] Assign(IEnumerable<HotKeyAssignment> assignments, HotKeyCommand command, Keys shortcut) {
		Guard.ArgumentNotNull(command, nameof(command));
		var others = (assignments ?? Enumerable.Empty<HotKeyAssignment>()).Where(assignment => assignment != null && !IsFor(assignment, command.Name));
		return shortcut == command.DefaultShortcut ? others.ToArray() : others.Append(new HotKeyAssignment { Command = command.Name, Shortcut = shortcut }).ToArray();
	}

	/// <summary>
	/// The commands sharing a shortcut with another command, keyed by command name, with the captions of the others; a shortcut
	/// can only be registered once, so all but one of them would fail.
	/// </summary>
	public static IReadOnlyDictionary<string, string[]> FindConflicts(IEnumerable<HotKeyCommand> commands, IEnumerable<HotKeyAssignment> assignments) {
		Guard.ArgumentNotNull(commands, nameof(commands));
		var resolved = commands.Select(command => (Command: command, Shortcut: Resolve(assignments, command))).Where(entry => entry.Shortcut != Keys.None).ToArray();
		var conflicts = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
		foreach (var group in resolved.GroupBy(entry => entry.Shortcut).Where(group => group.Count() > 1))
			foreach (var entry in group)
				conflicts[entry.Command.Name] = group.Where(other => other.Command != entry.Command).Select(other => other.Command.Caption).ToArray();
		return conflicts;
	}

	private static HotKeyAssignment Find(IEnumerable<HotKeyAssignment> assignments, string command)
		=> assignments?.LastOrDefault(assignment => assignment != null && IsFor(assignment, command));

	private static bool IsFor(HotKeyAssignment assignment, string command) => string.Equals(assignment.Command?.Trim(), command, StringComparison.OrdinalIgnoreCase);
}
