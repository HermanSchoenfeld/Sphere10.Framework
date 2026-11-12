// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TabsRenderingTests {

	[Test]
	public void TabsUseExistingPageResources([Values("default", "cms")] string theme, [Values] RenderEnvironment environment) {
		var tabs = Sample();
		var result = new HtmlRenderer().Render(new DocumentBlock { Children = [tabs] }, new RenderOptions { Themes = [theme], Environment = environment });
		var html = new HtmlParser().ParseDocument(result.Html);
		Assert.That(html.QuerySelectorAll("[data-ln-tabs]"), Has.Count.EqualTo(2));
		Assert.That(html.QuerySelectorAll("[data-ln-tab]"), Has.Count.EqualTo(4));
		Assert.That(html.QuerySelectorAll("[data-ln-tab][hidden]"), Is.Empty, "Every panel is readable without JavaScript.");
		Assert.That(html.QuerySelectorAll("script[src*='/tabs/'],link[href*='/tabs/']"), Is.Empty, "Tabs must not add separate CSS or JavaScript requests.");
		Assert.That(html.QuerySelectorAll("script[src$='/local-notion/js/ln.js']"), Has.Count.EqualTo(1));
		Assert.That(html.QuerySelectorAll("link[href$='/local-notion/css/ln.css']"), Has.Count.EqualTo(1));
		Assert.That(html.QuerySelectorAll("[data-ln-tab-label] script"), Is.Empty);
		Assert.That(html.QuerySelector("[data-ln-tab-label]").TextContent, Does.Contain("<script>literal</script>"));
		Assert.That(html.QuerySelector("#deep-heading"), Is.Not.Null);
		foreach (var link in html.QuerySelectorAll("script[src$='/local-notion/js/ln.js'],link[href$='/local-notion/css/ln.css']")) {
			var url = link.GetAttribute("src") ?? link.GetAttribute("href");
			if (environment == RenderEnvironment.Offline)
				Assert.That(result.Assets.Any(asset => url.EndsWith(asset.RelativePath, StringComparison.Ordinal)), Is.True);
			else
				Assert.That(url, Does.StartWith("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/local-notion/"));
		}
		Assert.That(result.Assets.Any(asset => asset.RelativePath.Contains("/tabs/")), Is.False);
		if (environment == RenderEnvironment.Offline) {
			var script = result.Assets.Single(asset => asset.RelativePath.EndsWith("/local-notion/js/ln.js", StringComparison.Ordinal));
			var stylesheet = result.Assets.Single(asset => asset.RelativePath.EndsWith("/local-notion/css/ln.css", StringComparison.Ordinal));
			Assert.That(System.Text.Encoding.UTF8.GetString(script.Content.Span), Does.Contain("window.localNotionTabs"));
			Assert.That(System.Text.Encoding.UTF8.GetString(stylesheet.Content.Span), Does.Contain(".ln-tabs-enhanced"));
		}
	}

	[Test]
	public void TabSnippetsUseTheirHostPageResources() {
		var result = new HtmlRenderer().RenderSnippet(Sample());
		var html = new HtmlParser().ParseDocument(result.Html);
		Assert.That(html.QuerySelectorAll("script,link"), Is.Empty);
		Assert.That(html.QuerySelectorAll("[data-ln-tab]"), Has.Count.EqualTo(4));
	}

	[Test]
	public void TextIncludesInactiveAndNestedPanelContent() {
		var text = new TextRenderer().RenderSnippet(Sample());
		Assert.That(text, Does.Contain("First body").And.Contain("Second body").And.Contain("Nested body").And.Contain("Deep heading"));
	}

	[Test]
	public void OrdinaryPagesDoNotReferenceTabRuntime() {
		var result = new HtmlRenderer().RenderSnippet(new ParagraphBlock { Text = [new TextInline { Text = "Ordinary" }] });
		Assert.That(result.Html, Does.Not.Contain("/tabs/tabs.js").And.Not.Contain("/tabs/tabs.css"));
	}

	[Test]
	public void ExportBrowserFixture([Values] RenderEnvironment environment) {
		var folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "tabs-preview-" + environment.ToString().ToLowerInvariant());
		Directory.CreateDirectory(folder);
		var result = new HtmlRenderer().Render(new DocumentBlock { Title = "Native tab verification", Children = [Sample()] }, new RenderOptions { Environment = environment });
		File.WriteAllText(Path.Combine(folder, "index.html"), result.Html);
		foreach (var asset in result.Assets) {
			var path = Path.Combine(folder, asset.RelativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllBytes(path, asset.Content.ToArray());
		}
		Assert.That(File.Exists(Path.Combine(folder, "index.html")), Is.True);
		TestContext.AddTestAttachment(Path.Combine(folder, "index.html"));
	}

	private static TabsBlock Sample() => new() {
		Children = [
			new TabBlock { Text = [new TextInline { Text = "Overview <script>literal</script>" }], Icon = new Icon { Emoji = "📋" }, Children = [new ParagraphBlock { Text = [new TextInline { Text = "First body" }] }] },
			new TabBlock { Text = [new TextInline { Text = "Details" }], Children = [
				new ParagraphBlock { Text = [new TextInline { Text = "Second body" }] },
				new TabsBlock { Children = [
					new TabBlock { Text = [new TextInline { Text = "Nested one" }], Children = [new ParagraphBlock { Text = [new TextInline { Text = "Nested body" }] }] },
					new TabBlock { Text = [new TextInline { Text = "Nested two" }], Children = [new HeadingBlock { Metadata = new Metadata { Anchor = "deep-heading" }, Level = 2, Text = [new TextInline { Text = "Deep heading" }] }] }
				] }
			] }
		]
	};
}
