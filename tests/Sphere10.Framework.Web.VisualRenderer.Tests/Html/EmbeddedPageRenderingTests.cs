// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EmbeddedPageRenderingTests {
	[TestCase(RenderMode.ReadOnly, RenderEnvironment.Offline)]
	[TestCase(RenderMode.ReadOnly, RenderEnvironment.Online)]
	[TestCase(RenderMode.Editable, RenderEnvironment.Offline)]
	[TestCase(RenderMode.Editable, RenderEnvironment.Online)]
	public void DefaultEmbeddedReferencePreservesExistingMarkup(RenderMode mode, RenderEnvironment environment) {
		var reference = new ReferenceBlock {
			Reference = new Reference { Url = "/manual#chapter", Label = "Manual", Icon = new Icon { Emoji = "📚" }, ShowIndicator = true }
		};
		var document = new DocumentBlock { Children = [reference] };
		var options = new RenderOptions { Mode = mode, Environment = environment, AssetBaseUrl = "/render-assets" };
		var renderer = new HtmlRenderer();
		var original = renderer.Render(document, options).Html;
		var embedded = renderer.Render(document with { Children = [reference with { IsEmbeddedPage = true }] }, options).Html;
		Assert.That(embedded, Is.EqualTo(original), "The default embedded-page slot must retain the existing reference HTML byte for byte.");
	}

	[TestCase(RenderMode.ReadOnly, RenderEnvironment.Offline)]
	[TestCase(RenderMode.ReadOnly, RenderEnvironment.Online)]
	[TestCase(RenderMode.Editable, RenderEnvironment.Offline)]
	[TestCase(RenderMode.Editable, RenderEnvironment.Online)]
	public void DocumentControlHidesOnlyContainedPageReferences(RenderMode mode, RenderEnvironment environment) {
		var document = new DocumentBlock {
			HideEmbeddedPages = true,
			Children = [
				new ReferenceBlock { IsEmbeddedPage = true, Reference = new Reference { Url = "/contained", Label = "Contained page" } },
				new ReferenceBlock { Reference = new Reference { Url = "/ordinary", Label = "Ordinary reference" } },
				new ParagraphBlock { Text = [
					new TextInline { Text = "Authored menu", Url = "/contained#offering" },
					new ReferenceInline { Reference = new Reference { Url = "/inline", Label = "Inline reference" } }
				] },
				new ColumnListBlock { Columns = [new ColumnBlock { Children = [
					new ReferenceBlock { IsEmbeddedPage = true, Reference = new Reference { Url = "/nested", Label = "Nested contained page" } },
					new ParagraphBlock { Text = [new TextInline { Text = "Keep column content" }] }
				] }] },
				new MediaBlock { Type = MediaType.Image, Url = "https://example.test/illustration.svg", AltText = "Keep image" }
			]
		};
		var html = new HtmlRenderer().Render(document, new RenderOptions { Mode = mode, Environment = environment, AssetBaseUrl = "/render-assets" }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector("a[href='/contained'],a[href='/nested']"), Is.Null);
		Assert.That(parsed.QuerySelector("a[href='/ordinary']")?.TextContent, Does.Contain("Ordinary reference"));
		Assert.That(parsed.QuerySelector("a[href='/contained#offering']")?.TextContent, Does.Contain("Authored menu"));
		Assert.That(parsed.QuerySelector("a[href='/inline']")?.TextContent, Does.Contain("Inline reference"));
		Assert.That(parsed.Body.TextContent, Does.Contain("Keep column content").And.Not.Contain("Contained page").And.Not.Contain("Nested contained page"));
		Assert.That(parsed.QuerySelector("img[src='https://example.test/illustration.svg']"), Is.Not.Null);
	}

	[Test]
	public void DocumentControlDoesNotHideAnIndependentDestinationDocument() {
		var document = new DocumentBlock {
			Title = "Independent destination",
			HideEmbeddedPages = true,
			Children = [
				new HeadingBlock { Metadata = new Metadata { Anchor = "offering" }, Level = 1, Text = [new TextInline { Text = "Offering details" }] },
				new ParagraphBlock { Text = [new TextInline { Text = "Preserve the complete article body." }] }
			]
		};
		var parsed = new HtmlParser().ParseDocument(new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html);
		Assert.That(parsed.Title, Is.EqualTo("Independent destination"));
		Assert.That(parsed.QuerySelector("h1#offering")?.TextContent, Is.EqualTo("Offering details"));
		Assert.That(parsed.Body.TextContent, Does.Contain("Preserve the complete article body."));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void NearestDocumentOwnsSuppressionIndependentlyOfThemeSelection(bool hideParent, bool hideNested) {
		var document = new DocumentBlock {
			HideEmbeddedPages = hideParent,
			Children = [
				new DocumentBlock {
					RenderFrame = false,
					HideEmbeddedPages = hideNested,
					Children = [new ReferenceBlock { IsEmbeddedPage = true, Reference = new Reference { Url = "/nested-visible", Label = "Nested visible page" } }]
				},
				new ReferenceBlock { IsEmbeddedPage = true, Reference = new Reference { Url = "/parent-hidden", Label = "Parent hidden page" } }
			]
		};
		var parsed = new HtmlParser().ParseDocument(new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html);
		Assert.That(parsed.QuerySelector("a[href='/nested-visible']") is null, Is.EqualTo(hideNested));
		Assert.That(parsed.QuerySelector("a[href='/parent-hidden']") is null, Is.EqualTo(hideParent));
	}

	[Test]
	public void DocumentControlDoesNotChangePlainText() {
		var document = new DocumentBlock {
			Children = [
				new ReferenceBlock { IsEmbeddedPage = true, Reference = new Reference { Url = "/contained", Label = "Contained page" } },
				new ReferenceBlock { Reference = new Reference { Url = "/ordinary", Label = "Ordinary reference" } }
			]
		};
		var renderer = new TextRenderer();
		var original = renderer.Render(document);
		Assert.That(renderer.Render(document with { HideEmbeddedPages = true }), Is.EqualTo(original));
		Assert.That(original, Does.Contain("Contained page").And.Contain("Ordinary reference"));
	}

	[TestCase("HideEmbeddedPages")]
	[TestCase("hide_embedded_pages")]
	public void VisibilityControlIsNotAnEmbeddedPresentationTheme(string name) {
		Assert.That(() => new ThemeCatalog().CreateSession(["default", name], RenderEnvironment.Online, RenderMode.ReadOnly),
			Throws.TypeOf<ThemeException>().With.Message.Contains(name));
	}
}
