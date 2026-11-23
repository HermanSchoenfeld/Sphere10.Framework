# Network

Host diagnostics such as DNS, ping, traceroute, WHOIS and a TCP port probe, passive DNS and TLS ClientHello decoding, a polling data source for probe sessions, SMTP mail, file download and URL shortening.

These types moved here from the core library, which no longer references the .NET networking assemblies. The `Tools.Network`, `Tools.Mail` and `Tools.Web.Downloader` facades keep their `Tools` and `Tools.Web` namespaces, so callers only add a reference to this package. The other types are in the `Sphere10.Framework.Communications` namespace. URL parsing, query strings and MIME types stay in the core [Network domain](../../Sphere10.Framework/Network/README.md), and MIME lookup is `Tools.FileSystem.GetMimeType`.

`Tools.Network` reads local addresses and MAC addresses, and runs bounded diagnostics. `ResolveHostAsync`, `PingAsync`, `TraceRouteAsync`, `ReadWhoisAsync` and `ProbeTcpPortAsync` each take an explicit timeout and run on a worker. `TryDecodeDnsMessage` and `TryDecodeTlsClientHello` project one complete captured message into the `DnsMessage` and `TlsClientHello` records, without stream reassembly or decryption, and return `false` for malformed input.

`ReadWhoisAsync` asks one server and returns a referral as data without following it. `ReadWhoisChainAsync` asks a first server and then each server the answer refers to, until an answer refers nowhere new or `maximumServers` have been asked (4 by default, 8 at most). It returns every answer in the order read, so the last is the most specific, and passes each answer to the optional `receive` callback on a worker thread as it arrives. `FindWhoisReferral` returns the server named by an answer's `refer`, `whois`, `Registrar WHOIS Server` or `ReferralServer` line, or `null` when it names none. Only plain WHOIS servers are followed, so an `rwhois://` referral is not.

`ProbeTcpPortAsync` tries one TCP connection and returns a `NetworkPortProbeResult` with the port, the elapsed time and a `NetworkPortState`: `Open` when the connection was accepted, `Closed` when the host refused it and `NoReply` when nothing answered within the timeout. It sends nothing on a connection that opens and closes it at once. Windows takes about two seconds to refuse a connection to one of its own closed ports, so a shorter timeout reports such a port as `NoReply`.

`PingAsync(address, timeout, timeToLive, packetSize, dontFragment, cancellationToken)` sends an echo request of `packetSize` data bytes (0 to 65,500) with a time to live of 1 to 255. With `dontFragment` a packet too large for a link on the way is refused rather than split, which finds the largest packet the path carries. The shorter overload sends 32 data bytes that must not be fragmented.

```csharp
// Ping with 1,472 data bytes that must not be fragmented; a refusal means the path carries less.
var echo = await Tools.Network.PingAsync(IPAddress.Parse("192.0.2.1"), TimeSpan.FromSeconds(2), 128, 1472, true);
var fits = echo.Status == "Success";
```

```csharp
using Sphere10.Framework.Communications;

// Follow WHOIS referrals from the root server; the last answer is the most specific record.
var answers = await Tools.Network.ReadWhoisChainAsync("example.com", "whois.iana.org", TimeSpan.FromSeconds(10));
var record = answers[^1].Response;

// Try one TCP port. Nothing is sent on a connection that opens.
var probe = await Tools.Network.ProbeTcpPortAsync(IPAddress.Loopback, 443, TimeSpan.FromSeconds(3));
var listening = probe.State == NetworkPortState.Open;
```

`NetworkProbeDataSource` is a [DataSource](../../Sphere10.Framework/DataSource/README.md) polling source that owns one explicitly started probe command at a time and keeps at most 1000 observations or about 2 MB, newest first. `StartAsync` admits a command and runs its first round, a positive repeat interval repeats it, `CancelAsync` drains only the active command, and `StopAsync` drains it, clears the history and stops polling. Reading or paging the source never starts a probe.

## Mail, downloads and URL shortening

`Tools.Mail` sends SMTP mail through `System.Net.Mail` with optional SSL, credentials, attachments and alternate views. `SendEmail` and `SendEmailAsync` enable SMTP TLS when requested and use normal certificate validation, without installing a process-wide callback that accepts every certificate. With the default application policy, SMTP servers must present a certificate trusted by the host and valid for the server name. The helpers preserve any certificate policy installed by the application.

`requiresSSL: true` uses STARTTLS, not implicit TLS. Supply the provider's STARTTLS port explicitly (typically `port: Tools.Mail.SMTPSubmissionPort`, 587). The omitted-port default is 465, which is normally used for implicit TLS and is incompatible with `System.Net.Mail.SmtpClient`. See [Microsoft's supported SMTP TLS modes](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient.enablessl?view=net-10.0). `MailTlsIntegrationTests` exercises the helpers against a local STARTTLS server with a test certificate trusted only inside the test process. Delivery through a particular external mail provider is not verified.

`Tools.Web.Downloader` saves a URL to a file. `UrlShortner` calls the third-party `tiny-url.info` service (not `tinyurl.com`) and the Google URL-shortener API over `HttpClient` with a caller-supplied key. [Google discontinued that API on March 30, 2019](https://developers.googleblog.com/transitioning-google-url-shortener-to-firebase-dynamic-links/), and live shortening through `tiny-url.info` is unverified, so treat `UrlShortner` as legacy.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DnsMessage`](NetworkProtocolRecords.cs) | record | Decoded DNS message header with its questions and resource records. |
| [`DnsQuestion`](NetworkProtocolRecords.cs) | record | One DNS question with its name, type and class. |
| [`DnsResourceRecord`](NetworkProtocolRecords.cs) | record | One DNS answer, authority or additional record with its TTL and rendered value. |
| [`NetworkDnsResult`](NetworkProbeObservation.cs) | record | Host name, aliases and addresses returned by `Tools.Network.ResolveHostAsync`. |
| [`NetworkPortProbeResult`](NetworkProbeObservation.cs) | record | Port, state, elapsed milliseconds and error detail of one TCP connection attempt by `Tools.Network.ProbeTcpPortAsync`. |
| [`NetworkPortState`](NetworkProbeObservation.cs) | enum | `Open`, `Closed` or `NoReply`: whether a TCP port accepted the connection, refused it or did not answer in time. |
| [`NetworkProbeDataSource`](NetworkProbeDataSource.cs) | class | Sealed polling data source that runs one explicit probe command at a time and serves its bounded observation history. |
| [`NetworkProbeObservation`](NetworkProbeObservation.cs) | record | One probe result with its time, operation, target, address, hop, status, round trip, elapsed time and detail. |
| [`NetworkWhoisResult`](NetworkProbeObservation.cs) | record | Server, query and response text of one WHOIS lookup, with a flag for truncated output. |
| [`TlsClientHello`](NetworkProtocolRecords.cs) | record | Decoded TLS ClientHello versions, server names, ALPN protocols, cipher-suite count and extension types. |
| [`Tools.Mail`](MailTool.cs) | tools | SMTP and mail port constants, email syntax validation, synchronous and asynchronous `SendEmail`, and comma-separated address parsing. |
| [`Tools.Network`](NetworkTool.cs) | tools | Local IP and MAC addresses, DNS resolution, ping with a chosen packet size and fragmentation, traceroute, WHOIS from one server or along its referrals, a TCP port probe and DNS or TLS ClientHello decoding. |
| [`Tools.Web.Downloader`](Downloader.cs) | tools | Downloads a URL to a file, optionally without certificate validation, and parses a file name from a URL. |
| [`UrlShortner`](UrlShortner.cs) | static class | Shortens a URL through the tiny-url.info or Google URL-shortener APIs, synchronously or asynchronously. |
