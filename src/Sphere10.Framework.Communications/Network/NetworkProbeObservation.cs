// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Net;

namespace Sphere10.Framework.Communications;

public sealed record NetworkProbeObservation(DateTime TimestampUtc, string Operation, string Target, string Address,
	int? Hop, string Status, long? RoundTripMilliseconds, double? ElapsedMilliseconds, string Detail);

public sealed record NetworkDnsResult(string HostName, string[] Aliases, IPAddress[] Addresses);

public sealed record NetworkWhoisResult(string Server, string Query, string Response, bool IsLimited);

/// <summary>What one attempt to connect to a TCP port found.</summary>
public enum NetworkPortState {
	/// <summary>Something accepted the connection.</summary>
	Open,

	/// <summary>The host answered and refused the connection: nothing listens on the port.</summary>
	Closed,

	/// <summary>Nothing answered in time. A firewall may be dropping the attempt, or the host may be unreachable.</summary>
	NoReply
}

/// <summary>The result of one TCP connection attempt.</summary>
/// <param name="Port">The port that was tried.</param>
/// <param name="State">Whether the port accepted, refused or did not answer.</param>
/// <param name="ElapsedMilliseconds">How long the attempt took.</param>
/// <param name="Detail">The network error when the attempt neither connected nor was refused; otherwise empty.</param>
public sealed record NetworkPortProbeResult(int Port, NetworkPortState State, double ElapsedMilliseconds, string Detail);
