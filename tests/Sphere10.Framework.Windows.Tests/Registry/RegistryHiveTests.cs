// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class RegistryHiveTests {
	[Test]
	public async Task LoadingRejectsAMissingFileAndAnInvalidKeyNameBeforeAnyNativeCall() {
		var missing = Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName(), "NTUSER.DAT");
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.Registry.LoadHiveAsync(missing, "Sphere10_Test")), Is.Not.Null);
		var file = Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName() + ".dat");
		await File.WriteAllBytesAsync(file, new byte[] { 1 });
		using var delete = Tools.Scope.ExecuteOnDispose(() => File.Delete(file));
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.Registry.LoadHiveAsync(file, @"Nested\Name")), Is.InstanceOf<ArgumentException>());
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.Registry.LoadHiveAsync(file, string.Empty)), Is.InstanceOf<ArgumentException>());
		using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
		Assert.That(users.GetSubKeyNames(), Has.None.StartsWith("Sphere10_Test"), "A rejected request must leave nothing loaded.");
	}

	[Test]
	[Category("Integration")]
	public async Task LoadingAFileThatIsNotAHiveFailsAndLeavesNothingLoadedOrSaysTheCallerIsNotElevated() {
		var file = Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName() + ".dat");
		await File.WriteAllBytesAsync(file, new byte[4096]);
		using var delete = Tools.Scope.ExecuteOnDispose(() => File.Delete(file));
		var name = "Sphere10_Test_" + Guid.NewGuid().ToString("N");
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.Registry.LoadHiveAsync(file, name));
		// An unelevated caller is told so; an elevated one gets Windows' refusal of the malformed file.
		Assert.That(error, Is.InstanceOf<UnauthorizedAccessException>().Or.InstanceOf<System.ComponentModel.Win32Exception>());
		using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
		Assert.That(users.GetSubKeyNames(), Has.None.EqualTo(name));
	}

	[Test]
	[Category("Integration")]
	public async Task TheProfileListContainsTheCurrentUsersLoadedHive() {
		var sid = WindowsIdentity.GetCurrent().User.Value;
		var profiles = await Tools.Windows.Security.ReadUserProfilesAsync();
		var current = profiles.SingleOrDefault(profile => profile.Sid == sid);
		Assert.That(current, Is.Not.Null, "The signed-in user has a registered profile.");
		Assert.That(current.IsLoaded, Is.True, "Windows holds a signed-in user's hive at HKEY_USERS\\SID.");
		Assert.That(current.HivePath, Does.EndWith(@"\NTUSER.DAT").IgnoreCase);
		Assert.That(Path.IsPathFullyQualified(current.ProfilePath), Is.True, "Environment variables in the profile path are expanded.");
		Assert.That(current.AccountName, Does.Contain(@"\"));
		Assert.That(current.IsSystemAccount, Is.False);
		Assert.That(profiles.Count(profile => profile.Sid == null), Is.LessThanOrEqualTo(1), "The template profile is listed once, after the accounts.");
		Assert.That(profiles.Select(profile => profile.Sid).Where(value => value != null), Is.Unique);
	}
}
