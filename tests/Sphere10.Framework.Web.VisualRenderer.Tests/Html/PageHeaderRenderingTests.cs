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
public class PageHeaderRenderingTests {
	[TestCase(null, false)]
	[TestCase("", true)]
	[TestCase("#section", true)]
	public void InlineLinkPreservesAnExplicitEmptyDestination(string url, bool expectedLink) {
		var document = new DocumentBlock { Children = [new ParagraphBlock { Text = [new TextInline { Text = "Link text", Url = url }] }] };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var link = new HtmlParser().ParseDocument(html).QuerySelector(".ln-page-children a");
		Assert.That(link != null, Is.EqualTo(expectedLink));
		if (expectedLink)
			Assert.That(link.GetAttribute("href"), Is.EqualTo(url));
	}

	[Test]
	public void SubtitleUsesLegacyDescriptionToken() {
		var document = new DocumentBlock { Subtitle = [new TextInline { Text = "Database description & details" }] };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".mt-n3").TextContent, Is.EqualTo("Database description & details"));
		Assert.That(html, Does.Not.Contain("{description}"));
	}

	[Test]
	public void DocumentMetadataPreservesSourceIdentifier() {
		const string sourceId = "017f1a97-ddeb-82a5-8100-0111870b1e75";
		var document = new DocumentBlock { Metadata = new Metadata { SourceId = sourceId } };
		var html = new PageIdentifierRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		Assert.That(html, Does.Contain("ID: " + sourceId + "; Object ID: " + sourceId));
	}

	[TestCase(null, false, false)]
	[TestCase("", false, false)]
	[TestCase("https://example.test/cover.png", false, true)]
	[TestCase(null, true, false)]
	[TestCase("", true, false)]
	[TestCase("https://example.test/cover.png", true, true)]
	public void ThumbnailPositionMatchesActualCoverPresence(string coverUrl, bool imageIcon, bool hasCover) {
		var document = new DocumentBlock {
			CoverUrl = coverUrl,
			Icon = imageIcon ? new Icon { Url = "https://example.test/icon.png" } : new Icon { Emoji = "📚" }
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var overlap = parsed.QuerySelector(".ln-page-content .ln-page-thumbnail-overlap");
		var thumbnail = hasCover ? overlap?.QuerySelector("span") : parsed.QuerySelector(".ln-page-content span.bottom-0");
		Assert.That(parsed.QuerySelectorAll(".ln-cover").Length, Is.EqualTo(hasCover ? 1 : 0));
		Assert.That(overlap != null, Is.EqualTo(hasCover), "Only a real cover uses the native overlap wrapper.");
		Assert.That(thumbnail, Is.Not.Null);
		Assert.That(thumbnail.ClassList.Contains("position-absolute"), Is.False, "The lower half of the icon must retain space in normal document flow.");
		Assert.That(thumbnail.ParentElement.ClassList.Contains("position-relative"), Is.EqualTo(hasCover), "Only cover thumbnails need an overlap wrapper.");
		if (hasCover) {
			Assert.That(thumbnail.ParentElement, Is.SameAs(overlap));
			Assert.That(overlap.ClassList, Does.Contain("d-flex").And.Contain("align-items-start"));
			Assert.That(overlap.QuerySelector(".position-absolute"), Is.Null);
			Assert.That(thumbnail.GetAttribute("style"), Does.Match(@"\bmargin-top\s*:\s*-"), "The native icon crosses the cover edge while its lower half remains in flow.");
		}
		if (imageIcon) {
			Assert.That(thumbnail.QuerySelector("img").GetAttribute("src"), Is.EqualTo("https://example.test/icon.png"));
			Assert.That(thumbnail.QuerySelector("img").ClassList, Does.Contain("ln-page-thumbnail"));
		} else
			Assert.That(thumbnail.TextContent, Is.EqualTo("📚"));
	}

	private sealed class PageIdentifierRenderer : HtmlRenderer {
		protected override string FetchTemplate(string widgetName, string fileExt) => widgetName == "page"
			? "<!-- ID: {id}; Object ID: {object-id} -->{page_content}"
			: base.FetchTemplate(widgetName, fileExt);
	}

}
