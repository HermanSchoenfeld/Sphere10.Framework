// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes retained toolbar/menu inputs and their dynamic children without treating commands as edited state.</summary>
public sealed class ToolStripControlStateEventProvider : ControlStateEventProviderBase<ToolStrip> {
	private readonly HashSet<ToolStrip> _strips = new();
	private readonly Dictionary<ToolStripItem, ItemObservation> _items = new();
	private bool _reconciling;

	/// <summary>Follows the owning strip and already-created dropdowns without creating native windows.</summary>
	protected override void RegisterStateChangedListener(ToolStrip control, EventHandlerEx eventHandler) {
		ObserveStrip(control);
	}

	/// <summary>Releases every input and dropdown listener while leaving all borrowed components alive.</summary>
	protected override void DeregisterStateChangedListener(ToolStrip control, EventHandlerEx eventHandler) {
		foreach (var strip in _strips.ToArray())
			UnobserveStrip(strip);
	}

	// One observer handles a strip even when a dropdown is shared by more than one item.
	private void ObserveStrip(ToolStrip strip) {
		if (strip.IsDisposed || !_strips.Add(strip))
			return;

		strip.ItemAdded += ItemAdded;
		strip.ItemRemoved += ItemRemoved;
		strip.Layout += StripLayout;
		strip.Disposed += StripDisposed;
		foreach (ToolStripItem item in strip.Items)
			ObserveItem(item);
	}

	// Remove descendants before releasing the strip's own collection handlers.
	private void UnobserveStrip(ToolStrip strip) {
		if (!_strips.Remove(strip))
			return;

		strip.ItemAdded -= ItemAdded;
		strip.ItemRemoved -= ItemRemoved;
		strip.Layout -= StripLayout;
		strip.Disposed -= StripDisposed;
		foreach (var entry in _items.Values.Where(entry => ReferenceEquals(entry.Owner, strip)).ToArray())
			UnobserveItem(entry.Item);
	}

	// Aggregate selectors own their choice; their checked child menu items are presentation only.
	private void ObserveItem(ToolStripItem item) {
		if (item.IsDisposed || _items.ContainsKey(item))
			return;

		var entry = new ItemObservation(item, item.Owner);
		_items.Add(item, entry);
		item.Disposed += ItemDisposed;
		if (item is ToolStripChoiceSelector selector) {
			selector.SelectedIndexChanged += ItemStateChanged;
			return;
		}

		if (item is ToolStripMenuItem menuItem)
			menuItem.CheckStateChanged += ItemStateChanged;
		else if (item is ToolStripButton button)
			button.CheckedChanged += ItemStateChanged;
		else if (item is ToolStripControlHost host && ControlStateEventProviderManager.Instance.TryGetControlStateProvider(host.Control.GetType(), out var provider)) {
			entry.HostProvider = provider;
			entry.HostStateChanged = () => HostedStateChanged(entry);
			provider.SetControl(host.Control);
			provider.StateChanged += entry.HostStateChanged;
		}

		if (item is ToolStripDropDownItem dropdownItem) {
			dropdownItem.DropDownOpening += DropDownOpening;
			ObserveDropDown(entry, dropdownItem);
		}
	}

	// A moved or disposed item immediately stops contributing to its previous owner.
	private void UnobserveItem(ToolStripItem item) {
		if (!_items.Remove(item, out var entry))
			return;

		item.Disposed -= ItemDisposed;
		if (item is ToolStripChoiceSelector selector)
			selector.SelectedIndexChanged -= ItemStateChanged;
		else {
			if (item is ToolStripMenuItem menuItem)
				menuItem.CheckStateChanged -= ItemStateChanged;
			else if (item is ToolStripButton button)
				button.CheckedChanged -= ItemStateChanged;

			if (entry.HostProvider != null) {
				entry.HostProvider.StateChanged -= entry.HostStateChanged;
				entry.HostProvider.Clear();
			}
			if (item is ToolStripDropDownItem dropdownItem)
				dropdownItem.DropDownOpening -= DropDownOpening;
		}
		ReleaseDropDown(entry);
	}

	// HasDropDown avoids creating an empty native menu for every command item.
	private void ObserveDropDown(ItemObservation entry, ToolStripDropDownItem item) {
		var dropdown = item.HasDropDown ? item.DropDown : null;
		if (ReferenceEquals(entry.DropDown, dropdown))
			return;

		ReleaseDropDown(entry);
		entry.DropDown = dropdown;
		if (dropdown != null)
			ObserveStrip(dropdown);
	}

	// Shared dropdowns remain observed until their final owning item is released.
	private void ReleaseDropDown(ItemObservation entry) {
		var dropdown = entry.DropDown;
		entry.DropDown = null;
		if (dropdown != null && !_items.Values.Any(other => ReferenceEquals(other.DropDown, dropdown)))
			UnobserveStrip(dropdown);
	}

	// Native dropdown replacement has no public changed event; reconcile on layout and before interaction.
	private void ReconcileDropDowns() {
		if (_reconciling)
			return;

		_reconciling = true;
		try {
			foreach (var entry in _items.Values.ToArray())
				if (_items.ContainsKey(entry.Item) && entry.Item is ToolStripDropDownItem dropdown && entry.Item is not ToolStripChoiceSelector)
					ObserveDropDown(entry, dropdown);
		} finally {
			_reconciling = false;
		}
	}

	// Check the current ownership graph so a replaced but still borrowed dropdown cannot send stale events.
	private bool IsCurrentStrip(ToolStrip strip) {
		if (ReferenceEquals(strip, Control))
			return true;
		return strip != null && _items.Values.Any(entry => ReferenceEquals(entry.DropDown, strip)
			&& entry.Item is ToolStripDropDownItem dropdown && dropdown.HasDropDown && ReferenceEquals(dropdown.DropDown, strip)
			&& IsCurrentStrip(entry.Item.Owner));
	}

	// Collection changes install the same observers used for initial items.
	private void ItemAdded(object sender, ToolStripItemEventArgs args) {
		ObserveItem(args.Item);
	}

	// Removing an item detaches its complete nested observer graph.
	private void ItemRemoved(object sender, ToolStripItemEventArgs args) {
		UnobserveItem(args.Item);
	}

	// Component disposal also covers items removed by specialized native collections.
	private void ItemDisposed(object sender, EventArgs args) {
		UnobserveItem((ToolStripItem)sender);
	}

	// Only checked/selected input values use this handler; ordinary item clicks have no subscription.
	private void ItemStateChanged(object sender, EventArgs args) {
		ReconcileDropDowns();
		if (sender is ToolStripItem item && _items.ContainsKey(item) && IsCurrentStrip(item.Owner))
			NotifyStateChanged();
	}

	// Hosted native inputs use their existing provider, including its type-specific committed-value rules.
	private void HostedStateChanged(ItemObservation entry) {
		ReconcileDropDowns();
		if (_items.TryGetValue(entry.Item, out var current) && ReferenceEquals(entry, current) && IsCurrentStrip(entry.Item.Owner))
			NotifyStateChanged();
	}

	// A dropdown first populated after attachment is reconciled before a user can edit its inputs.
	private void DropDownOpening(object sender, EventArgs args) {
		ReconcileDropDowns();
	}

	// Layout follows dynamic dropdown creation without forcing lazy command menus into existence.
	private void StripLayout(object sender, LayoutEventArgs args) {
		ReconcileDropDowns();
	}

	// An externally disposed borrowed dropdown ends all of its input observation.
	private void StripDisposed(object sender, EventArgs args) {
		UnobserveStrip((ToolStrip)sender);
	}

	// Holds only subscriptions owned by this provider, never ownership of the item or its hosted control.
	private sealed class ItemObservation {
		// Retain the borrowed item and owner alongside this attachment's listeners.
		public ItemObservation(ToolStripItem item, ToolStrip owner) {
			Item = item;
			Owner = owner;
		}

		public ToolStripItem Item {
			get;
		}

		public ToolStrip Owner {
			get;
		}

		public IControlStateEventProvider HostProvider {
			get; set;
		}

		public EventHandlerEx HostStateChanged {
			get; set;
		}

		public ToolStripDropDown DropDown {
			get; set;
		}
	}
}
