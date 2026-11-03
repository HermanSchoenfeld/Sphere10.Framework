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
using System.Reflection;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes completed values and choices of standard editable native controls.</summary>
public class CommonControlStateEventProvider : ControlStateEventProviderBase<Control> {
	private EventHandlerEx _stateChanged;
	private (int Index, string Text) _selection;
	private Dictionary<int, (object Item, CheckState State)> _pendingCheckChanges;
	private bool _checkNotificationPosted;

	/// <summary>Attaches stable handlers to the control's value events.</summary>
	protected override void RegisterStateChangedListener(Control control, EventHandlerEx eventHandler) {
		_stateChanged = eventHandler;
		if (control is ComboBox comboBox)
			_selection = (comboBox.SelectedIndex, comboBox.Text);
		else if (control is DomainUpDown domainUpDown)
			_selection = (domainUpDown.SelectedIndex, domainUpDown.Text);

		TypeSwitch.For(control,
			TypeSwitch.Case<NumericUpDown>(item => item.ValueChanged += ControlChanged),
			TypeSwitch.Case<TextBoxBase>(item => {
				var numericEvent = GetNumericValueChangedEvent(item);
				if (numericEvent != null)
					numericEvent.AddEventHandler(item, (EventHandler)ControlChanged);
				else
					item.TextChanged += ControlChanged;
			}),
			TypeSwitch.Case<ComboBox>(item => {
				item.SelectedIndexChanged += ControlChanged;
				item.TextChanged += ControlChanged;
			}),
			TypeSwitch.Case<RadioButton>(item => item.CheckedChanged += ControlChanged),
			TypeSwitch.Case<CheckBox>(item => item.CheckStateChanged += ControlChanged),
			TypeSwitch.Case<CheckedListBox>(item => {
				item.SelectedIndexChanged += ControlChanged;
				item.ItemCheck += ItemCheck;
			}),
			TypeSwitch.Case<DateTimePicker>(item => item.ValueChanged += ControlChanged),
			TypeSwitch.Case<ListBox>(item => item.SelectedIndexChanged += ControlChanged),
			TypeSwitch.Case<MonthCalendar>(item => item.DateChanged += DateChanged),
			TypeSwitch.Case<TrackBar>(item => item.ValueChanged += ControlChanged),
			TypeSwitch.Case<ScrollBar>(item => item.ValueChanged += ControlChanged),
			TypeSwitch.Case<DomainUpDown>(item => {
				item.SelectedItemChanged += ControlChanged;
				item.TextChanged += ControlChanged;
			}),
			TypeSwitch.Case<PropertyGrid>(item => item.PropertyValueChanged += PropertyValueChanged),
			TypeSwitch.Default(() => throw new SoftwareException($"Control '{control.GetType().Name}' is not supported"))
		);
	}

	/// <summary>Removes the matching handlers and drops queued notifications from the old attachment.</summary>
	protected override void DeregisterStateChangedListener(Control control, EventHandlerEx eventHandler) {
		TypeSwitch.For(control,
			TypeSwitch.Case<NumericUpDown>(item => item.ValueChanged -= ControlChanged),
			TypeSwitch.Case<TextBoxBase>(item => {
				var numericEvent = GetNumericValueChangedEvent(item);
				if (numericEvent != null)
					numericEvent.RemoveEventHandler(item, (EventHandler)ControlChanged);
				else
					item.TextChanged -= ControlChanged;
			}),
			TypeSwitch.Case<ComboBox>(item => {
				item.SelectedIndexChanged -= ControlChanged;
				item.TextChanged -= ControlChanged;
			}),
			TypeSwitch.Case<RadioButton>(item => item.CheckedChanged -= ControlChanged),
			TypeSwitch.Case<CheckBox>(item => item.CheckStateChanged -= ControlChanged),
			TypeSwitch.Case<CheckedListBox>(item => {
				item.SelectedIndexChanged -= ControlChanged;
				item.ItemCheck -= ItemCheck;
			}),
			TypeSwitch.Case<DateTimePicker>(item => item.ValueChanged -= ControlChanged),
			TypeSwitch.Case<ListBox>(item => item.SelectedIndexChanged -= ControlChanged),
			TypeSwitch.Case<MonthCalendar>(item => item.DateChanged -= DateChanged),
			TypeSwitch.Case<TrackBar>(item => item.ValueChanged -= ControlChanged),
			TypeSwitch.Case<ScrollBar>(item => item.ValueChanged -= ControlChanged),
			TypeSwitch.Case<DomainUpDown>(item => {
				item.SelectedItemChanged -= ControlChanged;
				item.TextChanged -= ControlChanged;
			}),
			TypeSwitch.Case<PropertyGrid>(item => item.PropertyValueChanged -= PropertyValueChanged)
		);
		_stateChanged = null;
		_selection = default;
		_pendingCheckChanges = null;
		_checkNotificationPosted = false;
	}

	// Every closed NumericBoxBase<T> publishes ValueChanged after parsing; raw TextChanged is earlier.
	private static EventInfo GetNumericValueChangedEvent(TextBoxBase control) {
		return control.GetType().IsSubtypeOfGenericType(typeof(NumericBoxBase<>), out var numericType)
			? numericType.GetEvent(nameof(NumericBoxBase<int>.ValueChanged))
			: null;
	}

	// Choice controls expose both text and index events; one completed state is reported once.
	private void ControlChanged(object sender, EventArgs args) {
		if (sender is ComboBox comboBox) {
			var selection = (comboBox.SelectedIndex, comboBox.Text);
			if (_selection == selection)
				return;
			_selection = selection;
		} else if (sender is DomainUpDown domainUpDown) {
			var selection = (domainUpDown.SelectedIndex, domainUpDown.Text);
			if (_selection == selection)
				return;
			_selection = selection;
		}
		_stateChanged?.Invoke();
	}

	// Calendar ranges are committed before this native event is raised.
	private void DateChanged(object sender, DateRangeEventArgs args) {
		_stateChanged?.Invoke();
	}

	// The PropertyGrid has already applied and validated the edited property value.
	private void PropertyValueChanged(object sender, PropertyValueChangedEventArgs args) {
		_stateChanged?.Invoke();
	}

	// ItemCheck precedes the BCL mutation; scope completion or one UI post observes the final value.
	private void ItemCheck(object sender, ItemCheckEventArgs args) {
		if (args.NewValue == args.CurrentValue)
			return;
		var control = (CheckedListBox)sender;
		if (NotifyUpdatingOwners(control))
			return;

		// No native edit can occur before a dispatcher exists, and initialization must not create a handle.
		var dispatcher = (Control)control;
		while (dispatcher != null && !dispatcher.IsHandleCreated)
			dispatcher = dispatcher.Parent;
		if (dispatcher == null || dispatcher.IsDisposed || dispatcher.Disposing)
			return;

		_pendingCheckChanges ??= new Dictionary<int, (object, CheckState)>();
		_pendingCheckChanges.TryAdd(args.Index, (control.Items[args.Index], args.CurrentValue));
		if (_checkNotificationPosted)
			return;
		_checkNotificationPosted = true;
		var changes = _pendingCheckChanges;
		dispatcher.BeginInvoke(() => PublishCheckedState(control, changes));
	}

	// Mark existing scope owners without copying the pre-mutation checkbox value into their models.
	private static bool NotifyUpdatingOwners(Control control) {
		var updating = false;
		for (var current = control.Parent; current != null; current = current.Parent) {
			if (current is UserControlEx userControl && userControl.Updating) {
				userControl.NotifyStateChanged();
				updating = true;
			} else if (current is FormEx form && form.Updating) {
				form.NotifyStateChanged();
				updating = true;
			}
		}
		return updating;
	}

	// Cancelled checks and stale attachments never turn a queued callback into a false edit.
	private void PublishCheckedState(CheckedListBox control, Dictionary<int, (object Item, CheckState State)> changes) {
		if (!ReferenceEquals(Control, control) || !ReferenceEquals(_pendingCheckChanges, changes) || control.IsDisposed || control.Disposing)
			return;

		_checkNotificationPosted = false;
		var changed = changes.Any(pair => pair.Key < control.Items.Count
			&& ReferenceEquals(control.Items[pair.Key], pair.Value.Item) && control.GetItemCheckState(pair.Key) != pair.Value.State);
		changes.Clear();
		if (changed)
			_stateChanged?.Invoke();
	}
}
