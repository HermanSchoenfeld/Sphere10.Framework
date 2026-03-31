// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Instance-based utility class for Windows shell operations such as creating shortcuts
/// and managing startup items. Access via <c>Tools.Windows.Shell</c>.
/// </summary>
public class ShellUtility {

	public void CreateShortcutForApplication(string executablePath, string shortcutPath, string arguments = null, bool overwrite = true, ShellLink.LinkDisplayMode displayMode = ShellLink.LinkDisplayMode.Normal) {
		using (var Shortcut = new ShellLink()) {
			Shortcut.Target = executablePath;
			Shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath);
			Shortcut.Description = "My Shorcut Name Here";
			Shortcut.DisplayMode = displayMode;
			Shortcut.Arguments = arguments;
			if (File.Exists(shortcutPath) && overwrite)
				File.Delete(shortcutPath);
			Shortcut.Save(shortcutPath);
		}
	}

	public string DetermineStartupShortcutFilename(string applicationName) {
		return string.Format(
			"{1}{0}{2}.LNK",
			Path.DirectorySeparatorChar,
			Environment.GetFolderPath(Environment.SpecialFolder.Startup),
			Tools.FileSystem.ToWellFormedPath(applicationName)
		);
	}

	/// <summary>
	/// Opens a file in explorer with the file highlighted.
	/// </summary>
	public void HighlightFileInExplorer(string filePath) {
		if (File.Exists(filePath))
			Process.Start("explorer.exe", @"/select, " + filePath);
	}
}

