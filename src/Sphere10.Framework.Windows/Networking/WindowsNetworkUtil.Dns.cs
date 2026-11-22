// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Networking;

namespace Tools;

public static partial class Windows {

	public static partial class Network {
		// A linked list longer than this is treated as corrupt rather than walked forever.
		private const int MaximumDnsListLength = 1_000_000;
		private const int MaximumTxtStrings = 4096;

		/// <summary>Asks Windows' DNS client for the records of a name, as <c>nslookup</c> or <c>Resolve-DnsName</c> do. For a PTR query an IP
		/// address may be given instead of a name, and its reverse name (in-addr.arpa or ip6.arpa) is asked for. A name that does not exist
		/// and a name without records of that type both give an empty array.</summary>
		/// <param name="name">The name to look up, or an IP address for a PTR query.</param>
		/// <param name="type">The record type to ask for.</param>
		/// <param name="server">An IPv4 DNS server to ask instead of the computer's configured servers, or null. The cache is bypassed when a server is given.</param>
		/// <param name="bypassCache">Whether to skip the DNS client cache and ask the network.</param>
		public static WindowsDnsRecord[] QueryDns(string name, WindowsDnsRecordType type, IPAddress server = null, bool bypassCache = false) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			uint[] servers = null;
			if (server != null) {
				if (server.AddressFamily != AddressFamily.InterNetwork)
					throw new NotSupportedException("Windows' DnsQuery asks only IPv4 DNS servers, so give the server's IPv4 address.");
				servers = new[] { 1U, BitConverter.ToUInt32(server.GetAddressBytes(), 0) };
			}
			var options = bypassCache || server != null ? WinAPI.DNSAPI.DNS_QUERY_BYPASS_CACHE : WinAPI.DNSAPI.DNS_QUERY_STANDARD;
			var status = QueryDnsRecords(GetQueryName(name, type), (ushort)type, options, servers, out var records);
			if (status is WinAPI.DNSAPI.DNS_ERROR_RCODE_NAME_ERROR or WinAPI.DNSAPI.DNS_INFO_NO_RECORDS)
				return System.Array.Empty<WindowsDnsRecord>();
			if (status != 0)
				throw new Win32Exception(status);
			return records;
		}

		public static Task<WindowsDnsRecord[]> QueryDnsAsync(string name, WindowsDnsRecordType type, IPAddress server = null, bool bypassCache = false, CancellationToken cancellationToken = default)
			=> Task.Run(() => QueryDns(name, type, server, bypassCache), cancellationToken);

		/// <summary>Reads Windows' DNS client cache, which is what <c>ipconfig /displaydns</c> lists. Each cached name and type is read back from
		/// the cache only, without a network query, so one name can give several rows (one per record, such as an alias and its addresses).
		/// Windows' list also keeps names whose records have expired. A name it lists without returning a record gives one row with no time to
		/// live and no data: an expired name, a cached "name does not exist" answer, and on Windows 10 many names that other applications
		/// resolved, which a cache-only query answers with DNS_ERROR_RECORD_DOES_NOT_EXIST although <c>Get-DnsClientCache</c> shows their records.</summary>
		/// <exception cref="NotSupportedException">This version of Windows does not provide the cache listing.</exception>
		public static WindowsDnsCacheEntry[] GetDnsCache() {
			Debug.AssertNotMainThread();
			var head = IntPtr.Zero;
			var error = 0;
			var listed = CallDnsExport("DnsGetCacheDataTable", () => {
				var result = WinAPI.DNSAPI.DnsGetCacheDataTable(out head);
				error = Marshal.GetLastPInvokeError();
				return result;
			});
			(string Name, ushort Type)[] cached;
			try {
				if (!listed && error != 0)
					throw new Win32Exception(error);
				// A failure that carries no error code is taken as an empty cache.
				cached = listed ? ReadDnsCacheList(head) : System.Array.Empty<(string, ushort)>();
			} finally {
				FreeDnsCacheList(head);
			}
			var rows = new List<WindowsDnsCacheEntry>(cached.Length);
			foreach (var (name, type) in cached) {
				if (string.IsNullOrEmpty(name))
					continue;
				var status = QueryDnsRecords(name, type, WinAPI.DNSAPI.DNS_QUERY_NO_WIRE_QUERY, null, out var records);
				if (status != 0 || records.Length == 0)
					rows.Add(new WindowsDnsCacheEntry(name, (WindowsDnsRecordType)type, null, string.Empty));
				else
					rows.AddRange(records.Select(record => new WindowsDnsCacheEntry(string.IsNullOrEmpty(record.Name) ? name : record.Name, record.Type, record.TimeToLive, record.Data)));
			}
			return rows.ToArray();
		}

		public static Task<WindowsDnsCacheEntry[]> GetDnsCacheAsync(CancellationToken cancellationToken = default) => Task.Run(GetDnsCache, cancellationToken);

		/// <summary>Empties Windows' DNS client cache, as <c>ipconfig /flushdns</c> does.</summary>
		/// <exception cref="NotSupportedException">This version of Windows does not provide the cache flush.</exception>
		public static void FlushDnsCache() {
			Debug.AssertNotMainThread();
			var error = 0;
			var flushed = CallDnsExport("DnsFlushResolverCache", () => {
				var result = WinAPI.DNSAPI.DnsFlushResolverCache();
				error = Marshal.GetLastPInvokeError();
				return result;
			});
			if (!flushed)
				throw DnsChangeError(error, "flush the DNS client cache");
		}

		public static Task FlushDnsCacheAsync(CancellationToken cancellationToken = default) => Task.Run(FlushDnsCache, cancellationToken);

		/// <summary>Removes every cached record of one name from Windows' DNS client cache. A name that is not cached is left as it is.</summary>
		/// <exception cref="NotSupportedException">This version of Windows does not provide the entry flush.</exception>
		public static void FlushDnsCacheEntry(string name) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			var error = 0;
			var flushed = CallDnsExport("DnsFlushResolverCacheEntry_W", () => {
				var result = WinAPI.DNSAPI.DnsFlushResolverCacheEntry_W(name);
				error = Marshal.GetLastPInvokeError();
				return result;
			});
			// A failure that carries no error code is taken as a name that is not cached.
			if (!flushed && error != 0)
				throw DnsChangeError(error, "flush the DNS cache entry");
		}

		public static Task FlushDnsCacheEntryAsync(string name, CancellationToken cancellationToken = default) => Task.Run(() => FlushDnsCacheEntry(name), cancellationToken);

		/// <summary>The name a reverse (PTR) lookup of an address asks for: the IPv4 octets reversed under in-addr.arpa, or the 32 IPv6
		/// nibbles reversed under ip6.arpa.</summary>
		internal static string GetReverseLookupName(IPAddress address) {
			Guard.ArgumentNotNull(address, nameof(address));
			var bytes = address.GetAddressBytes().Reverse();
			return address.AddressFamily == AddressFamily.InterNetwork
				? string.Join(".", bytes.Select(item => item.ToString(CultureInfo.InvariantCulture))) + ".in-addr.arpa"
				: string.Join(".", bytes.SelectMany(item => new[] { item & 0xF, item >> 4 }).Select(nibble => nibble.ToString("x", CultureInfo.InvariantCulture))) + ".ip6.arpa";
		}

		/// <summary>Reads one DNS_RECORDW into a detached record. The record's memory stays with its owner.</summary>
		internal static WindowsDnsRecord ReadDnsRecord(IntPtr record) {
			var name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(record, IntPtr.Size)) ?? string.Empty;
			var type = unchecked((ushort)Marshal.ReadInt16(record, WinAPI.DNSAPI.DNS_RECORD_TYPE_OFFSET));
			var flags = unchecked((uint)Marshal.ReadInt32(record, WinAPI.DNSAPI.DNS_RECORD_TYPE_OFFSET + 4));
			var timeToLive = unchecked((uint)Marshal.ReadInt32(record, WinAPI.DNSAPI.DNS_RECORD_TYPE_OFFSET + 8));
			var data = ReadDnsData((WindowsDnsRecordType)type, IntPtr.Add(record, WinAPI.DNSAPI.DNS_RECORD_DATA_OFFSET));
			return new WindowsDnsRecord(name, (WindowsDnsRecordType)type, TimeSpan.FromSeconds(timeToLive), data, GetDnsSectionName(flags & 3));
		}

		/// <summary>Reads the names and types of a DNS_CACHE_ENTRY list without releasing it.</summary>
		internal static (string Name, ushort Type)[] ReadDnsCacheList(IntPtr head) {
			var entries = new List<(string, ushort)>();
			for (var entry = head; entry != IntPtr.Zero; entry = Marshal.ReadIntPtr(entry)) {
				Guard.Ensure(entries.Count < MaximumDnsListLength, "The DNS cache list did not end.");
				entries.Add((Marshal.PtrToStringUni(Marshal.ReadIntPtr(entry, IntPtr.Size)), unchecked((ushort)Marshal.ReadInt16(entry, WinAPI.DNSAPI.DNS_CACHE_ENTRY_TYPE_OFFSET))));
			}
			return entries.ToArray();
		}

		private static int QueryDnsRecords(string name, ushort type, uint options, uint[] servers, out WindowsDnsRecord[] records) {
			var status = WinAPI.DNSAPI.DnsQuery_W(name, type, options, servers, out var results, IntPtr.Zero);
			try {
				records = status == 0 ? ReadDnsRecordList(results) : System.Array.Empty<WindowsDnsRecord>();
				return status;
			} finally {
				if (results != IntPtr.Zero)
					WinAPI.DNSAPI.DnsRecordListFree(results, WinAPI.DNSAPI.DNS_FREE_TYPE.DnsFreeRecordList);
			}
		}

		private static WindowsDnsRecord[] ReadDnsRecordList(IntPtr head) {
			var records = new List<WindowsDnsRecord>();
			for (var record = head; record != IntPtr.Zero; record = Marshal.ReadIntPtr(record)) {
				Guard.Ensure(records.Count < MaximumDnsListLength, "The DNS record list did not end.");
				records.Add(ReadDnsRecord(record));
			}
			return records.ToArray();
		}

		private static string ReadDnsData(WindowsDnsRecordType type, IntPtr data) {
			var pointer = IntPtr.Size;
			switch (type) {
				case WindowsDnsRecordType.A:
					return ReadAddress(data, 4).ToString();
				case WindowsDnsRecordType.AAAA:
					return ReadAddress(data, 16).ToString();
				case WindowsDnsRecordType.NS or WindowsDnsRecordType.CNAME or WindowsDnsRecordType.PTR:
					return ReadDnsString(data, 0);
				case WindowsDnsRecordType.MX:
					return $"{ReadWord(data, pointer)} {ReadDnsString(data, 0)}";
				case WindowsDnsRecordType.TXT:
					var count = Math.Clamp(Marshal.ReadInt32(data), 0, MaximumTxtStrings);
					var text = new StringBuilder();
					for (var index = 0; index < count; index++)
						text.Append(ReadDnsString(data, pointer + index * pointer));
					return text.ToString();
				case WindowsDnsRecordType.SRV:
					return $"{ReadWord(data, pointer)} {ReadWord(data, pointer + 2)} {ReadWord(data, pointer + 4)} {ReadDnsString(data, 0)}";
				case WindowsDnsRecordType.SOA:
					var numbers = Enumerable.Range(0, 5).Select(index => unchecked((uint)Marshal.ReadInt32(data, 2 * pointer + index * 4)).ToString(CultureInfo.InvariantCulture));
					return $"{ReadDnsString(data, 0)} {ReadDnsString(data, pointer)} {string.Join(" ", numbers)}";
				default:
					return string.Empty;
			}
		}

		private static IPAddress ReadAddress(IntPtr data, int length) {
			var bytes = new byte[length];
			Marshal.Copy(data, bytes, 0, length);
			return new IPAddress(bytes);
		}

		private static string ReadDnsString(IntPtr data, int offset) => Marshal.PtrToStringUni(Marshal.ReadIntPtr(data, offset)) ?? string.Empty;

		private static string ReadWord(IntPtr data, int offset) => unchecked((ushort)Marshal.ReadInt16(data, offset)).ToString(CultureInfo.InvariantCulture);

		// Windows leaves the section of the records it makes itself, such as localhost and hosts-file names, at zero (the question
		// section). A query result never holds the question, so zero is an answer, as Get-DnsClientCache also shows it.
		private static string GetDnsSectionName(uint section) => section switch {
			(uint)WinAPI.DNSAPI.DNS_SECTION.DnsSectionAuthority => "Authority",
			(uint)WinAPI.DNSAPI.DNS_SECTION.DnsSectionAddtional => "Additional",
			_ => "Answer"
		};

		private static string GetQueryName(string name, WindowsDnsRecordType type)
			=> type == WindowsDnsRecordType.PTR && TryParseAddressLiteral(name.Trim(), out var address) ? GetReverseLookupName(address) : name;

		// IPAddress.TryParse also accepts forms such as "1" or "1.2", which are names here, so an IPv4 literal needs all four octets.
		private static bool TryParseAddressLiteral(string name, out IPAddress address)
			=> IPAddress.TryParse(name, out address) && (address.AddressFamily == AddressFamily.InterNetworkV6 ? name.Contains(':') : name.Split('.').Length == 4);

		private static void FreeDnsCacheList(IntPtr head) {
			var entry = head;
			while (entry != IntPtr.Zero) {
				var next = Marshal.ReadIntPtr(entry);
				var name = Marshal.ReadIntPtr(entry, IntPtr.Size);
				if (name != IntPtr.Zero)
					WinAPI.DNSAPI.DnsFree(name, WinAPI.DNSAPI.DNS_FREE_TYPE.DnsFreeFlat);
				WinAPI.DNSAPI.DnsFree(entry, WinAPI.DNSAPI.DNS_FREE_TYPE.DnsFreeFlat);
				entry = next;
			}
		}

		// The cache exports are undocumented, so a Windows without them is reported in a sentence instead of a missing entry point.
		private static bool CallDnsExport(string export, Func<bool> call) {
			try {
				return call();
			} catch (EntryPointNotFoundException error) {
				throw new NotSupportedException($"This version of Windows does not provide {export} in dnsapi.dll.", error);
			}
		}

		private static Exception DnsChangeError(int error, string action) => error switch {
			(int)WinAPI.IPHLPAPI.ERROR_ACCESS_DENIED => new UnauthorizedAccessException($"Windows refused to {action}; administrator rights are required."),
			0 => new InvalidOperationException($"Windows could not {action}."),
			_ => new Win32Exception(error)
		};
	}
}
