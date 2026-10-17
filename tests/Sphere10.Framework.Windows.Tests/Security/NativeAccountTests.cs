// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using NUnit.Framework;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests.Security;

[TestFixture]
public class NativeAccountTests {
	[Test]
	public void EnumerationReadsActualEntriesAcrossPagesAndReleasesEachPage() {
		var pages = new Queue<int[]>(new[] { new[] { 1, 2 }, new[] { 3 } });
		var released = 0;
		var result = NetApiEnumeration.Read<NativeEntry, int>((out IntPtr buffer, out uint count) => {
			var values = pages.Dequeue();
			count = (uint)values.Length;
			buffer = Marshal.AllocHGlobal(values.Length * sizeof(int));
			Marshal.Copy(values, 0, buffer, values.Length);
			return pages.Count > 0 ? WinAPI.NETAPI32.NET_API_STATUS.ERROR_MORE_DATA : 0;
		}, entry => entry.Value, release: buffer => { released++; Marshal.FreeHGlobal(buffer); });
		Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
		Assert.That(released, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void EnumerationReleasesTheReturnedBufferOnNativeOrConversionFailure(bool conversionFailure) {
		var released = 0;
		Assert.That(() => NetApiEnumeration.Read<NativeEntry, int>((out IntPtr buffer, out uint count) => {
			buffer = Marshal.AllocHGlobal(sizeof(int));
			Marshal.WriteInt32(buffer, 42);
			count = 1;
			return conversionFailure ? 0 : WinAPI.NETAPI32.NET_API_STATUS.ERROR_ACCESS_DENIED;
		}, entry => throw new InvalidOperationException("Conversion failed"), release: buffer => { released++; Marshal.FreeHGlobal(buffer); }),
			conversionFailure ? Throws.TypeOf<InvalidOperationException>() : Throws.TypeOf<NetApiException>());
		Assert.That(released, Is.EqualTo(1));
	}

	[Test]
	public void UserNativeDataCopiesOwnedLogonHoursAndUsesUnixSeconds() {
		var hours = Enumerable.Range(0, 21).Select(value => (byte)value).ToArray();
		var user = new NTLocalUser();
		using (var buffer = new UnmanagedMemoryScope(21)) {
			var pointer = buffer.Item;
			Marshal.Copy(hours, 0, pointer, hours.Length);
			user.CopyNativeInfo("Remote", new WinAPI.NETAPI32.USER_INFO_2 {
				usri2_name = "Alice", usri2_units_per_week = 168, usri2_logon_hours = pointer,
				usri2_acct_expires = 1700000000, usri2_last_logon = 1700000000
			});
		}
		Assert.That(user.LogonHours, Is.EqualTo(hours));
		Assert.That(user.Workstations, Is.Empty);
		Assert.That(user.LastLogon, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime));
		Assert.That(user.AccountExpires, Is.EqualTo(user.LastLogon));
		user.AccountExpires = DateTime.MaxValue;
		Assert.That(user.AccountExpires, Is.EqualTo(DateTime.MaxValue));
		user.CopyNativeInfo("Remote", new WinAPI.NETAPI32.USER_INFO_2 { usri2_name = "Alice", usri2_units_per_week = 168, usri2_acct_expires = uint.MaxValue });
		Assert.That(user.LogonHours, Is.Empty);
	}

	[Test]
	public void ShareNativeSnapshotOwnsItsDescriptorAndPreservesType() {
		var descriptor = new RawSecurityDescriptor("O:SYG:BAD:(A;;FA;;;SY)");
		var bytes = new byte[descriptor.BinaryLength];
		descriptor.GetBinaryForm(bytes, 0);
		var share = new NTShare();
		using (var buffer = new UnmanagedMemoryScope(bytes.Length)) {
			var pointer = buffer.Item;
			Marshal.Copy(bytes, 0, pointer, bytes.Length);
			share.CopyNativeInfo("Remote", new WinAPI.NETAPI32.SHARE_INFO_502 {
				shi502_netname = "Hidden$", shi502_type = (uint)WinAPI.NETAPI32.SHARE_TYPE.STYPE_HIDDEN_DISK,
				shi502_path = "C:\\Data", shi502_security_descriptor = pointer
			});
		}
		Assert.That(share.Type, Is.EqualTo(WinAPI.NETAPI32.SHARE_TYPE.STYPE_HIDDEN_DISK));
		Assert.That(share.SecurityDescriptor, Is.EqualTo(IntPtr.Zero));
		Assert.That(share.SecurityDescriptorBytes, Is.EqualTo(bytes));
		share.SecurityDescriptorBytes[0] = 99;
		Assert.That(share.SecurityDescriptorBytes, Is.EqualTo(bytes));
	}

	[Test]
	public void FailedHostIdentityIsUnavailableWithItsActualDiagnostic() {
		var host = new NTHost(Environment.MachineName, null, FailLookup);
		Assert.That(host.SID, Is.Null);
		Assert.That(host.SidNameUsage, Is.EqualTo(WinAPI.ADVAPI32.SidNameUse.Invalid));
		Assert.That(host.SidResolutionError, Does.Contain(WindowsException.ErrorCodeToDescription(5)));
		Assert.That(host.Host, Is.EqualTo(Environment.MachineName));
	}

	[TestCase((UserFlags)0)]
	[TestCase(UserFlags.NormalAccount)]
	[TestCase(UserFlags.NormalAccount | UserFlags.Script | UserFlags.Lockout)]
	public void InvalidInitialUserFlagsAreRejectedBeforeAnyNativeCreation(UserFlags flags) {
		var host = new NTHost(Environment.MachineName, null, FailLookup);
		var checkpoints = 0;
		Assert.That(() => host.CreateLocalUser("Invalid/Account", string.Empty, flags, () => checkpoints++), Throws.ArgumentException);
		Assert.That(checkpoints, Is.Zero, "Invalid creation flags must fail before the native-call admission checkpoint.");
	}

	[Test]
	public void ExistingCreationOverloadUsesValidDefaultFlagsBeforeItsCheckpoint() {
		var host = new NTHost(Environment.MachineName, null, FailLookup);
		var checkpoints = 0;
		Assert.That(() => host.CreateLocalUser("Invalid/Account", string.Empty, () => {
			checkpoints++;
			throw new OperationCanceledException("Stop before NetUserAdd; never create a developer account.");
		}), Throws.TypeOf<OperationCanceledException>());
		Assert.That(checkpoints, Is.EqualTo(1), "The established overload supplies valid default flags and reaches only the cooperative checkpoint.");
	}

	[Test]
	public void DeletingInvalidLocalUserReportsNativeErrorWithoutMutatingAccounts() {
		if (!OperatingSystem.IsWindows())
			Assert.Ignore("Native account errors require Windows.");
		// Slash, pipe and colon are forbidden in real local account names. This cannot identify an existing account.
		var invalidName = "Sphere10/Invalid|Account:" + Guid.NewGuid().ToString("N");
		var user = new NTLocalUser();
		user.CopyNativeInfo(Environment.MachineName, new WinAPI.NETAPI32.USER_INFO_2 { usri2_name = invalidName });
		var error = Assert.Throws<NetApiException>(user.Delete);
		Assert.That(error.Message, Does.Contain(invalidName).And.Contain(Environment.MachineName));
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct NativeEntry { public int Value; }

	private static bool FailLookup(string host, string name, out SecurityIdentifier sid, out string domain, out WinAPI.ADVAPI32.SidNameUse usage, out int error) {
		sid = null;
		domain = null;
		usage = WinAPI.ADVAPI32.SidNameUse.Invalid;
		error = 5;
		return false;
	}

}
