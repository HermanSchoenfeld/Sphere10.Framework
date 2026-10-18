// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sphere10.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows;

public class EnvironmentUtil {
	private static readonly SynchronizedObject _writes = new();

	public string ExpandVariables(string value) {
		Guard.ArgumentNotNull(value, nameof(value));
		return Environment.ExpandEnvironmentVariables(value);
	}

	public Dictionary<string, string> ReadAll(EnvironmentVariableTarget scope) => ReadAllCore(scope, null);

	public Task<Dictionary<string, string>> ReadAllAsync(EnvironmentVariableTarget scope, CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadAllCore(scope, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static Dictionary<string, string> ReadAllCore(EnvironmentVariableTarget scope, Action checkpoint) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(typeof(EnvironmentUtil));
		Guard.Argument(scope is EnvironmentVariableTarget.Process or EnvironmentVariableTarget.User or EnvironmentVariableTarget.Machine,
			nameof(scope), "Specify Process, User or Machine environment scope.");
		checkpoint?.Invoke();
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables(scope)) {
			checkpoint?.Invoke();
			if (entry.Key is string name)
				result[name] = entry.Value?.ToString() ?? string.Empty;
		}
		checkpoint?.Invoke();
		return result;
	}

	public Result Validate(string name, string value, EnvironmentVariableTarget scope) {
		var result = new Result();
		if (scope is not EnvironmentVariableTarget.Process and not EnvironmentVariableTarget.User and not EnvironmentVariableTarget.Machine)
			result.AddError("The environment scope must be Process, User or Machine.");
		if (string.IsNullOrWhiteSpace(name))
			result.AddError("An environment variable name is required.");
		else if (name.IndexOfAny(new[] { '=', '\0', '\r', '\n' }) >= 0)
			result.AddError("Variable names cannot contain equals signs, null characters or line breaks.");
		else if (name.Length >= (scope == EnvironmentVariableTarget.Process ? 32767 : 255))
			result.AddError(scope == EnvironmentVariableTarget.Process ? "Process variable names must contain fewer than 32,767 characters." : "Persistent variable names must contain fewer than 255 characters.");
		if (value == null)
			result.AddError("An environment variable value cannot be null; an empty value is allowed.");
		else if (value.Contains('\0'))
			result.AddError("Variable values cannot contain null characters.");
		else if (value.Length >= 32767)
			result.AddError("Variable values must contain fewer than 32,767 characters.");
		return result;
	}

	public Task<string> ReadAsync(string name, EnvironmentVariableTarget scope, CancellationToken cancellationToken = default) {
		Validate(name, string.Empty, scope).ThrowOnFailure();
		return Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return Read(name, scope);
		}, cancellationToken);
	}

	public Task<string> SetAsync(string name, string value, EnvironmentVariableTarget scope, string expectedOriginalValue, CancellationToken cancellationToken = default) {
		Validate(name, value, scope).ThrowOnFailure();
		return Task.Run(() => Change(name, value, scope, expectedOriginalValue, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
	}

	public Task<string> DeleteAsync(string name, EnvironmentVariableTarget scope, string expectedOriginalValue, CancellationToken cancellationToken = default) {
		Validate(name, string.Empty, scope).ThrowOnFailure();
		Guard.ArgumentNotNull(expectedOriginalValue, nameof(expectedOriginalValue));
		return Task.Run(() => Change(name, null, scope, expectedOriginalValue, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
	}

	private static string Read(string name, EnvironmentVariableTarget scope) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(typeof(EnvironmentUtil));
		if (scope == EnvironmentVariableTarget.Process)
			return Environment.GetEnvironmentVariable(name, scope);
		using var root = RegistryKey.OpenBaseKey(scope == EnvironmentVariableTarget.User ? RegistryHive.CurrentUser : RegistryHive.LocalMachine, RegistryView.Default);
		using var key = root.OpenSubKey(EnvironmentPath(scope), false);
		return key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
	}

	private static string Change(string name, string value, EnvironmentVariableTarget scope, string expectedOriginalValue, Action checkpoint) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(typeof(EnvironmentUtil));
		// Serialize changes made through this tool and recheck the original before overwriting it.
		using var write = _writes.EnterWriteScope();
		checkpoint?.Invoke();
		if (scope == EnvironmentVariableTarget.Process) {
			Guard.Ensure(string.Equals(Read(name, scope), expectedOriginalValue, StringComparison.Ordinal), "The current environment value no longer matches the expected original value.");
			Environment.SetEnvironmentVariable(name, value, scope);
			return string.Empty;
		}

		// Preserve expansion semantics instead of rewriting PATH and similar REG_EXPAND_SZ values as REG_SZ.
		using var root = RegistryKey.OpenBaseKey(scope == EnvironmentVariableTarget.User ? RegistryHive.CurrentUser : RegistryHive.LocalMachine, RegistryView.Default);
		using var key = root.CreateSubKey(EnvironmentPath(scope), true);
		var original = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
		Guard.Ensure(original == null || original is string, "This environment entry has an unsupported Windows registry type.");
		Guard.Ensure(string.Equals(original as string, expectedOriginalValue, StringComparison.Ordinal), "The current environment value no longer matches the expected original value.");
		checkpoint?.Invoke();
		if (value == null) {
			key.DeleteValue(name, false);
		} else {
			var kind = original == null ? value.Contains('%') ? RegistryValueKind.ExpandString : RegistryValueKind.String : key.GetValueKind(name);
			Guard.Ensure(kind is RegistryValueKind.String or RegistryValueKind.ExpandString, "This environment entry has an unsupported registry type.");
			key.SetValue(name, value, kind);
		}

		// Notify interested windows after persistence. The timeout is per target window, so stay off the UI.
		var notified = WinAPI.USER32.SendMessageTimeoutW(WinAPI.USER32.HWND_BROADCAST,
			WinAPI.USER32.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", WinAPI.USER32.SMTO_ABORTIFHUNG, 100, out _);
		return notified != IntPtr.Zero ? string.Empty : $"The change was saved, but some windows did not acknowledge the environment notification (Win32 {Marshal.GetLastWin32Error()}).";
	}

	private static string EnvironmentPath(EnvironmentVariableTarget scope) => scope == EnvironmentVariableTarget.User
		? "Environment" : @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
}
