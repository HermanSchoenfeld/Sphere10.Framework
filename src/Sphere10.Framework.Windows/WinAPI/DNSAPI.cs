// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {

	public static class DNSAPI {
		public const uint DNS_QUERY_STANDARD = 0x0;
		public const uint DNS_QUERY_BYPASS_CACHE = 0x8;
		public const uint DNS_QUERY_NO_WIRE_QUERY = 0x10;
		public const int DNS_ERROR_RCODE_NAME_ERROR = 9003;
		public const int DNS_INFO_NO_RECORDS = 9501;
		public const int DNS_ERROR_RECORD_DOES_NOT_EXIST = 9701;

		// DNS_RECORDW: pNext and pName pointers, then WORD wType, WORD wDataLength, DWORD Flags, DWORD dwTtl and DWORD dwReserved,
		// then the Data union. The union holds pointers, so it starts at 32 on x64 and at 24 on x86.
		// https://learn.microsoft.com/windows/win32/api/windns/ns-windns-dns_recordw
		public static readonly int DNS_RECORD_TYPE_OFFSET = IntPtr.Size * 2;
		public static readonly int DNS_RECORD_DATA_OFFSET = IntPtr.Size * 2 + 16;

		// DNS_CACHE_ENTRY (undocumented): pNext and pszName pointers, then WORD wType, WORD wDataLength and ULONG dwFlags.
		public static readonly int DNS_CACHE_ENTRY_TYPE_OFFSET = IntPtr.Size * 2;

		// pExtra is an IP4_ARRAY of DNS servers (a DWORD count followed by IPv4 addresses in network byte order) or null.
		// The results are released with DnsRecordListFree(results, DnsFreeRecordList), also when the status is an error.
		// https://learn.microsoft.com/windows/win32/api/windns/nf-windns-dnsquery_w
		[DllImport("dnsapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int DnsQuery_W(string name, ushort type, uint options, [In] uint[] extra, out IntPtr results, IntPtr reserved);

		// https://learn.microsoft.com/windows/win32/api/windns/nf-windns-dnsrecordlistfree
		[DllImport("dnsapi.dll", ExactSpelling = true)]
		public static extern void DnsRecordListFree(IntPtr recordList, DNS_FREE_TYPE freeType);

		// https://learn.microsoft.com/windows/win32/api/windns/nf-windns-dnsfree
		[DllImport("dnsapi.dll", ExactSpelling = true)]
		public static extern void DnsFree(IntPtr data, DNS_FREE_TYPE freeType);

		// Undocumented: returns the DNS client cache as a linked list of DNS_CACHE_ENTRY. Free each entry's name and the entry itself
		// with DnsFree(..., DnsFreeFlat). A missing export throws EntryPointNotFoundException when first called.
		[DllImport("dnsapi.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DnsGetCacheDataTable(out IntPtr cacheData);

		// Undocumented: what ipconfig /flushdns calls.
		[DllImport("dnsapi.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DnsFlushResolverCache();

		// Undocumented: removes every cached record of one name.
		[DllImport("dnsapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DnsFlushResolverCacheEntry_W(string name);

		#region Inner Types

		public enum DNS_FREE_TYPE {
			DnsFreeFlat = 0,
			DnsFreeRecordList = 1,
			DnsFreeParsedMessageFields = 2
		}

		public enum DNS_SECTION {
			DnsSectionQuestion = 0,
			DnsSectionAnswer = 1,
			DnsSectionAuthority = 2,
			DnsSectionAddtional = 3
		}

		#endregion
	}
}
