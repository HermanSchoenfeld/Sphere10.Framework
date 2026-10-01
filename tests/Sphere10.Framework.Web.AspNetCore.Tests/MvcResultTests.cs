// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Xml.Linq;
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
}
