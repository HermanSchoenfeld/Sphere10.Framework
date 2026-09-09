// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.Children)]
public class HtmlRenderingTests {
	private string _html = null;
	private IDocument _document = null;

	[SetUp]
	public void SetUp() {
		_html = new HtmlRenderer().Render(RendererTestData.CreateDocument(), RendererTestData.CreateOptions()).Html;
		_document = new HtmlParser().ParseDocument(_html);
	}

	[Test]
	public void PageMetadataPreservesTitleAndExplicitOccurrenceAnchor() {
		Assert.That(_document.Title, Is.EqualTo("Standalone <renderer>"));
		Assert.That(_document.QuerySelector("#explicit-anchor"), Is.Not.Null);
		Assert.That(_document.QuerySelector("#source-object"), Is.Null, "Source identity must not replace an explicit occurrence anchor.");
	}

	[Test]
	public void TrustedMarkupRendersAsHtml() {
		Assert.That(_document.QuerySelector("#trusted-content")?.TextContent, Is.EqualTo("Trusted"));
	}

	[TestCase("<DIV DATA-original = 'unchanged'><INPUT checked>\r\n\t<span>  exact spacing  </span></DIV>")]
	[TestCase("<TABLE><TR><TD>Keep original table markup</TD></TR></TABLE>")]
	public void RawHtmlRetainsOriginalMarkupWithoutDocumentNormalization(string markup) {
		var document = new DocumentBlock {
			Title = "Raw markup preservation",
			RenderFrame = true,
			Children = [new RawHtmlBlock { Html = markup }]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		Assert.That(html, Does.Contain(markup), "Rendering must preserve raw markup, attribute spelling, quotes and whitespace without reparsing the document.");
	}

	[Test]
	public void TemplateTokensInUserTextRemainLiteral() {
		Assert.That(_document.Body.TextContent, Does.Contain("{title}"));
	}

	[Test]
	public void StructuralTemplateTokensAreExpanded() {
		Assert.That(_html, Does.Not.Contain("{children}").And.Not.Contain("{page_content}"));
	}

	[Test]
	public void TableHeaderCellsRender() {
		Assert.That(_document.QuerySelectorAll("table th"), Is.Not.Empty);
	}

	[Test]
	public void DatabaseValuesTraverseNestedComputedAndCompoundValues() {
		Assert.That(_document.Body.TextContent, Does.Contain("Nested value"));
	}

	[Test]
	public void UnsupportedDatabaseValuesRenderTheirDiagnostic() {
		Assert.That(_document.Body.TextContent, Does.Contain("Unknown property diagnostic"));
	}

	[Test]
	public void BlockPresentedPropertyReferencesKeepParagraphWrapper() {
		Assert.That(_document.QuerySelector("td p a[href='https://example.test/row']"), Is.Not.Null);
	}

	[Test]
	public void CheckedBlocksAndBooleanPropertiesRenderAsChecked() {
		Assert.That(_document.QuerySelectorAll("input[checked]").Length, Is.GreaterThanOrEqualTo(2));
	}

	[Test]
	public void ResolvedLinksRetainTheirDestinationAnchor() {
		Assert.That(_document.QuerySelector("a[href='https://example.test/page#destination']"), Is.Not.Null);
	}

	[Test]
	public void AvailableEmptyDestinationRemainsCurrentDocumentLink() {
		Assert.That(_document.QuerySelector("a[href='']")?.TextContent, Does.Contain("Self reference"));
	}

	[Test]
	public void VideoProviderUsesItsEmbedDestination() {
		Assert.That(_document.QuerySelector("iframe[src='https://www.youtube.com/embed/video-123']"), Is.Not.Null);
	}

	[Test]
	public void NumberedListsRetainStartValue() {
		Assert.That(_document.QuerySelector("ol[start='4']"), Is.Not.Null);
	}

	[Test]
	public void OrdinaryCoverUrlRemainsUnchanged() {
		Assert.That(_document.QuerySelector(".ln-cover").GetAttribute("style"), Is.EqualTo("background-image:url('https://example.test/cover.png')"));
	}

	[Test]
	public void CoverUrlEscapesCssStringDelimitersWithoutDoubleEncodingPercentSequences() {
		DocumentBlock document = new DocumentBlock { CoverUrl = "https://example.test/cover%20existing.png?caption=x');color:red;/*\\\r\n\f" };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-cover").GetAttribute("style"),
			Is.EqualTo("background-image:url('https://example.test/cover%20existing.png?caption=x%27);color:red;/*%5C%0D%0A%0C')"));
	}

	[Test]
	public void CodeRemainsLiteral() {
		var options = RendererTestData.CreateOptions();
		var html = new HtmlRenderer().Render(RendererTestData.CreateDocument(), options).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelectorAll("pre code script"), Is.Empty, "Code must not become executable markup.");
		Assert.That(parsed.QuerySelector("pre code.language-html").TextContent, Does.Contain("<script>alert('literal')</script>"));
	}
}
