// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes tab choices and the editable state of their contents.</summary>
public class TabControlStateEventProvider : ContainerControlStateEventProviderBase<TabControl> {
	private EventHandlerEx _selectionChanged;

	/// <summary>Attaches the tab selection relay alongside child observers.</summary>
	protected override void RegisterStateChangedListener(TabControl control, EventHandlerEx eventHandler) {
		base.RegisterStateChangedListener(control, eventHandler);
		_selectionChanged = eventHandler;
		control.SelectedIndexChanged += SelectedIndexChanged;
	}

	/// <summary>Detaches the selection relay and all nested observers.</summary>
	protected override void DeregisterStateChangedListener(TabControl control, EventHandlerEx eventHandler) {
		control.SelectedIndexChanged -= SelectedIndexChanged;
		base.DeregisterStateChangedListener(control, eventHandler);
		_selectionChanged = null;
	}

	/// <summary>Includes the native pages without observing a page twice.</summary>
	protected override IEnumerable<Control> GetChildControls(TabControl control) {
		return base.GetChildControls(control).Union(control.TabPages.Cast<TabPage>());
	}

	// Native selection is already committed when this event runs.
	private void SelectedIndexChanged(object sender, EventArgs args) {
		_selectionChanged?.Invoke();
	}
}
