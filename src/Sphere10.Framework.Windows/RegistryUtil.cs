// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Provides utility methods for Windows registry operations.
/// </summary>
public class RegistryUtil {

	public bool TryResolveHive(string hiveName, out RegistryHive hive) {
		Debug.Assert(!String.IsNullOrEmpty(hiveName));
		bool retval = true;
		hive = RegistryHive.ClassesRoot;
		switch (hiveName.ToUpper()) {
			case "HKEY_CLASSES_ROOT":
				hive = RegistryHive.ClassesRoot;
				break;
			case "HKEY_CURRENT_USER":
				hive = RegistryHive.CurrentUser;
				break;
			case "HKEY_LOCAL_MACHINE":
				hive = RegistryHive.LocalMachine;
				break;
			case "HKEY_USERS":
				hive = RegistryHive.Users;
				break;
			case "HKEY_CURRENT_CONFIG":
				hive = RegistryHive.CurrentConfig;
				break;
			default:
				retval = false;
				break;
		}
		return retval;
	}

	public RegistryKey OpenRegistryHive(string hostname, RegistryHive hive) {
		RegistryKey retval = Registry.LocalMachine;
		if (String.IsNullOrEmpty(hostname)) {
			switch (hive) {
				case RegistryHive.ClassesRoot:
					retval = Registry.ClassesRoot;
					break;
				case RegistryHive.CurrentConfig:
					retval = Registry.CurrentConfig;
					break;
				case RegistryHive.CurrentUser:
					retval = Registry.CurrentUser;
					break;
				case RegistryHive.LocalMachine:
					retval = Registry.LocalMachine;
					break;
				case RegistryHive.PerformanceData:
					retval = Registry.PerformanceData;
					break;
				case RegistryHive.Users:
					retval = Registry.Users;
					break;
				default:
					throw new SoftwareException($"Unable to open registry hive '{hive}'");
			}
		} else {
			retval = RegistryKey.OpenRemoteBaseKey(hive, hostname);
		}
		return retval;
	}

	public bool TryOpenKey(string host, string address, out RegistryKey outputKey) {
		Debug.Assert(!String.IsNullOrEmpty(address));
		outputKey = null;
		bool retval = false;
		try {
			string[] splits = address.Split('\\');
			string hiveText = splits[0].ToUpper();
			RegistryHive hive;
			if (!String.IsNullOrEmpty(splits[0]) && TryResolveHive(splits[0], out hive)) {
				outputKey = OpenRegistryHive(host, hive);
				if (splits.Length > 0) {
					StringBuilder remainingAddress = new StringBuilder();
					for (int i = 1; i < splits.Length; i++) {
						if (i > 1) {
							remainingAddress.Append('\\');
						}
						remainingAddress.Append(splits[i]);
					}
					outputKey = outputKey.OpenSubKey(remainingAddress.ToString(), true);
					retval = true;
				} else {
					retval = true;
				}
			}
		} catch {
		}
		return retval;
	}

	public RegistryKey OpenKey(string host, string address) {
		Debug.Assert(!String.IsNullOrEmpty(address));
		RegistryKey retval;
		if (!TryOpenKey(host, address, out retval)) {
			throw new SoftwareException(
				String.Format("Could not retrieve the registry key '{0}' as it does not exist", address)
			);
		}
		return retval;
	}

	public bool KeyExists(string host, string address) {
		RegistryKey key;
		return TryOpenKey(host, address, out key);
	}

	public string[] GetSubKeys(string host, string key) {
		Debug.Assert(KeyExists(host, key));
		List<string> subKeys = new List<string>();

		string[] subKeyNames = OpenKey(host, key).GetSubKeyNames();

		foreach (string subKeyName in subKeyNames) {
			subKeys.Add(key + "\\" + subKeyName);
		}
		return subKeys.ToArray();
	}

}
