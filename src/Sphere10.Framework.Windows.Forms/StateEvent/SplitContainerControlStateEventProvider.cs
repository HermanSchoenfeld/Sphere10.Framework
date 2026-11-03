// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Combines state from both borrowed split-container panels.</summary>
public class SplitContainerControlStateEventProvider : ControlStateEventProviderBase<Control> {
	private ContainerControlStateEventProvider _panel1Listener;
	private ContainerControlStateEventProvider _panel2Listener;

	/// <summary>Creates independent child observers for the two panels.</summary>
	protected override void RegisterStateChangedListener(Control control, EventHandlerEx eventHandler) {
		Guard.ArgumentCast<SplitContainer>(control, out var splitContainer, nameof(control));
		_panel1Listener = new ContainerControlStateEventProvider();
		_panel2Listener = new ContainerControlStateEventProvider();
		_panel1Listener.SetControl(splitContainer.Panel1);
		_panel2Listener.SetControl(splitContainer.Panel2);
		_panel1Listener.StateChanged += eventHandler;
		_panel2Listener.StateChanged += eventHandler;
	}

	/// <summary>Detaches the relay and clears all nested panel subscriptions.</summary>
	protected override void DeregisterStateChangedListener(Control control, EventHandlerEx eventHandler) {
		if (_panel1Listener != null) {
			_panel1Listener.StateChanged -= eventHandler;
			_panel1Listener.Clear();
			_panel1Listener = null;
		}
		if (_panel2Listener != null) {
			_panel2Listener.StateChanged -= eventHandler;
			_panel2Listener.Clear();
			_panel2Listener = null;
		}
	}
}
