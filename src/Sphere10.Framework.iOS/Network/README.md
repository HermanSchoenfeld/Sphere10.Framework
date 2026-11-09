# Network

A legacy Xamarin.iOS network-reachability helper that reports remote-host, internet and local Wi-Fi status.

`Reachability` is a static class over `NetworkReachability`. `RemoteHostStatus` probes the host in the static `HostName` field, which defaults to `www.google.com`. `InternetConnectionStatus` probes the default route and `LocalWifiConnectionStatus` probes the link-local 169.254.0.0 address. The first call of each status method schedules its probe on the current `CFRunLoop`, and every scheduled probe raises the static `ReachabilityChanged` event without saying what changed. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`NetworkStatus`](NetworkStatus.cs) | enum | `NotReachable`, `ReachableViaCarrierDataNetwork` or `ReachableViaWiFiNetwork`. |
| [`Reachability`](Reachability.cs) | static class | Checks whether a host, the internet or the local Wi-Fi network is reachable and raises `ReachabilityChanged` on network changes. |
