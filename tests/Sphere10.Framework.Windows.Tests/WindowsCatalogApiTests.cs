// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NUnit.Framework;
using Sphere10.Framework.Windows.EventLogs;
using Sphere10.Framework.Windows.Startup;
using Sphere10.Framework.Windows.Storage;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsCatalogApiTests {
	[Test, Category("Integration")]
	public async Task NetworkInventoryIncludesDetachedHardwareAddressesAndLinkSpeeds() {
		var interfaces = await Tools.Windows.Network.GetInterfacesAsync();
		Assert.That(interfaces, Is.Not.Empty);
		var current = NetworkInterface.GetAllNetworkInterfaces();
		foreach (var item in interfaces) {
			Assert.That(item.PhysicalAddress, Is.Not.Null);
			var adapter = current.FirstOrDefault(candidate => candidate.Id == item.Id);
			if (adapter == null || item.HardwareError != null)
				continue;
			Assert.That(item.PhysicalAddress, Is.EqualTo(adapter.GetPhysicalAddress().GetAddressBytes()), "The resource query must retain the OS hardware identity.");
			Assert.That(item.LinkSpeedBitsPerSecond.HasValue, Is.True);
		}
	}

	[Test, Category("Integration")]
	public void DriveApiReturnsRealLocalCapacityWithoutApplicationLabels() {
		var drives = Tools.Windows.Drives.GetDrives(probeNetworkDrives: false);
		Assert.That(drives, Is.Not.Empty);
		Assert.That(drives.All(drive => !string.IsNullOrEmpty(drive.Name)), Is.True);
		var ready = drives.Where(drive => drive.IsReady == true && drive.ReadError == null).ToArray();
		Assert.That(ready, Is.Not.Empty);
		Assert.That(ready.All(drive => drive.CapacityBytes > 0 && drive.FreeBytes >= 0 && drive.AvailableBytes >= 0), Is.True);
	}

	[Test]
	public void StartupLocationsExposeWindowsViewsAndApprovalCategoriesWithoutUiScopeStrings() {
		var locations = Tools.Windows.Startup.GetRunLocations();
		Assert.That(locations.Count(location => location.Hive == RegistryHive.CurrentUser), Is.EqualTo(2));
		Assert.That(locations.Count(location => location.Hive == RegistryHive.LocalMachine), Is.EqualTo(Environment.Is64BitOperatingSystem ? 4 : 2));
		var user = locations.First(location => location.Hive == RegistryHive.CurrentUser && location.SubKeyPath.EndsWith("\\Run"));
		Assert.That(Tools.Windows.Startup.GetApprovalCategory(user), Is.EqualTo("Run"));
		Assert.That(Tools.Windows.Startup.GetApprovalCategory(user with { SubKeyPath = user.SubKeyPath + "Once" }), Is.Null);
	}

	[Test]
	public async Task CanceledNativeReadsStopBeforeDriveStartupOrEventEnumeration() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Drives.GetDrivesAsync(false, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Startup.GetStartupFilesAsync(false, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await Tools.Windows.EventLogs.ReadEventsAsync("Application", 1, TimeSpan.Zero, TimeSpan.FromSeconds(1), cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}
