// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsHookUtilTests {
	[TestCase(false)]
	[TestCase(true)]
	public void DefaultMouseFactoriesPreserveMonitorAndTimingWithoutInstalling(bool explicitInterval) {
		var interval = TimeSpan.FromMilliseconds(73);
		using var hook = explicitInterval ? Tools.Windows.Hooks.CreateMouseHook(interval) : Tools.Windows.Hooks.CreateMouseHook();
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(hook.MovingStoppedInterval, Is.EqualTo(explicitInterval ? interval : WindowsMouseHook.DefaultMovingStoppedInterval));
		Assert.That(hook.ShouldIntercept, Is.Null);
		Assert.That(typeof(BaseMouseHook).GetProperty("ActiveApplicationMonitor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hook),
			Is.TypeOf<PollDrivenActiveApplicationMonitor>());
		var disposed = 0;
		hook.Disposed += (_, _) => disposed++;
		hook.Dispose();
		hook.Dispose();
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(disposed, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SuppliedMouseMonitorRemainsBorrowedAndUnusedUntilInputIsProcessed(bool explicitInterval) {
		using var monitor = new TrackingMonitor();
		using var hook = explicitInterval ? Tools.Windows.Hooks.CreateMouseHook(monitor, TimeSpan.FromMilliseconds(73)) : Tools.Windows.Hooks.CreateMouseHook(monitor);
		using var independent = Tools.Windows.Hooks.CreateMouseHook(monitor);
		Assert.That(hook, Is.Not.SameAs(independent));
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(typeof(BaseMouseHook).GetProperty("ActiveApplicationMonitor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hook), Is.SameAs(monitor));
		Assert.That(hook.MovingStoppedInterval, Is.EqualTo(explicitInterval ? TimeSpan.FromMilliseconds(73) : WindowsMouseHook.DefaultMovingStoppedInterval));
		hook.Dispose();
		Assert.That(independent.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(monitor.Disposed, Is.False);
		Assert.That(monitor.Reads, Is.Zero);
		Assert.That(monitor.Subscriptions, Is.Zero);
	}

	[TestCase(-1)]
	[TestCase(0)]
	[TestCase(73)]
	public void MouseFactoryDoesNotIntroduceNewConstructorValidation(int milliseconds) {
		using var hook = Tools.Windows.Hooks.CreateMouseHook(new NoOpActiveApplicationMonitor(), TimeSpan.FromMilliseconds(milliseconds));
		Assert.That(hook.MovingStoppedInterval, Is.EqualTo(TimeSpan.FromMilliseconds(milliseconds)));
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
	}

	[Test]
	public void DefaultKeyboardFactoryPreservesUninstalledStatusAndExistingMonitor() {
		using var hook = Tools.Windows.Hooks.CreateKeyboardHook();
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(hook.ShouldIntercept, Is.Null);
		Assert.That(typeof(BaseKeyboardHook).GetProperty("ActiveApplicationMonitor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hook),
			Is.TypeOf<PollDrivenActiveApplicationMonitor>());
		var disposed = 0;
		hook.Disposed += (_, _) => disposed++;
		hook.Dispose();
		hook.Dispose();
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(disposed, Is.EqualTo(1));
	}

	[Test]
	public void SuppliedKeyboardMonitorIsBorrowedByFreshUninstalledHooks() {
		using var monitor = new TrackingMonitor();
		using var hook = Tools.Windows.Hooks.CreateKeyboardHook(monitor);
		using var independent = Tools.Windows.Hooks.CreateKeyboardHook(monitor);
		Assert.That(hook, Is.Not.SameAs(independent));
		Assert.That(typeof(BaseKeyboardHook).GetProperty("ActiveApplicationMonitor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hook), Is.SameAs(monitor));
		hook.Dispose();
		Assert.That(independent.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(monitor.Disposed, Is.False);
		Assert.That(monitor.Reads, Is.Zero);
		Assert.That(monitor.Subscriptions, Is.Zero);
	}

	#region Inner Types

	private sealed class TrackingMonitor : IActiveApplicationMonitor, IDisposable {
		public event EventHandler<ApplicationChangedEvent> ApplicationChanged {
			add => Subscriptions++;
			remove => Subscriptions--;
		}

		public int Reads { get; private set; }
		public int Subscriptions { get; private set; }
		public bool Disposed { get; private set; }

		public string GetActiveApplicationName() {
			Reads++;
			return "Owned Test Monitor";
		}

		public void Dispose() => Disposed = true;
	}

	#endregion
}
