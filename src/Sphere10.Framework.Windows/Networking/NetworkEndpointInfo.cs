// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows;

/// <summary>One TCP or UDP endpoint of a process, as the native endpoint tables report it.</summary>
public sealed class NetworkEndpointInfo {
	[Browsable(false), JsonIgnore]
	public ProcessIdentity Identity { get; set; }
	[DisplayName("Protocol"), Description("Transport protocol reported by the native endpoint table: TCP or UDP."), ReadOnly(true)]
	public string Protocol { get; set; } = string.Empty;
	[DisplayName("Address Family"), Description("Internet address family for this endpoint: IPv4 or IPv6."), ReadOnly(true)]
	public string AddressFamily { get; set; } = string.Empty;
	[DisplayName("Local Address"), Description("Local interface address; wildcard addresses indicate bindings to all applicable interfaces."), ReadOnly(true)]
	public string LocalAddress { get; set; } = string.Empty;
	[DisplayName("Local Port"), Description("Local transport port number after conversion from network byte order."), ReadOnly(true)]
	public int? LocalPort { get; set; }
	[DisplayName("Remote Address"), Description("Remote address for an established TCP endpoint; listeners and bound UDP endpoints have none."), ReadOnly(true)]
	public string RemoteAddress { get; set; } = string.Empty;
	[DisplayName("Remote Port"), Description("Remote TCP port number; listeners and bound UDP endpoints have no connected remote port."), ReadOnly(true)]
	public int? RemotePort { get; set; }
	[DisplayName("State"), Description("State reported by Windows for the thread or network endpoint."), ReadOnly(true)]
	public string State { get; set; } = string.Empty;
	[DisplayName("Process ID (PID)"), Description("Windows identifier for the process. A PID alone is not a permanent identity."), ReadOnly(true)]
	public int? ProcessId { get; set; }
	[DisplayName("Owning Process"), Description("Process name resolved from the endpoint owner PID. The process may exit during collection."), ReadOnly(true)]
	public string ProcessName { get; set; } = string.Empty;
	[DisplayName("Availability Details"), Description("Any failure, access restriction or source limitation affecting this snapshot."), ReadOnly(true)]
	public string Availability { get; set; } = string.Empty;
}
