# Middleware

ASP.NET Core middleware that replaces the connection's remote address with the client address Cloudflare reports.

`CloudflareConnectingIPMiddleware` reads the `cf-connecting-ip` request header and, when it parses as an IP address, assigns it to `HttpContext.Connection.RemoteIpAddress`. The library does not register it. Add it with `app.UseMiddleware<CloudflareConnectingIPMiddleware>()` before any middleware that reads the client address. The middleware trusts the header without checking the sender, so enable it only for a host that accepts requests from Cloudflare alone.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CloudflareConnectingIPMiddleware`](CloudFlareConnectingIpMiddleware.cs) | class | Sets the connection's remote IP address from a valid `cf-connecting-ip` header and then calls the next middleware. |
