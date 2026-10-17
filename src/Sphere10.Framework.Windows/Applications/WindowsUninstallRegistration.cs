// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows;

/// <summary>Accesses Windows uninstall-registration keys without deciding which applications to display or execute.</summary>
public sealed class WindowsUninstallRegistration {
	public const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
	private readonly RegistryUtil _registry = new();

	public static RegistryAddress[] GetRoots() {
		var view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
		var roots = new List<RegistryAddress> { new(RegistryHive.CurrentUser, view, RegistryPath), new(RegistryHive.LocalMachine, view, RegistryPath) };
		if (Environment.Is64BitOperatingSystem)
			roots.Add(new RegistryAddress(RegistryHive.LocalMachine, RegistryView.Registry32, RegistryPath));
		return roots.ToArray();
	}

	public RegistryAddress[] GetRegistrations(RegistryAddress root) {
		using var key = _registry.OpenKey(root, throwIfMissing: false);
		return key == null ? Array.Empty<RegistryAddress>() : _registry.GetSubKeys(root);
	}

	public RegistryTree ReadRegistration(RegistryAddress address, int maximumDepth, int maximumEntries, TimeSpan timeLimit)
		=> _registry.ReadTree(address, maximumDepth, maximumEntries, timeLimit);

	public Task<RegistryAddress[]> GetRegistrationsAsync(RegistryAddress root, CancellationToken cancellationToken = default)
		=> Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); return GetRegistrations(root); }, cancellationToken);

	public Task<RegistryTree> ReadRegistrationAsync(RegistryAddress address, int maximumDepth, int maximumEntries, TimeSpan timeLimit, CancellationToken cancellationToken = default)
		=> _registry.ReadTreeAsync(address, maximumDepth, maximumEntries, timeLimit, cancellationToken);

	/// <summary>Removes only the supplied registration key; verification/approval of that registration belongs to the caller.</summary>
	public void RemoveRegistration(RegistryAddress address) {
		_registry.DeleteKey(address);
	}

	/// <summary>Starts a caller-selected executable using the Windows shell. The returned process handle belongs to the caller.</summary>
	public Process Launch(string executable, string arguments) {
		Guard.ArgumentNotNull(executable, nameof(executable));
		Guard.Argument(Path.IsPathFullyQualified(executable), nameof(executable), "An uninstall executable must have an absolute path.");
		Guard.FileExists(executable);
		return Process.Start(new ProcessStartInfo(executable, arguments ?? string.Empty) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(executable) });
	}
}
