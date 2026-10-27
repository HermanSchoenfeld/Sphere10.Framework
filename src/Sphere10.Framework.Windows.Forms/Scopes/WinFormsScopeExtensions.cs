// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

// ReSharper disable CheckNamespace
namespace Tools;

/// <summary>Windows Forms scope factories. Import the Tools namespace to discover these static extension members.</summary>
public static class WinFormsScopeExtensions {
	extension(Scope) {
		public static ControlRenderingScope EnterRenderingScope(Control control, bool repaintImmediately = true) => control.EnterRenderingScope(repaintImmediately);

		public static WindowHighlightScope EnterHighlightScope(Rectangle bounds, Color color, int thickness = 3) => new(bounds, color, thickness);

		public static UpdateScope EnterUpdateScope(IUpdatable updateable, FinishedUpdateBehaviour behaviour = FinishedUpdateBehaviour.Default) => new(updateable, behaviour);
	}
}
