// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A reusable model-aware control with scoped, semantic UI state notifications.</summary>
public class UserControlEx : UserControl, IUpdatable {
	/// <summary>Reports edited UI state after the model copy and template hook complete.</summary>
	public event EventHandlerEx StateChanged;

	private bool _updating;
	private bool _changedDuringUpdateScope;
	private readonly IControlStateEventProvider _stateEventProvider;

	/// <summary>Creates an ordinary control without requiring application services or a native handle.</summary>
	public UserControlEx() {
		Loaded = false;
		_updating = false;
		EnableStateChangeEvent = true;
		Disposables = new Disposables();
		UpdateModelOnStateChanged = true;
		_stateEventProvider = !Tools.Runtime.IsDesignMode ? new ChildStateEventProvider() : new NoOpControlStateProvider();
		_stateEventProvider.StateChanged += NotifyStateChanged;
		_stateEventProvider.SetControl(this);
	}

	/// <summary>Resources owned by this control and released after its native children are disposed.</summary>
	public Disposables Disposables {
		get;
	}

	/// <summary>Enables semantic state notifications after the first UI initialization.</summary>
	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[DefaultValue(true)]
	public bool EnableStateChangeEvent {
		get; set;
	}

	/// <summary>Whether the current model/UI update scope is active.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Updating {
		get {
			return _updating;
		}
	}

	/// <summary>Copies edited UI state to the model before notifying observers.</summary>
	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[DefaultValue(true)]
	public bool UpdateModelOnStateChanged {
		get; set;
	}

	/// <summary>Whether the control's initial model and UI setup has completed.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected bool Loaded {
		get; private set;
	}

	/// <summary>Repaints and copies the model without publishing the resulting UI assignments.</summary>
	public override void Refresh() {
		base.Refresh();
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
			CopyModelToUI();
	}

	/// <summary>Primes and copies the initial model once, before accepting edited state.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (Loaded || DesignMode || Tools.Runtime.IsDesignMode)
			return;

		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			InitializeUIPrimingData();
			CopyModelToUI();
			SetLocalizedText();
		}
		Loaded = true;
	}

	/// <summary>Lets derived controls refresh localized captions.</summary>
	public virtual void SetLocalizedText(CultureInfo culture = null) {
	}

	/// <summary>Coalesces model and UI changes using the requested completion behavior.</summary>
	public UpdateScope EnterUpdateScope(FinishedUpdateBehaviour behaviour = FinishedUpdateBehaviour.Default) {
		return new UpdateScope(this, behaviour);
	}

	/// <summary>Detaches state observers before native children; owned resources retain their existing cleanup order.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing && _stateEventProvider != null) {
			_stateEventProvider.StateChanged -= NotifyStateChanged;
			_stateEventProvider.Clear();
		}
		base.Dispose(disposing);
		if (disposing)
			Disposables?.Dispose();
	}

	/// <summary>Controls automatic observation of direct child controls.</summary>
	protected virtual bool ShouldObserveChildStateChanges(Control child) {
		return true;
	}

	/// <summary>Loads lists or other UI data before the initial model copy.</summary>
	protected virtual void InitializeUIPrimingData() {
	}

	// Nested UpdateScope values leave the first acquisition responsible for completion.
	void IUpdatable.BeginUpdate() {
		if (_updating)
			return;
		_updating = true;
		_changedDuringUpdateScope = false;
	}

	// Keep completion copies inside the gate, then publish the requested notification once.
	void IUpdatable.FinishUpdate(FinishedUpdateBehaviour behaviour) {
		if (!Updating)
			return;

		var changed = _changedDuringUpdateScope;
		var forcedCopy = behaviour.HasFlag(FinishedUpdateBehaviour.ForceCopyUIToModel);
		try {
			if (changed) {
				if (forcedCopy)
					CopyUIToModel();
				if (behaviour.HasFlag(FinishedUpdateBehaviour.CopyModelToUI))
					CopyModelToUI();
			}
		} finally {
			_updating = false;
			_changedDuringUpdateScope = false;
		}

		if (changed && behaviour.HasFlag(FinishedUpdateBehaviour.NotifyStateChanged))
			NotifyStateChanged(!forcedCopy);
	}

	/// <summary>Copies the derived control's model into its UI.</summary>
	protected virtual void CopyModelToUI() {
	}

	/// <summary>Publishes edited state, or records it for the active update scope's completion.</summary>
	protected internal void NotifyStateChanged() {
		NotifyStateChanged(true);
	}

	// A forced completion copy must not be repeated when the same scope publishes its notification.
	private void NotifyStateChanged(bool copyUIToModel) {
		if (!Loaded || !EnableStateChangeEvent || IsDisposed || Disposing)
			return;
		if (Updating) {
			_changedDuringUpdateScope = true;
			return;
		}

		if (copyUIToModel && UpdateModelOnStateChanged)
			CopyUIToModel();
		OnStateChanged();
		StateChanged?.Invoke();
	}

	/// <summary>Copies accepted UI values into the derived control's model.</summary>
	protected virtual void CopyUIToModel() {
	}

	/// <summary>Lets derived controls react before public state observers.</summary>
	protected virtual void OnStateChanged() {
	}

	#region Inner Types

	/// <summary>Relays the completed state of a reusable control as one child observation.</summary>
	public class StateEventProvider : ControlStateEventProviderBase<UserControlEx> {
		/// <summary>Subscribes to the control's completed semantic state.</summary>
		protected override void RegisterStateChangedListener(UserControlEx control, EventHandlerEx eventHandler) {
			control.StateChanged += eventHandler;
		}

		/// <summary>Removes the matching relay without disposing the control.</summary>
		protected override void DeregisterStateChangedListener(UserControlEx control, EventHandlerEx eventHandler) {
			control.StateChanged -= eventHandler;
		}
	}

	// The owning control chooses whether its children or its own semantic operations define state.
	private sealed class ChildStateEventProvider : ContainerControlStateEventProvider {
		// Apply the owning composite's policy before attaching a child observer.
		protected override bool ShouldObserveChildControl(Control control) {
			return ((UserControlEx)Control).ShouldObserveChildStateChanges(control);
		}
	}

	#endregion
}
