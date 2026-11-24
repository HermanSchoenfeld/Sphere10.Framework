// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
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
public class ShadowsAllThemeTests {

	[Test]
	public void ShadowsAllGivesHeadingsImagesAndCalloutsTheSharedShadow() {
		var page = new GroupBlock {
			Children = [
				new HeadingBlock { Metadata = new Metadata { Anchor = "title" }, Level = 1, Text = [new TextInline { Text = "Title" }] },
				new HeadingBlock { Metadata = new Metadata { Anchor = "section" }, Level = 2, Text = [new TextInline { Text = "Section" }] },
				new MediaBlock { Metadata = new Metadata { Anchor = "photo" }, Type = MediaType.Image, Url = "https://example.test/photo.png", AltText = "Photo" },
				new CalloutBlock { Metadata = new Metadata { Anchor = "note" }, Text = [new TextInline { Text = "Note" }] }
			]
		};
		var plain = Parse(page, "cms");
		var shadowed = Parse(page, "cms", "shadows_all", "shadow_md");

		Assert.That(Classes(plain, "#photo img"), Does.Not.Contain("shadow"), "Without the theme the CMS adds no shadow.");
		Assert.That(Classes(shadowed, "#photo img"), Does.Contain("shadow"));
		Assert.That(Classes(shadowed, "#title"), Does.Contain("text-shadow-md"), "Headings take the shared text shadow at the chosen strength.");
		Assert.That(Classes(shadowed, "#section"), Does.Contain("text-shadow-md"));
		Assert.That(shadowed.QuerySelector("#note")?.OuterHtml ?? string.Empty, Does.Contain("shadow"));
	}

	private static AngleSharp.Html.Dom.IHtmlDocument Parse(VisualNode page, params string[] themes)
		=> new HtmlParser().ParseDocument(new HtmlRenderer().RenderSnippet(page, new RenderOptions { Themes = themes, Environment = RenderEnvironment.Online }).Html);

	private static string Classes(AngleSharp.Html.Dom.IHtmlDocument document, string selector) {
		var element = document.QuerySelector(selector);
		if (element == null)
			return string.Empty;
		// The anchor can sit on a wrapper; the classes of the element and its descendants are what render the shadow.
		return element.ClassName + " " + string.Join(" ", System.Linq.Enumerable.Select(element.QuerySelectorAll("*"), child => child.ClassName));
	}
}
