// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests.Security;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class NetApiBufferScopeTests {
	[Test]
	public void NativeEnumerationBufferRemainsUsableInsideItsScopeAndReleasesExactlyOnce() {
		var resume = UIntPtr.Zero;
		var status = WinAPI.NETAPI32.NetLocalGroupEnumPaged(null, 0, out var pointer, WinAPI.NETAPI32.MAX_PREFERRED_LENGTH, out var count, out _, ref resume);
		using var buffer = new NetApiBufferScope(pointer);
		Assert.That(status, Is.EqualTo(WinAPI.NETAPI32.NET_API_STATUS.NERR_Success).Or.EqualTo(WinAPI.NETAPI32.NET_API_STATUS.ERROR_MORE_DATA));
		Assert.That(count, Is.GreaterThan(0));
		var name = Marshal.PtrToStructure<WinAPI.NETAPI32.LOCALGROUP_INFO_0>(buffer.Item).name;
		Assert.That(name, Is.Not.Null.And.Not.Empty);
		buffer.Dispose();
		Assert.That(buffer.Item, Is.EqualTo(IntPtr.Zero));
		buffer.Dispose();
		Assert.That(name, Is.Not.Empty, "The copied managed value must survive release of its owning native buffer.");
	}

	[Test]
	public void FailedNativeReadWithNoBufferCanStillCloseItsOwnershipScope() {
		using var buffer = new NetApiBufferScope(IntPtr.Zero);
		Assert.That(() => buffer.Dispose(), Throws.Nothing);
		Assert.That(buffer.Item, Is.EqualTo(IntPtr.Zero));
	}
}
