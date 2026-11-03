// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes one borrowed control and detaches its listeners when cleared or disposed.</summary>
public abstract class ControlStateEventProviderBase<TControl> : IControlStateEventProvider<TControl>
	where TControl : Control {
	/// <summary>Reports a completed change to the observed control's editable state.</summary>
	public event EventHandlerEx StateChanged;

	/// <summary>The native control type accepted by this provider.</summary>
	public Type ControlType {
		get {
			return typeof(TControl);
		}
	}

	/// <summary>The borrowed control currently observed, or null after cleanup.</summary>
	public TControl Control {
		get; private set;
	}

	/// <summary>Attaches to one compatible control, clearing any previous attachment first.</summary>
	public void SetControl(Control control) {
		Guard.ArgumentCast<TControl>(control, out var typedControl, nameof(control));
		if (ReferenceEquals(Control, typedControl))
			return;

		Clear();
		Control = typedControl;
		control.Disposed += ControlDisposed;
		try {
			RegisterStateChangedListener(typedControl, NotifyStateChanged);
		} catch {
			// A failed attachment must not leave callbacks on a control the caller still owns.
			Clear();
			throw;
		}
	}

	/// <summary>Subscribes to the control's semantic state events.</summary>
	protected abstract void RegisterStateChangedListener(TControl control, EventHandlerEx eventHandler);

	/// <summary>Releases subscriptions without disposing the borrowed control.</summary>
	public void Clear() {
		var control = Control;
		if (control == null)
			return;

		Control = null;
		control.Disposed -= ControlDisposed;
		DeregisterStateChangedListener(control, NotifyStateChanged);
	}

	/// <summary>Removes exactly the listeners installed by the matching attachment.</summary>
	protected abstract void DeregisterStateChangedListener(TControl control, EventHandlerEx eventHandler);

	/// <summary>Lets specialized providers react before public state observers.</summary>
	protected virtual void OnStateChanged() {
	}

	/// <summary>Publishes a completed state change while an attachment remains live.</summary>
	protected void NotifyStateChanged() {
		if (Control == null || Control.IsDisposed || Control.Disposing)
			return;

		OnStateChanged();
		StateChanged?.Invoke();
	}

	// Disposal ends observation but never transfers ownership of the native control.
	private void ControlDisposed(object sender, EventArgs args) {
		Clear();
	}
}
