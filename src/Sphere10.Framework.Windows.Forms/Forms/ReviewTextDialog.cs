// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Reviews complete read-only text before optionally saving a copy to a chosen file.</summary>
public partial class ReviewTextDialog : Form {
	public const string DefaultFilter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
	private readonly int _ownerThreadId;
	private readonly CancellationTokenSource _lifetime = new();
	private CancellationToken _dialogCancellationToken;
	private bool _saving;
	private bool _resourcesDisposed;
	private Encoding _outputEncoding = new UTF8Encoding(false);
	private ILogger _logger = new NoOpLogger();

	public ReviewTextDialog() {
		_ownerThreadId = Environment.CurrentManagedThreadId;
		InitializeComponent();
	}

	[Category("Appearance")]
	[DefaultValue("")]
	public string Instructions {
		get => _instructionsLabel.Text;
		set { EnsureOwnerThread(); _instructionsLabel.Text = value ?? string.Empty; }
	}

	[Category("Appearance")]
	[DefaultValue("")]
	public string PreviewText {
		get => _previewTextBox.Text;
		set {
			EnsureOwnerThread();
			Guard.Ensure(!_saving, "Wait for the current save before replacing the preview.");
			_previewTextBox.Text = value ?? string.Empty;
		}
	}

	[Category("Behavior")]
	[DefaultValue("export.txt")]
	public string DefaultFileName { get; set; } = "export.txt";

	[Category("Behavior")]
	[DefaultValue(DefaultFilter)]
	public string Filter { get; set; } = DefaultFilter;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Encoding OutputEncoding {
		get => (Encoding)_outputEncoding.Clone();
		set { EnsureOwnerThread(); _outputEncoding = (Encoding)(value ?? new UTF8Encoding(false)).Clone(); }
	}

	/// <summary>Borrowed error output. The dialog never changes its options or disposes it.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ILogger Logger {
		get => _logger;
		set => _logger = value ?? new NoOpLogger();
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string SavedPath { get; private set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsSaving => _saving;

	/// <summary>Copies the complete reviewed text without changing its selection or saving a file.</summary>
	public void CopyToClipboard() {
		EnsureOwnerThread();
		Guard.Ensure(!_saving, "Wait for the current save before copying the preview.");
		if (PreviewText.Length == 0)
			Clipboard.Clear();
		else
			Clipboard.SetText(PreviewText, TextDataFormat.UnicodeText);
		_statusLabel.Text = "Copied to Clipboard";
	}

	/// <summary>Shows the review modally. User closure returns null; token cancellation throws and disposes the owned dialog.</summary>
	public static async Task<string> ShowAsync(IWin32Window owner, string title, string instructions, string text,
		string defaultFileName = "export.txt", string filter = DefaultFilter, Encoding encoding = null,
		CancellationToken cancellationToken = default, ILogger logger = null
	) {
		cancellationToken.ThrowIfCancellationRequested();
		using var dialog = new ReviewTextDialog {
			Text = title ?? "Review Text",
			Instructions = instructions,
			PreviewText = text,
			DefaultFileName = defaultFileName,
			Filter = filter,
			OutputEncoding = encoding,
			Logger = logger,
			_dialogCancellationToken = cancellationToken
		};
		var showing = dialog.ShowDialogAsync(owner);
		try {
			var result = await showing.WaitAsync(cancellationToken);
			return result == DialogResult.OK ? dialog.SavedPath : null;
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// Disposal ends the owned dialog, but native modality restores its owner when the show task unwinds.
			dialog.Dispose();
			// Cancellation can precede the posted native show.
			_ = await Tools.Exceptions.ExecuteCapturingExceptionAsync<ObjectDisposedException>(() => showing);
			throw;
		}
	}

	/// <summary>Saves the reviewed text on the creating UI thread. The existing destination changes only after the complete temporary copy succeeds.</summary>
	public async Task<string> SaveAsync(string path, CancellationToken cancellationToken = default) {
		EnsureOwnerThread();
		Guard.Ensure(!Tools.Runtime.IsDesignMode, "The review dialog cannot save on the design surface.");
		Guard.Ensure(!_saving, "A save is already in progress.");
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _dialogCancellationToken, _lifetime.Token);
		cancellation.Token.ThrowIfCancellationRequested();
		var destination = Path.GetFullPath(path);
		var text = PreviewText;
		var encoding = OutputEncoding;
		var logger = Logger;
		string temporary = null;
		var temporaryCreated = false;
		_saving = true;
		SetSavingState();
		using var saveLifetime = Tools.Scope.ExecuteOnDispose(() => {
			if (temporary != null && temporaryCreated)
				Tools.Exceptions.Execute(() => File.Delete(temporary), error => LogSaveFailure(logger, error));
			_saving = false;
			if (!IsDisposed && !Disposing)
				SetSavingState();
		});
		try {
			temporary = Tools.FileSystem.GetTempFileName(Path.GetDirectoryName(destination), false);
			using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous)) {
				temporaryCreated = true;
				using var writer = new StreamWriter(stream, encoding);
				await WritePreviewAsync(writer, text, cancellation.Token);
				await writer.FlushAsync(cancellation.Token);
			}
			cancellation.Token.ThrowIfCancellationRequested();
			File.Move(temporary, destination, true);
			temporary = null;
			SavedPath = destination;
			return destination;
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception error) {
			LogSaveFailure(logger, error);
			throw;
		}
	}

	protected override void OnShown(EventArgs e) {
		base.OnShown(e);
		_previewTextBox.Select(0, 0);
		_previewTextBox.ScrollToCaret();
		ActiveControl = _saveButton;
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		if (_saving) {
			e.Cancel = true;
			return;
		}
		base.OnFormClosing(e);
	}

	/// <summary>Customizes filename selection while retaining the dialog's save and ownership workflow.</summary>
	protected virtual string SelectSavePath() {
		using var picker = new SaveFileDialog {
			Title = "Save Reviewed Text",
			FileName = DefaultFileName ?? string.Empty,
			Filter = string.IsNullOrEmpty(Filter) ? DefaultFilter : Filter,
			AddExtension = true,
			OverwritePrompt = true,
			CheckPathExists = true,
			RestoreDirectory = true
		};
		return picker.ShowDialog(this) == DialogResult.OK ? picker.FileName : null;
	}

	/// <summary>Writes the captured preview to the dialog-owned temporary writer. Completion precedes destination replacement.</summary>
	protected virtual Task WritePreviewAsync(StreamWriter writer, string text, CancellationToken cancellationToken)
		=> writer.WriteAsync(text.AsMemory(), cancellationToken);

	private void EnsureOwnerThread() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Use the review dialog on its creating UI thread.");
		ObjectDisposedException.ThrowIf(IsDisposed || Disposing, this);
	}

	private void SetSavingState() {
		_copyButton.Enabled = _saveButton.Enabled = _closeButton.Enabled = !_saving;
		_statusLabel.Text = _saving ? "Saving..." : string.Empty;
	}

	private static void LogSaveFailure(ILogger logger, Exception error) {
		Tools.Exceptions.ExecuteIgnoringException(() => logger.Exception(error, "Could not save reviewed text."));
	}

	private async void CopyButtonClick(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			CopyToClipboard();
		}, out var error);
		if (error != null) {
			if (!IsDisposed && !Disposing)
				await DialogEx.ShowAsync(this, SystemIconType.Error, "Copy Reviewed Text", error.Message, "OK");
		}
	}

	private async void SaveButtonClick(object sender, EventArgs e) {
		try {
			var path = SelectSavePath();
			if (path == null)
				return;
			await SaveAsync(path);
			if (IsDisposed || Disposing)
				return;
			DialogResult = DialogResult.OK;
			Close();
		} catch (OperationCanceledException) {
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				await DialogEx.ShowAsync(this, SystemIconType.Error, "Save Reviewed Text", error.Message, "OK");
		}
	}
}
