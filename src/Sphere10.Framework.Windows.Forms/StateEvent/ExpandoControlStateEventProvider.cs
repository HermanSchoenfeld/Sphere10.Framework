// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Combines child input with the ExplorerBar group's retained collapsed or expanded state.</summary>
public sealed class ExpandoControlStateEventProvider : ContainerControlStateEventProviderBase<Expando> {
	private bool _collapsed;

	/// <summary>Follows native child additions and the group's completed state event.</summary>
	protected override void RegisterStateChangedListener(Expando control, EventHandlerEx eventHandler) {
		_collapsed = control.Collapsed;
		base.RegisterStateChangedListener(control, eventHandler);
		control.StateChanged += ExpandoStateChanged;
	}

	/// <summary>Detaches group and child observation without disposing their controls.</summary>
	protected override void DeregisterStateChangedListener(Expando control, EventHandlerEx eventHandler) {
		control.StateChanged -= ExpandoStateChanged;
		base.DeregisterStateChangedListener(control, eventHandler);
	}

	// The legacy event also reports presentation changes; only the retained collapse choice is an edit.
	private void ExpandoStateChanged(object sender, ExpandoEventArgs args) {
		if (Control == null || _collapsed == Control.Collapsed)
			return;

		_collapsed = Control.Collapsed;
		NotifyStateChanged();
	}
}
