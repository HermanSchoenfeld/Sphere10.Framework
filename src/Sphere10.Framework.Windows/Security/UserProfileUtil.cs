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
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Security;

namespace Tools;

public static partial class Windows {

	// The computer's user profiles, read from the profile list Windows keeps in the registry.
	public static partial class Security {
		private const string ProfileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
		private const string HiveFileName = "NTUSER.DAT";

		/// <summary>Reads every registered user profile and the template profile, with the state of each one's NTUSER.DAT. Nothing is opened or changed.</summary>
		public static WindowsUserProfile[] ReadUserProfiles() {
			Debug.AssertNotMainThread();
			using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using var list = machine.OpenSubKey(ProfileListPath);
			Guard.Ensure(list != null, "Windows has no user profile list on this computer.");
			string[] loaded;
			using (var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default))
				loaded = users.GetSubKeyNames();
			var profiles = new List<WindowsUserProfile>();
			foreach (var sid in list.GetSubKeyNames()) {
				using var entry = list.OpenSubKey(sid);
				if (entry?.GetValue("ProfileImagePath") is not string path || string.IsNullOrWhiteSpace(path))
					continue;
				profiles.Add(Describe(sid, ResolveAccountName(sid), path, loaded.Contains(sid, StringComparer.OrdinalIgnoreCase), sid is "S-1-5-18" or "S-1-5-19" or "S-1-5-20"));
			}
			// New users start from a copy of this profile, so editing its hive changes what every new account begins with.
			if (list.GetValue("Default") is string template && !string.IsNullOrWhiteSpace(template))
				profiles.Add(Describe(null, null, template, false, false));
			return profiles.OrderBy(profile => profile.Sid == null).ThenBy(profile => profile.IsSystemAccount).ThenBy(profile => profile.AccountName ?? profile.Sid, StringComparer.OrdinalIgnoreCase).ToArray();
		}

		public static Task<WindowsUserProfile[]> ReadUserProfilesAsync(CancellationToken cancellationToken = default)
			=> Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				return ReadUserProfiles();
			}, cancellationToken);

		private static WindowsUserProfile Describe(string sid, string accountName, string profilePath, bool loaded, bool systemAccount) {
			var expanded = System.Environment.ExpandEnvironmentVariables(profilePath);
			var hive = Path.Combine(expanded, HiveFileName);
			long? length = null;
			DateTime? written = null;
			Tools.Exceptions.ExecuteCapturingException(() => {
				var file = new FileInfo(hive);
				Guard.Ensure(file.Exists, "The profile has no NTUSER.DAT.");
				length = file.Length;
				written = file.LastWriteTimeUtc;
			}, out var error);
			return new WindowsUserProfile(sid, accountName, expanded, hive, loaded, systemAccount, length, written, error?.Message);
		}

		// A deleted or unreachable domain account leaves a profile whose identifier no longer has a name.
		private static string ResolveAccountName(string sid) {
			string name = null;
			Tools.Exceptions.ExecuteIgnoringException(() => name = new SecurityIdentifier(sid).Translate(typeof(NTAccount)).Value);
			return name;
		}
	}
}
