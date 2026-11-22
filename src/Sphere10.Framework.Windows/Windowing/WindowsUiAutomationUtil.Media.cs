// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

// Media queries use their own bounded persistent protocol; diagnostic captures retain their existing protocol and lifetime.
public sealed partial class WindowsUiAutomationUtil {
	/// <summary>Creates a lazy isolated media session using this utility's configured worker host.</summary>
	public WindowsUiAutomationMediaSession CreateMediaSession(WindowsUiAutomationMediaOptions options = null) => new(options, WorkerHost);

	/// <summary>Dispatches the media helper before application, single-instance and UI startup.</summary>
	public static bool TryRunMediaWorker(string[] args, out int exitCode) => WindowsUiAutomationMediaSession.TryRunWorker(args, out exitCode);

	/// <summary>Creates a lazy isolated session that reads other windows' title-bar content, using this utility's configured worker host.</summary>
	public WindowsTitleBarSession CreateTitleBarSession() => new(WorkerHost);

	/// <summary>Dispatches the title-bar helper before application, single-instance and UI startup.</summary>
	public static bool TryRunTitleBarWorker(string[] args, out int exitCode) => WindowsTitleBarSession.TryRunWorker(args, out exitCode);
}
