// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A text box with a browse button for a file or folder path.</summary>
/// <remarks>With <see cref="ForcePathExists"/>, a typed path is checked on a worker when the text box loses focus, because a path on a
/// slow or unreachable share can take many seconds to answer. Focus moves on meanwhile; <see cref="PathChanged"/> follows once the path is
/// found, and a missing path sets <see cref="IsPathMissing"/> and returns focus to the box while its form is active.</remarks>
[DefaultEvent("PathChanged")]
public partial class PathSelectorControl : UserControlEx {
	public event EventHandlerEx PathChanged;
	private string _textOnEnter;
	private Task _validation = Task.CompletedTask;
	private int _validationGeneration;

	public PathSelectorControl() {
		InitializeComponent();
		// The browse button stands as tall as the path box at every display scale.
		_fileSelectorButton.KeepHeightOf(_filenameTextBox);
	}

	public override Size GetPreferredSize(Size proposedSize) {
		var preferred = base.GetPreferredSize(proposedSize);
		var height = Math.Max(_filenameTextBox?.PreferredHeight ?? Font.Height, _fileSelectorButton?.Height ?? 0);
		return new Size(preferred.Width, height + Padding.Vertical);
	}

	[Browsable(true)]
	[DefaultValue(PathSelectionMode.OpenFile)]
	public PathSelectionMode Mode { get; set; }


	[Browsable(true)]
	[DefaultValue(false)]
	public bool ForcePathExists { get; set; }

	[Browsable(true)]
	[DefaultValue(true)]
	[Description("Ask before selecting an existing file in save-file mode. Disable for append-only consumers.")]
	public bool OverwritePrompt { get; set; } = true;

	/// <summary>The file dialogs' file type filter, such as "Text Files|*.txt|All Files|*.*", or null for every file.</summary>
	[Browsable(true)]
	[DefaultValue(null)]
	public string Filter { get; set; }

	/// <summary>The caption of the dialog the browse button opens, or null for the dialog's own caption.</summary>
	[Browsable(true)]
	[DefaultValue(null)]
	public string DialogTitle { get; set; }

	/// <summary>The folder the dialog starts in while the box holds no path, or null for the folder Windows remembers.</summary>
	[Browsable(true)]
	[DefaultValue(null)]
	public string InitialDirectory { get; set; }

	[Browsable(true)]
	[DefaultValue(true)]
	public bool Editable {
		get => !_filenameTextBox.ReadOnly;
		set => _filenameTextBox.ReadOnly = !value;
	}

	public string Path {
		get => _filenameTextBox.Text;
		set {
			// A path set from code is taken as it is; a check still running for a typed one no longer applies.
			_validationGeneration++;
			IsPathMissing = false;
			_filenameTextBox.Text = value;
			if (!this.DesignMode) {
				NotifyPathChanged();
			}
		}
	}

	/// <summary>Whether the last typed path that <see cref="ForcePathExists"/> checked does not exist. Setting <see cref="Path"/> clears it.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsPathMissing { get; private set; }

	/// <summary>Completes when the existence check of the last typed path has finished and its outcome has been applied.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Task ValidationCompletion => _validation;


	[Browsable(true)]
	public string PlaceHolderText {
		get => _filenameTextBox != null ? _filenameTextBox.PlaceHolderText : null;
		set {
			if (_filenameTextBox != null)
				_filenameTextBox.PlaceHolderText = value;
		}
	}

	/// <summary>Opens the dialog the browse button opens, for <see cref="Mode"/>, and puts the chosen path in the box.</summary>
	/// <param name="owner">The window that owns the dialog, or null for this control.</param>
	/// <returns>Whether a path was chosen.</returns>
	public bool Browse(IWin32Window owner = null) => Mode switch {
		PathSelectionMode.OpenFile => ShowOpenDialog(owner ?? this),
		PathSelectionMode.SaveFile => ShowSaveDialog(owner ?? this),
		PathSelectionMode.File => ShowOpenSaveDialog(owner ?? this),
		PathSelectionMode.Folder => ShowSelectFolderDialog(owner ?? this),
		_ => false
	};

	protected virtual void OnPathChanged() {
	}

	private void NotifyPathChanged() {
		OnPathChanged();
		PathChanged?.Invoke();
	}

	// Checks a typed path on a worker and applies the outcome on the UI thread, unless the path was changed meanwhile.
	private async Task CheckPathExistsAsync(string path, string textOnEnter) {
		var generation = ++_validationGeneration;
		var folder = Mode.IsIn(PathSelectionMode.Folder);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			var exists = await Task.Run(() => folder ? Directory.Exists(path) : File.Exists(path));
			if (generation != _validationGeneration || IsDisposed || _filenameTextBox.Text != path)
				return;
			IsPathMissing = !exists;
			if (exists) {
				NotifyPathChanged();
				return;
			}
			// As the check on leaving used to, the box keeps the missing path for correction, and leaving it again checks it again.
			_textOnEnter = textOnEnter;
			if (FindForm() is { } form && Form.ActiveForm == form) {
				_filenameTextBox.Focus();
				_filenameTextBox.SelectAll();
			}
		});
		if (error != null) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(error);
		}
	}

	private bool ShowOpenDialog(IWin32Window owner) {
		using var dlg = new OpenFileDialog();
		dlg.CheckFileExists = ForcePathExists;
		dlg.CheckPathExists = ForcePathExists;
		dlg.Multiselect = false;
		ApplyDialogOptions(dlg);
		if (dlg.ShowDialog(owner) != DialogResult.OK)
			return false;
		Path = dlg.FileName;
		return true;
	}

	private bool ShowSaveDialog(IWin32Window owner) {
		using var dlg = new SaveFileDialog();
		dlg.CheckPathExists = ForcePathExists;
		dlg.CheckFileExists = ForcePathExists;
		dlg.OverwritePrompt = OverwritePrompt;
		ApplyDialogOptions(dlg);
		if (dlg.ShowDialog(owner) != DialogResult.OK)
			return false;
		Path = dlg.FileName;
		return true;
	}

	private bool ShowOpenSaveDialog(IWin32Window owner) {
		using var dlg = new OpenFileDialog();
		dlg.Title = "Select File";
		dlg.CheckFileExists = ForcePathExists;
		dlg.CheckPathExists = ForcePathExists;
		dlg.Multiselect = false;
		ApplyDialogOptions(dlg);
		if (dlg.ShowDialog(owner) != DialogResult.OK)
			return false;
		Path = dlg.FileName;
		return true;
	}

	private bool ShowSelectFolderDialog(IWin32Window owner) {
		using var dlg = new FolderBrowserDialog();
		if (!string.IsNullOrEmpty(DialogTitle)) {
			dlg.Description = DialogTitle;
			dlg.UseDescriptionForTitle = true;
		}
		if (!string.IsNullOrEmpty(Path))
			dlg.SelectedPath = Path;
		else if (!string.IsNullOrEmpty(InitialDirectory))
			dlg.InitialDirectory = InitialDirectory;
		if (dlg.ShowDialog(owner) != DialogResult.OK)
			return false;
		Path = dlg.SelectedPath;
		return true;
	}

	// The file dialogs start beside the path in the box, or in the initial folder while the box is empty.
	private void ApplyDialogOptions(FileDialog dialog) {
		if (!string.IsNullOrEmpty(Filter))
			dialog.Filter = Filter;
		if (!string.IsNullOrEmpty(DialogTitle))
			dialog.Title = DialogTitle;
		var directory = string.IsNullOrWhiteSpace(Path) ? null : System.IO.Path.GetDirectoryName(Path.Trim());
		dialog.InitialDirectory = !string.IsNullOrEmpty(directory) ? directory : InitialDirectory;
	}

	#region Event Handlers

	private async void _filenameTextBox_Enter(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (_textOnEnter == null)
				_textOnEnter = this._filenameTextBox.Text;
		}, out var error);
		if (error != null) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(error);
		}
	}

	private async void _filenameTextBox_Validating(object sender, CancelEventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			var textOnLeave = _filenameTextBox.Text;
			var textOnEnter = _textOnEnter;
			_textOnEnter = null;
			if (textOnEnter == textOnLeave)
				return;
			// The existence check never holds up the focus change: a path on an unreachable share would freeze the window until it timed out.
			// A cleared box is reported too, with nothing to check, so a consumer can return to having no path.
			if (ForcePathExists && !string.IsNullOrEmpty(textOnLeave))
				_validation = CheckPathExistsAsync(textOnLeave, textOnEnter);
			else {
				IsPathMissing = false;
				NotifyPathChanged();
			}
		}, out var error);
		if (error != null) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(error);
		}
	}

	private async void _filenameTextBox_Validated(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
		}, out var error);
		if (error != null) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(error);
		}
	}

	private async void _fileSelectorButton_Click(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException(() => Browse(), out var error);
		if (error != null) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(error);
		}
	}

	protected override async void OnEnabledChanged(EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			base.OnEnabledChanged(e);
			_filenameTextBox.Enabled =
				_fileSelectorButton.Enabled = this.Enabled;
		}, out var error);
		if (error != null) {
			SystemLog.Exception(error);
			await ExceptionDialog.ShowAsync(error);
		}
	}

	#endregion


	public new class StateEventProvider : ControlStateEventProviderBase<PathSelectorControl> {
		protected override void RegisterStateChangedListener(PathSelectorControl control, EventHandlerEx eventHandler) => control.PathChanged += eventHandler;

		protected override void DeregisterStateChangedListener(PathSelectorControl control, EventHandlerEx eventHandler) => control.PathChanged -= eventHandler;
	}
}

