// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes the padlock's retained choice without treating ordinary button commands as edits.</summary>
public sealed class PadLockButtonControlStateEventProvider : ControlStateEventProviderBase<PadLockButton> {
	/// <summary>Attaches to the completed lock-state notification.</summary>
	protected override void RegisterStateChangedListener(PadLockButton control, EventHandlerEx eventHandler) {
		control.PadLockStateChanged += LockStateChanged;
	}

	/// <summary>Releases the same lock-state listener.</summary>
	protected override void DeregisterStateChangedListener(PadLockButton control, EventHandlerEx eventHandler) {
		control.PadLockStateChanged -= LockStateChanged;
	}

	// The control has already stored the new state before notifying observers.
	private void LockStateChanged(PadLockButton control, PadLockButton.PadLockState state) {
		NotifyStateChanged();
	}
}
