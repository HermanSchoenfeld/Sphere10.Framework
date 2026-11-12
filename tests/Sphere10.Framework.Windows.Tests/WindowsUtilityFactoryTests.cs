// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Startup;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsUtilityFactoryTests {
	[Test]
	public void InventoryFactoriesLeaveEveryNativeCaptureLazyAndDoNotSharePublishedMembership() {
		var processes = Tools.Windows.Processes.CreateDataSource();
		var services = Tools.Windows.Services.CreateDataSource();
		var drivers = Tools.Windows.Services.CreateDataSource(drivers: true);
		var endpoints = Tools.Windows.Network.CreateEndpointDataSource(true, AddressFamily.InterNetwork, 23);
		var interfaces = Tools.Windows.Network.CreateInterfaceDataSource();
		var drives = Tools.Windows.Drives.CreateDataSource();
		var disks = Tools.Windows.Drives.CreatePhysicalDiskDataSource();
		var partitions = Tools.Windows.Drives.CreatePartitionDataSource();
		var displays = Tools.Windows.Display.CreateDataSource();
		var startup = Tools.Windows.Startup.CreateRegistrationDataSource(WindowsStartupSources.LogonRegistry);
		var plans = Tools.Windows.Computer.CreatePowerPlanDataSource();
		var settings = Tools.Windows.Computer.CreatePowerSettingDataSource(Guid.NewGuid());
		var diagnostics = Tools.Windows.Computer.CreatePowerDiagnosticDataSource();
		Assert.That(new[] {
			processes.RequiresLoad, services.RequiresLoad, drivers.RequiresLoad, endpoints.RequiresLoad, interfaces.RequiresLoad, drives.RequiresLoad, disks.RequiresLoad,
			partitions.RequiresLoad, displays.RequiresLoad, startup.RequiresLoad, plans.RequiresLoad, settings.RequiresLoad, diagnostics.RequiresLoad
		}, Is.All.True, "Factories must not query native inventories or start collectors.");
		var second = Tools.Windows.Processes.CreateDataSource();
		Assert.That(second, Is.Not.SameAs(processes), "The thread-cached utility must not cache a mutable inventory owner.");
		Assert.That(endpoints.ProcessId, Is.EqualTo(23));
		Assert.That(endpoints.AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
	}

	[Test]
	public async Task ProcessFactoryRetainsLazyCapturePagingAndIndependentReloadOwnership() {
		var captures = 0;
		var current = new[] {
			new WindowsProcessInventoryEntry { ProcessId = 23, ExecutableName = "match-z.exe" },
			new WindowsProcessInventoryEntry { ProcessId = 24, ExecutableName = "match-a.exe" },
			new WindowsProcessInventoryEntry { ProcessId = 25, ExecutableName = "other.exe" }
		};
		Task<IEnumerable<WindowsProcessInventoryEntry>> Capture(CancellationToken token) {
			token.ThrowIfCancellationRequested();
			captures++;
			return Task.FromResult<IEnumerable<WindowsProcessInventoryEntry>>(current);
		}
		var source = new ActionAsyncListDataSource<WindowsProcessInventoryEntry>(Capture);
		await using var independent = new ActionRefreshableDataSource<WindowsProcessInventoryEntry>(() => new ActionAsyncListDataSource<WindowsProcessInventoryEntry>(Capture));
		Assert.That(captures, Is.Zero);
		var page = await source.ReadRangeAsync("match", 1, 0, nameof(WindowsProcessInventoryEntry.ExecutableName), SortDirection.Ascending);
		Assert.That(page.TotalCount, Is.EqualTo(2));
		Assert.That(page.Items.Single().ProcessId, Is.EqualTo(24));
		Assert.That(independent.RequiresLoad, Is.True);
		current = new[] { new WindowsProcessInventoryEntry { ProcessId = 26, ExecutableName = "replacement.exe" } };
		Assert.That((await Tools.DataSources.ReadAllAsync(source)).Length, Is.EqualTo(3));
		Assert.That(captures, Is.EqualTo(1));
		await independent.RefreshAsync();
		Assert.That((await Tools.DataSources.ReadAllAsync(independent)).Single().ProcessId, Is.EqualTo(26));
		Assert.That((await Tools.DataSources.ReadAllAsync(source)).Length, Is.EqualTo(3));
		Assert.That(captures, Is.EqualTo(2));
	}

	[Test]
	public async Task ProcessResourceFactoryTransfersOneVerifiedHandleAndPreservesCancellation() {
		using var acquired = await Tools.Windows.Processes.OpenAsync(Environment.ProcessId);
		var identity = await Tools.Windows.Processes.CaptureIdentityAsync(Environment.ProcessId);
		var actual = await Task.Run(() => acquired.Identity);
		Assert.That(actual, Is.EqualTo(identity));
		Assert.That(await Tools.Windows.Processes.IsCurrentAsync(identity), Is.True);
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Processes.OpenAsync(Environment.ProcessId, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		acquired.Dispose();
		Assert.That(() => acquired.ReadCounters(), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public async Task SuspensionFactoryReturnsIndependentApplicationOwnedFinalizersWithoutAcquiringIncrements() {
		await using var first = Tools.Windows.Processes.CreateSuspensionService();
		await using var second = Tools.Windows.Processes.CreateSuspensionService();
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(first.OwnedSuspensions, Is.Empty);
		Assert.That(second.OwnedSuspensions, Is.Empty);
		Assert.That(await first.ReleaseAllAsync(), Is.Empty);
		Assert.That(await second.ReleaseAllAsync(), Is.Empty);
	}

	[Test]
	public async Task PipePeerDiscoveryBorrowsConnectedStreamsWithoutClosingThem() {
		var name = "sphere10-peer-" + Guid.NewGuid().ToString("N");
		// The stream name is retained explicitly; native discovery never opens or adopts another pipe instance.
		using var namedServer = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
		using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await Task.WhenAll(namedServer.WaitForConnectionAsync(timeout.Token), client.ConnectAsync(timeout.Token));
		Assert.That(await Task.Run(() => Tools.Windows.Pipes.GetClientProcessId(namedServer), timeout.Token), Is.EqualTo(Environment.ProcessId));
		Assert.That(await Task.Run(() => Tools.Windows.Pipes.GetServerProcessId(client), timeout.Token), Is.EqualTo(Environment.ProcessId));
		var buffer = new byte[1];
		var reading = namedServer.ReadAsync(buffer, timeout.Token);
		await client.WriteAsync(new byte[] { 42 }, timeout.Token);
		Assert.That(await reading, Is.EqualTo(1));
		Assert.That(buffer[0], Is.EqualTo(42));
	}
}
