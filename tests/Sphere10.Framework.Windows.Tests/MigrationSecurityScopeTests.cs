// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class MigrationSecurityScopeTests {
	[Test]
	public void NullPrivilegesAreRejectedBeforeNativeAcquisition() {
		Assert.That(() => new TokenPrivilegeScope(null), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("privileges"));
	}

	[Test]
	public void TokenPrivilegeLayoutMatchesSingleNativePrivilege() {
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.TokenPrivilege>(), Is.EqualTo(16));
		Assert.That(Marshal.OffsetOf<WinAPI.ADVAPI32.TokenPrivilege>(nameof(WinAPI.ADVAPI32.TokenPrivilege.Luid)).ToInt32(), Is.EqualTo(4));
	}

	[Test]
	public void EmptyPrivilegeScopeDoesNotChangeIdentityAndIsDisposable() {
		using var identity = WindowsIdentity.GetCurrent();
		using (Tools.Windows.Security.EnterScope()) {
			using var current = WindowsIdentity.GetCurrent();
			Assert.That(current.User, Is.EqualTo(identity.User));
		}
		using var after = WindowsIdentity.GetCurrent();
		Assert.That(after.User, Is.EqualTo(identity.User));
	}

	[Test]
	public void OptionalRestorePrivilegePreservesAnExistingImpersonationOnSuccessOrMissingPrivilege() {
		using var caller = WindowsIdentity.GetCurrent();
		WindowsIdentity.RunImpersonated(caller.AccessToken, () => {
			using var before = WindowsIdentity.GetCurrent();
			var admitted = Tools.Windows.Security.TryEnterScope(new[] { "SeRestorePrivilege" }, out var scope);
			try {
				using var during = WindowsIdentity.GetCurrent();
				Assert.That(during.User, Is.EqualTo(before.User));
				Assert.That(scope.Equals(default(TokenPrivilegeScope)), Is.EqualTo(!admitted));
			} finally {
				scope.Dispose();
			}
			using var after = WindowsIdentity.GetCurrent();
			Assert.That(after.User, Is.EqualTo(before.User));
			Assert.That(after.ImpersonationLevel, Is.EqualTo(before.ImpersonationLevel));
		});
	}

	[Test]
	public async Task PrivilegeScopesRejectAsyncDisposalAndKeepTheirNativeCleanupSynchronous() {
		using var before = WindowsIdentity.GetCurrent();
		Tools.Windows.Security.TryEnterScope(new[] { "SeRestorePrivilege" }, out var scope);
		try {
			await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.TypeOf<NotSupportedException>().With.Message.EqualTo("This scope does not support async"));
			using var current = WindowsIdentity.GetCurrent();
			Assert.That(current.User, Is.EqualTo(before.User));
		} finally {
			scope.Dispose();
		}
		using var after = WindowsIdentity.GetCurrent();
		Assert.That(after.User, Is.EqualTo(before.User));
	}

	[Test]
	public void NativeAdmissionFailureDoesNotDiscardExistingCallerImpersonation() {
		using var caller = WindowsIdentity.GetCurrent();
		WindowsIdentity.RunImpersonated(caller.AccessToken, () => {
			using var before = WindowsIdentity.GetCurrent();
			Assert.That(() => Tools.Windows.Security.EnterScope("Sphere10MissingPrivilegeForOwnedTest"), Throws.TypeOf<Win32Exception>());
			using var after = WindowsIdentity.GetCurrent();
			Assert.That(after.User, Is.EqualTo(before.User));
			Assert.That(after.ImpersonationLevel, Is.EqualTo(before.ImpersonationLevel));
		});
	}

	[Test]
	public void InvalidCredentialArgumentsDoNotInvokeTheWorkerOrChangeCallerIdentity() {
		using var before = WindowsIdentity.GetCurrent();
		var calls = 0;
		Assert.That(() => Impersonator.RunAs(string.Empty, ".", "", () => calls++), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Impersonator.RunAs("user", ".", "", null), Throws.InstanceOf<ArgumentException>());
		Assert.That(calls, Is.Zero);
		using var after = WindowsIdentity.GetCurrent();
		Assert.That(after.User, Is.EqualTo(before.User));
	}

	[TestCase(1)]
	[TestCase(2)]
	public void OwnedPrimaryAndImpersonationTokensSupportManagedWorkerImpersonation(int tokenType) {
		using var caller = WindowsIdentity.GetCurrent();
		var access = WinAPI.ADVAPI32.TokenQuery | WinAPI.ADVAPI32.TokenDuplicate | WinAPI.ADVAPI32.TokenImpersonate | WinAPI.ADVAPI32.TokenAdjustPrivileges;
		Assert.That(WinAPI.ADVAPI32.DuplicateTokenEx(caller.AccessToken, access, IntPtr.Zero, 2, tokenType, out var token), Is.True);
		using (token) {
			var calls = 0;
			WindowsIdentity.RunImpersonated(token, () => { using var current = WindowsIdentity.GetCurrent(); Assert.That(current.User, Is.EqualTo(caller.User)); calls++; });
			Assert.That(calls, Is.EqualTo(1));
		}
		using var after = WindowsIdentity.GetCurrent();
		Assert.That(after.User, Is.EqualTo(caller.User));
	}
}
