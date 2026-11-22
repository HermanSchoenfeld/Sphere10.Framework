// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes accepted entity identity even when two choices have the same displayed caption.</summary>
public sealed class VisualGridDropDownControlStateEventProvider : ControlStateEventProviderBase<VisualGridDropDown> {
	private object _entity;

	/// <summary>Uses completed semantic grid state rather than native placeholder indices or intermediate legacy events.</summary>
	protected override void RegisterStateChangedListener(VisualGridDropDown control, EventHandlerEx eventHandler) {
		_entity = control.SelectedEntity;
		control.VisualGrid.StateChanged += GridStateChanged;
		control.VisualGrid.DataBindingComplete += GridDataBindingComplete;
	}

	/// <summary>Detaches the accepted-entity listener.</summary>
	protected override void DeregisterStateChangedListener(VisualGridDropDown control, EventHandlerEx eventHandler) {
		control.VisualGrid.StateChanged -= GridStateChanged;
		control.VisualGrid.DataBindingComplete -= GridDataBindingComplete;
		_entity = null;
	}

	// A completed user choice is one final reference even when legacy callbacks passed through null first.
	private void GridStateChanged() {
		if (Control == null || ReferenceEquals(_entity, Control.SelectedEntity))
			return;

		_entity = Control.SelectedEntity;
		NotifyStateChanged();
	}

	// Automatic snapshot remapping updates identity without becoming an edited reference value.
	private void GridDataBindingComplete(VisualGrid grid) {
		if (Control != null)
			_entity = Control.SelectedEntity;
	}
}
