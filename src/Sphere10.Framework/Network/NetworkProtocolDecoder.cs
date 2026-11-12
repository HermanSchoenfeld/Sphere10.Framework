// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Sphere10.Framework;

/// <summary>Bounded RFC 1035 message and complete RFC 8446/6066 ClientHello projections, without stream reassembly or decryption.</summary>
internal static class NetworkProtocolDecoder {
	internal static bool TryDns(ReadOnlySpan<byte> data, out DnsMessage message) {
		message = null;
		if (data.Length is < 12 or > 65535) return false;
		var questions = U16(data, 4);
		var answers = U16(data, 6); var authority = U16(data, 8); var additional = U16(data, 10);
		if (questions > 64 || answers + authority + additional > 256) return false;
		var offset = 12;
		var questionRows = new List<DnsQuestion>();
		for (var index = 0; index < questions; index++) {
			if (!TryName(data, ref offset, out var name) || offset > data.Length - 4) return false;
			questionRows.Add(new DnsQuestion(name, U16(data, offset), U16(data, offset + 2)));
			offset += 4;
		}
		var records = new List<DnsResourceRecord>();
		for (var index = 0; index < answers + authority + additional; index++) {
			if (!TryName(data, ref offset, out var name) || offset > data.Length - 10) return false;
			var type = U16(data, offset); var recordClass = U16(data, offset + 2);
			var ttl = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 4, 4));
			var size = U16(data, offset + 8); offset += 10;
			if (size > data.Length - offset) return false;
			var value = string.Empty;
			var next = offset + size;
			if (type == 1 && size == 4 || type == 28 && size == 16) value = new IPAddress(data.Slice(offset, size)).ToString();
			else if (type is 2 or 5 or 12) {
				var at = offset;
				if (!TryName(data, ref at, out value) || at > next) return false;
			} else if (type == 15 && size >= 3) {
				var at = offset + 2;
				if (!TryName(data, ref at, out var exchange) || at > next) return false;
				value = U16(data, offset) + " " + exchange;
			} else if (type == 16) {
				var text = new StringBuilder(); var at = offset;
				while (at < next) {
					var length = data[at++];
					if (length > next - at) return false;
					if (text.Length < 1024) { if (text.Length > 0) text.Append(' '); text.Append(Escape(data.Slice(at, Math.Min((int)length, 255)))); }
					at += length;
				}
				value = text.Length > 1024 ? text.ToString(0, 1024) + "… (Text Preview Limited)" : text.ToString();
			} else value = "Raw: " + Convert.ToHexString(data.Slice(offset, Math.Min(size, (ushort)256))) + (size > 256 ? "… (Raw Prefix; Packet Retains Full RDATA)" : string.Empty);
			records.Add(new DnsResourceRecord(index < answers ? "Answer" : index < answers + authority ? "Authority" : "Additional",
				name, type, recordClass, ttl, size, value));
			offset = next;
		}
		var flags = U16(data, 2);
		message = new DnsMessage(U16(data, 0), (flags & 0x8000) != 0, flags, (byte)(flags & 15), (flags & 0x0200) != 0, questionRows.ToArray(), records.ToArray());
		return true;
	}

	internal static bool TryClientHello(ReadOnlySpan<byte> data, out TlsClientHello hello) {
		hello = null;
		if (data.Length < 9 || data[0] != 22 || data[1] != 3 || U16(data, 3) > data.Length - 5 || data[5] != 1) return false;
		var recordEnd = 5 + U16(data, 3);
		var helloSize = (data[6] << 16) | (data[7] << 8) | data[8];
		if (helloSize < 35 || helloSize > recordEnd - 9) return false;
		var end = 9 + helloSize;
		var legacy = U16(data, 9);
		var offset = 9 + 34;
		if (offset >= end) return false;
		var session = data[offset++];
		if (session > 32 || session > end - offset) return false;
		offset += session;
		if (offset > end - 2) return false;
		var suites = U16(data, offset); offset += 2;
		if (suites == 0 || suites % 2 != 0 || suites > end - offset) return false;
		offset += suites;
		if (offset >= end) return false;
		var compression = data[offset++];
		if (compression == 0 || compression > end - offset) return false;
		offset += compression;
		var names = new List<string>(); var protocols = new List<string>(); var versions = new List<ushort>(); var types = new List<ushort>();
		if (offset < end) {
			if (offset > end - 2) return false;
			var extensions = U16(data, offset); offset += 2;
			if (extensions != end - offset) return false;
			while (offset < end) {
				if (types.Count >= 256 || offset > end - 4) return false;
				var type = U16(data, offset); var size = U16(data, offset + 2); offset += 4;
				if (size > end - offset) return false;
				var next = offset + size; types.Add(type);
				if (type == 0) {
					if (size < 2 || U16(data, offset) != size - 2) return false;
					var at = offset + 2;
					while (at < next) {
						if (at > next - 3) return false;
						var nameType = data[at++]; var length = U16(data, at); at += 2;
						if (length == 0 || length > next - at || names.Count >= 64) return false;
						if (nameType == 0) names.Add(Escape(data.Slice(at, length)));
						at += length;
					}
				} else if (type == 16) {
					if (size < 2 || U16(data, offset) != size - 2) return false;
					var at = offset + 2;
					while (at < next) {
						var length = data[at++];
						if (length == 0 || length > next - at || protocols.Count >= 64) return false;
						protocols.Add(Escape(data.Slice(at, length))); at += length;
					}
				} else if (type == 43) {
					if (size < 1 || data[offset] != size - 1 || data[offset] % 2 != 0) return false;
					for (var at = offset + 1; at < next; at += 2) versions.Add(U16(data, at));
				}
				offset = next;
			}
		}
		hello = new TlsClientHello(U16(data, 1), legacy, versions.ToArray(), names.ToArray(), protocols.ToArray(), suites / 2, types.ToArray());
		return true;
	}

	private static bool TryName(ReadOnlySpan<byte> data, ref int offset, out string name) {
		name = null;
		var at = offset; var consumed = -1; var expanded = 0;
		var result = new StringBuilder();
		for (var step = 0; step < 128; step++) {
			if (at >= data.Length) return false;
			var size = data[at++];
			if (size == 0) { offset = consumed < 0 ? at : consumed; name = result.Length == 0 ? "." : result.ToString(); return true; }
			if ((size & 0xC0) == 0xC0) {
				if (at >= data.Length) return false;
				var pointer = ((size & 0x3F) << 8) | data[at++];
				if (pointer >= at - 2) return false;
				if (consumed < 0) consumed = at;
				at = pointer; continue;
			}
			if ((size & 0xC0) != 0 || size > data.Length - at || (expanded += size + 1) > 255) return false;
			if (result.Length > 0) result.Append('.');
			result.Append(Escape(data.Slice(at, size), nameLabel: true)); at += size;
		}
		return false;
	}

	private static ushort U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
	private static string Escape(ReadOnlySpan<byte> bytes, bool nameLabel = false) {
		var text = new StringBuilder();
		foreach (var value in bytes)
			if (value is >= 33 and <= 126 && value != '\\' && (!nameLabel || value != '.')) text.Append((char)value);
			else text.Append('\\').Append(value.ToString("D3", System.Globalization.CultureInfo.InvariantCulture));
		return text.ToString();
	}
}
