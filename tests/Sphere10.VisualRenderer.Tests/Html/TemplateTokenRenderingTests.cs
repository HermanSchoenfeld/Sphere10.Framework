// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TemplateTokenRenderingTests {

	[Test]
	public void MissingPageColorsDoNotActivateDefaultBackgroundStyles([Values] bool titleOnCover) {
		var document = RendererTestData.CreateDocument() with { TitleOnCover = titleOnCover };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var unresolved = parsed.All.SelectMany(element => element.Attributes)
			.Where(attribute => attribute.Value.Contains("{color}"))
			.Select(attribute => attribute.Name + "=" + attribute.Value)
			.ToArray();
		Assert.That(unresolved, Is.Empty, "Page sections, covers, titles, captions and navigation must not retain unresolved color tokens.");
		Assert.That(parsed.QuerySelector(".ln-page-content").ClassList, Does.Not.Contain("ln-color-default"));
		Assert.That(parsed.QuerySelector(".ln-cover").ClassList, Does.Not.Contain("ln-color-default"), "An absent color must not reset cover size/position through background:inherit.");
	}

	[TestCase("default")]
	[TestCase("green-background")]
	[TestCase("custom-theme-color")]
	public void ExplicitPageColorStillAppliesToPageDecorations(string color) {
		var document = new DocumentBlock {
			Title = "Colored page",
			CoverUrl = "https://example.test/cover.png",
			Tokens = new Dictionary<string, string> { ["color"] = color }
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		foreach (var element in parsed.QuerySelectorAll(".ln-page-content, .ln-cover, #page-title"))
			Assert.That(element.ClassList, Does.Contain("ln-color-" + color));
	}

	[Test]
	public void MissingDecorationColorRetainsAnExplicitAncestorColor() {
		var document = new DocumentBlock {
			RenderFrame = false,
			ShowPageHeader = false,
			Children = [
				new CalloutBlock {
					Color = Color.BlueBackground,
					Children = [new DocumentBlock { Title = "Nested", RenderFrame = false, CoverUrl = "https://example.test/cover.png" }]
				}
			]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-cover").ClassList, Does.Contain("ln-color-blue-background"));
	}

	[TestCase("background-image:url('{cover_url}')")]
	[TestCase("background-image:url('{cover_url}');background-size:contain;background-position:left top")]
	public void MissingColorPreservesDeployedCoverTemplateAndCss(string coverStyle) {
		const string coverUrl = "https://example.test/cover.png";
		const string css = ".ln-cover{background-size:cover;background-position:center}.ln-color-default{background:inherit;color:inherit}";
		var root = Path.Combine(Path.GetTempPath(), "localnotion-cover-theme-" + Guid.NewGuid().ToString("N"));
		var templatePath = Path.Combine(root, "default", "page_cover.html");
		var cssPath = Path.Combine(root, "default", "resources", "local-notion", "css", "ln.css");
		var template = "<div class=\"ln-cover custom-cover {include://color_text_aux.inc}\" style=\"" + coverStyle + "\">{cover_title}</div>";
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(cssPath));
			File.WriteAllText(templatePath, template);
			File.WriteAllText(cssPath, css);
			var themes = new ThemeCatalog(new ThemeOptions { ThemesDirectory = root });
			var html = new HtmlRenderer(themes).Render(new DocumentBlock { Title = "Cover", CoverUrl = coverUrl }, RendererTestData.CreateOptions()).Html;
			var cover = new HtmlParser().ParseDocument(html).QuerySelector(".custom-cover");
			Assert.That(cover, Is.Not.Null);
			Assert.That(cover.ClassList, Does.Not.Contain("ln-color-default"), "Legacy CSS must not reset a previously uncolored cover.");
			Assert.That(cover.ClassName, Does.Not.Contain("{color}"));
			Assert.That(cover.GetAttribute("style"), Is.EqualTo(coverStyle.Replace("{cover_url}", coverUrl)), "A deployed template retains its own cover positioning.");
			Assert.That(File.ReadAllText(templatePath), Is.EqualTo(template));
			Assert.That(File.ReadAllText(cssPath), Is.EqualTo(css));
		} finally {
			if (Directory.Exists(root))
				Directory.Delete(root, recursive: true);
		}
	}

	[Test]
	public void UserTextKeepsLiteralColorAndThemeTokenSyntax() {
		const string text = "Literal {color}, {title}, {include://color_text.inc} and {{braces}}";
		var document = new DocumentBlock { Children = [new ParagraphBlock { Text = [new TextInline { Text = text }] }] };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-page-children p").TextContent.Trim(), Is.EqualTo(text));
	}

	[Test]
	public void UnicodeCodePointsSurviveTextAndIconRendering(
		[Values("\U0001F4DA", "\U0001F469\U0001F3FD\u200D\U0001F4BB", "\U0001F1E6\U0001F1FA", "\u00A9 \u2019 \u2605 \uFE0F")] string text
	) {
		var document = new DocumentBlock {
			Title = text,
			Icon = new Icon { Emoji = text },
			Children = [new ParagraphBlock { Text = [new TextInline { Text = text }] }]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.Title, Is.EqualTo(text));
		Assert.That(parsed.QuerySelector(".ln-page-children p").TextContent.Trim(), Is.EqualTo(text));
		Assert.That(parsed.QuerySelector(".ln-page-content span.bottom-0").TextContent, Is.EqualTo(text));
		Assert.That(html, Does.Not.Contain("\uFFFD"));
	}
}
