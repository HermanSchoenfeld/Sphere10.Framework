// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Security.Principal;
using NUnit.Framework;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests.Security;

[TestFixture]
public class SecurityAccountLookupTests {
	[Test]
	public void CurrentUserCanBeLookedUpBothWaysWithoutRetainingNativeBuffers() {
		var current = SecurityTool.GetCurrentUserSid();
		Assert.That(SecurityTool.TryLookupAccountSid(null, current.Value, out var name, out var domain, out var usage, out var error), Is.True,
			WindowsException.ErrorCodeToDescription(error));
		Assert.That(error, Is.Zero);
		Assert.That(name, Is.Not.Empty);
		Assert.That(usage, Is.EqualTo(WinAPI.ADVAPI32.SidNameUse.User));
		var qualified = string.IsNullOrEmpty(domain) ? name : domain + "\\" + name;
		Assert.That(SecurityTool.TryLookupAccountName(null, qualified, out var resolved, out var resolvedDomain, out var resolvedUsage, out error), Is.True,
			WindowsException.ErrorCodeToDescription(error));
		Assert.That(resolved, Is.EqualTo(current));
		Assert.That(resolvedDomain, Is.EqualTo(domain));
		Assert.That(resolvedUsage, Is.EqualTo(usage));
		for (var index = 0; index < 4; index++)
			Assert.That(SecurityTool.TryLookupAccountName(null, qualified, out _, out _, out _, out error), Is.True);
		Assert.That(resolved.Value, Is.EqualTo(current.Value), "The managed SID remains valid after subsequent native buffers have been released.");
	}

	[Test]
	public void MissingLocalAccountNameReportsNoneMappedWithoutInventingAnIdentity() {
		var name = Environment.MachineName + "\\Sphere10Missing-" + Guid.NewGuid().ToString("N");
		Assert.That(SecurityTool.TryLookupAccountName(null, name, out var sid, out var domain, out var usage, out var error), Is.False);
		Assert.That(error, Is.EqualTo(1332));
		Assert.That(sid, Is.Null);
		Assert.That(domain, Is.Null);
		Assert.That(usage, Is.EqualTo(WinAPI.ADVAPI32.SidNameUse.Invalid));
	}

	[Test]
	public void WellKnownSidLookupReturnsItsActualNameAndUsage() {
		var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
		Assert.That(SecurityTool.TryLookupAccountSid(null, world.Value, out var name, out _, out var usage, out var error), Is.True);
		Assert.That(error, Is.Zero);
		Assert.That(name, Is.Not.Empty);
		Assert.That(usage, Is.EqualTo(WinAPI.ADVAPI32.SidNameUse.WellKnownGroup));
	}
}
