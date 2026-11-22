// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows;

/// <summary>
/// A registry hive file loaded as a sub-key of HKEY_USERS so that it can be read and edited like any other key, such as the
/// NTUSER.DAT of a user who is not signed in. Create one with <c>Tools.Windows.Registry.LoadHive</c>. While it is loaded Windows
/// holds the file, so that user cannot sign in until it is unloaded: unload it as soon as the work is done.
/// </summary>
/// <remarks>
/// Unloading writes the hive's changes to its file. Windows refuses to unload while any key of the hive is open, so dispose
/// every key first. <see cref="Unload"/> reports that refusal and leaves the hive loaded; disposal tries once and does not throw.
/// </remarks>
public sealed class RegistryHiveMount : SyncDisposable {
	private const int ErrorAccessDenied = 5;
	private int _loaded;

	internal RegistryHiveMount(string name, string filePath) {
		Name = name;
		FilePath = filePath;
		_loaded = 1;
	}

	/// <summary>The name of the sub-key of HKEY_USERS that holds the hive.</summary>
	public string Name { get; }

	/// <summary>The full path of the hive file.</summary>
	public string FilePath { get; }

	/// <summary>The loaded hive's root key.</summary>
	public RegistryAddress Address => new(RegistryHive.Users, RegistryView.Default, Name);

	/// <summary>Whether this mount still holds the hive.</summary>
	public bool IsLoaded => Volatile.Read(ref _loaded) != 0;

	/// <summary>Unloads the hive, writing its changes to the file and releasing it. Throws while a key of the hive is still open, leaving it loaded.</summary>
	public void Unload() {
		Debug.AssertNotMainThread();
		if (!IsLoaded)
			return;
		var error = UnloadCore();
		if (error == ErrorAccessDenied) {
			// A key handle that was dropped without disposal keeps the hive open until it is finalized.
			GC.Collect();
			GC.WaitForPendingFinalizers();
			error = UnloadCore();
		}
		if (error != 0)
			throw new Win32Exception(error, error == ErrorAccessDenied
				? $"Windows cannot unload the registry hive '{Name}' while one of its keys is open, or without the backup and restore privileges."
				: $"Windows could not unload the registry hive '{Name}': {new Win32Exception(error).Message}");
		Volatile.Write(ref _loaded, 0);
	}

	public Task UnloadAsync(CancellationToken cancellationToken = default) => Task.Run(Unload, cancellationToken);

	protected override void DisposeInternal() {
		if (IsLoaded && UnloadCore() == 0)
			Volatile.Write(ref _loaded, 0);
	}

	internal static int LoadCore(string name, string filePath) {
		using var privileges = Tools.Windows.Security.EnterScope(WinAPI.NETAPI32.SE_BACKUP_NAME, WinAPI.NETAPI32.SE_RESTORE_NAME);
		using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
		return WinAPI.ADVAPI32.RegLoadKey(users.Handle, name, filePath);
	}

	private int UnloadCore() {
		// A missing privilege is reported like any other refusal, so disposal never throws.
		if (!Tools.Windows.Security.TryEnterScope(new[] { WinAPI.NETAPI32.SE_BACKUP_NAME, WinAPI.NETAPI32.SE_RESTORE_NAME }, out var privileges))
			return ErrorAccessDenied;
		using (privileges) {
			using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
			return WinAPI.ADVAPI32.RegUnLoadKey(users.Handle, Name);
		}
	}
}
