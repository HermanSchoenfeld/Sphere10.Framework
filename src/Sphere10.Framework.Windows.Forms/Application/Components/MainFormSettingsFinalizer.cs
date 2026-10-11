// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Saves accepted main-window placement once during framework shutdown, before settings providers are disposed.</summary>
public class MainFormSettingsFinalizer : SyncApplicationFinalizerBase {
	private FormWindowSettings? _pendingSettings;
	private Action _captureAcceptedClose;

	/// <summary>Restores on load and captures approved shutdown or completed closure without writing settings. Dispose the returned scope to detach.</summary>
	public IDisposable Attach(Form Window, string SettingsID = "MainForm") {
		var tracking = Tools.WinForms.TrackWindowSettings(Window, SettingsID, Settings => _pendingSettings = Settings, out var captureAcceptedClose);
		_captureAcceptedClose = captureAcceptedClose;
		return Tools.Scope.ExecuteOnDispose(() => {
			tracking.Dispose();
			if (ReferenceEquals(_captureAcceptedClose, captureAcceptedClose))
				_captureAcceptedClose = null;
		});
	}

	// Preserve the public application lifecycle method; it is not a CLR finalizer.
#pragma warning disable CS0465
	public override void Finalize() {
#pragma warning restore CS0465
		var settings = _pendingSettings;
		_pendingSettings = null;
		if (settings == null)
			return;
		try {
			settings.Save();
		} catch (Exception error) {
			SystemLog.Exception(error);
		}
	}

	/// <summary>Queues placement after exit and native closing permissions approve shutdown, before finalization while the window is alive.</summary>
	internal void CaptureAcceptedClose() => _captureAcceptedClose?.Invoke();
}
