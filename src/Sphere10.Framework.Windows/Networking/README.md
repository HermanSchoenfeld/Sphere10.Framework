# Networking

Reads TCP and UDP endpoint tables, network interfaces and interface counters, closes verified TCP connections and drives the in-box `pktmon.exe` packet monitor.

`WindowsNetworkUtil` (`Tools.Windows.Network`) reads the IP Helper owner-PID tables for one protocol and address family as `WindowsNetworkEndpoint` records, reads `WindowsNetworkInterface` inventories with addresses, gateways and DNS servers, and reads raw `WindowsInterfaceCounters` byte totals for one interface. It holds no timers and computes no rates, so the caller owns sampling and deltas. The synchronous reads assert in debug builds when called on the main thread. Closing a connection is a two-step plan. `PrepareTcpCloseAsync` captures a `WindowsTcpConnectionIdentity` with the bind time and a verified [`ProcessIdentity`](../Processes/README.md), and `CloseTcpConnectionAsync` rechecks that tuple before it calls `SetTcpEntry` with `DELETE_TCB`. Only IPv4 TCP connections can be closed this way.

`WindowsNetworkEndpointDataSource` and `WindowsNetworkInterfaceDataSource` are read-only `AsyncListDataSource` inventories that load on first use and then page the captured rows. A PID filter does not check process creation time. `ProcessNetworkEndpointDataSource` covers all four TCP/UDP and IPv4/IPv6 tables of one verified process, keeps the process handle open during capture and rejects an inventory that crossed the process's exit. `PktMonSentry` runs `pktmon.exe` with structured arguments to read status and filters, start and stop a capture and convert captures to text or pcapng. The caller owns capture, session and filter policy.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`PktMonSentry`](PktMonSentry.cs) | class | `ProcessSentry` for `pktmon.exe` that reads status, filters and help, starts and stops captures and converts them to text or pcapng |
| [`WindowsInterfaceCounters`](WindowsInterfaceCounters.cs) | record | Raw `GetIfEntry2` row for one interface: identity, status, hardware and filter flags, link speeds and byte totals |
| [`WindowsNetworkEndpoint`](WindowsNetworkEndpoint.cs) | record | One owner-table row: protocol, family, local and remote endpoint, TCP state and owning PID |
| [`WindowsNetworkEndpointDataSource`](WindowsNetworkEndpointDataSource.cs) | class | Read-only data source over one TCP or UDP owner table for one address family, optionally filtered to a PID |
| [`WindowsNetworkInterface`](WindowsNetworkInterface.cs) | class | One network interface with type, status, MAC address, link speed, IP addresses, gateways, DNS servers and per-section read errors |
| [`WindowsNetworkInterfaceDataSource`](WindowsNetworkInterfaceDataSource.cs) | class | Read-only data source over the network-interface inventory without traffic-rate sampling |
| [`WindowsNetworkUtil`](WindowsNetworkUtil.cs) | class | `Tools.Windows.Network` reads of endpoint tables, interfaces and counters, data-source factories and the verified IPv4 TCP close |
