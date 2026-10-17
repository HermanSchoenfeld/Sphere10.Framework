// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Net.NetworkInformation;

namespace Sphere10.Framework.Windows.Networking;

public sealed record WindowsInterfaceCounters(Guid InterfaceId, ulong InterfaceLuid, uint InterfaceIndex, OperationalStatus Status,
	bool IsHardware, bool IsFilter, ulong ReceiveLinkSpeed, ulong TransmitLinkSpeed, ulong BytesReceived, ulong BytesSent);
