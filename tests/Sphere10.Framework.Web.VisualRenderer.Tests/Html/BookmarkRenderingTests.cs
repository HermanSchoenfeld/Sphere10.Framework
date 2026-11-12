// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file LICENSE
// or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BookmarkRenderingTests {

	[Test]
	public void CardEncodesMetadataAndKeepsCaptionOutsideTheLink([Values("default", "cms")] string theme) {
		var block = new BookmarkBlock {
			Url = "https://example.test/?q=a&b=c", Title = "<script>literal</script>", Description = "A & B",
			ImageUrl = "https://example.test/preview.webp", IconUrl = "https://example.test/icon.png",
			Caption = [new TextInline { Text = "Authored caption" }]
		};
		var result = new HtmlRenderer().RenderSnippet(block, new RenderOptions { Themes = [theme] });
		var document = new HtmlParser().ParseDocument(result.Html);
		Assert.That(document.QuerySelectorAll(".ln-bookmark-block > a"), Has.Length.EqualTo(1));
		Assert.That(document.QuerySelector(".ln-bookmark-title").TextContent, Is.EqualTo(block.Title));
		Assert.That(document.QuerySelector(".ln-bookmark-description").TextContent, Is.EqualTo(block.Description));
		Assert.That(document.QuerySelector(".ln-bookmark").GetAttribute("href"), Is.EqualTo(block.Url));
		Assert.That(document.QuerySelector(".ln-bookmark-image img").GetAttribute("src"), Is.EqualTo(block.ImageUrl));
		Assert.That(document.QuerySelector(".ln-bookmark-block > figcaption").TextContent, Is.EqualTo("Authored caption"));
		Assert.That(document.QuerySelectorAll(".ln-bookmark-block script"), Is.Empty);
	}

	[Test]
	public void MissingMetadataDoesNotEmitBrokenImagesAndUnsafeUrlsAreRejected() {
		var result = new HtmlRenderer().RenderSnippet(new BookmarkBlock { Url = "https://example.test/", ImageUrl = "javascript:alert(1)", IconUrl = "data:text/html,bad" });
		var document = new HtmlParser().ParseDocument(result.Html);
		Assert.That(document.QuerySelectorAll(".ln-bookmark img"), Is.Empty);
		Assert.That(document.QuerySelector(".ln-bookmark-title").TextContent, Is.EqualTo("https://example.test/"));
	}
}
