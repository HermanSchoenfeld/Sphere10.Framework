// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public abstract class ApplicationMenuItemBase : SharedUI.ApplicationMenuItemBase, IApplicationMenuItem {
	protected ApplicationMenuItemBase()
		: this(null) {
	}

	protected ApplicationMenuItemBase(Image image16x16)
		: this(image16x16, true, true, false) {
	}

	protected ApplicationMenuItemBase(Image image16x16, bool showOnExplorerBar, bool showOnToolBar, bool executeOnLoad) {
		Image16x16 = image16x16;
		ShowOnExplorerBar = showOnExplorerBar;
		ShowOnToolStrip = showOnToolBar;
		ExecuteOnLoad = executeOnLoad;
	}

	public new IApplicationMenu Parent {
		get => (IApplicationMenu)base.Parent;
		set => base.Parent = value;
	}

	public Image Image16x16 { get; set; }

	public bool ShowOnExplorerBar { get; set; }

	public bool ShowOnToolStrip { get; set; }

	public bool ExecuteOnLoad { get; set; }

	public virtual void Dispose() {
		Parent = null;
		if (Image16x16 != null) {
			Image16x16.Dispose();
		}
	}
}

