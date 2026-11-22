// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;

namespace Sphere10.Framework.Windows.Networking;

/// <summary>The DNS record types that <c>Tools.Windows.Network.QueryDns</c> asks for and decodes. The values are the DNS
/// protocol's own, so a record of another type keeps its number as an undefined value of this enum.</summary>
public enum WindowsDnsRecordType : ushort {
	/// <summary>An IPv4 address.</summary>
	[Description("IPv4 address")]
	A = 1,

	/// <summary>A name server of the zone.</summary>
	[Description("Name server")]
	NS = 2,

	/// <summary>An alias for another name.</summary>
	[Description("Canonical name (alias)")]
	CNAME = 5,

	/// <summary>The start of authority of a zone.</summary>
	[Description("Start of authority")]
	SOA = 6,

	/// <summary>A pointer to a name, which reverse lookups use.</summary>
	[Description("Pointer (reverse lookup)")]
	PTR = 12,

	/// <summary>A mail server with its preference.</summary>
	[Description("Mail exchange")]
	MX = 15,

	/// <summary>Free text.</summary>
	[Description("Text")]
	TXT = 16,

	/// <summary>An IPv6 address.</summary>
	[Description("IPv6 address")]
	AAAA = 28,

	/// <summary>A service's host and port with priority and weight.</summary>
	[Description("Service locator")]
	SRV = 33,

	/// <summary>Every record of the name, which a query can ask for but a record never has.</summary>
	[Description("Any record")]
	Any = 255
}

/// <summary>One record of a DNS answer, with its value in readable form.</summary>
/// <param name="Name">The name the record belongs to.</param>
/// <param name="Type">The record type. A type outside the enum keeps its number.</param>
/// <param name="TimeToLive">How long the record may be cached, as the answer gave it.</param>
/// <param name="Data">The value: the address for A and AAAA, the host name for NS, CNAME and PTR, "preference host" for MX,
/// the strings joined together for TXT, "priority weight port target" for SRV and "primary admin serial refresh retry expire minimum"
/// for SOA. It is empty for a type that is not decoded.</param>
/// <param name="Section">The part of the answer the record came from: Answer, Authority or Additional. Records Windows makes itself,
/// such as localhost and hosts-file names, are answers.</param>
public sealed record WindowsDnsRecord(string Name, WindowsDnsRecordType Type, TimeSpan TimeToLive, string Data, string Section);

/// <summary>One record in Windows' DNS client cache, which is what <c>ipconfig /displaydns</c> lists.</summary>
/// <param name="Name">The name the record belongs to.</param>
/// <param name="Type">The record type. A type outside the enum keeps its number.</param>
/// <param name="TimeToLive">How long the record stays cached, or null when Windows listed the name but returned no record for it. That is
/// the case for a cached "name does not exist" answer, a name whose records have expired, and on Windows 10 for many names that other
/// applications resolved, whose records a cache-only query does not return.</param>
/// <param name="Data">The value in the form <see cref="WindowsDnsRecord.Data"/> uses, empty when there is no record.</param>
public sealed record WindowsDnsCacheEntry(string Name, WindowsDnsRecordType Type, TimeSpan? TimeToLive, string Data);
