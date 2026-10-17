// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Win32Exception = System.ComponentModel.Win32Exception;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;
using Sphere10.Framework.Windows.Services;
using Sphere10.Framework.Windows.Storage;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsInventoryDataSourceTests {
	[Test]
	public async Task NativeServiceRecordsSupportFilteredSortedPagesWithoutRecollectingOrChangingServices() {
		var calls = 0;
		var denied = new WindowsServiceInfo { Name = "alpha", DisplayName = "Match Alpha", StatusErrorCode = 5 };
		var running = new WindowsServiceInfo { Name = "beta", DisplayName = "Match Beta", Status = ServiceControllerStatus.Running, ProcessId = 23 };
		var source = new WindowsServiceDataSource(_ => { calls++; return Task.FromResult(new[] { running, denied, new WindowsServiceInfo { Name = "other" } }); });
		IDataSource<WindowsServiceInfo> contract = source;
		var page = await contract.ReadRangeAsync("Match", 1, 1, nameof(WindowsServiceInfo.Name), SortDirection.Ascending);
		Assert.That(page.TotalCount, Is.EqualTo(2));
		Assert.That(page.Items.Single(), Is.SameAs(running));
		Assert.That((await contract.ReadRangeAsync("Match", 1, 0, nameof(WindowsServiceInfo.Name), SortDirection.Ascending)).Items.Single(), Is.SameAs(denied));
		Assert.That(denied.Status, Is.Null);
		Assert.That(denied.StatusErrorCode, Is.EqualTo(5));
		Assert.That(await contract.CountAsync, Is.EqualTo(3));
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public async Task EndpointDataSourceAppliesProtocolFamilyAndOwnerBeforePaging() {
		var calls = 0;
		var source = new WindowsNetworkEndpointDataSource(true, AddressFamily.InterNetwork, 23, _ => {
			calls++;
			return Task.FromResult(new[] {
				Endpoint(true, AddressFamily.InterNetwork, 10, 23), Endpoint(true, AddressFamily.InterNetwork, 20, 23),
				Endpoint(true, AddressFamily.InterNetwork, 30, 0), Endpoint(true, AddressFamily.InterNetwork, 40, 24),
				Endpoint(false, AddressFamily.InterNetwork, 50, 23), Endpoint(true, AddressFamily.InterNetworkV6, 60, 23)
			});
		});
		IDataSource<WindowsNetworkEndpoint> contract = source;
		var last = await contract.ReadRangeAsync(null, 1, int.MaxValue, nameof(WindowsNetworkEndpoint.LocalPort), SortDirection.Ascending);
		Assert.That(last.TotalCount, Is.EqualTo(2));
		Assert.That(last.Page, Is.EqualTo(1));
		Assert.That(last.Items.Single().LocalPort, Is.EqualTo(20));
		Assert.That((await source.GetSnapshotAsync()).All(row => row.ProcessId == 23 && row.IsTcp && row.AddressFamily == AddressFamily.InterNetwork), Is.True);
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public async Task ProcessSourcePreservesRawIdentityCountersAndPerRowAccessFailure() {
		var identity = new ProcessIdentity(23, DateTime.UtcNow);
		var readable = new WindowsProcessInventoryEntry { ProcessId = 23, ExecutableName = "owned.exe", Counters = new WindowsProcessCounters { Identity = identity, WorkingSetBytes = 1024 } };
		var denied = new WindowsProcessInventoryEntry { ProcessId = 24, ExecutableName = "protected.exe", ReadError = new Win32Exception(5) };
		var source = new WindowsProcessDataSource(_ => Task.FromResult(new[] { readable, denied }));
		var page = await source.ReadRangeAsync(null, 1, 0, nameof(WindowsProcessInventoryEntry.WorkingSetBytes), SortDirection.Descending);
		Assert.That(page.Items.Single(), Is.SameAs(readable));
		Assert.That(readable.Identity, Is.EqualTo(identity));
		Assert.That(denied.Identity.IsVerified, Is.False);
		Assert.That(denied.ReadError, Is.TypeOf<Win32Exception>());
		Assert.That(denied.WorkingSetBytes, Is.Null);
	}

	[Test, Category("Integration")]
	public async Task NativeProcessDataSourceFindsAnOwnedChildThroughIDataSource() {
		var start = new ProcessStartInfo {
			FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
			UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
			RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
		};
		start.ArgumentList.Add("/d");
		start.ArgumentList.Add("/q");
		using var child = Process.Start(start);
		using var terminateChild = Tools.Scope.ExecuteOnDispose(() => {
			if (!child.HasExited)
				child.Kill(entireProcessTree: true);
			child.WaitForExit(5000);
		});
		using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		var native = new WindowsProcessDataSource();
		IDataSource<WindowsProcessInventoryEntry> source = native;
		await native.ReloadAsync(lifetime.Token);
		var page = await source.ReadRangeAsync(child.Id.ToString(CultureInfo.InvariantCulture), 100, 0, nameof(WindowsProcessInventoryEntry.ProcessId), SortDirection.Ascending);
		var row = page.Items.Single(entry => entry.ProcessId == child.Id);
		Assert.That(row.Identity, Is.EqualTo(new ProcessIdentity(child.Id, child.StartTime.ToUniversalTime())));
		Assert.That(row.ReadError, Is.Null);
		Assert.That(row.WorkingSetBytes, Is.GreaterThan(0));
		Assert.That(row.ExecutablePath, Does.EndWith("cmd.exe").IgnoreCase);
		Assert.That(row.CounterTimestamp, Is.GreaterThan(0));
	}

	[TestCase(AddressFamily.InterNetwork, true)]
	[TestCase(AddressFamily.InterNetwork, false)]
	[TestCase(AddressFamily.InterNetworkV6, true)]
	[TestCase(AddressFamily.InterNetworkV6, false)]
	[Category("Integration")]
	public async Task NativeEndpointSourceFindsAnOwnedListenerAndRetainsItUntilExplicitReload(AddressFamily family, bool tcp) {
		if (family == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
			Assert.Ignore("IPv6 is unavailable on this Windows host.");
		using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var socket = new Socket(family, tcp ? SocketType.Stream : SocketType.Dgram, tcp ? ProtocolType.Tcp : ProtocolType.Udp);
		var address = family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
		socket.Bind(new IPEndPoint(address, 0));
		if (tcp)
			socket.Listen(1);
		var port = ((IPEndPoint)socket.LocalEndPoint).Port;
		var native = new WindowsNetworkEndpointDataSource(tcp, family, (uint)Environment.ProcessId);
		IDataSource<WindowsNetworkEndpoint> source = native;
		var page = await source.ReadRangeAsync(port.ToString(CultureInfo.InvariantCulture), 10, 0, nameof(WindowsNetworkEndpoint.LocalPort), SortDirection.Ascending);
		var row = page.Items.Single(entry => entry.LocalPort == port && entry.LocalAddress.Equals(address));
		Assert.That(row.ProcessId, Is.EqualTo((uint)Environment.ProcessId));
		socket.Dispose();
		Assert.That((await source.ReadRangeAsync(port.ToString(CultureInfo.InvariantCulture), 10, 0, null, SortDirection.None)).Items, Does.Contain(row));
		await native.ReloadAsync(lifetime.Token);
		Assert.That((await native.GetSnapshotAsync(lifetime.Token)).Any(entry => entry.LocalPort == port && entry.LocalAddress.Equals(address)), Is.False);
	}

	[Test, Category("Integration")]
	public async Task NativeServiceDriveAndInterfaceSourcesExposeBoundedPages() {
		using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var services = new WindowsServiceDataSource();
		await services.ReloadAsync(lifetime.Token);
		var servicePage = await services.ReadRangeAsync(null, 3, 0, nameof(WindowsServiceInfo.Name), SortDirection.Ascending);
		Assert.That(servicePage.Items.Count(), Is.InRange(1, 3));
		Assert.That(servicePage.TotalCount, Is.EqualTo(await services.CountAsync));
		var drives = new WindowsDriveDataSource();
		var driveRows = await drives.GetSnapshotAsync(lifetime.Token);
		Assert.That(driveRows.Any(row => string.Equals(row.Name, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase)), Is.True);
		var interfaces = new WindowsNetworkInterfaceDataSource();
		await interfaces.ReloadAsync(lifetime.Token);
		var interfacePage = await interfaces.ReadRangeAsync(null, 1, 0, nameof(WindowsNetworkInterface.Name), SortDirection.Ascending);
		Assert.That(interfacePage.Items.Count(), Is.EqualTo(1));
		Assert.That(interfacePage.TotalCount, Is.EqualTo(await interfaces.CountAsync));
	}

	[Test]
	public async Task PhysicalDiskAndPartitionSourcesKeepTypedDataAndNativeErrorsAcrossPages() {
		var disk = new WindowsPhysicalDiskInfo { DeviceId = "PhysicalDrive0", Index = 0, SizeBytes = 1024, ReadError = new Win32Exception(5) };
		var partition = new WindowsPartitionInfo { DeviceId = "Disk0Partition0", DiskIndex = 0, Index = 0, DriveRoots = new[] { "C:\\" } };
		IDataSource<WindowsPhysicalDiskInfo> disks = new WindowsPhysicalDiskDataSource(_ => Task.FromResult(new[] { disk }));
		IDataSource<WindowsPartitionInfo> partitions = new WindowsPartitionDataSource(_ => Task.FromResult(new[] { partition }));
		Assert.That((await disks.ReadRangeAsync(null, 1, int.MaxValue, nameof(WindowsPhysicalDiskInfo.Index), SortDirection.Ascending)).Items.Single(), Is.SameAs(disk));
		Assert.That((await partitions.ReadRangeAsync("Disk0", 1, 0, nameof(WindowsPartitionInfo.Index), SortDirection.Ascending)).Items.Single(), Is.SameAs(partition));
		Assert.That(disk.ReadError, Is.TypeOf<Win32Exception>());
	}

	private static WindowsNetworkEndpoint Endpoint(bool tcp, AddressFamily family, int port, uint processId)
		=> new(tcp, family, family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback, port, null, null, null, processId);
}
