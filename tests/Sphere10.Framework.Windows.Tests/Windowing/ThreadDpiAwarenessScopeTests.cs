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

[TestFixture, NonParallelizable]
public class ThreadDpiAwarenessScopeTests {
	[TestCase(DpiAwarenessContext.Unaware)]
	[TestCase(DpiAwarenessContext.SystemAware)]
	[TestCase(DpiAwarenessContext.PerMonitorAwareV2)]
	public void NestedScopesRestoreTheExactCallingContextAfterSuccessAndFailure(DpiAwarenessContext context) {
		var original = WinAPI.USER32.GetThreadDpiAwarenessContext();
		using (var outer = new ThreadDpiAwarenessScope(context)) {
			AssertContext(new IntPtr((int)context));
			var outerContext = WinAPI.USER32.GetThreadDpiAwarenessContext();
			Assert.That(() => {
				using var inner = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
				AssertContext(new IntPtr((int)DpiAwarenessContext.PerMonitorAwareV2));
				throw new InvalidOperationException("Owned scope failure.");
			}, Throws.InvalidOperationException.With.Message.EqualTo("Owned scope failure."));
			AssertContext(outerContext);
		}
		AssertContext(original);
	}

	[Test]
	public void InvalidContextDoesNotChangeTheCallingThread() {
		var original = WinAPI.USER32.GetThreadDpiAwarenessContext();
		Assert.That(() => new ThreadDpiAwarenessScope((DpiAwarenessContext)0), Throws.ArgumentException);
		AssertContext(original);
	}

	[Test]
	public void WrongThreadDisposalIsRejectedAndTheOwnerCanStillRestoreIt() {
		var original = WinAPI.USER32.GetThreadDpiAwarenessContext();
		using (var scope = new ThreadDpiAwarenessScope(DpiAwarenessContext.Unaware)) {
			Exception failure = null;
			var worker = new Thread(() => {
				try { scope.Dispose(); }
				catch (Exception error) { failure = error; }
			});
			worker.Start();
			Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(failure, Is.TypeOf<InvalidOperationException>());
			AssertContext(new IntPtr((int)DpiAwarenessContext.Unaware));
		}
		AssertContext(original);
	}

	[Test]
	public void AsyncDisposalIsRejectedAndSynchronousRestorationCannotRunTwice() {
		var original = WinAPI.USER32.GetThreadDpiAwarenessContext();
		using var scope = new ThreadDpiAwarenessScope(DpiAwarenessContext.Unaware);
		// Inspect the completed rejection without suspending while a thread-affine scope is active.
		var rejection = scope.DisposeAsync();
		Assert.That(rejection.IsCompleted, Is.True);
		Assert.That(() => rejection.GetAwaiter().GetResult(), Throws.TypeOf<NotSupportedException>());
		scope.Dispose();
		AssertContext(original);
		using (var next = new ThreadDpiAwarenessScope(DpiAwarenessContext.SystemAware)) {
			scope.Dispose();
			AssertContext(new IntPtr((int)DpiAwarenessContext.SystemAware));
		}
		AssertContext(original);
	}

	private static void AssertContext(IntPtr expected)
		=> Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), expected), Is.True,
			"The native thread must retain its exact inherited/overridden awareness context.");
}
