// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.MVC;

namespace Sphere10.Framework.Web.AspNetCore.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MvcResultTests {

	[TestCase(200)]
	[TestCase(404)]
	[TestCase(422)]
	public void EmptyXmlResultPreservesStatusCode(int statusCode) {
		var result = new XmlResult(statusCode);

		Assert.That(result.StatusCode, Is.EqualTo(statusCode));
		Assert.That(result.ContentType, Is.EqualTo("application/xml"));
		Assert.That(result.Content, Is.Empty);
	}

	[Test]
	public void BootstrapFormsScriptRemainsEmbeddedInMvcAssembly() {
		using var script = typeof(FormModelBase).Assembly.GetManifestResourceStream("Sphere10.Framework.Web.AspNetCore.MVC.Forms.hydrogen-bootstrap-forms-1.0.0.js");

		Assert.That(script, Is.Not.Null);
		Assert.That(script.Length, Is.GreaterThan(0));
	}

	[Test]
	public void SitemapCanBeSerializedThroughMvcResult() {
		var sitemap = new SitemapXml();
		sitemap.Add("https://example.com/", new DateTime(2026, 10, 1), SitemapXml.Frequency.Daily, 0.8);

		var result = new XmlResult(sitemap, 201);
		var document = XDocument.Parse(result.Content);
		XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
		var url = document.Root.Element(sitemapNamespace + "url");

		Assert.That(result.StatusCode, Is.EqualTo(201));
		Assert.That(document.Root.Name, Is.EqualTo(sitemapNamespace + "urlset"));
		Assert.That(url.Element(sitemapNamespace + "loc").Value, Is.EqualTo("https://example.com/"));
		Assert.That(url.Element(sitemapNamespace + "changefreq").Value, Is.EqualTo("daily"));
	}

	[Test]
	public void EnumSelectListPreservesSelectionAndSortOrder() {
		var items = Tools.Web.Mvc.ToSelectList<DayOfWeek>(DayOfWeek.Wednesday, SortDirection.Ascending).ToArray();

		Assert.That(items.Select(item => item.Text), Is.Ordered.Using<string>(StringComparer.InvariantCultureIgnoreCase));
		Assert.That(items.Where(item => item.Selected).Select(item => item.Value), Is.EqualTo(new[] { "Wednesday" }));
	}

	[Test]
	[SupportedOSPlatform("windows")]
	public async Task ImageResultWritesAsynchronously() {
		Assume.That(OperatingSystem.IsWindows(), Is.True);
		var httpContext = new DefaultHttpContext();
		var body = new AsynchronousOnlyStream();
		httpContext.Response.Body = body;
		var result = new ImageResult(new Bitmap(2, 2), ImageFormat.Png);

		await result.ExecuteResultAsync(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));

		var bytes = body.ToArray();
		Assert.That(httpContext.Response.ContentType, Is.EqualTo("image/png"));
		Assert.That(httpContext.Response.ContentLength, Is.EqualTo(bytes.Length));
		Assert.That(bytes.Take(4), Is.EqualTo(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }));
	}

	[Test]
	public void FilterFactoryRejectsAnUnregisteredFilter() {
		using var services = new ServiceCollection().BuildServiceProvider();

		Assert.That(() => new TestFilterFactoryAttribute().CreateInstance(services), Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public async Task CustomRoutesAreMappedAsConventionalControllerRoutes() {
		var builder = WebApplication.CreateBuilder();
		builder.Services.AddControllers().AddApplicationPart(typeof(CustomRouteSampleController).Assembly);
		await using var app = builder.Build();

		CustomRouteAttribute.RegisterCustomRoutesFromAssembly(app, typeof(CustomRouteSampleController).Assembly);

		var patterns = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(source => source.Endpoints)
			.OfType<RouteEndpoint>()
			.Select(endpoint => endpoint.RoutePattern.RawText)
			.ToArray();
		Assert.That(patterns, Does.Contain("custom/sample"));
	}

	private sealed class TestFilter : IActionFilter {
		public void OnActionExecuting(ActionExecutingContext context) {
		}

		public void OnActionExecuted(ActionExecutedContext context) {
		}
	}

	private sealed class TestFilterFactoryAttribute : FilterFactoryBase<TestFilter> {
	}

	private sealed class AsynchronousOnlyStream : Stream {
		private readonly MemoryStream _written = new();

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();

		public override long Position {
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public byte[] ToArray() => _written.ToArray();

		public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous operations are disallowed.");

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			_written.Write(buffer, offset, count);
			return Task.CompletedTask;
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
			_written.Write(buffer.Span);
			return ValueTask.CompletedTask;
		}

		public override void Flush() {
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();
	}
}

[CustomRoute("custom/sample", DefaultAction = "Index")]
public class CustomRouteSampleController : Controller {
	public IActionResult Index() => Content("sample");
}
