// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Sphere10.Framework.Windows;

/// <summary>Builds argument-safe start information for a caller-configured child worker host.</summary>
public sealed class WindowsProcessWorkerHost {
	/// <summary>Uses the process entry assembly and its adjacent apphost.</summary>
	public WindowsProcessWorkerHost()
		: this(Assembly.GetEntryAssembly()?.Location, string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location) ? Environment.ProcessPath : null) {
	}

	/// <param name="assemblyPath">The host application's managed assembly, or null for an explicit standalone/single-file executable.</param>
	/// <param name="executablePath">Optional application executable. Without one, an adjacent apphost is preferred, then dotnet runs the managed assembly.</param>
	public WindowsProcessWorkerHost(string assemblyPath, string executablePath = null) {
		Guard.Argument(!string.IsNullOrWhiteSpace(assemblyPath) || !string.IsNullOrWhiteSpace(executablePath), nameof(assemblyPath), "Supply a diagnostic host assembly or executable.");
		AssemblyPath = string.IsNullOrWhiteSpace(assemblyPath) ? null : Path.GetFullPath(assemblyPath);
		if (AssemblyPath != null)
			Guard.FileExists(AssemblyPath);
		if (!string.IsNullOrWhiteSpace(executablePath)) {
			ExecutablePath = Path.GetFullPath(executablePath);
			Guard.FileExists(ExecutablePath);
		} else {
			var appHost = Path.ChangeExtension(AssemblyPath, ".exe");
			ExecutablePath = File.Exists(appHost) ? appHost : null;
		}
	}

	public string AssemblyPath { get; }

	/// <summary>The application executable, or null when the managed assembly is launched through dotnet.</summary>
	public string ExecutablePath { get; }

	public ProcessStartInfo CreateStartInfo(params string[] arguments) {
		Guard.ArgumentNotNull(arguments, nameof(arguments));
		var result = new ProcessStartInfo(ExecutablePath ?? "dotnet") {
			UseShellExecute = false, CreateNoWindow = true,
			WorkingDirectory = Path.GetDirectoryName(AssemblyPath ?? ExecutablePath)
		};
		if (ExecutablePath == null)
			result.ArgumentList.Add(AssemblyPath);
		foreach (var argument in arguments)
			result.ArgumentList.Add(argument);
		return result;
	}
}
