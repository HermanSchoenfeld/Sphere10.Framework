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

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class UnmanagedMemoryScopeTests {
	[Test]
	public void DisposalInvalidatesOwnedPointerEvenWhenOperationThrows() {
		var scope = new UnmanagedMemoryScope(16);
		Assert.That(scope.Item, Is.Not.EqualTo(IntPtr.Zero));
		Assert.That(() => {
			using (scope) {
				Marshal.WriteInt32(scope.Item, 42);
				throw new InvalidOperationException("Fixture operation failed.");
			}
		}, Throws.InvalidOperationException);
		Assert.That(scope.Item, Is.EqualTo(IntPtr.Zero));
		scope.Dispose();
	}

	[Test]
	public void UnicodeStringScopeIncludesTerminatorAndRetainsUtf16Data() {
		const string text = "Native 🧪 value";
		using var scope = UnmanagedMemoryScope.FromUnicodeString(text);
		Assert.That(scope.Size, Is.EqualTo((text.Length + 1) * sizeof(char)));
		Assert.That(Marshal.PtrToStringUni(scope.Item), Is.EqualTo(text));
		Assert.That(Marshal.ReadInt16(scope.Item, scope.Size - sizeof(char)), Is.Zero);
	}
}
