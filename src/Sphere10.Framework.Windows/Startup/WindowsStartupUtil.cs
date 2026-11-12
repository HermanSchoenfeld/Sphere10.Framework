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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sphere10.Framework.Windows.Startup;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Provides Windows startup registration, folder and StartupApproved data without interpreting its UI state.</summary>
	public static class Startup {
		private static readonly AsyncSynchronizedGate _registrationReaders = new();
		private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\";
		private const string ApprovalPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

		/// <summary>Creates a fresh read-only snapshot of explicitly selected additional launch-source families.</summary>
		public static WindowsStartupRegistrationDataSource CreateRegistrationDataSource(WindowsStartupSources sources, int maximumRows = 5000, int queryTimeoutSeconds = 10)
			=> new WindowsStartupRegistrationDataSource(sources, maximumRows, queryTimeoutSeconds);

		/// <summary>Reads only explicitly selected additional launch-source families. Run/folder inventory and state changes remain separate.</summary>
		public static async Task<WindowsStartupRegistration[]> ReadRegistrationsAsync(WindowsStartupSources sources, int maximumRows = 5000, int queryTimeoutSeconds = 10, CancellationToken cancellationToken = default) {
			using var admission = await _registrationReaders.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
			return await Task.Run(() => new WindowsStartupRegistrationReader(maximumRows, queryTimeoutSeconds, cancellationToken.ThrowIfCancellationRequested).Read(sources), cancellationToken).ConfigureAwait(false);
		}

		public static WindowsStartupRegistration[] ReadRegistrations(WindowsStartupSources sources, int maximumRows = 5000, int queryTimeoutSeconds = 10) {
			Debug.AssertNotMainThread();
			return Task.ExecuteSyncOverAsync(() => ReadRegistrationsAsync(sources, maximumRows, queryTimeoutSeconds));
		}

		/// <summary>Interprets one registered command without PATH searches, process creation or source edits. Explicit executable fields bypass command-line parsing.</summary>
		public static WindowsStartupTarget ReadTarget(string command, string executable = null, string arguments = null, string workingDirectory = null) {
			Debug.AssertNotMainThread();
			var target = string.Empty;
			var capturedArguments = arguments ?? string.Empty;
			Tools.Exceptions.ExecuteCapturingException(() => {
				if (!string.IsNullOrWhiteSpace(executable)) {
					target = System.Environment.ExpandEnvironmentVariables(executable.Trim().Trim('"'));
				} else {
					Guard.ArgumentNotNullOrWhitespace(command, nameof(command));
					var expanded = System.Environment.ExpandEnvironmentVariables(command.Trim());
					Guard.Argument(!expanded.Contains('\r') && !expanded.Contains('\n'), nameof(command), "Multiple-command or template syntax is retained verbatim; select an explicit file instead.");
					var parsed = Tools.Runtime.ParseCommandLine(expanded);
					target = parsed.executable;
					capturedArguments = parsed.arguments;
					Guard.Argument(expanded.StartsWith('"') || !target.Contains(','), nameof(command), "An unquoted multi-command target is retained verbatim; select an explicit file instead.");
					Guard.Argument(expanded.StartsWith('"') || !string.IsNullOrEmpty(Path.GetExtension(target)), nameof(command), "An unquoted command with no unambiguous executable extension cannot identify a file safely.");
				}
				Guard.Argument(!string.IsNullOrWhiteSpace(target) && Path.IsPathFullyQualified(target) && !target.StartsWith(@"\\", StringComparison.Ordinal)
					&& !target.Contains('%') && !target.Contains('\0'), nameof(command), "No absolute local file target is available. Relative/PATH, network, unresolved environment and provider template targets are not inferred.");
				target = Path.GetFullPath(target);
			}, out var error);
			return error == null ? new WindowsStartupTarget(target, capturedArguments, workingDirectory ?? string.Empty,
				"Stored absolute target; this does not establish that it executed." + ((command ?? executable ?? string.Empty).Contains('%') ? " Environment variables use the inspector's current process; another task/service user's environment is not emulated." : string.Empty))
				: new WindowsStartupTarget(string.Empty, capturedArguments, workingDirectory ?? string.Empty, error.Message);
		}

		/// <summary>Explicitly reads stored shortcut metadata on the existing Shell STA; it never invokes Resolve or searches/repairs a target.</summary>
		public static async Task<WindowsStartupTarget> ReadShortcutTargetAsync(string shortcutPath, CancellationToken cancellationToken = default) {
			var shortcut = await Tools.Windows.Shell.ReadShortcutAsync(shortcutPath, cancellationToken).ConfigureAwait(false);
			return await Task.Run(() => {
				if (shortcut.Target.Length >= 259)
					return new WindowsStartupTarget(string.Empty, shortcut.Arguments, shortcut.WorkingDirectory, "The Shell target reached MAX_PATH; a complete target is unavailable.");
				return ReadTarget(null, shortcut.Target, shortcut.Arguments, shortcut.WorkingDirectory);
			}, cancellationToken).ConfigureAwait(false);
		}

		public static RegistryAddress[] GetRunLocations() {
			var native = System.Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
			var roots = new List<RegistryAddress>();
			foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine }) {
				var views = hive == RegistryHive.LocalMachine && System.Environment.Is64BitOperatingSystem ? new[] { native, RegistryView.Registry32 } : new[] { native };
				foreach (var view in views)
					foreach (var name in new[] { "Run", "RunOnce" })
						roots.Add(new RegistryAddress(hive, view, RunPath + name));
			}
			return roots.ToArray();
		}

		public static RegistryValue[] ReadRunValues(RegistryAddress location) => new RegistryUtil().ReadValues(location, throwIfMissing: false);

		public static Task<RegistryValue[]> ReadRunValuesAsync(RegistryAddress location, CancellationToken cancellationToken = default)
			=> new RegistryUtil().ReadValuesAsync(location, throwIfMissing: false, cancellationToken: cancellationToken);

		public static string GetApprovalCategory(RegistryAddress location) {
			var name = location.SubKeyPath[(location.SubKeyPath.LastIndexOf('\\') + 1)..];
			if (!name.Equals("Run", StringComparison.OrdinalIgnoreCase))
				return null;
			return location.Hive == RegistryHive.LocalMachine && location.View == RegistryView.Registry32 && System.Environment.Is64BitOperatingSystem ? "Run32" : "Run";
		}

		public static byte[] ReadApproval(RegistryHive hive, string category, string name) {
			Guard.ArgumentNotNullOrWhitespace(category, nameof(category));
			Guard.Argument(!category.Contains('\\') && !category.Contains('\0'), nameof(category), "Specify one startup approval category.");
			var view = System.Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
			return new RegistryUtil().ReadValue(new RegistryAddress(hive, view, ApprovalPath + category), name)?.Data as byte[];
		}

		public static string GetStartupDirectory(bool allUsers)
			=> System.Environment.GetFolderPath(allUsers ? System.Environment.SpecialFolder.CommonStartup : System.Environment.SpecialFolder.Startup);

		public static string[] GetStartupFiles(bool allUsers) => GetStartupFilesCore(allUsers, null);

		public static Task<string[]> GetStartupFilesAsync(bool allUsers, CancellationToken cancellationToken = default)
			=> Task.Run(() => GetStartupFilesCore(allUsers, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

		private static string[] GetStartupFilesCore(bool allUsers, Action checkpoint) {
			checkpoint?.Invoke();
			var folder = GetStartupDirectory(allUsers);
			if (string.IsNullOrEmpty(folder))
				return System.Array.Empty<string>();
			var files = new List<string>();
			foreach (var file in Directory.EnumerateFiles(folder)) {
				checkpoint?.Invoke();
				files.Add(file);
			}
			return files.ToArray();
		}
	}
}
