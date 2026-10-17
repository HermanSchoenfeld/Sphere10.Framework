// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable, Category("Integration")]
public class WindowsUninstallRegistrationTests {
	[Test]
	public void RawRegistrationReadAndRemovalStayInsideTheCallerOwnedContainer() {
		var registry = new RegistryUtil();
		var root = new RegistryAddress(RegistryHive.CurrentUser, RegistryView.Default, "Software\\Sphere10\\Tests\\UninstallApi-" + Guid.NewGuid().ToString("N"));
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => registry.DeleteKey(root, ignoreMissing: true));
		using (registry.OpenKey(root, writable: true, createIfMissing: true)) { }
		var selected = registry.CreateKey(root, "Selected");
		var sibling = registry.CreateKey(root, "Sibling");
		registry.SetValue(selected, "UninstallString", RegistryValueKind.ExpandString, "%SystemRoot%\\uninstaller.exe /raw-arguments");
		registry.SetValue(sibling, "Other", RegistryValueKind.String, "Retained");
		var api = new WindowsUninstallRegistration();
		Assert.That(api.GetRegistrations(root), Is.EquivalentTo(new[] { selected, sibling }));
		var captured = api.ReadRegistration(selected, 4, 32, TimeSpan.FromSeconds(3));
		Assert.That(captured.Values.Single().Data, Is.EqualTo("%SystemRoot%\\uninstaller.exe /raw-arguments"));
		api.RemoveRegistration(selected);
		Assert.That(api.GetRegistrations(root), Is.EqualTo(new[] { sibling }));
		Assert.That(registry.ReadValue(sibling, "Other").Data, Is.EqualTo("Retained"));
	}

	[Test]
	public void StandardRootsEnumerateUserRegistrationOnceAndBothMachineViewsWhenSupported() {
		var roots = WindowsUninstallRegistration.GetRoots();
		Assert.That(roots.Count(address => address.Hive == RegistryHive.CurrentUser), Is.EqualTo(1));
		Assert.That(roots.Count(address => address.Hive == RegistryHive.LocalMachine), Is.EqualTo(Environment.Is64BitOperatingSystem ? 2 : 1));
		Assert.That(roots.All(address => address.SubKeyPath == WindowsUninstallRegistration.RegistryPath), Is.True);
	}

	[Test]
	public async Task AsyncRegistrationCaptureCompletesBeforeOwnedKeyRemovalAndRejectsPreCancellation() {
		var registry = new RegistryUtil();
		var address = new RegistryAddress(RegistryHive.CurrentUser, RegistryView.Default, "Software\\Sphere10\\Tests\\UninstallCapture-" + Guid.NewGuid().ToString("N"));
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => registry.DeleteKey(address, ignoreMissing: true));
		using (registry.OpenKey(address, writable: true, createIfMissing: true)) { }
		registry.SetValue(address, "Owned data", RegistryValueKind.String, "Detached capture");
		var api = new WindowsUninstallRegistration();
		var captured = await api.ReadRegistrationAsync(address, 4, 32, TimeSpan.FromSeconds(3));
		api.RemoveRegistration(address);
		Assert.That(captured.Values.Single().Data, Is.EqualTo("Detached capture"));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await api.ReadRegistrationAsync(address, 4, 32, TimeSpan.FromSeconds(3), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}
