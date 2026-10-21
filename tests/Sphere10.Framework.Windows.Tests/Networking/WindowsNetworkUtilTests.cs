// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Win32Exception = System.ComponentModel.Win32Exception;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsNetworkUtilTests {
	[Test]
	public void InterfaceCounterFacadeMatchesTheWindowsSdkLayout() {
		Assert.That(Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(), Is.EqualTo(1352));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(nameof(WinAPI.IPHLPAPI.MIB_IF_ROW2.InterfaceIndex)).ToInt32(), Is.EqualTo(8));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(nameof(WinAPI.IPHLPAPI.MIB_IF_ROW2.InOctets)).ToInt32(), Is.EqualTo(1208));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_IF_ROW2>(nameof(WinAPI.IPHLPAPI.MIB_IF_ROW2.OutOctets)).ToInt32(), Is.EqualTo(1280));
	}

	[TestCase(AddressFamily.InterNetwork, true)]
	[TestCase(AddressFamily.InterNetworkV6, true)]
	[TestCase(AddressFamily.InterNetwork, false)]
	[TestCase(AddressFamily.InterNetworkV6, false)]
	[Category("Integration")]
	public async Task TypedEndpointUtilReportsTheOwnedSocketAndExactAddressFamily(AddressFamily family, bool tcp) {
		if (family == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
			Assert.Ignore("IPv6 is unavailable on this Windows host.");
		using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var socket = new Socket(family, tcp ? SocketType.Stream : SocketType.Dgram, tcp ? ProtocolType.Tcp : ProtocolType.Udp);
		var address = family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
		socket.Bind(new IPEndPoint(address, 0));
		if (tcp)
			socket.Listen(1);
		var port = ((IPEndPoint)socket.LocalEndPoint).Port;
		var row = (await Tools.Windows.Network.GetEndpointsAsync(tcp, family, lifetime.Token)).Single(value => value.LocalPort == port && value.LocalAddress.Equals(address));
		Assert.That(row.AddressFamily, Is.EqualTo(family));
		Assert.That(row.ProcessId, Is.EqualTo((uint)Environment.ProcessId));
		Assert.That(row.IsTcp, Is.EqualTo(tcp));
		Assert.That(row.RemoteAddress, Is.Null);
		Assert.That(row.RemotePort, Is.Null);
	}

	[Test, Category("Integration")]
	public void NativeInterfaceCountersKeepTheGuidLuidAndUnsignedTotals() {
		var interfaces = Tools.Windows.Network.GetInterfaces();
		var available = 0;
		foreach (var adapter in interfaces) {
			if (!Guid.TryParse(adapter.Id, out var id))
				continue;
			WindowsInterfaceCounters counters;
			try { counters = Tools.Windows.Network.GetInterfaceCounters(id); }
			catch (Win32Exception error) { TestContext.WriteLine($"Interface {id} counters unavailable: Win32 {error.NativeErrorCode}."); continue; }
			Assert.That(counters.InterfaceId, Is.EqualTo(id));
			Assert.That(counters.InterfaceIndex, Is.GreaterThan(0));
			Assert.That(counters.InterfaceLuid, Is.GreaterThan(0));
			available++;
		}
		Assert.That(available, Is.GreaterThan(0));
	}

	[Test]
	public async Task CancellationStopsBeforeNativeEndpointOrAdapterReads() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var endpoints = Tools.Windows.Network.GetEndpointsAsync(true, AddressFamily.InterNetwork, cancellation.Token);
		var interfaces = Tools.Windows.Network.GetInterfacesAsync(cancellation.Token);
		await Assert.ThatAsync(async () => await endpoints, Throws.InstanceOf<OperationCanceledException>().With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellation.Token));
		await Assert.ThatAsync(async () => await interfaces, Throws.InstanceOf<OperationCanceledException>().With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellation.Token));
		Assert.That(endpoints.Status, Is.EqualTo(TaskStatus.Canceled), "Canceling enumeration must not report success or turn expected cancellation into a fault.");
		Assert.That(interfaces.Status, Is.EqualTo(TaskStatus.Canceled), "The caller must receive cancellation rather than an empty interface inventory.");
	}

	[Test]
	public async Task CancellationDuringANativeReadWaitsForCleanupAndDoesNotThrowInTheWorker() {
		using var cancellation = new CancellationTokenSource();
		using var release = new ManualResetEventSlim();
		using var finishRead = Tools.Scope.ExecuteOnDispose(release.Set);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var workerThread = 0;
		var workerCancellationExceptions = 0;
		var cleanedUp = false;
		EventHandler<FirstChanceExceptionEventArgs> observe = (_, args) => {
			if (args.Exception is OperationCanceledException && Environment.CurrentManagedThreadId == Volatile.Read(ref workerThread))
				Interlocked.Increment(ref workerCancellationExceptions);
		};
		Task<WindowsNetworkInterface[]> pending;
		AppDomain.CurrentDomain.FirstChanceException += observe;
		using (Tools.Scope.ExecuteOnDispose(() => AppDomain.CurrentDomain.FirstChanceException -= observe)) {
			pending = new WindowsNetworkUtil().GetInterfacesAsync(() => {
				Volatile.Write(ref workerThread, Environment.CurrentManagedThreadId);
				using var cleanup = Tools.Scope.ExecuteOnDispose(() => cleanedUp = true);
				entered.TrySetResult();
				release.Wait();
				return Array.Empty<NetworkInterface>();
			}, cancellation.Token);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
			cancellation.Cancel();
			Assert.That(pending.IsCompleted, Is.False, "Cancellation cannot finish the operation while its synchronous native read still owns work.");
			release.Set();
			Assert.That(await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(20))), Is.SameAs(pending));
			Assert.That(pending.Status, Is.EqualTo(TaskStatus.Canceled));
			Assert.That(cleanedUp, Is.True);
			Assert.That(workerCancellationExceptions, Is.Zero, "Expected cancellation must not throw from the synchronous enumeration worker.");
		}
		await Assert.ThatAsync(async () => await pending, Throws.InstanceOf<OperationCanceledException>()
			.With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellation.Token));
	}

	[Test]
	public async Task NativeReadFailuresRemainFaultedWithTheOriginalException() {
		var expected = new NetworkInformationException(5);
		var pending = new WindowsNetworkUtil().GetInterfacesAsync(() => throw expected, CancellationToken.None);
		await Assert.ThatAsync(async () => await pending, Throws.Exception.SameAs(expected));
		Assert.That(pending.Status, Is.EqualTo(TaskStatus.Faulted));
	}
}
