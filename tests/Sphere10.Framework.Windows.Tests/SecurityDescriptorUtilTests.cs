// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using NUnit.Framework;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class SecurityDescriptorUtilTests {
	[Test]
	public void ExactIdentitiesIncludeWellKnownAliasesAndDoNotMatchSidPrefixes() {
		var administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
		var prefix = new SecurityIdentifier("S-1-5-21-1-2-3-100");
		var descriptor = Bytes("O:BAG:BAD:(A;;FA;;;BA)(A;;FR;;;S-1-5-21-1-2-3-1000)");
		var references = Tools.Windows.SecurityDescriptors.GetSidReferences(descriptor);
		Assert.That(references.Single(item => item.Sid.Equals(administrators)).Sections,
			Is.EqualTo(AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access));
		Assert.That(references.Any(item => item.Sid.Equals(prefix)), Is.False);
	}

	[Test]
	public void TranslationIsSimultaneousAndInvokesEachOriginalSidOnlyOnce() {
		var first = new SecurityIdentifier("S-1-5-21-1-2-3-100");
		var second = new SecurityIdentifier("S-1-5-21-1-2-3-200");
		var third = new SecurityIdentifier("S-1-5-21-1-2-3-300");
		var original = Bytes($"O:{first}G:{first}D:(A;;FA;;;{first})(A;;FR;;;{second})S:(AU;SA;FR;;;{first})");
		var calls = new Dictionary<SecurityIdentifier, int>();
		var translated = Tools.Windows.SecurityDescriptors.TranslateSecurityDescriptor(original, sid => {
			calls[sid] = calls.GetValueOrDefault(sid) + 1;
			return sid.Equals(first) ? second : sid.Equals(second) ? third : sid;
		}, AccessControlSections.All, out var changed);
		var descriptor = new RawSecurityDescriptor(translated, 0);
		Assert.That(descriptor.Owner, Is.EqualTo(second));
		Assert.That(descriptor.Group, Is.EqualTo(second));
		Assert.That(((KnownAce)descriptor.DiscretionaryAcl[0]).SecurityIdentifier, Is.EqualTo(second));
		Assert.That(((KnownAce)descriptor.DiscretionaryAcl[1]).SecurityIdentifier, Is.EqualTo(third));
		Assert.That(((KnownAce)descriptor.SystemAcl[0]).SecurityIdentifier, Is.EqualTo(second));
		Assert.That(calls.Values, Is.All.EqualTo(1));
		Assert.That(changed, Is.EqualTo(AccessControlSections.All));
	}

	[Test]
	public void SelectedSectionsKeepOwnershipAuditInheritanceObjectGuidsAndOpaqueData() {
		var source = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
		var target = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
		var objectType = Guid.NewGuid();
		var inheritedObjectType = Guid.NewGuid();
		var opaque = new byte[] { 1, 2, 3, 4 };
		var acl = new RawAcl(GenericAcl.AclRevisionDS, 1);
		acl.InsertAce(0, new ObjectAce(AceFlags.Inherited | AceFlags.ContainerInherit, AceQualifier.AccessAllowed, 0x1234, source,
			ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent, objectType, inheritedObjectType, true, opaque));
		var security = new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent | ControlFlags.DiscretionaryAclProtected
			| ControlFlags.SystemAclPresent | ControlFlags.SelfRelative, source, source, new RawAcl(GenericAcl.AclRevision, 0), acl);
		var bytes = new byte[security.BinaryLength];
		security.GetBinaryForm(bytes, 0);
		var translated = Tools.Windows.SecurityDescriptors.TranslateSecurityDescriptor(bytes, _ => target, AccessControlSections.Access, out var changed);
		var result = new RawSecurityDescriptor(translated, 0);
		var ace = (ObjectAce)result.DiscretionaryAcl[0];
		Assert.That(changed, Is.EqualTo(AccessControlSections.Access));
		Assert.That(result.Owner, Is.EqualTo(source));
		Assert.That(result.Group, Is.EqualTo(source));
		Assert.That(result.ControlFlags, Is.EqualTo(security.ControlFlags));
		Assert.That(result.SystemAcl.Count, Is.Zero);
		Assert.That(ace.SecurityIdentifier, Is.EqualTo(target));
		Assert.That(ace.AceFlags, Is.EqualTo(AceFlags.Inherited | AceFlags.ContainerInherit));
		Assert.That(ace.AccessMask, Is.EqualTo(0x1234));
		Assert.That(ace.ObjectAceType, Is.EqualTo(objectType));
		Assert.That(ace.InheritedObjectAceType, Is.EqualTo(inheritedObjectType));
		Assert.That(ace.GetOpaque(), Is.EqualTo(opaque));
	}

	[Test]
	public void NoTranslationRetainsOriginalBytes() {
		var bytes = Bytes("O:BAG:BAD:(A;;FA;;;BA)");
		Assert.That(Tools.Windows.SecurityDescriptors.TranslateSecurityDescriptor(bytes, _ => null), Is.SameAs(bytes));
		Assert.That(Tools.Windows.SecurityDescriptors.TranslateSecurityDescriptor(bytes, sid => sid, AccessControlSections.Access, out var changed), Is.SameAs(bytes));
		Assert.That(changed, Is.EqualTo(AccessControlSections.None));
	}

	[Test]
	public void RegistryUtilityReturnsOnlyExistingOwnedReadHandlesAndDoesNotDisposeTheSharedHive() {
		var path = "Software\\Sphere10\\Tests\\SID-" + Guid.NewGuid().ToString("N");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => Registry.CurrentUser.DeleteSubKeyTree(path, false));
		using (Registry.CurrentUser.CreateSubKey(path)) { }
		var utility = new RegistryUtil();
		Assert.That(utility.TryOpenKey(string.Empty, "HKEY_CURRENT_USER\\" + path + "\\Missing", false, out var missing), Is.False);
		Assert.That(missing, Is.Null);
		using (var key = utility.OpenKey(string.Empty, "HKEY_CURRENT_USER\\" + path, false)) {
			Assert.That(key.Name, Does.EndWith(path));
			Assert.That(() => key.SetValue("Rejected", "No read-handle writes"), Throws.InstanceOf<UnauthorizedAccessException>());
		}
		using (var key = utility.OpenKey(string.Empty, "HKEY_CURRENT_USER\\" + path, RegistryRights.ReadKey | RegistryRights.SetValue))
			key.SetValue("Owned fixture", "Written through explicit mutation rights");
		using (var key = utility.OpenKey(string.Empty, "HKEY_CURRENT_USER\\" + path, RegistryRights.ReadKey)) {
			Assert.That(key.GetValue("Owned fixture"), Is.EqualTo("Written through explicit mutation rights"));
			Assert.That(key.GetValue("Rejected"), Is.Null);
			Assert.That(() => key.SetValue("Rejected", "Still read only"), Throws.InstanceOf<UnauthorizedAccessException>());
		}
		var rootProbe = "Sphere10ReadOnlyProbe-" + Guid.NewGuid().ToString("N");
		using var rootCleanup = Tools.Scope.ExecuteOnDispose(() => Registry.CurrentUser.DeleteValue(rootProbe, false));
		using (var key = utility.OpenKey(string.Empty, "HKEY_CURRENT_USER", false)) {
			Assert.That(() => key.SetValue(rootProbe, "A read-only root must reject this temporary value"), Throws.InstanceOf<UnauthorizedAccessException>());
			Assert.That(Registry.CurrentUser.GetValue(rootProbe), Is.Null);
		}
		Assert.That(Registry.CurrentUser.GetSubKeyNames(), Is.Not.Null);
		Assert.That(utility.KeyExists(string.Empty, "HKEY_CURRENT_USER\\" + path), Is.True);
	}

	private static byte[] Bytes(string sddl) {
		var descriptor = new RawSecurityDescriptor(sddl);
		var result = new byte[descriptor.BinaryLength];
		descriptor.GetBinaryForm(result, 0);
		return result;
	}

}
