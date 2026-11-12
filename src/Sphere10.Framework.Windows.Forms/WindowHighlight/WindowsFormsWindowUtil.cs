// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class WinForms {

	/// <summary>Discovers caller-owned desktop window UI helpers. Highlight scopes remain available through Tools.Scope.</summary>
	public static class Windowing {
		/// <summary>Creates a one-use picker on the dispatcher's UI thread. The dispatcher must have a live handle; dispose the picker on that same thread after PickAsync completes.</summary>
		public static WindowPicker CreatePicker(Control dispatcher) => new WindowPicker(dispatcher);
	}
}
