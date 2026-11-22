// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Net;

namespace Sphere10.Framework.Windows.Networking;

/// <summary>How well Windows knows a neighbour's hardware address. The values are Windows' own.</summary>
public enum WindowsNeighborState {
	/// <summary>The address did not answer.</summary>
	Unreachable = 0,

	/// <summary>A request has been sent and no answer has come yet.</summary>
	Incomplete = 1,

	/// <summary>The entry is being checked again.</summary>
	Probe = 2,

	/// <summary>The entry is old and will be checked when it is next used.</summary>
	Delay = 3,

	/// <summary>The entry was learned some time ago and has not been confirmed since.</summary>
	Stale = 4,

	/// <summary>The address answered recently.</summary>
	Reachable = 5,

	/// <summary>The entry is fixed, such as a broadcast or multicast address or one added by hand.</summary>
	Permanent = 6
}

/// <summary>One entry of Windows' neighbour cache: an IP address on a local network and the hardware address that answers for it.</summary>
/// <param name="Address">The IP address.</param>
/// <param name="PhysicalAddress">The hardware (MAC) address, empty when Windows has not learned it.</param>
/// <param name="InterfaceIndex">The index of the network interface the neighbour was seen on.</param>
/// <param name="InterfaceName">That interface's name, or null when it could not be matched.</param>
/// <param name="State">How well Windows knows the entry.</param>
/// <param name="IsRouter">Whether the neighbour announced itself as a router.</param>
public sealed record WindowsNetworkNeighbor(IPAddress Address, byte[] PhysicalAddress, int InterfaceIndex, string InterfaceName, WindowsNeighborState State, bool IsRouter);
