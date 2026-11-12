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
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PageCoverRenderingTests {

	[Test]
	public void HeaderCoverGroupsOnlyTheNativeTitleAndOptionalSubtitle(
		[Values] bool hasSubtitle, [Values] RenderEnvironment environment, [Values] RenderMode mode
	) {
		var document = CreateCoverDocument() with {
			Themes = ["cms", "header_cover", "header_large"],
			Subtitle = hasSubtitle ? [new TextInline { Text = "Native <subtitle> & {cover_subtitle}", Url = "https://example.test/details" }] : [],
			Children = [new HeadingBlock { Level = 3, Text = [new TextInline { Text = "Body technical subtitle" }] }]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions() with { Environment = environment, Mode = mode }).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var group = parsed.QuerySelector(".ln-cover > .ln-cover-content");
		Assert.That(group, Is.Not.Null);
		Assert.That(parsed.QuerySelector(".ln-cover").ClassList, Does.Contain("d-flex"));
		Assert.That(parsed.QuerySelector(".ln-cover").ClassList, Does.Contain("align-items-center"));
		Assert.That(parsed.QuerySelector(".ln-cover").ClassList, Does.Contain("justify-content-center"));
		Assert.That(group.ClassList, Does.Not.Contain("h-100"), "The centered title/subtitle group must take its natural content height.");
		Assert.That(group.QuerySelector("#page-cover-title").TextContent, Is.EqualTo("Service <details> & {title}"));
		Assert.That(group.QuerySelector("#page-cover-title").HasAttribute("data-toc-skip"), Is.True);
		Assert.That(group.Children.Length, Is.EqualTo(hasSubtitle ? 2 : 1));
		Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3"), Is.Null, "A cover subtitle must not also appear below the cover.");
		Assert.That(parsed.QuerySelector(".ln-page-children h3").TextContent.Trim(), Is.EqualTo("Body technical subtitle"), "Body headings are independent of native document subtitles.");
		if (hasSubtitle) {
			var subtitle = group.QuerySelector("#page-cover-subtitle");
			Assert.That(subtitle.TextContent.Trim(), Is.EqualTo("Native <subtitle> & {cover_subtitle}"));
			Assert.That(subtitle.QuerySelector("a").GetAttribute("href"), Is.EqualTo("https://example.test/details"));
			Assert.That(subtitle.QuerySelector("subtitle"), Is.Null, "Rich text must remain escaped content, not source markup.");
		} else
			Assert.That(parsed.QuerySelector("#page-cover-subtitle"), Is.Null);
	}

	[TestCase("", 35)]
	[TestCase("header_tiny", 15)]
	[TestCase("header_small", 25)]
	[TestCase("header_large", 75)]
	[TestCase("header_full", 100)]
	public void HeaderCoverUsesThemeOwnedViewportHeight(string theme, int viewportHeight) {
		var document = CreateCoverDocument() with { Themes = theme.Length > 0 ? ["cms", "header_cover", theme] : ["cms", "header_cover"] };
		var result = new HtmlRenderer().Render(document, RendererTestData.CreateOptions());
		var cover = new HtmlParser().ParseDocument(result.Html).QuerySelector(".ln-cover");
		var css = result.Assets.Where(asset => asset.RelativePath.EndsWith("/resources/local-notion/css/ln.css", StringComparison.Ordinal))
			.Select(asset => Encoding.UTF8.GetString(asset.Content.Span)).Distinct(StringComparer.Ordinal).Single();
		Assert.That(cover.GetAttribute("style"), Does.Match(@"height:\s*" + viewportHeight + @"vh\s*;"));
		Assert.That(cover.ClassList.Any(value => Regex.IsMatch(value, @"^h-\d+$")), Is.False,
			"A definite cover height must not compete with Bootstrap's important percentage-height utilities.");
		Assert.That(css, Does.Not.Match(@"\.ln-cover\.h-\d+"), "Cover sizing belongs to the selected Theme, not rewritten shared height classes.");
		var coverRule = Regex.Match(css, @"\.ln-cover\s*\{([^}]+)\}").Groups[1].Value;
		Assert.That(coverRule, Does.Not.Match(@"display:\s*flex\s*;"));
		Assert.That(coverRule, Does.Match(@"height:\s*35vh\s*;"));
		Assert.That(coverRule, Does.Match(@"min-height:\s*200px\s*;"));
	}

	[Test]
	public void HeaderCoverDefaultHeightMatchesTheStockCoverStylesheet() {
		// The centered cover owns its height but preserves the stock default and shared CSS bytes.
		var renderer = new HtmlRenderer();
		var stock = renderer.Render(CreateCoverDocument(), RendererTestData.CreateOptions());
		var centered = renderer.Render(CreateCoverDocument() with { Themes = ["cms", "header_cover"] }, RendererTestData.CreateOptions());
		var stockCss = stock.Assets.Where(asset => asset.RelativePath.EndsWith("/resources/local-notion/css/ln.css", StringComparison.Ordinal)).Select(asset => asset.Content.ToArray()).ToArray();
		var centeredCss = centered.Assets.Where(asset => asset.RelativePath.EndsWith("/resources/local-notion/css/ln.css", StringComparison.Ordinal)).Select(asset => asset.Content.ToArray()).ToArray();
		Assert.That(stockCss, Is.Not.Empty);
		Assert.That(centeredCss, Is.Not.Empty);
		Assert.That(stockCss.All(bytes => bytes.SequenceEqual(stockCss[0])), Is.True);
		var stockRule = Regex.Match(Encoding.UTF8.GetString(stockCss[0]), @"\.ln-cover\s*\{([^}]+)\}").Groups[1].Value;
		var stockHeight = Regex.Match(stockRule, @"(?:^|;)\s*height:\s*([^;]+);").Groups[1].Value.Trim();
		var cover = new HtmlParser().ParseDocument(centered.Html).QuerySelector(".ln-cover");
		Assert.That(stockHeight, Is.EqualTo("35vh"));
		Assert.That(cover.GetAttribute("style"), Does.Match(@"height:\s*" + Regex.Escape(stockHeight) + @"\s*;"));
		Assert.That(centeredCss.All(bytes => bytes.SequenceEqual(stockCss[0])), Is.True, "Centering the native cover must not rewrite the shared default stylesheet.");
		Assert.That(stockRule, Does.Match(@"min-height:\s*200px\s*;"));
	}

	[Test]
	public void DefaultCoverRetainsItsOriginalTitleTemplateAndOutsideSubtitle() {
		var html = new HtmlRenderer().Render(CreateCoverDocument() with { Subtitle = [new TextInline { Text = "Outside subtitle" }] }, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var group = parsed.QuerySelector(".ln-cover > div");
		Assert.That(group.ClassList, Does.Contain("h-100"));
		Assert.That(group.ClassList, Does.Contain("d-flex"));
		Assert.That(parsed.QuerySelector(".ln-cover-content, #page-cover-subtitle"), Is.Null);
		Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3").TextContent, Is.EqualTo("Outside subtitle"));
	}

	[Test]
	public void HeaderCoverDoesNotChangeExistingCarouselComposition() {
		var document = CreateCoverDocument() with {
			Subtitle = [new TextInline { Text = "Native document subtitle" }],
			Themes = ["cms", "cms_header", "shape_curve_down", "header_large"],
			Children = [new ToggleBlock { Text = [new TextInline { Text = "Authored slide" }], Children = [new ParagraphBlock { Text = [new TextInline { Text = "Slide content" }] }] }]
		};
		var baseline = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var result = new HtmlRenderer().Render(document with { Themes = ["cms", "cms_header", "shape_curve_down", "header_cover", "header_large"] }, RendererTestData.CreateOptions()).Html;
		Assert.That(result, Is.EqualTo(baseline));
		var parsed = new HtmlParser().ParseDocument(result);
		Assert.That(parsed.QuerySelector(".carousel").ClassList, Does.Contain("h-75"));
		Assert.That(parsed.QuerySelector(".ln-cover"), Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void LegacyCoverTitleKeepsSubtitleOutsideWithoutRequiringANewFragment(bool escapedSlot) {
		var template = "<div class=\"legacy-title text-white\"><h1 id=\"page-cover-title\" data-toc-skip>{text}</h1>" + (escapedSlot ? "{{cover_subtitle}}" : "") + "</div>";
		var renderer = new CoverTemplateRenderer(new Dictionary<string, string> { ["page_cover_title"] = template }, rejectSubtitleFragment: true);
		var html = renderer.Render(CreateCoverDocument() with { Subtitle = [new TextInline { Text = "Outside subtitle" }] }, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-cover .legacy-title.text-white"), Is.Not.Null, "Existing custom title styling must survive.");
		Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3").TextContent, Is.EqualTo("Outside subtitle"));
		Assert.That(parsed.QuerySelector("#page-cover-subtitle"), Is.Null);
		if (escapedSlot)
			Assert.That(parsed.QuerySelector(".legacy-title").TextContent, Does.Contain("{cover_subtitle}"), "An escaped literal is not an adopted subtitle slot.");
	}

	[Test]
	public void CustomCoverThatOmitsItsTitleKeepsSubtitleOutside() {
		var renderer = new CoverTemplateRenderer(new Dictionary<string, string> { ["page_cover"] = "<div class=\"ln-cover\"></div>" });
		var html = renderer.Render(CreateCoverDocument() with { Subtitle = [new TextInline { Text = "Outside subtitle" }] }, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector("#page-cover-subtitle"), Is.Null);
		Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3").TextContent, Is.EqualTo("Outside subtitle"));
	}

	[TestCase(null, true)]
	[TestCase("", true)]
	[TestCase(null, false)]
	[TestCase("", false)]
	public void NoCoverPreservesRequestedBodyTitlePolicyAndOutsideSubtitle(string coverUrl, bool titleOnCover) {
		var document = CreateCoverDocument() with {
			CoverUrl = coverUrl, TitleOnCover = titleOnCover, Subtitle = [new TextInline { Text = "Outside subtitle" }],
			Themes = ["cms", "header_cover", "header_large"]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-cover"), Is.Null);
		Assert.That(parsed.QuerySelector("#page-cover-title, #page-cover-subtitle"), Is.Null);
		Assert.That(parsed.QuerySelector("#page-title").TextContent, Is.EqualTo(titleOnCover ? string.Empty : document.Title),
			"TitleOnCover must preserve body-title suppression when a parent CMS or custom frame supplies the title without a native Page cover.");
		Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3").TextContent, Is.EqualTo("Outside subtitle"));
	}

	[Test]
	public void TitleBelowCoverKeepsSubtitleBelowCover([Values] bool useCoverTheme) {
		var document = CreateCoverDocument() with {
			TitleOnCover = false, Subtitle = [new TextInline { Text = "Outside subtitle" }],
			Themes = useCoverTheme ? ["cms", "header_cover", "header_large"] : ["cms"]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector(".ln-cover #page-cover-title"), Is.Null);
		Assert.That(parsed.QuerySelector("#page-title").TextContent, Is.EqualTo(document.Title));
		Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3").TextContent, Is.EqualTo("Outside subtitle"));
	}

	[Test]
	public void AThemeCanOwnTheNativeSubtitleWithoutAHardcodedThemeName() {
		var root = Path.Combine(Path.GetTempPath(), "localnotion-cover-subtitle-" + Guid.NewGuid().ToString("N"));
		var themePath = Path.Combine(root, "example_cover");
		try {
			Directory.CreateDirectory(themePath);
			File.WriteAllText(Path.Combine(themePath, ".config.json"), "{\"type\":\"html\",\"tokens\":{\"page_subtitle_placement\":{\"offline\":\"cover\",\"online\":\"cover\"}}}");
			File.WriteAllText(Path.Combine(themePath, "page_cover_title.html"), "{include://cover_content.inc}");
			File.WriteAllText(Path.Combine(themePath, "cover_content.inc"), "<div class=\"custom-content text-white\"><h1 id=\"page-cover-title\" data-toc-skip>{text}</h1>{cover_subtitle}</div>");
			File.WriteAllText(Path.Combine(themePath, "page_cover_subtitle.html"), "<div class=\"custom-subtitle\">{description}</div>");
			var renderer = new HtmlRenderer(new ThemeCatalog(new ThemeOptions { ThemesDirectory = root }));
			var html = renderer.Render(CreateCoverDocument() with { Themes = ["cms", "example_cover"], Subtitle = [new TextInline { Text = "Native subtitle" }] }, RendererTestData.CreateOptions()).Html;
			var parsed = new HtmlParser().ParseDocument(html);
			Assert.That(parsed.QuerySelector(".ln-cover .custom-content.text-white .custom-subtitle").TextContent, Is.EqualTo("Native subtitle"));
			Assert.That(parsed.QuerySelector(".ln-page-content .mt-n3"), Is.Null);
		} finally {
			if (Directory.Exists(root))
				Directory.Delete(root, recursive: true);
		}
	}

	private static DocumentBlock CreateCoverDocument() => new() {
		Title = "Service <details> & {title}",
		CoverUrl = "https://example.test/cover.png",
		TitleOnCover = true,
		Themes = ["cms"]
	};

	private sealed class CoverTemplateRenderer(IReadOnlyDictionary<string, string> templates, bool rejectSubtitleFragment = false) : HtmlRenderer {
		protected override string FetchTemplate(string widgetName, string fileExt) {
			if (widgetName == "page_cover_subtitle" && rejectSubtitleFragment)
				throw new ThemeException("This legacy theme does not supply a cover subtitle fragment.");
			return templates.TryGetValue(widgetName, out var template) ? template : base.FetchTemplate(widgetName, fileExt);
		}
	}

}
