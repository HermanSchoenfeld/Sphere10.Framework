// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsSessionInfo {
	public string ServerName { get; init; }
	public uint SessionId { get; init; }
	public string StationName { get; init; }
	public string UserName { get; init; }
	public string Domain { get; init; }
	public WinAPI.WTSAPI32.WTS_CONNECTSTATE_CLASS State { get; init; }
	public DateTime? LogonTimeUtc { get; init; }
	public string ClientName { get; init; }
	public ushort? ClientProtocolType { get; init; }
	public int ReadErrorCode { get; init; }
	public bool IsVerified => !string.IsNullOrEmpty(UserName) && LogonTimeUtc.HasValue;

	public bool MatchesIdentity(WindowsSessionInfo other) => other != null && IsVerified && other.IsVerified && SessionId == other.SessionId
		&& string.Equals(ServerName, other.ServerName, StringComparison.OrdinalIgnoreCase) && LogonTimeUtc == other.LogonTimeUtc
		&& string.Equals(UserName, other.UserName, StringComparison.OrdinalIgnoreCase) && string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase);
}
