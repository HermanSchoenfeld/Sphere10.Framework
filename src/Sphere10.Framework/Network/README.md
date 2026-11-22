# Network

URL parsing and building, MIME types, SMTP mail, file download, host diagnostics such as DNS, ping, traceroute, WHOIS and a TCP port probe, passive DNS and TLS ClientHello decoding, and a polling data source for probe sessions.

`Tools.Url` parses, combines and encodes URLs, builds query strings, makes URL slugs and DOM-safe identifiers, and recognizes YouTube, Rumble, Vimeo and BitChute links as a `VideoSharingPlatform`. `QueryStringBuilder` assembles query strings from typed values. `Tools.Network` maps file extensions to MIME types through `MimeHelper`, reads local addresses and MAC addresses, and runs bounded diagnostics. `ResolveHostAsync`, `PingAsync`, `TraceRouteAsync`, `ReadWhoisAsync` and `ProbeTcpPortAsync` each take an explicit timeout and run on a worker. `TryDecodeDnsMessage` and `TryDecodeTlsClientHello` project one complete captured message into the `DnsMessage` and `TlsClientHello` records, without stream reassembly or decryption, and return `false` for malformed input.

`ReadWhoisAsync` asks one server and returns a referral as data without following it. `ReadWhoisChainAsync` asks a first server and then each server the answer refers to, until an answer refers nowhere new or `maximumServers` have been asked (4 by default, 8 at most). It returns every answer in the order read, so the last is the most specific, and passes each answer to the optional `receive` callback on a worker thread as it arrives. `FindWhoisReferral` returns the server named by an answer's `refer`, `whois`, `Registrar WHOIS Server` or `ReferralServer` line, or `null` when it names none. Only plain WHOIS servers are followed, so an `rwhois://` referral is not.

`ProbeTcpPortAsync` tries one TCP connection and returns a `NetworkPortProbeResult` with the port, the elapsed time and a `NetworkPortState`: `Open` when the connection was accepted, `Closed` when the host refused it and `NoReply` when nothing answered within the timeout. It sends nothing on a connection that opens and closes it at once. Windows takes about two seconds to refuse a connection to one of its own closed ports, so a shorter timeout reports such a port as `NoReply`.

`PingAsync(address, timeout, timeToLive, packetSize, dontFragment, cancellationToken)` sends an echo request of `packetSize` data bytes (0 to 65,500) with a time to live of 1 to 255. With `dontFragment` a packet too large for a link on the way is refused rather than split, which finds the largest packet the path carries. The shorter overload sends 32 data bytes that must not be fragmented.

```csharp
// Ping with 1,472 data bytes that must not be fragmented; a refusal means the path carries less.
var echo = await Tools.Network.PingAsync(IPAddress.Parse("192.0.2.1"), TimeSpan.FromSeconds(2), 128, 1472, true);
var fits = echo.Status == "Success";
```

```csharp
// Follow WHOIS referrals from the root server; the last answer is the most specific record.
var answers = await Tools.Network.ReadWhoisChainAsync("example.com", "whois.iana.org", TimeSpan.FromSeconds(10));
var record = answers[^1].Response;

// Try one TCP port. Nothing is sent on a connection that opens.
var probe = await Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, 443, TimeSpan.FromSeconds(3));
var listening = probe.State == NetworkPortState.Open;
```

`NetworkProbeDataSource` is a [DataSource](../DataSource/README.md) polling source that owns one explicitly started probe command at a time and keeps at most 1000 observations or about 2 MB, newest first. `StartAsync` admits a command and runs its first round, a positive repeat interval repeats it, `CancelAsync` drains only the active command, and `StopAsync` drains it, clears the history and stops polling. Reading or paging the source never starts a probe.

`Tools.Mail` sends SMTP mail through `System.Net.Mail` with optional SSL, credentials, attachments and alternate views. `Tools.Web.Downloader` saves a URL to a file, and `UrlShortner` calls the tiny-url.info and Google URL-shortener web APIs with a caller-supplied key.

## Contents

`Tools.Url.ToUrlSlug(value, allowUnicode: true)` retains Unicode letters, decimal digits and combining marks in lowercase NFC-normalized slugs, including supplementary-plane letters. It still removes reserved syntax and control characters. The result is a readable path segment, not a percent-encoded request URI. The single-argument overload retains its existing ASCII/diacritic-removal behavior; `ToHtml4DOMObjectID` also retains that character policy. Both slug modes spell the language name `C#` as `c-sharp` before filtering punctuation (for example, `Learn C#` becomes `learn-c-sharp`). Unrelated hashes are still removed.

| Type | Kind | Purpose |
|---|---|---|
| [`DnsMessage`](NetworkProtocolRecords.cs) | record | Decoded DNS message header with its questions and resource records. |
| [`DnsQuestion`](NetworkProtocolRecords.cs) | record | One DNS question with its name, type and class. |
| [`DnsResourceRecord`](NetworkProtocolRecords.cs) | record | One DNS answer, authority or additional record with its TTL and rendered value. |
| [`MimeHelper`](MimeHelper.cs) | static class | Maps a file extension, with or without a leading dot, to a MIME type. |
| [`NetworkDnsResult`](NetworkProbeObservation.cs) | record | Host name, aliases and addresses returned by `Tools.Network.ResolveHostAsync`. |
| [`NetworkPortProbeResult`](NetworkProbeObservation.cs) | record | Port, state, elapsed milliseconds and error detail of one TCP connection attempt by `Tools.Network.ProbeTcpPortAsync`. |
| [`NetworkPortState`](NetworkProbeObservation.cs) | enum | `Open`, `Closed` or `NoReply`: whether a TCP port accepted the connection, refused it or did not answer in time. |
| [`NetworkProbeDataSource`](NetworkProbeDataSource.cs) | class | Sealed polling data source that runs one explicit probe command at a time and serves its bounded observation history. |
| [`NetworkProbeObservation`](NetworkProbeObservation.cs) | record | One probe result with its time, operation, target, address, hop, status, round trip, elapsed time and detail. |
| [`NetworkWhoisResult`](NetworkProbeObservation.cs) | record | Server, query and response text of one WHOIS lookup, with a flag for truncated output. |
| [`QueryStringBuilder`](QueryStringBuilder.cs) | class | Collects name and value pairs and renders them as a query string, optionally appended to a base URL. |
| [`TlsClientHello`](NetworkProtocolRecords.cs) | record | Decoded TLS ClientHello versions, server names, ALPN protocols, cipher-suite count and extension types. |
| [`Tools.Mail`](MailTool.cs) | tools | SMTP and mail port constants, email syntax validation, synchronous and asynchronous `SendEmail`, and comma-separated address parsing. |
| [`Tools.Network`](NetworkTool.cs) | tools | MIME lookup, local IP and MAC addresses, DNS resolution, ping with a chosen packet size and fragmentation, traceroute, WHOIS from one server or along its referrals, a TCP port probe and DNS or TLS ClientHello decoding. |
| [`Tools.Url`](UrlTool.cs) | tools | URL parsing, combining, encoding, query strings, slugs, breadcrumbs, DOM identifiers and video-sharing link detection. |
| [`Tools.Web.Downloader`](Downloader.cs) | tools | Downloads a URL to a file, optionally without certificate validation, and parses a file name from a URL. |
| [`UrlShortner`](UrlShortner.cs) | static class | Shortens a URL through the tiny-url.info or Google URL-shortener APIs, synchronously or asynchronously. |
| [`VideoSharingPlatform`](VideoSharingPlatform.cs) | enum | YouTube, Rumble, BitChute or Vimeo. |
