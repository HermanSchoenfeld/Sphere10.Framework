using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;


namespace Sphere10.Framework.Web.AspNetCore;

public class CloudflareConnectingIPMiddleware {
	private readonly RequestDelegate _next;

	public CloudflareConnectingIPMiddleware(RequestDelegate next) => _next = next;

	public async Task Invoke(HttpContext context) {
		if (context.Request.Headers.TryGetValue("cf-connecting-ip", out var values)) {
			// A value that is not an IP address is ignored, so the connection keeps its own address.
			if (IPAddress.TryParse(values.FirstOrDefault(), out var ipAddress)) {
				context.Connection.RemoteIpAddress = ipAddress;
			}
		}

		await _next(context);
	}
}

