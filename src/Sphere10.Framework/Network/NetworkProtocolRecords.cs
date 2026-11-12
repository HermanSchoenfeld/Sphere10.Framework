// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public sealed record DnsMessage(ushort TransactionId, bool IsResponse, ushort Flags, byte BaseResponseCode, bool Truncated,
	DnsQuestion[] Questions, DnsResourceRecord[] Records);
public sealed record DnsQuestion(string Name, ushort Type, ushort Class);
public sealed record DnsResourceRecord(string Section, string Name, ushort Type, ushort Class, uint RawTimeToLiveField, int DataBytes, string Value) {
	public uint? TimeToLiveSeconds => Type == 41 ? null : RawTimeToLiveField;
}
public sealed record TlsClientHello(ushort RecordVersion, ushort LegacyClientVersion, ushort[] SupportedVersions, string[] ServerNames,
	string[] ApplicationProtocols, int CipherSuiteCount, ushort[] ExtensionTypes);
