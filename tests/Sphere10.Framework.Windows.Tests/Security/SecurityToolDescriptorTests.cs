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
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using NUnit.Framework;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests.Security;

[TestFixture]
[NonParallelizable]
public class SecurityToolDescriptorTests {
	[TestCase(false)]
	[TestCase(true)]
	public void FileAndDirectoryDescriptorWritesPreserveOwnershipWhenOnlyAccessChanges(bool directory) {
		var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Sphere10Security-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => Directory.Delete(root, true));
		var path = directory ? root : Path.Combine(root, "owned.txt");
		if (!directory)
			File.WriteAllText(path, "Owned descriptor fixture");
		const AccessControlSections sections = AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access;
		FileSystemSecurity Read() => directory ? SecurityTool.GetDirectorySecurity(path, sections) : SecurityTool.GetFileSecurity(path, sections);
		var before = Read();
		var owner = before.GetOwner(typeof(SecurityIdentifier));
		var group = before.GetGroup(typeof(SecurityIdentifier));
		var update = Read();
		var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
		update.AddAccessRule(new FileSystemAccessRule(world, FileSystemRights.ReadAttributes, AccessControlType.Allow));
		if (directory)
			SecurityTool.SetDirectorySecurity(path, (DirectorySecurity)update);
		else
			SecurityTool.SetFileSecurity(path, (FileSecurity)update);
		var after = Read();
		Assert.That(after.GetOwner(typeof(SecurityIdentifier)), Is.EqualTo(owner));
		Assert.That(after.GetGroup(typeof(SecurityIdentifier)), Is.EqualTo(group));
		Assert.That(after.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>()
			.Any(rule => rule.IdentityReference.Equals(world) && rule.FileSystemRights.HasFlag(FileSystemRights.ReadAttributes)), Is.True);
	}

	[Test]
	public void RegistryDescriptorAccessorDoesNotCreateMissingKeysAndWritesOnlyRequestedSections() {
		var path = "Software\\Sphere10\\Tests\\Descriptor-" + Guid.NewGuid().ToString("N");
		var address = "HKEY_CURRENT_USER\\" + path;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => Registry.CurrentUser.DeleteSubKeyTree(path, false));
		Assert.That(() => SecurityTool.GetRegistrySecurity(string.Empty, address, AccessControlSections.Access), Throws.InstanceOf<SoftwareException>());
		Assert.That(new RegistryUtil().KeyExists(string.Empty, address), Is.False);
		using (Registry.CurrentUser.CreateSubKey(path)) { }
		const AccessControlSections sections = AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access;
		var before = SecurityTool.GetRegistrySecurity(string.Empty, address, sections);
		var update = new RegistrySecurity();
		update.SetSecurityDescriptorBinaryForm(before.GetSecurityDescriptorBinaryForm(), sections);
		var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
		update.AddAccessRule(new RegistryAccessRule(world, RegistryRights.QueryValues, AccessControlType.Allow));
		// The source marks ownership dirty too; the accessor must still persist only the requested access section.
		update.SetOwner(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
		SecurityTool.SetRegistrySecurity(string.Empty, address, update, AccessControlSections.Access);
		var after = SecurityTool.GetRegistrySecurity(string.Empty, address, sections);
		Assert.That(after.GetOwner(typeof(SecurityIdentifier)), Is.EqualTo(before.GetOwner(typeof(SecurityIdentifier))));
		Assert.That(after.GetGroup(typeof(SecurityIdentifier)), Is.EqualTo(before.GetGroup(typeof(SecurityIdentifier))));
		Assert.That(after.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<RegistryAccessRule>()
			.Any(rule => rule.IdentityReference.Equals(world) && rule.RegistryRights.HasFlag(RegistryRights.QueryValues)), Is.True);
	}
}
