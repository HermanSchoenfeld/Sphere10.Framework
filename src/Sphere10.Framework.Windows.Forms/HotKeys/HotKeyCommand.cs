// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>An operation the user can bind to a global shortcut, as listed by <see cref="HotKeyCommandEditor"/> and registered by <see cref="GlobalHotKeySet"/>.</summary>
/// <param name="Name">The stable identifier stored in <see cref="HotKeyAssignment.Command"/>; compared ignoring case.</param>
/// <param name="Caption">The text the editor shows and registration errors name.</param>
/// <param name="DefaultShortcut">The shortcut used until the user assigns another; <see cref="Keys.None"/> for none.</param>
/// <param name="Execute">Runs on the UI thread when the shortcut is pressed.</param>
public sealed record HotKeyCommand(string Name, string Caption, Keys DefaultShortcut, Action Execute) {
	/// <summary>The heading the editor lists the command under, for example "General" or "Effects".</summary>
	public string Group { get; init; } = string.Empty;
}
