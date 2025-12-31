// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public interface IMenu : IDisposable {
	IApplicationBlock Parent { get; set; }

	string Text { get; }

	IMenuItem[] Items { get; }

	Image Image32x32 { get; }

	bool ShowInMenuStrip { get; }

}

