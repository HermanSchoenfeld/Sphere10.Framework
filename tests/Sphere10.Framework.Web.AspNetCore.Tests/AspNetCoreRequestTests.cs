// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Sphere10.Framework.Web.Processing;
using HostLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Sphere10.Framework.Web.AspNetCore.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class AspNetCoreRequestTests {

	[TestCase("https", null, "https://example.com/path?x=1")]
	[TestCase("http", null, "http://example.com/path?x=1")]
	[TestCase("https", 8443, "https://example.com:8443/path?x=1")]
	public void GetUriUsesTheSchemeDefaultPortWhenTheHostHasNone(string scheme, int? port, string expected) {
		var request = new DefaultHttpContext().Request;
		request.Scheme = scheme;
		request.Host = port.HasValue ? new HostString("example.com", port.Value) : new HostString("example.com");
		request.Path = "/path";
		request.QueryString = new QueryString("?x=1");

		Assert.That(request.GetUri().AbsoluteUri, Is.EqualTo(expected));
	}

	[Test]
	public async Task ToStringAsyncWritesTheProtocolOnceAndRestoresABufferedBody() {
		var request = new DefaultHttpContext().Request;
		request.Method = "POST";
		request.Path = "/submit";
		request.QueryString = new QueryString("?a=1");
		request.Protocol = "HTTP/1.1";
		request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload"));

		var text = await request.ToStringAsync();

		Assert.That(text, Does.StartWith("POST /submit?a=1 HTTP/1.1\n"));
		Assert.That(text, Does.EndWith("\npayload"));
		Assert.That(request.Body.Position, Is.EqualTo(0));
	}

	[Test]
	public void QueryParametersAreReportedOnlyWhenTheRequestCarriesThem() {
		var request = new DefaultHttpContext().Request;
		request.QueryString = new QueryString("?page-number=7");

		Assert.That(request.ContainsParameter(TestParameter.PageNumber), Is.True);
		Assert.That(request.GetParameter<int>(TestParameter.PageNumber), Is.EqualTo(7));
		Assert.That(request.ContainsParameter(TestParameter.Missing), Is.False);
		Assert.That(() => request.GetParameter<int>(TestParameter.Missing), Throws.InstanceOf<SoftwareException>());
	}

	[TestCase("203.0.113.9", "203.0.113.9")]
	[TestCase("not-an-address", "192.0.2.1")]
	public async Task CloudflareMiddlewareReplacesTheClientAddressOnlyWithAValidHeader(string header, string expected) {
		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
		context.Request.Headers["cf-connecting-ip"] = header;
		var nextCalled = false;
		var middleware = new CloudflareConnectingIPMiddleware(_ => {
			nextCalled = true;
			return Task.CompletedTask;
		});

		await middleware.Invoke(context);

		Assert.That(nextCalled, Is.True);
		Assert.That(context.Connection.RemoteIpAddress, Is.EqualTo(IPAddress.Parse(expected)));
	}

	[Test]
	public void LoggerAdapterForwardsExceptions() {
		var logger = new CapturingLogger { Options = LogOptions.VerboseProfile & ~LogOptions.ExceptionDetailEnabled };
		var adapter = new MicrosoftExtensionsLoggerAdapter(logger);

		adapter.Log(HostLogLevel.Error, default, "request failed", new InvalidOperationException("boom"), (state, _) => state);
		adapter.Log(HostLogLevel.Warning, default, "retrying", new InvalidOperationException("transient"), (state, _) => state);
		adapter.Log(HostLogLevel.Information, default, "started", null, (state, _) => state);

		Assert.That(logger.Messages, Has.Count.EqualTo(3));
		Assert.That(logger.Messages[0], Does.StartWith("Error:request failed").And.Contain("boom").IgnoreCase);
		Assert.That(logger.Messages[1], Does.StartWith("Warning:retrying").And.Contain("transient").IgnoreCase);
		Assert.That(logger.Messages[2], Is.EqualTo("Info:started"));
	}

	public enum TestParameter {
		[Parameter(Name = "page-number")]
		PageNumber,

		Missing
	}

	private sealed class CapturingLogger : LoggerBase {
		public List<string> Messages { get; } = new();

		protected override void Log(LogLevel logLevel, string message) => Messages.Add($"{logLevel}:{message}");
	}

}
