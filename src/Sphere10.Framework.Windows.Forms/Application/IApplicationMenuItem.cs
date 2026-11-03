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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public interface IApplicationMenuItem : IDisposable, SharedUI.IApplicationMenuItem {
	new IApplicationMenu Parent { get; set; }
	Image Image16x16 { get; }
	bool ShowOnExplorerBar { get; }
	bool ShowOnToolStrip { get; }
	bool ExecuteOnLoad { get; }

	#region Base Interface Bridges

	SharedUI.IApplicationMenu SharedUI.IApplicationMenuItem.Parent {
		get => Parent;
		set => Parent = value == null ? null : Guard.ArgumentCast<IApplicationMenu>(value, nameof(value));
	}

	string SharedUI.IApplicationMenuItem.Title => this is IWinFormsLinkMenuItem link ? link.Text : string.Empty;

	#endregion
}
