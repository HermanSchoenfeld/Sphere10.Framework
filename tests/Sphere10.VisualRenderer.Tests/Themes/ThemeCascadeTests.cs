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
using System.Text;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ThemeCascadeTests {
	private string _temporaryRoot;
	private ThemeCatalog _catalog;

	[SetUp]
	public void SetUp() {
		_temporaryRoot = Path.Combine(Path.GetTempPath(), "localnotion-theme-cascade-" + Guid.NewGuid().ToString("N"));
		_catalog = new ThemeCatalog(new ThemeOptions { ThemesDirectory = _temporaryRoot });
	}

	[TearDown]
	public void TearDown() {
		if (Directory.Exists(_temporaryRoot))
			Directory.Delete(_temporaryRoot, recursive: true);
	}

	[Test]
	public void SharedBaseDoesNotResetEarlierFilesTokensOrAssets([Values] RenderEnvironment environment) {
		Write("shared/.config.json", """{"type":"html","tokens":{"Accent":{"offline":"base offline","online":"base online"}}}""");
		Write("shared/layout.html", "base layout");
		Write("shared/resources/palette.css", "base bytes");
		Write("first/.config.json", """{"type":"html","base":"shared","tokens":{"Accent":{"offline":"first offline","online":"first online"}}}""");
		Write("first/layout.html", "first layout");
		Write("first/resources/palette.css", "first bytes");
		Write("decoration/.config.json", """{"type":"html","base":"shared"}""");
		Write("decoration/shape.inc", "decorative shape");
		var session = _catalog.CreateSession(["first", "decoration"], environment, RenderMode.ReadOnly);
		Assert.That(session.GetTemplate("layout"), Is.EqualTo("first layout"));
		Assert.That(session.Tokens["Accent"], Is.EqualTo(environment == RenderEnvironment.Offline ? "first offline" : "first online"));
		Assert.That(session.Tokens["include://shape.inc"], Is.EqualTo("decorative shape"));
		var asset = session.Assets.Single(item => item.RelativePath.EndsWith("/resources/palette.css", StringComparison.Ordinal));
		Assert.That(Encoding.UTF8.GetString(asset.Content.Span), Is.EqualTo("first bytes"));
	}

	[TestCase("first", "second", "second")]
	[TestCase("second", "first", "first")]
	public void LaterExplicitSelectionOverridesEarlierTemplatesAndTokens(string firstTheme, string secondTheme, string expected) {
		Write("first/.config.json", """{"type":"html","tokens":{"Accent":{"offline":"first","online":"first"}}}""");
		Write("first/layout.html", "first");
		Write("second/.config.json", """{"type":"html","base":"first","tokens":{"Accent":{"offline":"second","online":"second"}}}""");
		Write("second/layout.html", "second");
		var session = _catalog.CreateSession([firstTheme, secondTheme], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.GetTemplate("layout"), Is.EqualTo(expected));
		Assert.That(session.Tokens["Accent"], Is.EqualTo(expected));
	}

	[Test]
	public void LaterThemeAddsOwnDefinitionsWithoutImportingItsUnselectedBase() {
		Write("first/.config.json", """{"type":"html"}""");
		Write("first/layout.html", "first layout");
		Write("unselected/.config.json", """{"type":"html","tokens":{"Unselected":{"offline":"base","online":"base"}}}""");
		Write("unselected/layout.html", "unselected layout");
		Write("unselected/inherited-only.inc", "unselected content");
		Write("second/.config.json", """{"type":"html","base":"unselected","tokens":{"Selected":{"offline":"second","online":"second"}}}""");
		Write("second/own.inc", "second content");
		var session = _catalog.CreateSession(["first", "second"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.GetTemplate("layout"), Is.EqualTo("first layout"));
		Assert.That(session.Tokens.ContainsKey("Unselected"), Is.False);
		Assert.That(session.Tokens.ContainsKey("include://inherited-only.inc"), Is.False);
		Assert.That(session.Tokens["Selected"], Is.EqualTo("second"));
		Assert.That(session.Tokens["include://own.inc"], Is.EqualTo("second content"));
	}

	[Test]
	public void FirstSelectedThemeInheritsEntireBaseChain() {
		Write("foundation/.config.json", """{"type":"html"}""");
		Write("foundation/layout.html", "foundation layout");
		Write("foundation/inherited.inc", "foundation content");
		Write("base/.config.json", """{"type":"html","base":"foundation"}""");
		Write("base/layout.html", "base layout");
		Write("base/own.inc", "base content");
		Write("first/.config.json", """{"type":"html","base":"base"}""");
		Write("first/layout.html", "first layout");
		var session = _catalog.CreateSession(["first"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.GetTemplate("layout"), Is.EqualTo("first layout"));
		Assert.That(session.Tokens["include://inherited.inc"], Is.EqualTo("foundation content"));
		Assert.That(session.Tokens["include://own.inc"], Is.EqualTo("base content"));
	}

	[Test]
	public void DecorativeHeaderThemesPreserveCarouselSlides([Values] RenderEnvironment environment) {
		var document = new DocumentBlock {
			RenderFrame = false,
			Themes = ["cms_header", "header_particles", "color_text_white", "shape_curve_down", "header_fade"],
			Children = [
				new ToggleBlock {
					Text = [new TextInline { Text = "Slide title" }],
					Children = [new ParagraphBlock { Text = [new TextInline { Text = "Slide body" }] }]
				}
			]
		};
		var html = new HtmlRenderer().Render(document, new RenderOptions { Environment = environment }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".carousel.carousel-fade"), Is.Not.Null);
		Assert.That(parsed.QuerySelector(".carousel-inner > .carousel-item.text-white .carousel-caption").TextContent, Does.Contain("Slide body"));
		Assert.That(parsed.QuerySelector(".accordion"), Is.Null);
		Assert.That(parsed.QuerySelector(".carousel #particles-js"), Is.Not.Null);
		Assert.That(parsed.QuerySelector(".carousel .shape-bottom svg"), Is.Not.Null);
	}

	[Test]
	public void DecorativeFooterThemesPreserveColumnTypographyAndSpacing([Values] RenderEnvironment environment) {
		var document = new DocumentBlock {
			RenderFrame = false,
			Themes = ["cms_footer", "columns_md", "h3_as_h6", "link_muted", "image_maxwidth_sm", "text_small", "text_muted", "color_text_dark",
				"color_bg_body_secondary", "section_padding_bottom_none", "section_padding_top_xxs"],
			Children = [
				new ColumnListBlock {
					Columns = [
						new ColumnBlock { Children = [new HeadingBlock { Level = 3, Text = [new TextInline { Text = "Footer heading" }] }] },
						new ColumnBlock { Children = [new ParagraphBlock { Text = [new TextInline { Text = "Footer link", Url = "https://example.test/footer" }] }] }
					]
				}
			]
		};
		var html = new HtmlRenderer().Render(document, new RenderOptions { Environment = environment }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelectorAll(".ln-page-content .row > .col-md").Length, Is.EqualTo(2));
		Assert.That(parsed.QuerySelector("h3.fs-6").TextContent, Is.EqualTo("Footer heading"));
		Assert.That(parsed.QuerySelector("p.small.text-dark a.text-muted").GetAttribute("href"), Is.EqualTo("https://example.test/footer"));
		Assert.That(parsed.QuerySelector(".ln-page-content.bg-body-secondary.pt-xxs.pb-0"), Is.Not.Null);
	}
	private void Write(string path, string contents) {
		var fullPath = Path.Combine(_temporaryRoot, path.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
		File.WriteAllText(fullPath, contents, new UTF8Encoding(false));
	}
}