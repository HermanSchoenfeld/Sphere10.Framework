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
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ArticlePaginationRenderingTests {

	[Test]
	public void PortalRendersTheSuppliedPageRangeAndNativeNavigation() {
		var document = CreatePortal(true,
			CreatePageLink("/articles", "Previous"),
			CreatePageLink("/articles", "1"),
			new TemplateBlock { Template = "articles_page_active", Tokens = new Dictionary<string, string> { ["label"] = "2" } },
			new TemplateBlock { Template = "articles_page_gap" },
			CreatePageLink("/articles?page=8", "8"),
			CreatePageLink("/articles?page=3", "Next")
		);
		var result = new HtmlRenderer().Render(document, new RenderOptions { Environment = RenderEnvironment.Offline });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var pagination = parsed.QuerySelector(".ln-articles-pagination");
		Assert.That(pagination.QuerySelector("p").TextContent, Is.EqualTo("Showing 21–40 of 143 articles"));
		Assert.That(pagination.GetAttribute("data-page-count"), Is.EqualTo("8"));
		Assert.That(pagination.QuerySelectorAll("nav[aria-label='Article pages'] a").Select(link => link.GetAttribute("href")),
			Is.EqualTo(new[] { "/articles", "/articles", "/articles?page=8", "/articles?page=3" }));
		Assert.That(pagination.QuerySelector("[aria-current='page']").TextContent.Trim(), Is.EqualTo("2"));
		Assert.That(pagination.QuerySelector("[aria-current='page'] a"), Is.Null, "The current page is a marker, not another navigation request.");
		Assert.That(pagination.QuerySelector("[aria-hidden='true']").TextContent.Trim(), Is.EqualTo("…"));
		Assert.That(pagination.QuerySelector("[aria-hidden='true'] a"), Is.Null, "A gap is not a fabricated destination.");
		Assert.That(pagination.QuerySelector("button"), Is.Null, "Pagination works through native links without a catalogue script.");
	}

	[TestCase(RenderEnvironment.Offline)]
	[TestCase(RenderEnvironment.Online)]
	public void ExistingPortalModelsWithoutPaginationMetadataRemainEmpty(RenderEnvironment environment) {
		var html = new HtmlRenderer().Render(CreatePortal(false), new RenderOptions { Environment = environment }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-articles-pagination"), Is.Null);
		Assert.That(parsed.QuerySelector("nav[aria-label='Article pages']"), Is.Null);
		Assert.That(parsed.QuerySelector("main").TextContent, Does.Contain("Current page summaries"));
		Assert.That(html, Does.Not.Contain("{pagination}"));
		Assert.That(html, Does.Not.Contain("{range_start}"));
		Assert.That(html, Does.Not.Contain("{total_count}"));
	}

	[TestCase(0, 0, 0)]
	[TestCase(1, 7, 7)]
	public void SinglePageRangeDoesNotInventNavigation(int start, int end, int total) {
		var document = CreatePortal(true) with {
			Children = [CreateArticles(true, new Dictionary<string, string> {
				["range_start"] = start.ToString(),
				["range_end"] = end.ToString(),
				["total_count"] = total.ToString(),
				["page_number"] = "1",
				["page_count"] = "1"
			})]
		};
		var result = new HtmlRenderer().Render(document);
		var html = result.Html;
		var pagination = new HtmlParser().ParseDocument(html).QuerySelector(".ln-articles-pagination");
		Assert.That(pagination.QuerySelector("p").TextContent, Is.EqualTo($"Showing {start}–{end} of {total} articles"));
		Assert.That(pagination.GetAttribute("data-page-count"), Is.EqualTo("1"));
		Assert.That(pagination.QuerySelectorAll("a"), Is.Empty);
		Assert.That(pagination.QuerySelectorAll("li"), Is.Empty);
		Assert.That(html, Does.Not.Contain("{links}"));
		var stylesheet = Encoding.UTF8.GetString(result.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/css/articles.css", StringComparison.Ordinal)).Content.Span);
		Assert.That(stylesheet, Does.Match(@"\.ln-articles-pagination\[data-page-count=""1""\] \.ln-articles-page-links\s*\{\s*display:\s*none;\s*\}"));
	}

	[TestCase("../cms/article-list-page-2.html")]
	[TestCase("https://example.test/nested/articles?page=2&scope=all")]
	public void PageLinkRetainsTheResolvedHostDestination(string destination) {
		var html = new HtmlRenderer().RenderSnippet(CreatePageLink(destination, "Next"), new RenderOptions { Themes = ["cms_articles"] }).Html;
		var link = new HtmlParser().ParseDocument(html).QuerySelector("a");
		Assert.That(link.GetAttribute("href"), Is.EqualTo(destination));
		Assert.That(link.TextContent, Is.EqualTo("Next"));
	}

	[Test]
	public void PortalUsesSuppliedCollectionTitleAndIndependentNestedDestinations() {
		const string collectionUrl = "https://example.test/fixed/fixed2/field-notes/device-setup";
		const string articleUrl = collectionUrl + "/installing-sensors";
		var body = CreateArticles(true, new Dictionary<string, string> {
			["range_start"] = "21", ["range_end"] = "40", ["total_count"] = "41", ["page_number"] = "2", ["page_count"] = "3"
		}, CreatePageLink(collectionUrl + "?page=3", "Next"));
		var slots = new Dictionary<string, VisualNode>(body.Slots);
		slots["category_root"] = new TemplateBlock {
			Template = "articles_category",
			Tokens = new Dictionary<string, string> { ["url"] = collectionUrl, ["title"] = "Device Setup", ["indent_level"] = "1" }
		};
		slots["summaries"] = new TemplateBlock {
			Template = "articles_summary",
			Tokens = new Dictionary<string, string> { ["url"] = articleUrl, ["title"] = "Installing sensors" }
		};
		var document = new DocumentBlock { Title = "Device Setup", Themes = ["cms_articles"], Children = [body with { Slots = slots }] };
		var html = new HtmlRenderer().Render(document).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.Title, Is.EqualTo("Device Setup"));
		Assert.That(parsed.QuerySelector("main nav a").GetAttribute("href"), Is.EqualTo(collectionUrl));
		Assert.That(parsed.QuerySelector(".article-summary > a.ln-article-summary-link").GetAttribute("href"), Is.EqualTo(articleUrl));
		Assert.That(parsed.QuerySelector(".article-summary > a.ln-article-summary-link h2").TextContent, Is.EqualTo("Installing sensors"));
		Assert.That(parsed.QuerySelector(".ln-articles-pagination a").GetAttribute("href"), Is.EqualTo(collectionUrl + "?page=3"));
		Assert.That(parsed.QuerySelectorAll("main a").Select(link => link.GetAttribute("href")), Is.All.Not.Contains("/articles"));
	}

	[Test]
	public void PreparedScalarTextAndDestinationStayEncodedAndLiteral() {
		const string destination = "/articles?page=2&scope=\"<root>\"";
		const string label = "Next <script>alert(1)</script> & {total_count}";
		var html = new HtmlRenderer().RenderSnippet(CreatePageLink(destination, label), new RenderOptions { Themes = ["cms_articles"] }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector("a").GetAttribute("href"), Is.EqualTo(destination));
		Assert.That(parsed.QuerySelector("a").TextContent, Is.EqualTo(label));
		Assert.That(parsed.QuerySelector("script"), Is.Null);
		Assert.That(parsed.QuerySelector("root"), Is.Null);
	}

	[TestCase(RenderEnvironment.Offline)]
	[TestCase(RenderEnvironment.Online)]
	public void PaginationUsesTheExistingAssetBundleAndLoadingFrame(RenderEnvironment environment) {
		var options = new RenderOptions { Environment = environment };
		var renderer = new HtmlRenderer();
		var withoutPagination = renderer.Render(CreatePortal(false), options);
		var withPagination = renderer.Render(CreatePortal(true, CreatePageLink("/articles?page=3", "Next")), options);
		Assert.That(withPagination.Assets.Select(asset => asset.RelativePath), Is.EqualTo(withoutPagination.Assets.Select(asset => asset.RelativePath)));
		foreach (var asset in withPagination.Assets)
			Assert.That(asset.Content.ToArray(), Is.EqualTo(withoutPagination.Assets.Single(previous => previous.RelativePath == asset.RelativePath).Content.ToArray()));
		var parser = new HtmlParser();
		var previousLoader = parser.ParseDocument(withoutPagination.Html).QuerySelector("#preloader");
		var currentLoader = parser.ParseDocument(withPagination.Html).QuerySelector("#preloader");
		Assert.That(previousLoader, Is.Not.Null);
		Assert.That(currentLoader.OuterHtml, Is.EqualTo(previousLoader.OuterHtml));
	}

	private static DocumentBlock CreatePortal(bool withPagination, params VisualNode[] links) {
		return new DocumentBlock {
			Title = "Articles",
			Themes = ["cms_articles"],
			Children = [CreateArticles(withPagination, new Dictionary<string, string> {
				["range_start"] = "21",
				["range_end"] = "40",
				["total_count"] = "143",
				["page_number"] = "2",
				["page_count"] = "8"
			}, links)]
		};
	}

	private static TemplateBlock CreateArticles(bool withPagination, Dictionary<string, string> tokens, params VisualNode[] links) {
		var slots = new Dictionary<string, VisualNode> {
			["category_root"] = new GroupBlock(),
			["categories"] = new GroupBlock(),
			["summaries"] = new ParagraphBlock { Text = [new TextInline { Text = "Current page summaries" }] }
		};
		if (withPagination)
			slots["pagination"] = new TemplateBlock {
				Template = "articles_pagination",
				Slots = new Dictionary<string, VisualNode> { ["links"] = new GroupBlock { Children = links } }
			};
		return new TemplateBlock {
			Template = "articles",
			Tokens = withPagination ? tokens : new Dictionary<string, string>(),
			Slots = slots
		};
	}

	private static TemplateBlock CreatePageLink(string destination, string label) {
		// Template scalars are prepared output values; the host encodes ordinary source text for its HTML context.
		return new TemplateBlock {
			Template = "articles_page_link",
			Tokens = new Dictionary<string, string> {
				["url"] = WebUtility.HtmlEncode(destination),
				["label"] = WebUtility.HtmlEncode(label)
			}
		};
	}
}
