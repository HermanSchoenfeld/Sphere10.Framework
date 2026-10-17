// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Sphere10.Framework.Windows.Networking;

public sealed record WindowsNetworkEndpoint(bool IsTcp, AddressFamily AddressFamily, IPAddress LocalAddress, int LocalPort,
	IPAddress RemoteAddress, int? RemotePort, TcpState? State, uint ProcessId);
