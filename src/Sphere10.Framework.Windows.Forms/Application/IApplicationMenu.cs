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

public interface IWinFormsApplicationMenu : IDisposable, IApplicationMenu<IWinFormsApplicationMenuItem> {
	new IWinFormsApplicationBlock Parent { get; set; }
	new string Text { get; }
	Image Image32x32 { get; }
	bool ShowInMenuStrip { get; }

	#region Base Interface Bridges

	IApplicationBlock IApplicationMenu.Parent {
		get => Parent;
		set => Parent = value == null ? null : Guard.ArgumentCast<IWinFormsApplicationBlock>(value, nameof(value));
	}

	string IApplicationMenu.Text => Text;

	#endregion
}
