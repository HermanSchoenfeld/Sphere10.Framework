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

/// <summary>Combines child state notifications while retaining ownership only of their providers.</summary>
public class ContainerControlStateEventProviderBase<TControl> : ControlStateEventProviderBase<TControl>
	where TControl : Control {
	private readonly IDictionary<Control, IControlStateEventProvider> _childEventProviders;
	private EventHandlerEx _childStateChanged;

	/// <summary>Creates an empty observer; child subscriptions begin with SetControl.</summary>
	public ContainerControlStateEventProviderBase() => _childEventProviders = new Dictionary<Control, IControlStateEventProvider>();

	/// <summary>Observes existing children and follows subsequent additions and removals.</summary>
	protected override void RegisterStateChangedListener(TControl control, EventHandlerEx eventHandler) {
		_childStateChanged = eventHandler;
		foreach (var child in GetChildControls(control))
			TryObserveChild(child);
		control.ControlAdded += ChildAdded;
		control.ControlRemoved += ChildRemoved;
	}

	/// <summary>Stops following the container and clears every owned child observer.</summary>
	protected override void DeregisterStateChangedListener(TControl control, EventHandlerEx eventHandler) {
		control.ControlAdded -= ChildAdded;
		control.ControlRemoved -= ChildRemoved;
		foreach (var child in _childEventProviders.Keys.ToArray())
			TryUnobserveChild(child);
		_childStateChanged = null;
	}

	/// <summary>Returns the container's state-bearing child candidates.</summary>
	protected virtual IEnumerable<Control> GetChildControls(TControl control) => control.Controls.Cast<Control>();

	// Follow runtime parenting without retaining anonymous delegates on former owners.
	private void ChildAdded(object sender, ControlEventArgs args) => TryObserveChild(args.Control);

	// A child's provider is unique to this attachment; controls remain borrowed.
	private bool TryObserveChild(Control control) {
		if (control == null || control.IsDisposed || !ShouldObserveChildControl(control) || _childEventProviders.ContainsKey(control)
			|| !ControlStateEventProviderManager.Instance.TryGetControlStateProvider(control.GetType(), out var provider))
			return false;

		provider.SetControl(control);
		provider.StateChanged += _childStateChanged;
		control.Disposed += ChildDisposed;
		_childEventProviders.Add(control, provider);
		return true;
	}

	/// <summary>Lets an owning composite replace broad child observation with its own semantic events.</summary>
	protected virtual bool ShouldObserveChildControl(Control control) => true;

	// Reparented children stop contributing to their previous container immediately.
	private void ChildRemoved(object sender, ControlEventArgs args) => TryUnobserveChild(args.Control);

	// Remove the exact child, including listeners retained by nested containers.
	private void TryUnobserveChild(Control control) {
		if (!_childEventProviders.Remove(control, out var provider))
			return;

		control.Disposed -= ChildDisposed;
		provider.StateChanged -= _childStateChanged;
		provider.Clear();
	}

	// Also release disposed children whose specialized parent does not raise ControlRemoved.
	private void ChildDisposed(object sender, EventArgs args) => TryUnobserveChild((Control)sender);
}
