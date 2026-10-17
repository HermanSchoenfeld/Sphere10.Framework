// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Security.AccessControl;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

public class RegistryUtil {
	private readonly SynchronizedObject _registryAccess = new();

	public bool TryResolveHive(string hiveName, out RegistryHive hive) {
		hive = RegistryHive.ClassesRoot;
		switch (hiveName?.ToUpperInvariant()) {
			case "HKEY_CLASSES_ROOT": hive = RegistryHive.ClassesRoot; return true;
			case "HKEY_CURRENT_USER": hive = RegistryHive.CurrentUser; return true;
			case "HKEY_LOCAL_MACHINE": hive = RegistryHive.LocalMachine; return true;
			case "HKEY_USERS": hive = RegistryHive.Users; return true;
			case "HKEY_CURRENT_CONFIG": hive = RegistryHive.CurrentConfig; return true;
			default: return false;
		}
	}

	/// <summary>The returned local or remote hive handle belongs to the caller.</summary>
	public RegistryKey OpenRegistryHive(string hostname, RegistryHive hive)
		=> string.IsNullOrEmpty(hostname) ? RegistryKey.OpenBaseKey(hive, RegistryView.Default)
			: RegistryKey.OpenRemoteBaseKey(hive, hostname.TrimStart('\\'));

	public bool TryOpenKey(string host, string address, out RegistryKey outputKey) => TryOpenKey(host, address, true, out outputKey);

	public bool TryOpenKey(string host, string address, bool writable, out RegistryKey outputKey)
		=> TryOpenKey(host, address, writable ? RegistryRights.ReadKey | RegistryRights.WriteKey : RegistryRights.ReadKey, out outputKey);

	public bool TryOpenKey(string host, string address, RegistryRights rights, out RegistryKey outputKey) {
		outputKey = null;
		RegistryKey root = null;
		using var rootCleanup = Tools.Scope.ExecuteOnDispose(() => root?.Dispose());
		try {
			if (string.IsNullOrWhiteSpace(address))
				return false;
			var separator = address.IndexOf('\\');
			var hiveText = separator < 0 ? address : address[..separator];
			if (!TryResolveHive(hiveText, out var hive))
				return false;
			root = OpenRegistryHive(host, hive);
			var subKey = separator < 0 ? string.Empty : address[(separator + 1)..];
			const RegistryRights mutationRights = RegistryRights.SetValue | RegistryRights.CreateSubKey | RegistryRights.CreateLink
				| RegistryRights.Delete | RegistryRights.ChangePermissions | RegistryRights.TakeOwnership;
			var check = (rights & mutationRights) != 0 ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree;
			// An empty subkey is a fresh handle to the hive itself, with the same explicit rights as any other address.
			outputKey = root.OpenSubKey(subKey, check, rights);
			return outputKey != null;
		} catch {
			outputKey?.Dispose();
			outputKey = null;
			return false;
		}
	}

	public RegistryKey OpenKey(string host, string address) => OpenKey(host, address, true);

	public RegistryKey OpenKey(string host, string address, bool writable) {
		if (!TryOpenKey(host, address, writable, out var key))
			throw new SoftwareException($"Could not open registry key '{address}' on '{host}' with {(writable ? "write" : "read")} access.");
		return key;
	}

	public RegistryKey OpenKey(string host, string address, RegistryRights rights) {
		if (!TryOpenKey(host, address, rights, out var key))
			throw new SoftwareException($"Could not open registry key '{address}' on '{host}' with rights '{rights}'.");
		return key;
	}

	public bool KeyExists(string host, string address) {
		var exists = TryOpenKey(host, address, false, out var key);
		using (key)
			return exists;
	}

	public string[] GetSubKeys(string host, string key) {
		using var opened = OpenKey(host, key, false);
		return opened.GetSubKeyNames().Select(name => key.TrimEnd('\\') + "\\" + name).ToArray();
	}


	/// <summary>The caller owns the returned handle. Missing keys return null only when explicitly requested.</summary>
	public RegistryKey OpenKey(RegistryAddress address, bool writable = false, bool createIfMissing = false, bool throwIfMissing = true) {
		ValidateAddress(address);
		Guard.Argument(!createIfMissing || writable, nameof(createIfMissing), "Creating a registry key requires write access.");
		using var hive = RegistryKey.OpenBaseKey(address.Hive, address.View);
		var key = createIfMissing ? hive.CreateSubKey(address.SubKeyPath, true) : hive.OpenSubKey(address.SubKeyPath, writable);
		Guard.Ensure(!throwIfMissing || key != null, $"Registry key does not exist: {address.Hive}\\{address.SubKeyPath} ({address.View}).");
		return key;
	}

	public RegistryAddress[] GetSubKeys(RegistryAddress address) => GetSubKeysCore(address, null);

	public Task<RegistryAddress[]> GetSubKeysAsync(RegistryAddress address, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetSubKeysCore(address, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private RegistryAddress[] GetSubKeysCore(RegistryAddress address, Action checkpoint) {
		using var scope = _registryAccess.EnterReadScope();
		checkpoint?.Invoke();
		using var key = OpenKey(address);
		var children = new List<RegistryAddress>();
		foreach (var name in key.GetSubKeyNames()) {
			checkpoint?.Invoke();
			children.Add(Child(address, name));
		}
		return children.ToArray();
	}

	public string[] GetValueNames(RegistryAddress address) {
		using var scope = _registryAccess.EnterReadScope();
		using var key = OpenKey(address);
		return key.GetValueNames();
	}

	/// <summary>Returns null for a missing key/value; preserves expandable strings without expansion.</summary>
	public RegistryValue ReadValue(RegistryAddress address, string name) {
		ValidateValueName(name);
		using var scope = _registryAccess.EnterReadScope();
		using var key = OpenKey(address, throwIfMissing: false);
		return key == null || !key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase) ? null : ReadValue(key, address, name);
	}

	public RegistryValue[] ReadValues(RegistryAddress address, bool throwIfMissing = true) => ReadValuesCore(address, throwIfMissing, null);

	public Task<RegistryValue[]> ReadValuesAsync(RegistryAddress address, bool throwIfMissing = true, CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadValuesCore(address, throwIfMissing, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private RegistryValue[] ReadValuesCore(RegistryAddress address, bool throwIfMissing, Action checkpoint) {
		using var scope = _registryAccess.EnterReadScope();
		checkpoint?.Invoke();
		using var key = OpenKey(address, throwIfMissing: throwIfMissing);
		if (key == null)
			return Array.Empty<RegistryValue>();
		var values = new List<RegistryValue>();
		foreach (var name in key.GetValueNames()) {
			checkpoint?.Invoke();
			try {
				values.Add(ReadValue(key, address, name));
			} catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException or ArgumentException) {
				values.Add(new RegistryValue(address, name, RegistryValueKind.Unknown, null, error));
			}
		}
		return values.ToArray();
	}

	public RegistryAddress CreateKey(RegistryAddress parent, string name) {
		ValidateComponent(name);
		using var scope = _registryAccess.EnterWriteScope();
		using var key = OpenKey(parent, true);
		Guard.Ensure(!key.GetSubKeyNames().Contains(name, StringComparer.OrdinalIgnoreCase), "A registry subkey with that name already exists.");
		using var created = key.CreateSubKey(name, true);
		Guard.Ensure(created != null, "Windows did not create the requested registry key.");
		return Child(parent, name);
	}

	public RegistryAddress RenameKey(RegistryAddress address, string newName) {
		ValidateComponent(newName);
		var parent = Parent(address, out var name);
		Guard.Argument(!string.Equals(name, newName, StringComparison.OrdinalIgnoreCase), nameof(newName), "The new registry key name must differ from the original.");
		using var scope = _registryAccess.EnterWriteScope();
		using var key = OpenKey(address, true);
		var error = WinAPI.ADVAPI32.RegRenameKey(key.Handle, null, newName);
		if (error != 0)
			throw new Win32Exception(error);
		return Child(parent, newName);
	}

	public void DeleteKey(RegistryAddress address, bool ignoreMissing = false) {
		var parent = Parent(address, out var name);
		using var scope = _registryAccess.EnterWriteScope();
		using var key = OpenKey(parent, true, throwIfMissing: !ignoreMissing);
		key?.DeleteSubKeyTree(name, !ignoreMissing);
	}

	public void SetValue(RegistryAddress address, string name, RegistryValueKind kind, object data, bool createOnly = false, RegistryValue expected = null, bool createKey = false) {
		ValidateValueName(name);
		ValidateValue(kind, data);
		using var scope = _registryAccess.EnterWriteScope();
		using var key = OpenKey(address, true, createIfMissing: createKey);
		Guard.Ensure(!createOnly || !key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase), "A registry value with that name already exists.");
		if (expected != null)
			VerifyExpected(key, address, name, expected);
		key.SetValue(name, data is Array array ? array.Clone() : data, kind);
	}

	public void DeleteValue(RegistryAddress address, string name, RegistryValue expected = null, bool ignoreMissing = false) {
		ValidateValueName(name);
		using var scope = _registryAccess.EnterWriteScope();
		using var key = OpenKey(address, true, throwIfMissing: !ignoreMissing);
		if (key == null)
			return;
		if (expected != null)
			VerifyExpected(key, address, name, expected);
		key.DeleteValue(name, !ignoreMissing);
	}

	public void RenameValue(RegistryAddress address, string name, string newName, RegistryValue expected = null) {
		ValidateValueName(name);
		ValidateValueName(newName);
		Guard.Argument(!string.Equals(name, newName, StringComparison.OrdinalIgnoreCase), nameof(newName), "The new registry value name must differ from the original.");
		using var scope = _registryAccess.EnterWriteScope();
		using var key = OpenKey(address, true);
		if (expected != null)
			VerifyExpected(key, address, name, expected);
		Guard.Ensure(!key.GetValueNames().Contains(newName, StringComparer.OrdinalIgnoreCase), "A registry value with the new name already exists.");
		var original = ReadValue(key, address, name);
		ValidateValue(original.Kind, original.Data);
		key.SetValue(newName, original.Data, original.Kind);
		try {
			key.DeleteValue(name, true);
		} catch (Exception error) {
			try {
				key.DeleteValue(newName, false);
			} catch (Exception rollbackError) {
				throw new AggregateException("Registry value rename and rollback both failed.", error, rollbackError);
			}
			throw;
		}
	}

	/// <summary>Creates cached futures that open registry handles by address only when values or children are requested.</summary>
	public RegistryTree ReadTree(RegistryAddress address, int maximumDepth, int maximumEntries, TimeSpan timeLimit)
		=> CreateTree(address, 0, new RegistryReadBudget(maximumDepth, maximumEntries, timeLimit, null));

	/// <summary>Fully materializes the same future-backed tree on a worker, respecting the supplied cancellation and read limits.</summary>
	public Task<RegistryTree> ReadTreeAsync(RegistryAddress address, int maximumDepth, int maximumEntries, TimeSpan timeLimit, CancellationToken cancellationToken = default)
		=> Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			var tree = CreateTree(address, 0, new RegistryReadBudget(maximumDepth, maximumEntries, timeLimit, cancellationToken.ThrowIfCancellationRequested));
			Materialize(tree, cancellationToken.ThrowIfCancellationRequested);
			return tree;
		}, cancellationToken);

	private RegistryTree CreateTree(RegistryAddress address, int depth, RegistryReadBudget budget, bool nodeCounted = false) {
		ValidateAddress(address);
		void CountNode() {
			budget.Check();
			Guard.Ensure(depth <= budget.MaximumDepth, "The registry read exceeded its depth limit.");
			if (!nodeCounted) {
				budget.TakeEntry();
				nodeCounted = true;
			}
		}
		var values = Tools.Values.Future.Reloadable(() => {
			using var load = budget.EnterLoad();
			using var read = _registryAccess.EnterReadScope();
			CountNode();
			using var key = OpenKey(address);
			var result = new List<RegistryValue>();
			foreach (var name in key.GetValueNames().OrderBy(name => name, StringComparer.Ordinal)) {
				budget.TakeEntry();
				result.Add(ReadValue(key, address, name));
			}
			budget.Check();
			return result.ToArray();
		});
		var children = Tools.Values.Future.Reloadable(() => {
			using var load = budget.EnterLoad();
			using var read = _registryAccess.EnterReadScope();
			CountNode();
			using var key = OpenKey(address);
			var result = new List<RegistrySubTree>();
			foreach (var name in key.GetSubKeyNames().OrderBy(name => name, StringComparer.Ordinal)) {
				budget.TakeEntry();
				Guard.Ensure(depth < budget.MaximumDepth, "The registry read exceeded its depth limit.");
				result.Add(new RegistrySubTree(name, CreateTree(Child(address, name), depth + 1, budget, true)));
			}
			budget.Check();
			return result.ToArray();
		});
		return new RegistryTree(values, children);
	}

	private static void Materialize(RegistryTree tree, Action checkpoint) {
		checkpoint();
		_ = tree.Values;
		foreach (var child in tree.SubKeys) {
			checkpoint();
			Materialize(child.Contents, checkpoint);
		}
	}

	private sealed class RegistryReadBudget : SynchronizedObject {
		private readonly TimeSpan _timeLimit;
		private readonly Action _checkpoint;
		private int _remaining;
		private TimeSpan _elapsed;
		private long _loadStarted;

		public RegistryReadBudget(int maximumDepth, int maximumEntries, TimeSpan timeLimit, Action checkpoint) {
			Guard.ArgumentGTE(maximumDepth, 0, nameof(maximumDepth));
			Guard.ArgumentGT(maximumEntries, 0, nameof(maximumEntries));
			Guard.Argument(timeLimit > TimeSpan.Zero, nameof(timeLimit), "Specify a positive registry read time limit.");
			MaximumDepth = maximumDepth;
			_remaining = maximumEntries;
			_timeLimit = timeLimit;
			_checkpoint = checkpoint;
		}

		public int MaximumDepth { get; }

		public IDisposable EnterLoad() {
			var write = EnterWriteScope();
			_loadStarted = Stopwatch.GetTimestamp();
			try {
				Check();
				return new ActionScope(() => {
					_elapsed += Stopwatch.GetElapsedTime(_loadStarted);
					write.Dispose();
				});
			} catch {
				write.Dispose();
				throw;
			}
		}

		public void Check() {
			_checkpoint?.Invoke();
			Guard.Ensure(_elapsed + Stopwatch.GetElapsedTime(_loadStarted) < _timeLimit, "The registry read exceeded its time limit.");
		}

		public void TakeEntry() {
			Check();
			Guard.Ensure(_remaining-- > 0, "The registry read exceeded its entry limit.");
		}
	}

	private static RegistryValue ReadValue(RegistryKey key, RegistryAddress address, string name)
		=> new(address, name, key.GetValueKind(name), key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames));

	private static void VerifyExpected(RegistryKey key, RegistryAddress address, string name, RegistryValue expected) {
		Guard.Argument(expected.Address.Hive == address.Hive && expected.Address.View == address.View
			&& string.Equals(expected.Address.SubKeyPath, address.SubKeyPath, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(expected.Name, name, StringComparison.OrdinalIgnoreCase), nameof(expected), "The expected registry value belongs to another address or name.");
		var current = ReadValue(key, address, name);
		var equal = current.Kind == expected.Kind && current.ReadError == null && expected.ReadError == null
			&& (current.Data is Array actual && expected.Data is Array previous ? actual.Cast<object>().SequenceEqual(previous.Cast<object>()) : Equals(current.Data, expected.Data));
		Guard.Ensure(equal, "The registry value no longer matches the expected data.");
	}

	private static RegistryAddress Child(RegistryAddress address, string name)
		=> address with { SubKeyPath = string.IsNullOrEmpty(address.SubKeyPath) ? name : address.SubKeyPath + "\\" + name };

	private static RegistryAddress Parent(RegistryAddress address, out string name) {
		ValidateAddress(address);
		Guard.Argument(address.SubKeyPath.Length > 0, nameof(address), "A registry hive cannot be renamed or deleted.");
		var separator = address.SubKeyPath.LastIndexOf('\\');
		name = address.SubKeyPath[(separator + 1)..];
		return address with { SubKeyPath = separator < 0 ? string.Empty : address.SubKeyPath[..separator] };
	}

	private static void ValidateAddress(RegistryAddress address) {
		Guard.Argument(Enum.IsDefined(address.Hive) && Enum.IsDefined(address.View), nameof(address), "Specify a valid registry hive and view.");
		Guard.ArgumentNotNull(address.SubKeyPath, nameof(address));
		Guard.Argument(!address.SubKeyPath.Contains('\0'), nameof(address), "Registry paths cannot contain NUL characters.");
	}

	private static void ValidateComponent(string name) {
		Guard.ArgumentNotNull(name, nameof(name));
		Guard.Argument(name.Length is > 0 and <= 255 && !name.Contains('\\') && !name.Contains('\0'), nameof(name), "A registry key name must be one valid component.");
	}

	private static void ValidateValueName(string name) {
		Guard.ArgumentNotNull(name, nameof(name));
		Guard.Argument(name.Length <= 16383 && !name.Contains('\0'), nameof(name), "A registry value name cannot contain NUL or exceed the Windows limit.");
	}

	private static void ValidateValue(RegistryValueKind kind, object data) {
		Guard.ArgumentNotNull(data, nameof(data));
		var valid = kind switch {
			RegistryValueKind.String or RegistryValueKind.ExpandString => data is string text && !text.Contains('\0'),
			RegistryValueKind.MultiString => data is string[] lines && lines.All(line => line != null && line.Length > 0 && !line.Contains('\0')),
			RegistryValueKind.Binary or RegistryValueKind.None => data is byte[],
			RegistryValueKind.DWord => data is int,
			RegistryValueKind.QWord => data is long,
			_ => false
		};
		Guard.Argument(valid, nameof(data), "The registry data does not match the Windows value type.");
	}
}
