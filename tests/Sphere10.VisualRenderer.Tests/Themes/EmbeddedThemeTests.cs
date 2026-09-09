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
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using AngleSharp.Html.Parser;
using Sphere10.VisualRenderer;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EmbeddedThemeTests {
	private static readonly Lazy<HtmlRenderResult> _renderedDemo = new(() => new HtmlRenderer().Render(CreateDemo(), new RenderOptions { AssetBaseUrl = "/render-assets" }));

	[Test]
	public void RendererAssemblyHasNoNotionOrHtmlParserDependencies() {
		Assert.That(typeof(ThemeCatalog).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name), Has.None.EqualTo("LocalNotion.Core"));
		Assert.That(typeof(ThemeCatalog).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name), Has.None.EqualTo("Notion.Client"));
		Assert.That(typeof(ThemeCatalog).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name), Has.None.EqualTo("AngleSharp"));
	}

	[Test]
	public void ManifestIncludesAllNamedThemesAndHiddenConfigs() {
		Assert.That(BundledThemes().Count(), Is.GreaterThan(200));
	}

	[TestCaseSource(nameof(BundledThemes))]
	public void BuiltInThemeHasDependencyBundleAndResolvesTemplateAssetTokens(string theme) {
		var session = new ThemeCatalog().CreateSession(["cms", theme], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.Assets.Count, Is.GreaterThan(40), "Bundle exists for built-in theme " + theme);
		foreach (var (template, token) in AssetReferences(session))
			Assert.That(session.Tokens.ContainsKey(token), Is.True, theme + " template " + template + " references " + token);
	}

	[TestCase("default")]
	[TestCase("cms")]
	[TestCase("cms_gallery")]
	[TestCase("cms_articles")]
	public void OfflineTemplateAssetsAreIncludedInOutputBundle(string theme) {
		var session = new ThemeCatalog().CreateSession([theme], RenderEnvironment.Offline, RenderMode.ReadOnly);
		var assetPaths = session.Assets.Select(asset => asset.RelativePath).ToArray();
		foreach (var (template, token) in AssetReferences(session)) {
			Assert.That(session.Tokens.ContainsKey(token), Is.True, theme + " template " + template + " references " + token);
			Assert.That(assetPaths, Does.Contain(session.Tokens[token].ToString()), "Referenced offline asset is included in the bundle");
		}
	}

	[Test]
	public void ArticleWidgetsPreserveFinalUrl(
		[Values("articles_summary", "articles_summary_alt", "articles_category", "articles_category_active")] string widget,
		[Values("../cms/article.html", "https://example.test/articles/a")] string url
	) {
		var session = new ThemeCatalog().CreateSession(["cms_articles"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		var html = session.Expand(session.GetTemplate(widget), new Dictionary<string, object> { ["slug"] = "articles/a", ["url"] = url });
		Assert.That(html, Does.Contain("href=\"" + url + "\""));
		Assert.That(html, Does.Not.Contain("href=\"/" + url + "\""));
	}

	[TestCase("cms")]
	[TestCase("cms_gallery")]
	[TestCase("cms_articles")]
	public void OfflineCmsFramesContainRenderedDocument(string theme) {
		var result = new HtmlRenderer().Render(CreateDemo() with { Themes = [theme] }, new RenderOptions { Environment = RenderEnvironment.Offline });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var main = parsed.QuerySelector("main");
		Assert.That(main, Is.Not.Null);
		Assert.That(main.TextContent, Does.Contain("A page served entirely from memory."));
		Assert.That(main.TextContent, Does.Not.Contain("{content}"));
	}

	[Test]
	public void OnlineRenderingUsesOwningThemeCdnWithoutThemeFiles([Values("default", "cms")] string theme, [Values] bool specifyMissingThemesDirectory) {
		var missingDirectory = Path.Combine(Path.GetTempPath(), "localnotion-online-theme-tests-" + Guid.NewGuid().ToString("N"));
		var catalog = specifyMissingThemesDirectory ? new ThemeCatalog(new ThemeOptions { ThemesDirectory = missingDirectory }) : new ThemeCatalog();
		Assert.That(Directory.Exists(missingDirectory), Is.False);
		var result = new HtmlRenderer(catalog).Render(
			CreateDemo() with { Themes = [theme] },
			new RenderOptions { Environment = RenderEnvironment.Online, AssetBaseUrl = "/.localnotion/render-assets" }
		);
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var assetUrls = parsed.QuerySelectorAll("script[src], link[rel=stylesheet][href]")
			.Select(element => element.GetAttribute("src") ?? element.GetAttribute("href"))
			.ToArray();
		string[] defaultAssets = [
			"resources/bootstrap/bootstrap.min.css",
			"resources/datatables/datatables.min.css",
			"resources/prism/prism.css",
			"resources/local-notion/css/ln.css",
			"resources/local-notion/css/readonly.css",
			"resources/jquery/jquery-3.7.1.min.js",
			"resources/bootstrap/bootstrap.bundle.min.js",
			"resources/local-notion/js/ln.js",
			"resources/datatables/datatables.min.js",
			"resources/mathjax/es5/tex-mml-chtml.js",
			"resources/prism/prism.js",
			"resources/x/widgets.js"
		];
		string[] cmsAssets = [
			"resources/css/aos.css",
			"resources/css/cms.css",
			"resources/js/cms_pre.js",
			"resources/js/feather.min.js",
			"resources/js/aos.min.js",
			"resources/js/typed.umd.js",
			"resources/js/imagesloaded.pkgd.min.js",
			"resources/js/isotope.pkgd.min.js",
			"resources/js/BigPicture.min.js",
			"resources/js/cms_post.js"
		];
		const string cdnBaseUrl = "https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/";
		var expectedUrls = defaultAssets.Select(path => cdnBaseUrl + "default/" + path);
		if (theme == "cms")
			expectedUrls = expectedUrls.Concat(cmsAssets.Select(path => cdnBaseUrl + "cms/" + path));
		Assert.That(assetUrls, Is.EquivalentTo(expectedUrls), "Every stylesheet and script must use its owning theme's CDN, including inherited default assets");
		Assert.That(parsed.QuerySelector("main").TextContent, Does.Contain("A page served entirely from memory."));
		Assert.That(result.Assets, Is.Empty, "Standalone online rendering requires no exported local assets");
		Assert.That(result.Html, Does.Not.Contain("render-assets"));
		Assert.That(result.Html, Does.Not.Contain("file://"));
		Assert.That(result.Html, Does.Not.Contain("{theme://"));
		Assert.That(Directory.Exists(missingDirectory), Is.False, "Rendering must not deploy the embedded themes to disk");
	}

	[Test]
	public void BuiltInTemplatesPreserveNumberedListStart() {
		Assert.That(_renderedDemo.Value.Html, Does.Contain("start=\"7\""));
	}

	[TestCaseSource(nameof(AssetRoutes))]
	public async Task RenderedAssetCanBeServedFromMemoryWithCorrectMimeTypeAndBytes(string url) {
		var memoryAssets = _renderedDemo.Value.Assets.ToDictionary(asset => "/render-assets/" + asset.RelativePath);
		using var client = new HttpClient(new MemoryAssetHandler(memoryAssets));
		using var response = await client.GetAsync("https://memory.test" + url);
		Assert.That(response.IsSuccessStatusCode, Is.True, "In-memory asset route " + url);
		var expected = memoryAssets[Uri.UnescapeDataString(url)];
		Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(expected.ContentType));
		Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.EqualTo(expected.Content.ToArray()));
	}

	[TestCaseSource(nameof(StaticAssets))]
	public void AssetBytesSurviveStaticFileOutput(string relativePath) {
		var asset = _renderedDemo.Value.Assets.Single(item => item.RelativePath == relativePath);
		var outputDirectory = Path.Combine(Path.GetTempPath(), "localnotion-asset-tests-" + Guid.NewGuid().ToString("N"));
		try {
			var destination = Path.Combine(outputDirectory, asset.RelativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(destination));
			File.WriteAllBytes(destination, asset.Content.ToArray());
			Assert.That(File.ReadAllBytes(destination), Is.EqualTo(asset.Content.ToArray()));
		} finally {
			if (Directory.Exists(outputDirectory))
				Directory.Delete(outputDirectory, recursive: true);
		}
	}

	public static IEnumerable<string> BundledThemes() {
		using var manifestStream = typeof(ThemeCatalog).Assembly.GetManifestResourceStream("Microsoft.Extensions.FileProviders.Embedded.Manifest.xml");
		var manifest = XDocument.Load(manifestStream);
		var themeRoot = manifest.Descendants("Directory").Single(directory => directory.Attribute("Name")?.Value == "Themes");
		return themeRoot.Elements("Directory")
			.Where(directory => directory.Elements("File").Any(file => file.Attribute("Name")?.Value == ".config.json"))
			.Select(directory => directory.Attribute("Name").Value)
			.ToArray();
	}

	public static IEnumerable<TestCaseData> AssetRoutes() {
		return Regex.Matches(_renderedDemo.Value.Html, "(?:href|src)=\"(/render-assets/[^\"]+)\"")
			.Select(reference => reference.Groups[1].Value)
			.Distinct()
			.Select(url => new TestCaseData(url).SetName("RenderedAssetCanBeServedFromMemoryWithCorrectMimeTypeAndBytes(" + Path.GetFileName(url) + ")"));
	}

	public static IEnumerable<TestCaseData> StaticAssets() {
		return _renderedDemo.Value.Assets.Select(asset => new TestCaseData(asset.RelativePath).SetName("AssetBytesSurviveStaticFileOutput(" + asset.RelativePath + ")"));
	}

	private static IEnumerable<(string Template, string Token)> AssetReferences(ThemeSession session) {
		foreach (var (key, value) in session.Tokens.Where(item => item.Key.StartsWith("include://") && (item.Key.EndsWith(".html") || item.Key.EndsWith(".inc")))) {
			var text = value.ToString().Replace("{render_mode}", "readonly");
			foreach (Match reference in Regex.Matches(text, @"\{theme://([^{}]+)\}"))
				yield return (key, "theme://" + reference.Groups[1].Value);
		}
	}

	private static DocumentBlock CreateDemo() => new() {
		Title = "Standalone embedded renderer",
		Children = [
			new ParagraphBlock { Text = [new TextInline { Text = "A page served entirely from memory." }] },
			new CodeBlock { Language = CodeLanguage.CSharp, Code = "Console.WriteLine(42);" },
			new EquationBlock { Expression = "x^2 + y^2 = z^2" },
			new ListBlock { Type = ListType.Numbered, Start = 7, Items = [new ListItemBlock { Text = [new TextInline { Text = "Seventh item" }] }] }
		]
	};
}