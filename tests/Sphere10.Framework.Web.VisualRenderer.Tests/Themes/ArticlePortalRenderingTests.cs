// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ArticlePortalRenderingTests {

	[TestCase(RenderEnvironment.Online)]
	[TestCase(RenderEnvironment.Offline)]
	public void ArticleThemePreservesInheritedSiteFrameAndPageHeader(RenderEnvironment environment) {
		var catalogue = new ThemeCatalog();
		var baseline = catalogue.CreateSession(["cms"], environment, RenderMode.ReadOnly);
		var articles = catalogue.CreateSession(["cms_articles"], environment, RenderMode.ReadOnly);
		foreach (var template in new[] { "page", "page_content", "page_title" })
			Assert.That(articles.GetTemplate(template), Is.EqualTo(baseline.GetTemplate(template)), "The article layout must not replace " + template + ".");
	}

	[Test]
	public void TopicHierarchyUsesOneNativeDisclosureAndKeepsNestedActiveDestination() {
		const string collection = "/fixed/fixed2/field-notes";
		const string activeCategory = collection + "/devices/installation";
		var body = Portal(new GroupBlock {
			Children = [Category("Devices", collection + "/devices", 1,
				Category("Installation", activeCategory, 2, new GroupBlock(), active: true))]
		});
		var html = new HtmlRenderer().RenderSnippet(body, new RenderOptions { Themes = ["cms_articles"] }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var portal = parsed.QuerySelector("section.ln-articles-portal");
		Assert.That(portal, Is.Not.Null);
		Assert.That(portal.QuerySelectorAll("aside.ln-articles-sidebar"), Has.Length.EqualTo(1));
		Assert.That(portal.QuerySelectorAll("details.ln-articles-topics"), Has.Length.EqualTo(1));
		var disclosure = portal.QuerySelector("details.ln-articles-topics");
		Assert.That(disclosure.HasAttribute("open"), Is.False, "The source is a closed native disclosure for phone users before progressive enhancement.");
		Assert.That(disclosure.QuerySelector("summary").TextContent.Trim(), Is.EqualTo("Browse topics"));
		Assert.That(disclosure.QuerySelectorAll("nav.ln-articles-category-nav > ul.ln-articles-categories"), Has.Length.EqualTo(1));
		var tree = disclosure.QuerySelector("ul.ln-articles-categories");
		Assert.That(tree.QuerySelector("li ul a[aria-current='page']").GetAttribute("href"), Is.EqualTo(activeCategory));
		Assert.That(tree.QuerySelectorAll("a").Select(link => link.GetAttribute("href")),
			Is.EqualTo(new[] { collection, collection + "/devices", activeCategory }));
		Assert.That(portal.QuerySelectorAll(".ln-articles-content"), Has.Length.EqualTo(1));
		Assert.That(portal.QuerySelector(".ln-articles-content main"), Is.Null, "The article section lives inside the existing page main element.");
		Assert.That(portal.QuerySelector("h1"), Is.Null, "The existing page banner retains ownership of the collection heading.");
	}

	[TestCase("articles_summary", true)]
	[TestCase("articles_summary_alt", false)]
	public void SummaryVariantsAlternateOneFeatureAndKeepIndependentArticleLinks(string template, bool featureFirst) {
		const string articleUrl = "/fixed/fixed2/field-notes/devices/installing-sensors";
		var summary = Widget(template, new Dictionary<string, string> {
			["title"] = "Installing sensors", ["url"] = articleUrl,
			["summary"] = "Configure the sensor and verify its first reading.",
			["created_on"] = "2026-11-04", ["created_on_formatted"] = "4 November 2026"
		}, new Dictionary<string, VisualNode> {
			["feature"] = Widget("articles_summary_feature", new Dictionary<string, string> { ["url"] = "/images/sensor-cover.png" },
				new Dictionary<string, VisualNode> { ["thumbnail"] = new RawHtmlBlock { Html = "<img data-fixture-cover src='/images/sensor.png' alt='A sensor'>" } })
		});
		var result = new HtmlRenderer().RenderSnippet(summary, new RenderOptions { Themes = ["cms_articles"] });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var article = parsed.QuerySelector("article.ln-article-summary.article-summary");
		Assert.That(article, Is.Not.Null);
		Assert.That(article.QuerySelectorAll("[data-fixture-cover]"), Has.Length.EqualTo(1), "Responsive layout reuses the same feature instead of duplicating its DOM.");
		Assert.That(article.QuerySelector(".ln-article-feature [data-fixture-cover]"), Is.Not.Null);
		Assert.That(article.QuerySelector(".ln-article-feature").GetAttribute("data-feature-url"), Is.EqualTo("/images/sensor-cover.png"));
		var link = article.QuerySelector("a.ln-article-summary-link");
		Assert.That(article.QuerySelectorAll("a"), Has.Length.EqualTo(1), "One native link covers the summary without nested or competing destinations.");
		Assert.That(link.ParentElement, Is.SameAs(article));
		Assert.That(link.GetAttribute("href"), Is.EqualTo(articleUrl));
		Assert.That(link.GetAttribute("aria-label"), Is.EqualTo("Installing sensors"));
		var header = article.QuerySelector(".ln-article-header");
		var contentRow = article.QuerySelector(".ln-article-content-row");
		Assert.That(header, Is.Not.Null);
		Assert.That(contentRow, Is.Not.Null);
		Assert.That(header.ParentElement, Is.SameAs(link));
		Assert.That(contentRow.ParentElement, Is.SameAs(link));
		Assert.That(Array.IndexOf(link.Children.ToArray(), header), Is.LessThan(Array.IndexOf(link.Children.ToArray(), contentRow)),
			"The title and date span the complete article before the alternating feature/summary row.");
		Assert.That(header.QuerySelector("h2").TextContent, Is.EqualTo("Installing sensors"));
		Assert.That(header.QuerySelector("time").GetAttribute("datetime"), Is.EqualTo("2026-11-04"));
		Assert.That(header.QuerySelector(".ln-article-created").TextContent.Trim(), Is.EqualTo("4 November 2026"), "The native date remains visible without a Created prefix.");
		Assert.That(contentRow.QuerySelector("h2, time"), Is.Null);
		Assert.That(contentRow.QuerySelector(".ln-article-read-link").TagName, Is.EqualTo("SPAN"));
		Assert.That(article.TextContent, Does.Contain("Configure the sensor and verify its first reading."));
		Assert.That(article.ClassList.Contains("ln-article-summary-alt"), Is.EqualTo(!featureFirst));
		Assert.That(contentRow.QuerySelector(".ln-article-feature").ParentElement, Is.SameAs(contentRow));
		Assert.That(contentRow.QuerySelector(".ln-article-body").ParentElement, Is.SameAs(contentRow));
		var children = contentRow.Children.ToArray();
		Assert.That(Array.IndexOf(children, contentRow.QuerySelector(".ln-article-feature")) < Array.IndexOf(children, contentRow.QuerySelector(".ln-article-body")),
			Is.EqualTo(featureFirst), "The existing alternate-summary variant places the feature on the opposite side without duplicating it.");
		var stylesheet = Encoding.UTF8.GetString(result.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/css/articles.css", StringComparison.Ordinal)).Content.Span);
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-article-summary\s*\{[^{}]*\bdisplay\s*:\s*block\s*;"));
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-article-summary\s*\{[^{}]*\bpadding\s*:\s*0\s*;"), "Legacy article padding must not leave unlinked space outside the summary link.");
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-article-summary-link\s*\{[^{}]*\bpadding-block\s*:"), "The clickable wrapper owns the summary's blank vertical padding.");
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-article-content-row\s*\{[^{}]*\bdisplay\s*:\s*flex\s*;"));
		Assert.That(stylesheet, Does.Not.Match(@"\bflex-direction\s*:\s*(?:row-reverse|column(?:-reverse)?)\b"), "Desktop and phone keep the alternating horizontal DOM order.");
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-article-summary-link:hover\s*,\s*\.ln-articles-portal \.ln-article-summary-link:focus-visible\s*\{[^{}]*\bbackground\s*:"));
	}

	[Test]
	public void ArticlePresentationStylesStayInsideTheArticleSection() {
		var result = new HtmlRenderer().RenderSnippet(Portal(new GroupBlock()), new RenderOptions { Themes = ["cms_articles"] });
		var stylesheet = Encoding.UTF8.GetString(result.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/css/articles.css", StringComparison.Ordinal)).Content.Span);
		var selectors = Regex.Matches(Regex.Replace(stylesheet, @"/\*.*?\*/", "", RegexOptions.Singleline), @"(?<selector>[^{}]+)\{")
			.Select(match => match.Groups["selector"].Value.Trim()).Where(selector => !selector.StartsWith('@')).ToArray();
		Assert.That(selectors, Is.Not.Empty);
		foreach (var selector in selectors)
			Assert.That(selector.Split(',').Select(part => part.Trim()), Is.All.StartsWith(".ln-articles-portal"), "Article CSS must not select the global menu, banner or unrelated content.");
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-articles-category-link\s*\{[^{}]*\bfont-weight\s*:\s*400\s*;"));
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-portal \.ln-articles-category-link\[aria-current=""page""\]\s*\{[^{}]*\bfont-weight\s*:\s*600\s*;"));
		var ancestorRule = Regex.Match(stylesheet,
			Regex.Escape(".ln-articles-portal .ln-articles-category:has(.ln-articles-category-link[aria-current=\"page\"]) > .ln-articles-category-link") + @"\s*\{(?<declarations>[^{}]+)\}");
		Assert.That(ancestorRule.Success, Is.True);
		Assert.That(ancestorRule.Groups["declarations"].Value, Does.Not.Contain("font-weight"), "Ancestors may show the active path's color without inheriting the exact active item's bold emphasis.");
	}

	[TestCase("default")]
	[TestCase("blue-background")]
	public void ArticleStylesKeepLiteralDeclarationBlocksWhenPageColorTokenIsSet(string color) {
		var document = new DocumentBlock {
			Themes = ["cms_articles"],
			Tokens = new Dictionary<string, string> { ["color"] = color },
			Children = [Portal(new GroupBlock())]
		};
		var result = new HtmlRenderer().RenderSnippet(document);
		var styles = Encoding.UTF8.GetString(result.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/css/articles.css", StringComparison.Ordinal)).Content.Span);
		var activeRule = Regex.Match(styles,
			Regex.Escape(".ln-articles-portal .ln-articles-category-link[aria-current=\"page\"]") + @"\s*\{(?<declarations>[^{}]+)\}");
		var titleRule = Regex.Match(styles, Regex.Escape(".ln-articles-portal .ln-article-title") + @"\s*\{(?<declarations>[^{}]+)\}");
		Assert.That(activeRule.Success, Is.True, "The theme color token must not replace the active-category CSS declaration block.");
		Assert.That(activeRule.Groups["declarations"].Value, Does.Match(@"\bcolor\s*:\s*var\(--bs-primary,\s*inherit\)\s*;"));
		Assert.That(activeRule.Groups["declarations"].Value, Does.Match(@"\bbackground\s*:\s*rgba\(var\(--bs-primary-rgb,"));
		Assert.That(titleRule.Success, Is.True, "The theme color token must not replace the article-title CSS declaration block.");
		Assert.That(titleRule.Groups["declarations"].Value, Does.Match(@"\bcolor\s*:\s*var\(--bs-heading-color,"));
		Assert.That(titleRule.Groups["declarations"].Value, Does.Match(@"\btext-decoration\s*:\s*none\s*;"));
		Assert.That(styles, Does.Not.Contain(".ln-articles-category-link[aria-current=\"page\"] " + color));
		Assert.That(styles, Does.Not.Contain(".ln-article-title " + color));
		Assert.That(styles, Does.Not.Contain("{{").And.Not.Contain("}}"), "Ordinary resource files retain literal CSS braces without template escaping.");
	}

	[Test]
	public void OnlinePortalUsesArticleResourcesFromTheirOwningThemeCdn() {
		const string cdn = "https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/cms_articles/";
		var result = new HtmlRenderer().RenderSnippet(Portal(new GroupBlock()), new RenderOptions {
			Themes = ["cms_articles"], Environment = RenderEnvironment.Online, AssetBaseUrl = "../render-assets"
		});
		var portal = new HtmlParser().ParseDocument(result.Html).QuerySelector(".ln-articles-portal");
		Assert.That(portal.QuerySelectorAll("link[rel='stylesheet']").Select(link => link.GetAttribute("href")),
			Is.EqualTo(new[] { cdn + "resources/css/articles.css" }));
		Assert.That(portal.QuerySelectorAll("script[src]").Select(script => script.GetAttribute("src")),
			Is.EqualTo(new[] { cdn + "resources/js/articles.js" }));
		Assert.That(portal.QuerySelector("script[src]").HasAttribute("defer"), Is.True);
		Assert.That(portal.QuerySelectorAll("style, script:not([src])"), Is.Empty, "Article CSS and initialization are published resources, not page-local includes.");
		Assert.That(result.Assets, Is.Empty, "Online dependencies use their existing owning CDN locations.");
		Assert.That(result.Html, Does.Not.Contain("{theme://"));
		var catalogue = new ThemeCatalog();
		var baseline = catalogue.CreateSession(["cms"], RenderEnvironment.Online, RenderMode.ReadOnly);
		var articles = catalogue.CreateSession(["cms_articles"], RenderEnvironment.Online, RenderMode.ReadOnly);
		foreach (var resource in baseline.Tokens.Where(token => token.Key.StartsWith("theme://resources/", StringComparison.Ordinal)))
			Assert.That(articles.Tokens[resource.Key], Is.EqualTo(resource.Value), "Inherited resource CDN ownership changed: " + resource.Key);
	}

	[Test]
	public void OfflinePortalReturnsCompleteArticleResourceBytesAtConfiguredAssetBase() {
		var result = new HtmlRenderer().RenderSnippet(Portal(new GroupBlock()), new RenderOptions {
			Themes = ["cms_articles"], Environment = RenderEnvironment.Offline, AssetBaseUrl = "../render-assets"
		});
		var portal = new HtmlParser().ParseDocument(result.Html).QuerySelector(".ln-articles-portal");
		var stylesheet = result.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/css/articles.css", StringComparison.Ordinal));
		var initializer = result.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/js/articles.js", StringComparison.Ordinal));
		Assert.That(portal.QuerySelector("link[rel='stylesheet']").GetAttribute("href"), Is.EqualTo("../render-assets/" + stylesheet.RelativePath));
		Assert.That(portal.QuerySelector("script[src]").GetAttribute("src"), Is.EqualTo("../render-assets/" + initializer.RelativePath));
		Assert.That(stylesheet.ContentType, Is.EqualTo("text/css"));
		Assert.That(initializer.ContentType, Is.AnyOf("text/javascript", "application/javascript"));
		Assert.That(Encoding.UTF8.GetString(stylesheet.Content.Span), Does.Contain(".ln-articles-portal").And.Not.Contain("<style>"));
		Assert.That(Encoding.UTF8.GetString(initializer.Content.Span), Does.Contain("observeHeader").And.Not.Contain("<script>"));
		Assert.That(portal.QuerySelectorAll("style, script:not([src])"), Is.Empty);
		Assert.That(result.Html, Does.Not.Contain("https://cdn.jsdelivr.net/"));
	}

	[Test]
	public void ArticleResourcesLeaveInheritedCmsAssetBytesUnchanged() {
		var catalogue = new ThemeCatalog();
		var baseline = catalogue.CreateSession(["cms"], RenderEnvironment.Offline, RenderMode.ReadOnly).Assets.ToDictionary(ResourcePath);
		var articles = catalogue.CreateSession(["cms_articles"], RenderEnvironment.Offline, RenderMode.ReadOnly).Assets.ToDictionary(ResourcePath);
		Assert.That(articles.Keys.Except(baseline.Keys), Is.EquivalentTo(new[] { "resources/css/articles.css", "resources/js/articles.js" }));
		foreach (var entry in baseline) {
			Assert.That(articles.ContainsKey(entry.Key), Is.True);
			Assert.That(articles[entry.Key].ContentType, Is.EqualTo(entry.Value.ContentType));
			Assert.That(articles[entry.Key].Content.ToArray(), Is.EqualTo(entry.Value.Content.ToArray()), "Inherited asset changed: " + entry.Key);
		}
	}

	private static TemplateBlock Portal(VisualNode categories) {
		return Widget("articles", slots: new Dictionary<string, VisualNode> {
			["category_root"] = Category("Field notes", "/fixed/fixed2/field-notes", 1, new GroupBlock()),
			["categories"] = categories,
			["summaries"] = new ParagraphBlock { Text = [new TextInline { Text = "Current page entries" }] }
		});
	}

	private static TemplateBlock Category(string title, string url, int indent, VisualNode children, bool active = false) {
		return Widget(active ? "articles_category_active" : "articles_category", new Dictionary<string, string> {
			["title"] = title, ["url"] = url, ["indent_level"] = indent.ToString()
		}, new Dictionary<string, VisualNode> { ["children"] = children });
	}

	private static TemplateBlock Widget(string template, Dictionary<string, string> tokens = null, Dictionary<string, VisualNode> slots = null) {
		return new TemplateBlock {
			Template = template,
			Tokens = (tokens ?? new()).ToDictionary(token => token.Key, token => WebUtility.HtmlEncode(token.Value)),
			Slots = slots ?? new()
		};
	}

	private static string ResourcePath(RenderAsset asset) {
		return asset.RelativePath[(asset.RelativePath.IndexOf("/resources/", StringComparison.Ordinal) + 1)..];
	}
}
