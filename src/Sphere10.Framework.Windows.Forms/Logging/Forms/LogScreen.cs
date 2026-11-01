// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A reusable host-owned log view. Worker writes are bounded and posted without waiting for its UI thread.</summary>
public partial class LogScreen : WinFormsApplicationScreen {
	private readonly ScreenWriter _writer;
	private bool _ownedWorkEnded;

	public LogScreen() {
		InitializeComponent();
		_writer = new ScreenWriter(this);
		Logger = new SynchronizedLogger(new TextWriterLogger(_writer));
	}

	/// <summary>The screen's logger; it drops messages before loading or after destruction. It adds no timestamp of its own.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ILogger Logger { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override bool CanRestoreAtStartup => false;

	protected override void OnLoad(EventArgs args) {
		base.OnLoad(args);
		if (!DesignMode && !Tools.Runtime.IsDesignMode)
			_writer.Start();
	}

	protected override void OnDestroyScreen() {
		EndOwnedWork();
		base.OnDestroyScreen();
	}

	private void EndOwnedWork() {
		if (_ownedWorkEnded)
			return;
		_ownedWorkEnded = true;
		_writer?.Dispose();
		_outputTextBox?.Clear();
	}

	protected override void OnHandleCreated(EventArgs args) {
		base.OnHandleCreated(args);
		_writer?.DispatchPending();
	}

	protected override void OnHandleDestroyed(EventArgs args) {
		_writer?.HandleDestroyed();
		base.OnHandleDestroyed(args);
	}

	private sealed class ScreenWriter : TextWriter {
		private const int MaximumPendingCharacters = 256 * 1024;
		private const int MaximumDisplayedCharacters = 1024 * 1024;
		private readonly LogScreen _screen;
		private readonly SynchronizedObject _queueScope = new();
		private readonly Queue<string> _pending = new();
		private int _pendingCharacters;
		private bool _dispatchPending;
		private bool _started;
		private bool _closed;

		public ScreenWriter(LogScreen screen) => _screen = screen;

		public override Encoding Encoding => Encoding.UTF8;

		public void Start() {
			using (_queueScope.EnterWriteScope())
				_started = !_closed;
			DispatchPending();
		}

		public override void Write(string value) {
			if (string.IsNullOrEmpty(value))
				return;
			using (_queueScope.EnterWriteScope()) {
				if (!_started || _closed)
					return;
				if (value.Length > MaximumPendingCharacters)
					value = value[^MaximumPendingCharacters..];
				while (_pendingCharacters + value.Length > MaximumPendingCharacters)
					_pendingCharacters -= _pending.Dequeue().Length;
				_pending.Enqueue(value);
				_pendingCharacters += value.Length;
			}
			DispatchPending();
		}

		public void HandleDestroyed() {
			using var scope = _queueScope.EnterWriteScope();
			_dispatchPending = false;
		}

		public void DispatchPending() {
			using (_queueScope.EnterWriteScope()) {
				if (!_started || _closed || _dispatchPending || _pending.Count == 0)
					return;
				_dispatchPending = true;
			}
			// Dispatch outside the queue scope. Closing a view never joins UI callbacks or workers.
			if (!_screen.IsDisposed && !_screen.Disposing && _screen.IsHandleCreated) {
				Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => _screen.BeginInvoke((Action)AppendPending), out var error);
				if (error == null)
					return;
			}
			// A detached/docked view can temporarily lose its handle. Keep the bounded backlog
			// until OnHandleCreated rather than ending the screen's logging lifetime.
			using var scope = _queueScope.EnterWriteScope();
			_dispatchPending = false;
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				using var scope = _queueScope.EnterWriteScope();
				_closed = true;
				_pending.Clear();
				_pendingCharacters = 0;
				_dispatchPending = false;
			}
			base.Dispose(disposing);
		}

		private void AppendPending() {
			string text;
			using (_queueScope.EnterWriteScope()) {
				if (_closed)
					return;
				var buffer = new StringBuilder(_pendingCharacters);
				while (_pending.Count > 0)
					buffer.Append(_pending.Dequeue());
				text = buffer.ToString();
				_pendingCharacters = 0;
				_dispatchPending = false;
			}
			if (_screen.IsDisposed || _screen.Disposing)
				return;
			var output = _screen._outputTextBox;
			var excess = output.TextLength + text.Length - MaximumDisplayedCharacters;
			if (excess > 0) {
				output.Select(0, Math.Min(excess, output.TextLength));
				output.SelectedText = string.Empty;
			}
			output.AppendText(text);
		}
	}
}
