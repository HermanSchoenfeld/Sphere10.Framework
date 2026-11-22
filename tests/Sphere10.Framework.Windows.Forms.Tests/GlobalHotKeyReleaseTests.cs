// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class GlobalHotKeyReleaseTests {
	[Test]
	public void AShortPressReleasesOnTheOwnerThreadWithoutInstallingAnInputHook() {
		var previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		using var key = new GlobalHotKey();
		Assume.That(key.TryRegister(Keys.Control | Keys.Alt | Keys.Shift | Keys.F11, out var error), Is.True, error);
		var calls = new List<string>();
		var ownerThread = Environment.CurrentManagedThreadId;
		var releaseThread = 0;
		key.Pressed += () => calls.Add("pressed");
		key.Released += () => {
			releaseThread = Environment.CurrentManagedThreadId;
			calls.Add("released");
		};
		Dispatch(key);
		Dispatch(key);
		Assert.That(calls, Is.EqualTo(new[] { "pressed" }), "Repeated messages cannot begin the same hold twice.");
		var deadline = Stopwatch.StartNew();
		while (calls.Count < 2 && deadline.Elapsed < TimeSpan.FromSeconds(2)) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(1);
		}
		Assert.That(calls, Is.EqualTo(new[] { "pressed", "released" }), "No physical input is injected: all constituent keys are already up.");
		Assert.That(releaseThread, Is.EqualTo(ownerThread));
		Assert.That(Field<BaseJob>(key, "_releaseJob"), Is.Null);
	}

	[Test]
	public void OrdinaryShortcutsNeverCreateAReleaseSchedulerAndUnregisterBalancesAHold() {
		using var key = new GlobalHotKey();
		Assume.That(key.TryRegister(Keys.Control | Keys.Alt | Keys.Shift | Keys.F11, out var error), Is.True, error);
		var presses = 0;
		var releases = 0;
		key.Pressed += () => presses++;
		Dispatch(key);
		Assert.That(Field<Scheduler<BaseJob>>(key, "_releaseScheduler"), Is.Null);
		key.Released += () => releases++;
		Dispatch(key);
		Assert.That(key.TryRegister(Keys.F12, out _), Is.False);
		Assert.That(releases, Is.Zero, "A failed replacement preserves the current held registration.");
		key.Unregister();
		key.Dispose();
		Assert.That(presses, Is.EqualTo(2));
		Assert.That(releases, Is.EqualTo(1));
	}

	private static T Field<T>(GlobalHotKey key, string name) => (T)typeof(GlobalHotKey).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(key);

	// Exercise the private message dispatch boundary without injecting keystrokes into the user's desktop.
	private static void Dispatch(GlobalHotKey key)
		=> typeof(GlobalHotKey).GetMethod("OnPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(key, new object[] { Field<int>(key, "_registeredId") });
}
