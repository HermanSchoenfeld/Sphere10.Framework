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

/// <summary>One shortcut a <see cref="GlobalHotKeySet"/> keeps registered.</summary>
/// <param name="Key">Identifies the binding across applications of the set, for example a command or item name; compared ignoring case.</param>
/// <param name="Caption">Names the shortcut in registration errors.</param>
/// <param name="Shortcut">The key combination; <see cref="Keys.None"/> releases the binding's registration.</param>
/// <param name="Action">Runs on the UI thread when the shortcut is pressed; its exceptions are ignored.</param>
public sealed record GlobalHotKeyBinding(string Key, string Caption, Keys Shortcut, Action Action);
