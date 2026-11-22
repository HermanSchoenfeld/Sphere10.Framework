// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Networking;

namespace Sphere10.Framework.Windows.Tests;

/// <summary>DNS queries through Windows' DNS client, the DNS client cache and the decoding of DNS_RECORDW and DNS_CACHE_ENTRY memory.
/// The ordinary cases only read; flushing the cache is explicit.</summary>
[TestFixture]
public class WindowsDnsTests {

	[Test]
	public async Task LocalhostResolvesToTheLoopbackAddress() {
		var records = await Tools.Windows.Network.QueryDnsAsync("localhost", WindowsDnsRecordType.A);
		Assert.That(records, Is.Not.Empty);
		Assert.That(records.Select(record => record.Type), Is.All.EqualTo(WindowsDnsRecordType.A));
		Assert.That(records.Select(record => record.Data), Does.Contain("127.0.0.1"));
		Assert.That(records.Select(record => record.Section), Is.All.EqualTo("Answer"));
	}

	[Test]
	public async Task ANameUnderInvalidGivesNoRecords()
		=> Assert.That(await Tools.Windows.Network.QueryDnsAsync("sphere10-framework-test.invalid", WindowsDnsRecordType.A), Is.Empty);

	[Test]
	public void ReverseLookupNamesAreBuiltForIPv4AndIPv6() {
		Assert.That(Tools.Windows.Network.GetReverseLookupName(IPAddress.Loopback), Is.EqualTo("1.0.0.127.in-addr.arpa"));
		Assert.That(Tools.Windows.Network.GetReverseLookupName(IPAddress.Parse("192.0.2.10")), Is.EqualTo("10.2.0.192.in-addr.arpa"));
		Assert.That(Tools.Windows.Network.GetReverseLookupName(IPAddress.IPv6Loopback), Is.EqualTo("1." + string.Concat(Enumerable.Repeat("0.", 31)) + "ip6.arpa"));
		Assert.That(Tools.Windows.Network.GetReverseLookupName(IPAddress.Parse("2001:db8::567:89ab")),
			Is.EqualTo("b.a.9.8.7.6.5.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa"));
	}

	[Test]
	public void AnIPv6ServerOrAMissingNameIsRefusedBeforeAnythingIsSent() {
		Assert.That(() => Tools.Windows.Network.QueryDnsAsync("localhost", WindowsDnsRecordType.A, IPAddress.IPv6Loopback), Throws.InstanceOf<NotSupportedException>());
		Assert.That(() => Tools.Windows.Network.QueryDnsAsync(" ", WindowsDnsRecordType.A), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	[Category("Integration")]
	public async Task AConfiguredIPv4DnsServerCanBeAskedDirectly() {
		var server = NetworkInterface.GetAllNetworkInterfaces().Where(item => item.OperationalStatus == OperationalStatus.Up)
			.SelectMany(item => item.GetIPProperties().DnsAddresses).FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
		if (server == null)
			Assert.Ignore("No IPv4 DNS server is configured.");
		var records = await Tools.Windows.Network.QueryDnsAsync("microsoft.com", WindowsDnsRecordType.A, server);
		if (records.Length == 0)
			Assert.Inconclusive($"{server} returned no A records for microsoft.com.");
		Assert.That(records.Where(record => record.Type == WindowsDnsRecordType.A).Select(record => IPAddress.Parse(record.Data).AddressFamily), Is.All.EqualTo(AddressFamily.InterNetwork));
		TestContext.WriteLine($"{server}: {string.Join(Environment.NewLine, records.Select(record => record.ToString()))}");
	}

	[Test]
	[Category("Integration")]
	public async Task TheDnsCacheIsReadWithNamesOnEveryRow() {
		var entries = await Tools.Windows.Network.GetDnsCacheAsync();
		Assert.That(entries, Is.Not.Null);
		Assert.That(entries.Select(entry => entry.Name), Is.All.Not.Null.And.Not.Empty);
		Assert.That(entries.Where(entry => entry.TimeToLive == null).Select(entry => entry.Data), Is.All.Empty);
		TestContext.WriteLine($"{entries.Length} cache rows,{entries.Count(entry => entry.TimeToLive != null)} with records, {entries.Select(entry => entry.Name).Distinct().Count()} names.");
	}

	[Test]
	public void AddressRecordsAreDecoded() {
		Assert.That(ReadSynthetic(WindowsDnsRecordType.A, 1, 60, "host.example", (data, _) => Marshal.Copy(new byte[] { 192, 0, 2, 1 }, 0, data, 4)),
			Is.EqualTo(new WindowsDnsRecord("host.example", WindowsDnsRecordType.A, TimeSpan.FromSeconds(60), "192.0.2.1", "Answer")));
		var ipv6 = IPAddress.Parse("2001:db8::1");
		Assert.That(ReadSynthetic(WindowsDnsRecordType.AAAA, 3, 0, "host.example", (data, _) => Marshal.Copy(ipv6.GetAddressBytes(), 0, data, 16)).Data, Is.EqualTo("2001:db8::1"));
	}

	[Test]
	public void NameRecordsAreDecoded() {
		// Section zero is what Windows gives the records it makes itself.
		var record = ReadSynthetic(WindowsDnsRecordType.CNAME, 0, 300, "www.example", (data, strings) => Marshal.WriteIntPtr(data, AllocateString(strings, "example")));
		Assert.That(record, Is.EqualTo(new WindowsDnsRecord("www.example", WindowsDnsRecordType.CNAME, TimeSpan.FromMinutes(5), "example", "Answer")));
	}

	[Test]
	public void MailExchangeRecordsGivePreferenceAndHost() {
		var record = ReadSynthetic(WindowsDnsRecordType.MX, 1, 3600, "example.com", (data, strings) => {
			Marshal.WriteIntPtr(data, AllocateString(strings, "mail.example.com"));
			Marshal.WriteInt16(data, IntPtr.Size, 10);
		});
		Assert.That(record.Data, Is.EqualTo("10 mail.example.com"));
	}

	[Test]
	public void TextRecordsJoinTheirStrings() {
		var record = ReadSynthetic(WindowsDnsRecordType.TXT, 1, 3600, "example.com", (data, strings) => {
			Marshal.WriteInt32(data, 2);
			Marshal.WriteIntPtr(data, IntPtr.Size, AllocateString(strings, "v=spf1 "));
			Marshal.WriteIntPtr(data, IntPtr.Size * 2, AllocateString(strings, "-all"));
		});
		Assert.That(record.Data, Is.EqualTo("v=spf1 -all"));
	}

	[Test]
	public void ServiceRecordsGivePriorityWeightPortAndTarget() {
		var record = ReadSynthetic(WindowsDnsRecordType.SRV, 3, 600, "_sip._tcp.example.com", (data, strings) => {
			Marshal.WriteIntPtr(data, AllocateString(strings, "sip.example.com"));
			Marshal.WriteInt16(data, IntPtr.Size, 10);
			Marshal.WriteInt16(data, IntPtr.Size + 2, 60);
			Marshal.WriteInt16(data, IntPtr.Size + 4, 5060);
		});
		Assert.That(record.Data, Is.EqualTo("10 60 5060 sip.example.com"));
		Assert.That(record.Section, Is.EqualTo("Additional"));
	}

	[Test]
	public void StartOfAuthorityRecordsGiveAllSevenFields() {
		var record = ReadSynthetic(WindowsDnsRecordType.SOA, 2, 900, "example.com", (data, strings) => {
			Marshal.WriteIntPtr(data, AllocateString(strings, "ns1.example.com"));
			Marshal.WriteIntPtr(data, IntPtr.Size, AllocateString(strings, "hostmaster.example.com"));
			var numbers = new[] { unchecked((int)2026111801U), 7200, 3600, 1209600, 300 };
			for (var index = 0; index < numbers.Length; index++)
				Marshal.WriteInt32(data, IntPtr.Size * 2 + index * 4, numbers[index]);
		});
		Assert.That(record.Data, Is.EqualTo("ns1.example.com hostmaster.example.com 2026111801 7200 3600 1209600 300"));
		Assert.That(record.Section, Is.EqualTo("Authority"));
	}

	[Test]
	public void ATypeOutsideTheEnumKeepsItsNumber() {
		var record = ReadSynthetic((WindowsDnsRecordType)99, 1, 1, "example.com", (_, _) => { });
		Assert.That((ushort)record.Type, Is.EqualTo(99));
		Assert.That(record.Data, Is.Empty);
	}

	[Test]
	public void TheCacheListIsWalkedWithNamesAndTypes() {
		var strings = new List<IntPtr>();
		var size = IntPtr.Size * 2 + 8;
		var first = Marshal.AllocHGlobal(size);
		var second = Marshal.AllocHGlobal(size);
		try {
			Marshal.WriteIntPtr(first, second);
			Marshal.WriteIntPtr(first, IntPtr.Size, AllocateString(strings, "example.com"));
			Marshal.WriteInt16(first, IntPtr.Size * 2, 1);
			Marshal.WriteIntPtr(second, IntPtr.Zero);
			Marshal.WriteIntPtr(second, IntPtr.Size, AllocateString(strings, "example.org"));
			Marshal.WriteInt16(second, IntPtr.Size * 2, 28);
			Assert.That(Tools.Windows.Network.ReadDnsCacheList(first), Is.EqualTo(new[] { ("example.com", (ushort)1), ("example.org", (ushort)28) }));
			Assert.That(Tools.Windows.Network.ReadDnsCacheList(IntPtr.Zero), Is.Empty);
		} finally {
			Marshal.FreeHGlobal(first);
			Marshal.FreeHGlobal(second);
			strings.ForEach(Marshal.FreeHGlobal);
		}
	}

	[Test]
	[Explicit("Empties this computer's DNS client cache.")]
	public async Task TheDnsCacheIsFlushed() {
		await Tools.Windows.Network.FlushDnsCacheAsync();
		await Tools.Windows.Network.QueryDnsAsync("localhost", WindowsDnsRecordType.A);
		Assert.That(await Tools.Windows.Network.GetDnsCacheAsync(), Is.Not.Null);
	}

	[Test]
	[Explicit("Removes a name from this computer's DNS client cache.")]
	public async Task OneNameIsFlushedFromTheDnsCache() {
		await Tools.Windows.Network.QueryDnsAsync("microsoft.com", WindowsDnsRecordType.A);
		await Tools.Windows.Network.FlushDnsCacheEntryAsync("microsoft.com");
		var entries = await Tools.Windows.Network.GetDnsCacheAsync();
		Assert.That(entries.Where(entry => entry.TimeToLive != null).Select(entry => entry.Name), Has.None.EqualTo("microsoft.com").IgnoreCase);
	}

	private static WindowsDnsRecord ReadSynthetic(WindowsDnsRecordType type, uint flags, uint timeToLive, string name, Action<IntPtr, List<IntPtr>> writeData) {
		var strings = new List<IntPtr>();
		var size = WinAPI.DNSAPI.DNS_RECORD_DATA_OFFSET + 64;
		var record = Marshal.AllocHGlobal(size);
		try {
			Marshal.Copy(new byte[size], 0, record, size);
			Marshal.WriteIntPtr(record, IntPtr.Size, AllocateString(strings, name));
			Marshal.WriteInt16(record, WinAPI.DNSAPI.DNS_RECORD_TYPE_OFFSET, unchecked((short)(ushort)type));
			Marshal.WriteInt32(record, WinAPI.DNSAPI.DNS_RECORD_TYPE_OFFSET + 4, (int)flags);
			Marshal.WriteInt32(record, WinAPI.DNSAPI.DNS_RECORD_TYPE_OFFSET + 8, (int)timeToLive);
			writeData(IntPtr.Add(record, WinAPI.DNSAPI.DNS_RECORD_DATA_OFFSET), strings);
			return Tools.Windows.Network.ReadDnsRecord(record);
		} finally {
			Marshal.FreeHGlobal(record);
			strings.ForEach(Marshal.FreeHGlobal);
		}
	}

	private static IntPtr AllocateString(List<IntPtr> strings, string value) {
		var pointer = Marshal.StringToHGlobalUni(value);
		strings.Add(pointer);
		return pointer;
	}
}
