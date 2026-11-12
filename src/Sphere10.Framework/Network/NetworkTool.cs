// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Net.NetworkInformation;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Sphere10.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace
namespace Tools;

public static class Network {

	public static string GetMimeType(string filePathOrExt)
		=> TryGetMimeType(filePathOrExt, out var mimeType) ? mimeType : throw new InvalidOperationException($"Unable to determine mimetype for {filePathOrExt}");

	public static bool TryGetMimeType(string filePathOrExt, out string mimeType)
		=> MimeHelper.TryGetMimeTypeFromFileExt(filePathOrExt, out mimeType);

	// TODO: needs refactoring like GetMacAddressses below
	public static IPAddress GetNetworkAddress() {

		// HS 2019-02-26: Android specifics refactored out in NET STANDARD 2
		foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()) {
			switch (netInterface.NetworkInterfaceType) {
				case NetworkInterfaceType.Loopback:
				case NetworkInterfaceType.Tunnel:
				case NetworkInterfaceType.Unknown:
					continue;
			}
			foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses) {
				if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork) {
					var ipAddress = addrInfo.Address; // use ipAddress as needed ... 
					return ipAddress;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Finds the first alpha-numerically sorted MAC address out of all MAC Addresses (that aren't loopbacks, tunnels or unknown).
	/// </summary>
	/// <remarks> This can be used as system identifier and handles cases of disabling/enabling NIC's, adding new ones. Will return different only
	/// if NIC is removed.</remarks>
	/// <returns>The MAC address.</returns>
	// EncryptedStringAttribute salts with this value, so it keeps the original unfiltered enumeration (which can yield the empty
	// address of an interface without hardware); filtering it would make values encrypted under the MAC-address policy unreadable.
	public static string GetMacAddressOrDefault() => GetPhysicalAddresses(false, true).Select(ToMacAddressString).OrderBy(x => x).FirstOrDefault();

	/// <summary>
	/// The distinct MAC addresses of this machine's network interfaces (excluding loopbacks, tunnels and unknown interfaces when
	/// <paramref name="ignoreInternalNics"/> is set). Interfaces without a hardware address, such as WAN miniports, virtual switches
	/// and debugger adapters, report an empty or all-zero address that identifies nothing, so they are skipped.
	/// </summary>
	public static IEnumerable<string> GetMacAddresses(bool onlyRunning = false, bool ignoreInternalNics = true)
		=> GetPhysicalAddresses(onlyRunning, ignoreInternalNics)
			.Where(address => address.Any(value => value != 0))
			.Select(ToMacAddressString)
			.Distinct();

	public static string ToMacAddressString(byte[] bytes) {
		var stringBuilder = new StringBuilder();
		for (int i = 0; i < bytes.Length; i++) {
			// Display the physical address in hexadecimal.
			stringBuilder.AppendFormat("{0}", bytes[i].ToString("X2"));
			// Insert a hyphen after each byte, unless we are at the end of the
			// address.
			if (i != bytes.Length - 1) {
				stringBuilder.AppendFormat("-");
			}
		}
		return stringBuilder.ToString();
	}


	/// <summary>Performs the complete resolver operation on a worker with an explicit deadline.</summary>
	public static Task<NetworkDnsResult> ResolveHostAsync(string host, TimeSpan timeout, CancellationToken cancellationToken = default) => Task.Run(async () => {
		Guard.ArgumentNotNullOrWhitespace(host, nameof(host));
		Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "Specify a positive resolver timeout.");
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		deadline.CancelAfter(timeout);
		var result = await Dns.GetHostEntryAsync(host, deadline.Token).ConfigureAwait(false);
		return new NetworkDnsResult(result.HostName, result.Aliases, result.AddressList);
	}, cancellationToken);

	public static Task<NetworkProbeObservation> PingAsync(IPAddress address, TimeSpan timeout, int timeToLive = 128, CancellationToken cancellationToken = default)
		=> Task.Run(() => ProbeAsync(address, timeout, timeToLive, "Ping", null, cancellationToken), cancellationToken);

	/// <summary>Bounded ICMP TTL/hop-limit probes. A missing hop is not evidence that routing stops there.</summary>
	public static Task<NetworkProbeObservation[]> TraceRouteAsync(IPAddress address, int maximumHops, TimeSpan hopTimeout,
		Action<NetworkProbeObservation> receive = null, CancellationToken cancellationToken = default) => Task.Run(async () => {
		Guard.ArgumentNotNull(address, nameof(address));
		Guard.ArgumentInRange(maximumHops, 1, 64, nameof(maximumHops));
		var rows = new List<NetworkProbeObservation>();
		for (var hop = 1; hop <= maximumHops; hop++) {
			cancellationToken.ThrowIfCancellationRequested();
			var row = await ProbeAsync(address, hopTimeout, hop, "Traceroute", hop, cancellationToken).ConfigureAwait(false);
			rows.Add(row);
			receive?.Invoke(row);
			if (row.Status == IPStatus.Success.ToString()) break;
		}
		return rows.ToArray();
	}, cancellationToken);

	/// <summary>Reads one explicitly selected WHOIS server. Referrals are returned as evidence, never followed automatically.</summary>
	public static Task<NetworkWhoisResult> ReadWhoisAsync(string query, string server, TimeSpan timeout, int maximumCharacters = 65536,
		int port = 43, CancellationToken cancellationToken = default) => Task.Run(async () => {
		Guard.ArgumentNotNullOrWhitespace(query, nameof(query));
		Guard.ArgumentNotNullOrWhitespace(server, nameof(server));
		Guard.Argument(query.Length <= 1024 && query.All(character => character is >= ' ' and <= '~'), nameof(query), "WHOIS requires a single bounded ASCII query; encode international domains as punycode.");
		Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "Specify a positive WHOIS timeout.");
		Guard.ArgumentInRange(maximumCharacters, 1, 1048576, nameof(maximumCharacters));
		Guard.ArgumentInRange(port, 1, 65535, nameof(port));
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		deadline.CancelAfter(timeout);
		using var client = new TcpClient();
		await client.ConnectAsync(server, port, deadline.Token).ConfigureAwait(false);
		await using var stream = client.GetStream();
		await stream.WriteAsync(Encoding.ASCII.GetBytes(query + "\r\n"), deadline.Token).ConfigureAwait(false);
		using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
		var response = new StringBuilder();
		var buffer = new char[4096];
		while (response.Length <= maximumCharacters) {
			var count = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maximumCharacters - response.Length + 1)), deadline.Token).ConfigureAwait(false);
			if (count == 0) break;
			response.Append(buffer, 0, count);
		}
		var limited = response.Length > maximumCharacters;
		if (limited) response.Length = maximumCharacters;
		return new NetworkWhoisResult(server, query, response.ToString(), limited);
	}, cancellationToken);

	/// <summary>Projects one complete bounded DNS message; malformed/unsupported input returns false without inferring fields.</summary>
	public static bool TryDecodeDnsMessage(ReadOnlySpan<byte> data, out DnsMessage message) => NetworkProtocolDecoder.TryDns(data, out message);

	/// <summary>Projects a complete ClientHello contained in one captured record. No TCP/record reassembly or payload decryption is implied.</summary>
	public static bool TryDecodeTlsClientHello(ReadOnlySpan<byte> data, out TlsClientHello hello) => NetworkProtocolDecoder.TryClientHello(data, out hello);
	private static async Task<NetworkProbeObservation> ProbeAsync(IPAddress address, TimeSpan timeout, int timeToLive, string operation, int? hop, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(address, nameof(address));
		Guard.Argument(timeout > TimeSpan.Zero && timeout.TotalMilliseconds <= int.MaxValue, nameof(timeout), "Specify a supported positive probe timeout.");
		Guard.ArgumentInRange(timeToLive, 1, 255, nameof(timeToLive));
		var capturedAt = DateTime.UtcNow;
		var started = Stopwatch.GetTimestamp();
		using var ping = new Ping();
		var result = await ping.SendPingAsync(address, timeout, new byte[32], new PingOptions(timeToLive, true), cancellationToken).ConfigureAwait(false);
		return new NetworkProbeObservation(capturedAt, operation, address.ToString(), result.Address?.ToString() ?? string.Empty,
			hop, result.Status.ToString(), result.Status == IPStatus.Success ? result.RoundtripTime : null,
			Stopwatch.GetElapsedTime(started).TotalMilliseconds, operation == "Traceroute" ? "ICMP Hop-Limit Probe; Filtered/Timed-Out Hops May Still Forward Traffic" : "ICMP Echo Probe");
	}

	// Raw hardware addresses of the selected interfaces, including the empty address of interfaces without hardware.
	private static IEnumerable<byte[]> GetPhysicalAddresses(bool onlyRunning, bool ignoreInternalNics) {
		var nics = NetworkInterface.GetAllNetworkInterfaces().AsEnumerable();
		if (onlyRunning)
			nics = nics.Where(x => x.OperationalStatus == OperationalStatus.Up);
		if (ignoreInternalNics)
			nics = nics.Where(x => !x.NetworkInterfaceType.IsIn(NetworkInterfaceType.Loopback, NetworkInterfaceType.Tunnel, NetworkInterfaceType.Unknown));
		return nics.Select(x => x.GetPhysicalAddress().GetAddressBytes());
	}
}

