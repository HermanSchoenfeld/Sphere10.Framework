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
public class ImageCaptionRenderingTests {
	private const string Caption = "Café “Locked Out” interview — 東京 & <friends>";

	[TestCase("default")]
	[TestCase("cms")]
	public void CaptionsKeepTheirCharacters(string theme) {
		var result = new HtmlRenderer().RenderSnippet(Image(), new RenderOptions { Themes = [theme], Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);

		Assert.That(parsed.QuerySelector("#photo figcaption")?.TextContent ?? parsed.QuerySelector("#photo")?.TextContent, Does.Contain(Caption));
		// Characters are written as themselves in UTF-8 pages: a numeric reference shows literally wherever text is encoded again.
		Assert.That(result.Html, Does.Contain("Café").And.Not.Contain("&#233;"));
		Assert.That(result.Html, Does.Contain("&amp; &lt;friends&gt;"), "Markup characters are still encoded.");
	}

	[Test]
	public void CmsImagesUseThePlainAltTextInTheirAltAttribute() {
		var result = new HtmlRenderer().RenderSnippet(Image(), new RenderOptions { Themes = ["cms"], Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);

		Assert.That(parsed.QuerySelector("#photo img")?.GetAttribute("alt"), Is.EqualTo(Caption), "The styled caption's markup stays out of the attribute.");
	}

	private static MediaBlock Image() => new() {
		Metadata = new Metadata { Anchor = "photo" },
		Type = MediaType.Image,
		Url = "https://example.test/photo.png",
		AltText = Caption,
		Caption = [new TextInline { Text = Caption, Style = new TextStyle { Bold = true } }]
	};
}
