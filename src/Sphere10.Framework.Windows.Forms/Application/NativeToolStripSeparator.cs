// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Marks a borrowed native separator for the existing command composition rules.</summary>
internal sealed class NativeToolStripSeparator : NativeToolStripCommand, IApplicationMenuSeparator {
	public NativeToolStripSeparator(ToolStripItem item, IWinFormsApplicationMenu parent = null)
		: base(item, parent) {
	}
}
