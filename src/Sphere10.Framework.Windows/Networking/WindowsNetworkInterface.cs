// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Net;
using System.Net.NetworkInformation;

namespace Sphere10.Framework.Windows.Networking;

public sealed class WindowsNetworkInterface {
	public string Id { get; internal set; }
	public string Name { get; internal set; }
	public string Description { get; internal set; }
	public OperationalStatus Status { get; internal set; }
	public NetworkInterfaceType InterfaceType { get; internal set; }
	public byte[] PhysicalAddress { get; internal set; } = Array.Empty<byte>();
	public long? LinkSpeedBitsPerSecond { get; internal set; }
	public Exception HardwareError { get; internal set; }
	public IPAddress[] Addresses { get; internal set; } = Array.Empty<IPAddress>();
	public IPAddress[] Gateways { get; internal set; } = Array.Empty<IPAddress>();
	public IPAddress[] DnsServers { get; internal set; } = Array.Empty<IPAddress>();
	public Exception AddressError { get; internal set; }
}
