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

/// <summary>A global shortcut owned and used on its creating UI thread, independently of input hooks.</summary>
public interface IGlobalHotKey : IDisposable {
	event EventHandlerEx Pressed;

	/// <summary>Raised on the owner thread when the held shortcut ends, including unregister and disposal.</summary>
	event EventHandlerEx Released;

	/// <summary>Registers a shortcut, preserving the previous registration on failure. Keys.None disables it.</summary>
	bool TryRegister(Keys shortcut, out string error);

	void Unregister();
}
