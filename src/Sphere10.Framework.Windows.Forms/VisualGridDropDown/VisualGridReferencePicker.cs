// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A searchable, paged selection of existing records. Selecting does not edit the referenced record.</summary>
public class CrudReferencePicker : UserControlEx {
	public event EventHandler? SelectionAccepted;

	public event EventHandler? SelectionCancelled;

	private readonly CrudReferenceBinding _binding;
	private readonly Label _status;
	private readonly Button _clearButton;
	private Task? _loadTask;
	private bool _loading;

	public CrudReferencePicker(CrudReferenceBinding Binding, object? SelectedEntity) {
		Guard.ArgumentNotNull(Binding, nameof(Binding));
		_binding = Binding;
		this.SelectedEntity = SelectedEntity;
		Guard.ArgumentGT(Binding.MaximumDropDownSize.Width, 0, nameof(Binding));
		Guard.ArgumentGT(Binding.MaximumDropDownSize.Height, 0, nameof(Binding));
		MaximumSize = Binding.MaximumDropDownSize;
		Size = MaximumSize;
		Padding = new Padding(4);
		Grid = new ReferenceGrid { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, MinimumSize = Size.Empty, Capabilities = CrudReferenceBinding.ReadOnlyCapabilities };
		Grid.SelectedEntityDirect = SelectedEntity!;
		Grid.EntitySelected += (_, entity) => {
			if (!_loading)
				AcceptSelection(entity);
		};
		_status = new Label { AutoEllipsis = true, Dock = DockStyle.Top, Height = Font.Height + 10, Padding = new Padding(3, 5, 3, 5), Text = $"Current: {Binding.GetDisplayText(SelectedEntity)}" };
		var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
		var cancelButton = new Button { AutoSize = true, Text = "Cancel", CausesValidation = false };
		cancelButton.Click += (_, _) => SelectionCancelled?.Invoke(this, EventArgs.Empty);
		_clearButton = new Button { AutoSize = true, Text = "Clear", Visible = Binding.AllowNull, CausesValidation = false };
		_clearButton.Click += (_, _) => AcceptSelection(null);
		buttons.Controls.Add(cancelButton);
		buttons.Controls.Add(_clearButton);
		Controls.Add(Grid);
		Controls.Add(_status);
		Controls.Add(buttons);
	}

	public VisualGrid Grid {
		get;
	}

	public object? SelectedEntity {
		get; private set;
	}

	public bool HasSelection {
		get; private set;
	}

	public Exception? LoadError {
		get; private set;
	}

	public void AcceptSelection(object? Entity) {
		Guard.Argument(Entity != null || _binding.AllowNull, nameof(Entity), "This reference cannot be cleared.");
		SelectedEntity = Entity;
		HasSelection = true;
		SelectionAccepted?.Invoke(this, EventArgs.Empty);
		NotifyStateChanged();
	}

	public Task LoadItemsAsync() => _loadTask ??= LoadItemsInternalAsync();

	/// <summary>Reports accepted references instead of the popup's search, paging and temporary row selection.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) => false;

	protected override async void OnLoad(EventArgs Args) {
		LimitDropDownSize();
		base.OnLoad(Args);
		await LoadItemsAsync();
	}

	protected override void OnFontChanged(EventArgs Args) {
		base.OnFontChanged(Args);
		if (_status != null)
			_status.Height = Font.Height + _status.Padding.Vertical;
	}

	protected override bool ProcessCmdKey(ref Message Message, Keys KeyData) {
		if (KeyData == Keys.Escape) {
			SelectionCancelled?.Invoke(this, EventArgs.Empty);
			return true;
		}
		return base.ProcessCmdKey(ref Message, KeyData);
	}

	private void LimitDropDownSize() {
		var workingArea = Screen.FromControl(this).WorkingArea;
		// Leave room for the native host's border and resize grip.
		var hostFrame = Math.Max(2, (int)Math.Ceiling(20 * DeviceDpi / 96.0));
		var limit = new Size(Math.Max(1, Math.Min(_binding.MaximumDropDownSize.Width, workingArea.Width - hostFrame)),
			Math.Max(1, Math.Min(_binding.MaximumDropDownSize.Height, workingArea.Height - hostFrame)));
		MaximumSize = limit;
		// The popup host can reduce the initial size further to fit above or below its owner.
		Size = new Size(Math.Min(Width, limit.Width), Math.Min(Height, limit.Height));
	}

	private async Task LoadItemsInternalAsync() {
		_loading = true;
		using var loadingScope = Tools.Scope.ExecuteOnDispose(() => _loading = false);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await _binding.BindAsync(Grid);
		});
		if (error != null) {
			LoadError = error;
			if (!IsDisposed)
				_status.Text = $"Unable to load records: {error.Message}";
		}
	}

	private sealed class ReferenceGrid : VisualGrid {
		public override Task EditEntity(object Entity) => Task.CompletedTask;
	}
}
