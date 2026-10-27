// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Tools;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class UnmanagedMemoryScopeTests {
	[Test]
	public void OwnedAllocationCanBeReleasedWhenTheOperationThrows() {
		var scope = Tools.Scope.EnterUnmanagedMemoryScope(16);
		Assert.That(scope.Item, Is.Not.EqualTo(IntPtr.Zero));
		Assert.That(() => {
			try {
				Marshal.WriteInt32(scope.Item, 42);
				throw new InvalidOperationException("Fixture operation failed.");
			} finally {
				scope.Dispose();
			}
		}, Throws.InvalidOperationException);
	}

	[Test]
	public void UnicodeStringScopeIncludesTerminatorAndRetainsUtf16Data() {
		const string text = "Native 🧪 value";
		using var scope = Tools.Scope.EnterUnmanagedUnicodeStringScope(text);
		Assert.That(scope.Size, Is.EqualTo((text.Length + 1) * sizeof(char)));
		Assert.That(Marshal.PtrToStringUni(scope.Item), Is.EqualTo(text));
		Assert.That(Marshal.ReadInt16(scope.Item, scope.Size - sizeof(char)), Is.Zero);
	}

	[Test]
	public async Task AsynchronousDisposalCanBeAwaitedUntilTheAllocationIsReleased() {
		var scope = new UnmanagedMemoryScope(16);
		Marshal.WriteInt32(scope.Item, 42);
		await scope.DisposeAsync();
	}

	[Test]
	public async Task DefaultMemoryHandleOwnsNoNativeAllocation() {
		var scope = default(UnmanagedMemoryScope);
		Assert.That(scope.Item, Is.EqualTo(IntPtr.Zero));
		Assert.That(scope.Size, Is.Zero);
		Assert.That(() => scope.Dispose(), Throws.Nothing);
		await scope.DisposeAsync();
	}
}
