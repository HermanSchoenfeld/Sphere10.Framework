// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public sealed class WindowsTcpCloseTests {
	[Test]
	public void FreshStateBindTimestampAndVerifiedProcessAreAllPartOfTheCloseIdentity() {
		Assert.That(Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_TCPROW>(), Is.EqualTo(20));
		Assert.That(Marshal.SizeOf<WinAPI.IPHLPAPI.MIB_TCPROW_OWNER_MODULE>(), Is.EqualTo(160));
		Assert.That(Marshal.OffsetOf<WinAPI.IPHLPAPI.MIB_TCPTABLE_OWNER_MODULE_HEADER>("FirstRow").ToInt32(), Is.EqualTo(8));
		var endpoint = new WindowsNetworkEndpoint(true, AddressFamily.InterNetwork, IPAddress.Loopback, 123, IPAddress.Loopback, 456, TcpState.Established, 42);
		var expected = new WindowsTcpConnectionIdentity(endpoint, new ProcessIdentity(42, DateTime.UnixEpoch), DateTime.UnixEpoch, DateTime.UtcNow);
		Assert.That(() => Tools.Windows.Network.VerifyTcpCloseIdentity(expected, expected), Throws.Nothing);
		Assert.That(() => Tools.Windows.Network.VerifyTcpCloseIdentity(expected, expected with { BoundAtUtc = DateTime.UnixEpoch.AddSeconds(1) }), Throws.InvalidOperationException);
		Assert.That(() => Tools.Windows.Network.VerifyTcpCloseIdentity(expected, expected with { Endpoint = endpoint with { State = TcpState.CloseWait } }), Throws.InvalidOperationException);
		Assert.That(() => Tools.Windows.Network.VerifyTcpCloseIdentity(expected, expected with { Process = new ProcessIdentity(42, DateTime.UtcNow) }), Throws.InvalidOperationException);
	}

	[Test]
	[Platform("Win")]
	public async Task NativeOwnedLoopbackPreflightReadsBindIdentityAndRejectsChangedStateBeforeAnyDelete() {
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port, deadline.Token);
		using var peer = await listener.AcceptTcpClientAsync(deadline.Token);
		var local = (IPEndPoint)client.Client.LocalEndPoint;
		var identity = await ProcessIdentity.CaptureAsync(Environment.ProcessId, deadline.Token);
		var endpoint = (await Tools.Windows.Network.GetEndpointsAsync(true, AddressFamily.InterNetwork, deadline.Token)).Single(row => row.LocalPort == local.Port && row.State == TcpState.Established);
		var plan = await Tools.Windows.Network.PrepareTcpCloseAsync(endpoint, identity, deadline.Token);
		Assert.That(plan.Process, Is.EqualTo(identity));
		Assert.That(plan.BoundAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
		Assert.That(plan.BoundAtUtc, Is.GreaterThanOrEqualTo(identity.StartTimeUtc.Value));
		peer.Dispose();
		var stateChanged = false;
		for (var attempt = 0; attempt < 50; attempt++) {
			var current = (await Tools.Windows.Network.GetEndpointsAsync(true, AddressFamily.InterNetwork, deadline.Token)).FirstOrDefault(row => row.LocalPort == local.Port);
			if (current?.State != TcpState.Established) { stateChanged = true; break; }
			await Task.Delay(10, deadline.Token);
		}
		Assert.That(stateChanged, Is.True, "This test must prove state invalidation before exercising the guarded no-delete path.");
		Assert.That(async () => await Tools.Windows.Network.CloseTcpConnectionAsync(plan, deadline.Token), Throws.InvalidOperationException.With.Message.Contains("changed"));
	}
}
