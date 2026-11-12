// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Net;

namespace Sphere10.Framework;

public sealed record NetworkProbeObservation(DateTime TimestampUtc, string Operation, string Target, string Address,
	int? Hop, string Status, long? RoundTripMilliseconds, double? ElapsedMilliseconds, string Detail);

public sealed record NetworkDnsResult(string HostName, string[] Aliases, IPAddress[] Addresses);

public sealed record NetworkWhoisResult(string Server, string Query, string Response, bool IsLimited);
