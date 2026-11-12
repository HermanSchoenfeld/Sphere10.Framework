# Network

URL parsing and building, MIME types, SMTP mail, file download, host diagnostics such as DNS, ping, traceroute and WHOIS, passive DNS and TLS ClientHello decoding, and a polling data source for probe sessions.

`Tools.Url` parses, combines and encodes URLs, builds query strings, makes URL slugs and DOM-safe identifiers, and recognizes YouTube, Rumble, Vimeo and BitChute links as a `VideoSharingPlatform`. `QueryStringBuilder` assembles query strings from typed values. `Tools.Network` maps file extensions to MIME types through `MimeHelper`, reads local addresses and MAC addresses, and runs bounded diagnostics. `ResolveHostAsync`, `PingAsync`, `TraceRouteAsync` and `ReadWhoisAsync` each take an explicit timeout and run on a worker. A WHOIS referral is returned as data and never followed. `TryDecodeDnsMessage` and `TryDecodeTlsClientHello` project one complete captured message into the `DnsMessage` and `TlsClientHello` records, without stream reassembly or decryption, and return `false` for malformed input.

`NetworkProbeDataSource` is a [DataSource](../DataSource/README.md) polling source that owns one explicitly started probe command at a time and keeps at most 1000 observations or about 2 MB, newest first. `StartAsync` admits a command and runs its first round, a positive repeat interval repeats it, `CancelAsync` drains only the active command, and `StopAsync` drains it, clears the history and stops polling. Reading or paging the source never starts a probe.

`Tools.Mail` sends SMTP mail through `System.Net.Mail` with optional SSL, credentials, attachments and alternate views. `Tools.Web.Downloader` saves a URL to a file, and `UrlShortner` calls the tiny-url.info and Google URL-shortener web APIs with a caller-supplied key.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DnsMessage`](NetworkProtocolRecords.cs) | record | Decoded DNS message header with its questions and resource records. |
| [`DnsQuestion`](NetworkProtocolRecords.cs) | record | One DNS question with its name, type and class. |
| [`DnsResourceRecord`](NetworkProtocolRecords.cs) | record | One DNS answer, authority or additional record with its TTL and rendered value. |
| [`MimeHelper`](MimeHelper.cs) | static class | Maps a file extension, with or without a leading dot, to a MIME type. |
| [`NetworkDnsResult`](NetworkProbeObservation.cs) | record | Host name, aliases and addresses returned by `Tools.Network.ResolveHostAsync`. |
| [`NetworkProbeDataSource`](NetworkProbeDataSource.cs) | class | Sealed polling data source that runs one explicit probe command at a time and serves its bounded observation history. |
| [`NetworkProbeObservation`](NetworkProbeObservation.cs) | record | One probe result with its time, operation, target, address, hop, status, round trip, elapsed time and detail. |
| [`NetworkWhoisResult`](NetworkProbeObservation.cs) | record | Server, query and response text of one WHOIS lookup, with a flag for truncated output. |
| [`QueryStringBuilder`](QueryStringBuilder.cs) | class | Collects name and value pairs and renders them as a query string, optionally appended to a base URL. |
| [`TlsClientHello`](NetworkProtocolRecords.cs) | record | Decoded TLS ClientHello versions, server names, ALPN protocols, cipher-suite count and extension types. |
| [`Tools.Mail`](MailTool.cs) | tools | SMTP and mail port constants, email syntax validation, synchronous and asynchronous `SendEmail`, and comma-separated address parsing. |
| [`Tools.Network`](NetworkTool.cs) | tools | MIME lookup, local IP and MAC addresses, DNS resolution, ping, traceroute, WHOIS and DNS or TLS ClientHello decoding. |
| [`Tools.Url`](UrlTool.cs) | tools | URL parsing, combining, encoding, query strings, slugs, breadcrumbs, DOM identifiers and video-sharing link detection. |
| [`Tools.Web.Downloader`](Downloader.cs) | tools | Downloads a URL to a file, optionally without certificate validation, and parses a file name from a URL. |
| [`UrlShortner`](UrlShortner.cs) | static class | Shortens a URL through the tiny-url.info or Google URL-shortener APIs, synchronously or asynchronously. |
| [`VideoSharingPlatform`](VideoSharingPlatform.cs) | enum | YouTube, Rumble, BitChute or Vimeo. |
