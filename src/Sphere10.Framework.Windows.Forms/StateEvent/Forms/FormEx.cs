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

/// <summary>A model-aware form with scoped UI state and the existing close/hide behavior.</summary>
public partial class FormEx : Form, IUpdatable {
	/// <summary>Reports edited UI state after the model copy and template hook complete.</summary>
	public event EventHandlerEx StateChanged;

	private bool _changedDuringUpdateScope;
	private readonly IControlStateEventProvider _stateEventProvider;

	/// <summary>Creates the native form and its independent child state observer.</summary>
	public FormEx() {
		InitializeComponent();
		CloseAction = FormCloseAction.Close;
		_changedDuringUpdateScope = false;
		Loaded = false;
		Updating = false;
		EnableStateChangeEvent = true;
		UpdateModelOnStateChanged = true;
		OpacityBeforeHide = 1;
		_stateEventProvider = !Tools.Runtime.IsDesignMode ? new ChildStateEventProvider() : new NoOpControlStateProvider();
		_stateEventProvider.StateChanged += NotifyStateChanged;
		_stateEventProvider.SetControl(this);
	}

	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[DefaultValue(FormCloseAction.Close)]
	public FormCloseAction CloseAction {
		get; set;
	}

	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[DefaultValue(true)]
	public bool EnableStateChangeEvent {
		get; set;
	}

	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[DefaultValue(true)]
	public bool UpdateModelOnStateChanged {
		get; set;
	}

	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[DefaultValue(true)]
	public bool AutoSave { get; set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Updating { get; private set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Loaded {
		get; private set;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	private double OpacityBeforeHide {
		get; set;
	}

	public virtual void SetLocalizedText(CultureInfo culture = null) {
	}

	public new virtual void Refresh() {
		using (this.EnterUpdateScope()) {
			CopyModelToUI();
		}
		base.Refresh();
	}

	public virtual new void Show() {
		if (WindowState == FormWindowState.Minimized) {
			TopMost = true;
			TopMost = false;
			Visible = true;
			WindowState = FormWindowState.Normal;
		} else {
			TopMost = true;
			TopMost = false;
			Visible = true;
		}
		if (Math.Abs(Opacity) < 0.00001D) // if Opacity == 0
			Opacity = OpacityBeforeHide;
	}

	protected override async void OnLoad(EventArgs e) {
		try {
			base.OnLoad(e);
			if (Loaded || DesignMode || Tools.Runtime.IsDesignMode)
				return;

			using (this.EnterUpdateScope()) {
				PopulatePrimingData();
				CopyModelToUI();
			}
			Loaded = true;
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	public UpdateScope EnterUpdateScope(FinishedUpdateBehaviour behaviour = FinishedUpdateBehaviour.Default) {
		return new UpdateScope(this, behaviour);
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		if (!CloseAction.HasFlag(FormCloseAction.Close)) {
			e.Cancel = true;
			if (CloseAction.HasFlag(FormCloseAction.Hide))
				Hide();

			if (CloseAction.HasFlag(FormCloseAction.Minimize))
				WindowState = FormWindowState.Minimized;
		}
		base.OnFormClosing(e);
	}

	public virtual new void Hide() {
		OpacityBeforeHide = Opacity;
		Opacity = 0;
		Visible = false;
	}

	// Stop state propagation before disposing children or designer-owned components.
	protected override void Dispose(bool disposing) {
		if (disposing) {
			// The observer may not yet exist when native construction fails.
			if (_stateEventProvider != null) {
				_stateEventProvider.StateChanged -= NotifyStateChanged;
				_stateEventProvider.Clear();
			}
			_components?.Dispose();
		}
		base.Dispose(disposing);
	}

	/// <summary>Loads lists or other UI data before the initial model copy.</summary>
	protected virtual void PopulatePrimingData() {
	}

	/// <summary>Controls automatic observation of direct child controls.</summary>
	protected virtual bool ShouldObserveChildStateChanges(Control child) {
		return true;
	}

	/// <summary>Lets derived forms save their accepted model.</summary>
	protected virtual void Save() {
	}

	void IUpdatable.BeginUpdate() {
		if (Updating == true)
			return;
		Updating = true;
		_changedDuringUpdateScope = false;
	}

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
			Updating = false;
			_changedDuringUpdateScope = false;
		}

		if (changed && behaviour.HasFlag(FinishedUpdateBehaviour.NotifyStateChanged))
			NotifyStateChanged(!forcedCopy);
	}

	/// <summary>Copies the form model into its UI.</summary>
	protected virtual void CopyModelToUI() {
	}

	/// <summary>Publishes edited state, or records it for the active update scope's completion.</summary>
	protected internal void NotifyStateChanged() {
		NotifyStateChanged(true);
	}

	// A forced completion copy must not be repeated when the same scope publishes its notification.
	private void NotifyStateChanged(bool copyUIToModel) {
		if (!EnableStateChangeEvent || IsDisposed || Disposing)
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

	/// <summary>Copies accepted UI values into the form model.</summary>
	protected virtual void CopyUIToModel() {
	}

	/// <summary>Lets derived forms react before public state observers.</summary>
	protected virtual void OnStateChanged() {
	}

	/// <summary>Relays the form's completed state as one child observation.</summary>
	public class StateEventProvider : ControlStateEventProviderBase<FormEx> {
		/// <summary>Subscribes to the form's completed semantic state.</summary>
		protected override void RegisterStateChangedListener(FormEx control, EventHandlerEx eventHandler) {
			control.StateChanged += eventHandler;
		}

		/// <summary>Removes the matching relay while leaving the borrowed form alive.</summary>
		protected override void DeregisterStateChangedListener(FormEx control, EventHandlerEx eventHandler) {
			control.StateChanged -= eventHandler;
		}
	}

	// The owning form can replace broad child observation with completed semantic operations.
	private sealed class ChildStateEventProvider : ContainerControlStateEventProvider {
		// Apply the owning form's policy before attaching a child observer.
		protected override bool ShouldObserveChildControl(Control control) {
			return ((FormEx)Control).ShouldObserveChildStateChanges(control);
		}
	}
}
