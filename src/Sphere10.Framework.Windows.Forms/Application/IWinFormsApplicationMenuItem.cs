// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public interface IWinFormsApplicationMenuItem : IDisposable, IApplicationMenuItem {
	IWinFormsApplicationMenu Parent { get; set; }
	Image Image16x16 { get; }
	bool ShowOnExplorerBar { get; }
	bool ShowOnToolStrip { get; }
	bool ExecuteOnLoad { get; }

	string IApplicationMenuItem.Title => this is IWinFormsLinkMenuItem link ? link.Text : string.Empty;
}
