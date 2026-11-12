// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public sealed class NetworkDiagnosticsTests {
	[Test]
	public async Task WhoisContactsOnlyTheExplicitServerAndBoundsItsResponse() {
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var query = string.Empty;
		var server = Task.Run(async () => {
			using var client = await listener.AcceptTcpClientAsync(deadline.Token);
			await using var stream = client.GetStream();
			using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
			query = await reader.ReadLineAsync(deadline.Token);
			await stream.WriteAsync(Encoding.ASCII.GetBytes("ReferralServer: whois://uncontacted.invalid\r\n" + new string('x', 1000)), deadline.Token);
		});
		var result = await Tools.Network.ReadWhoisAsync("owned.example", "127.0.0.1", TimeSpan.FromSeconds(2), 32,
			((IPEndPoint)listener.LocalEndpoint).Port, deadline.Token);
		await server;
		Assert.That(query, Is.EqualTo("owned.example"));
		Assert.That(result.Response.Length, Is.EqualTo(32));
		Assert.That(result.IsLimited, Is.True);
		Assert.That(result.Server, Is.EqualTo("127.0.0.1"));
		Assert.That(async () => await Tools.Network.ReadWhoisAsync("one\r\ntwo", "127.0.0.1", TimeSpan.FromSeconds(1)), Throws.ArgumentException);
	}

	[Test]
	public async Task ResolverAndIcmpRouteUseActualLoopbackObservationsWithoutPublicNetworkTraffic() {
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var dns = await Tools.Network.ResolveHostAsync("localhost", TimeSpan.FromSeconds(2), deadline.Token);
		Assert.That(dns.Addresses.Any(IPAddress.IsLoopback), Is.True);
		var rows = await Tools.Network.TraceRouteAsync(IPAddress.Loopback, 3, TimeSpan.FromSeconds(1), cancellationToken: deadline.Token);
		Assert.That(rows.Length, Is.InRange(1, 3));
		Assert.That(rows.Select(row => row.Hop), Is.EqualTo(Enumerable.Range(1, rows.Length).Select(value => (int?)value)));
		Assert.That(rows.All(row => row.ElapsedMilliseconds >= 0 && row.Target == "127.0.0.1"), Is.True);
		Assert.That(rows.Where(row => row.Status != "Success").All(row => !row.RoundTripMilliseconds.HasValue), Is.True);
	}
}
