// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class ThreadGroupAffinityScopeTests {

	[Test]
	public void PinsTheCurrentThreadToTheSelectedProcessorAndRestoresTheExactPreviousAffinity() {
		var original = Current();
		var lowest = original.Mask.ToUInt64() & (~original.Mask.ToUInt64() + 1);
		using (new ThreadGroupAffinityScope(original.Group, lowest)) {
			var pinned = Current();
			Assert.That(pinned.Group, Is.EqualTo(original.Group));
			Assert.That(pinned.Mask.ToUInt64(), Is.EqualTo(lowest));
		}
		var restored = Current();
		Assert.That(restored.Group, Is.EqualTo(original.Group));
		Assert.That(restored.Mask, Is.EqualTo(original.Mask));
	}

	[Test]
	public void RejectsAnEmptyMaskAndADefaultScopeDisposesWithoutNativeCalls() {
		Assert.That(() => new ThreadGroupAffinityScope(0, 0), Throws.ArgumentException);
		Assert.That(() => default(ThreadGroupAffinityScope).Dispose(), Throws.Nothing);
	}

	[Test]
	public void MustBeRestoredOnTheThreadThatEnteredIt() {
		var original = Current();
		var scope = new ThreadGroupAffinityScope(original.Group, original.Mask.ToUInt64());
		Exception error = null;
		var other = new Thread(() => {
			try {
				scope.Dispose();
			} catch (Exception caught) {
				error = caught;
			}
		});
		other.Start();
		other.Join();
		Assert.That(error, Is.InstanceOf<InvalidOperationException>());
		Assert.That(() => scope.Dispose(), Throws.Nothing);
	}

	private static WinAPI.KERNEL32.GROUP_AFFINITY Current() {
		Assume.That(WinAPI.KERNEL32.GetThreadGroupAffinity(WinAPI.KERNEL32.GetCurrentThread(), out var affinity), Is.True);
		return affinity;
	}
}
