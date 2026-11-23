# Networking

Reads TCP and UDP endpoint tables, network interfaces and interface counters, reads and changes the neighbour (ARP) table and resolves a hardware address by ARP, queries DNS and reads and flushes the DNS client cache, releases and renews DHCP leases, closes verified TCP connections and drives the in-box `pktmon.exe` packet monitor.

`Tools.Windows.Network` reads the IP Helper owner-PID tables for one protocol and address family as `WindowsNetworkEndpoint` records, reads `WindowsNetworkInterface` inventories with addresses, gateways and DNS servers, and reads raw `WindowsInterfaceCounters` byte totals for one interface. It holds no timers and computes no rates, so the caller owns sampling and deltas. The synchronous reads assert in debug builds when called on the main thread. Closing a connection is a two-step plan. `PrepareTcpCloseAsync` captures a `WindowsTcpConnectionIdentity` with the bind time and a verified [`ProcessIdentity`](../Processes/README.md), and `CloseTcpConnectionAsync` rechecks that tuple before it calls `SetTcpEntry` with `DELETE_TCB`. Only IPv4 TCP connections can be closed this way.

`GetNeighbors` reads Windows' neighbour cache, which is what `arp -a` lists: the IP addresses on the local networks whose hardware (MAC) addresses this computer has learned, by ARP for IPv4 and neighbour discovery for IPv6. Each `WindowsNetworkNeighbor` carries the IP address, the hardware address (empty when Windows has not learned it), the interface index and name, a `WindowsNeighborState` and whether the neighbour announced itself as a router. Reading the table sends nothing. `ResolvePhysicalAddress` sends an ARP request for one IPv4 address and returns the hardware address that answered. It returns `null` when nothing answered, as for an address on another network or a device that is switched off, and rejects an IPv6 address. `GetNeighborsAsync` and `ResolvePhysicalAddressAsync` run the same calls on a worker.

```csharp
var neighbours = await Tools.Windows.Network.GetNeighborsAsync();
foreach (var neighbour in neighbours.Where(item => item.PhysicalAddress.Length > 0))
	Console.WriteLine($"{neighbour.Address} {Convert.ToHexString(neighbour.PhysicalAddress)} {neighbour.State}");
// Tools.Network.ToMacAddressString, from Sphere10.Framework.Communications, writes the hyphenated form such as 00-1A-2B-3C-4D-5E.

// Sends an ARP request. The result is null when no device answers.
var hardware = await Tools.Windows.Network.ResolvePhysicalAddressAsync(IPAddress.Parse("192.168.1.1"));
```

The neighbour table can also be changed, which needs administrator rights. `DeleteNeighbor` removes the entry of a `WindowsNetworkNeighbor` that was read, by its address and interface index (`DeleteIpNetEntry2`). `FlushNeighbors(interfaceIndex)` clears one interface's entries, or those of every interface present in the table when no index is given (`FlushIpNetTable2`), and skips an interface that disappeared meanwhile. `AddStaticNeighbor(address, physicalAddress, interfaceIndex)` adds a permanent IPv4 or IPv6 entry (`CreateIpNetEntry2`). Each has an `...Async` twin that runs on a worker. A refusal for lack of rights is an `UnauthorizedAccessException` that says so, and any other failure is a `Win32Exception` with Windows' code.

`QueryDns(name, type, server, bypassCache)` asks Windows' DNS client (`DnsQuery_W`) for the records of one `WindowsDnsRecordType` and returns `WindowsDnsRecord` rows with the name, type, time to live, the value in readable form and the answer section. The value is the address for A and AAAA, the host name for NS, CNAME and PTR, `preference host` for MX, the strings joined together for TXT, `priority weight port target` for SRV and `primary admin serial refresh retry expire minimum` for SOA. Other types keep their number and have an empty value. For a PTR query an IP address can be given in place of the name, and its in-addr.arpa or ip6.arpa name is asked for. A name that does not exist and a name without records of that type give an empty array. An IPv4 `server` is asked directly, bypassing the cache, and an IPv6 server is refused with `NotSupportedException`, because `DnsQuery_W` takes only IPv4 server lists.

`GetDnsCache` lists Windows' DNS client cache as `WindowsDnsCacheEntry` rows through the undocumented `DnsGetCacheDataTable`, and reads each listed name back from the cache only, so one name can give several rows. Windows' list keeps names whose records have expired, and a name for which the cache returns no record gives one row with a null time to live and no data. On Windows 10 that also happens for many names that other applications resolved: a cache-only `DnsQuery_W` answers them with `DNS_ERROR_RECORD_DOES_NOT_EXIST` although `Get-DnsClientCache` shows their records. `FlushDnsCache` empties the cache, as `ipconfig /flushdns` does, and `FlushDnsCacheEntry(name)` removes one name. These three use undocumented exports, and a Windows without them gives `NotSupportedException` with a sentence. Each DNS member has an `...Async` twin.

`ReleaseDhcpAddress(interfaceIndex)` and `RenewDhcpAddress(interfaceIndex)` give up or renew the IPv4 lease of one adapter, as `ipconfig /release` and `/renew` do, through `GetInterfaceInfo`, `IpReleaseAddress` and `IpRenewAddress`. They need administrator rights and report failures as the neighbour changes do. An index that no IPv4 adapter has, and an adapter with a fixed address, give an `InvalidOperationException` before anything is sent. Renewing can block for many seconds, so UI code calls `RenewDhcpAddressAsync` and `ReleaseDhcpAddressAsync`.

```csharp
var mail = await Tools.Windows.Network.QueryDnsAsync("example.com", WindowsDnsRecordType.MX);
var reverse = await Tools.Windows.Network.QueryDnsAsync("192.168.1.1", WindowsDnsRecordType.PTR);
var cache = await Tools.Windows.Network.GetDnsCacheAsync();
```

`WindowsNetworkEndpointDataSource` and `WindowsNetworkInterfaceDataSource` are read-only `AsyncListDataSource` inventories that load on first use and then page the captured rows. A PID filter does not check process creation time. `ProcessNetworkEndpointDataSource` covers all four TCP/UDP and IPv4/IPv6 tables of one verified process, keeps the process handle open during capture and rejects an inventory that crossed the process's exit. `PktMonSentry` runs `pktmon.exe` with structured arguments to read status and filters, start and stop a capture and convert captures to text or pcapng. The caller owns capture, session and filter policy.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`NetworkEndpointInfo`](NetworkEndpointInfo.cs) | class | One TCP or UDP endpoint of a verified process with protocol, family, local and remote address and port, state and availability text |
| [`PktMonSentry`](PktMonSentry.cs) | class | `ProcessSentry` for `pktmon.exe` that reads status, filters and help, starts and stops captures and converts them to text or pcapng |
| [`ProcessNetworkEndpointDataSource`](ProcessNetworkEndpointDataSource.cs) | class | Read-only data source of every TCP and UDP, IPv4 and IPv6 endpoint owned by one verified process |
| [`WindowsDnsCacheEntry`](WindowsDnsRecord.cs) | record | One record of Windows' DNS client cache: name, type, time to live (null when the cache returned no record) and readable value |
| [`WindowsDnsRecord`](WindowsDnsRecord.cs) | record | One record of a DNS answer: name, type, time to live, readable value and answer section |
| [`WindowsDnsRecordType`](WindowsDnsRecord.cs) | enum | The DNS record types that are asked for and decoded, with the protocol's values: `A`, `NS`, `CNAME`, `SOA`, `PTR`, `MX`, `TXT`, `AAAA`, `SRV` and `Any` |
| [`WindowsInterfaceCounters`](WindowsInterfaceCounters.cs) | record | Raw `GetIfEntry2` row for one interface: identity, status, hardware and filter flags, link speeds and byte totals |
| [`WindowsNeighborState`](WindowsNetworkNeighbor.cs) | enum | How well Windows knows a neighbour's hardware address: `Unreachable`, `Incomplete`, `Probe`, `Delay`, `Stale`, `Reachable` or `Permanent`, with Windows' own values |
| [`WindowsNetworkEndpoint`](WindowsNetworkEndpoint.cs) | record | One owner-table row: protocol, family, local and remote endpoint, TCP state and owning PID |
| [`WindowsNetworkEndpointDataSource`](WindowsNetworkEndpointDataSource.cs) | class | Read-only data source over one TCP or UDP owner table for one address family, optionally filtered to a PID |
| [`WindowsNetworkInterface`](WindowsNetworkInterface.cs) | class | One network interface with type, status, MAC address, link speed, IP addresses, gateways, DNS servers and per-section read errors |
| [`WindowsNetworkInterfaceDataSource`](WindowsNetworkInterfaceDataSource.cs) | class | Read-only data source over the network-interface inventory without traffic-rate sampling |
| [`WindowsNetworkNeighbor`](WindowsNetworkNeighbor.cs) | record | One neighbour-cache entry: IP address, hardware address, interface index and name, state and router flag |
| [`Tools.Windows.Network`](WindowsNetworkUtil.cs) | static class | `Tools.Windows.Network` reads of endpoint tables, interfaces and counters, data-source factories and the verified IPv4 TCP close, with the neighbour table reads and changes (`GetNeighbors`, `ResolvePhysicalAddress`, `DeleteNeighbor`, `FlushNeighbors`, `AddStaticNeighbor`) in [WindowsNetworkUtil.Neighbors.cs](WindowsNetworkUtil.Neighbors.cs), DNS queries and the DNS client cache (`QueryDns`, `GetDnsCache`, `FlushDnsCache`, `FlushDnsCacheEntry`) in [WindowsNetworkUtil.Dns.cs](WindowsNetworkUtil.Dns.cs) and DHCP leases (`ReleaseDhcpAddress`, `RenewDhcpAddress`) in [WindowsNetworkUtil.Dhcp.cs](WindowsNetworkUtil.Dhcp.cs), each with an `...Async` twin |
| [`WindowsTcpConnectionIdentity`](WindowsTcpConnectionIdentity.cs) | record | A TCP endpoint with its verified owning process, bind time and capture time, used to recheck a connection before closing it |
