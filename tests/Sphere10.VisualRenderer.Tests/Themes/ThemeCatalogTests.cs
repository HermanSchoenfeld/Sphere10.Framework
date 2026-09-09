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
using AngleSharp.Html.Parser;
using Sphere10.VisualRenderer;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ThemeCatalogTests {
	private string _temporaryRoot = null;
	private ThemeCatalog _catalog = null;

	[SetUp]
	public void SetUp() {
		_temporaryRoot = Path.Combine(Path.GetTempPath(), "localnotion-theme-tests-" + Guid.NewGuid().ToString("N"));
		_catalog = new ThemeCatalog(new ThemeOptions { ThemesDirectory = _temporaryRoot });
	}

	[TearDown]
	public void TearDown() {
		if (Directory.Exists(_temporaryRoot))
			Directory.Delete(_temporaryRoot, recursive: true);
	}

	[Test]
	public void MissingOverrideDirectoryUsesEmbeddedResourcesWithoutCreatingFiles() {
		var session = DefaultSession();
		Assert.That(Directory.Exists(_temporaryRoot), Is.False);
		Assert.That(session.Assets.Count, Is.GreaterThan(40), "Default dependency bundle includes fonts and scripts");
		Assert.That(session.Assets.Select(asset => asset.RelativePath), Has.Some.EndsWith("/MathJax_Main-Regular.woff"));
		Assert.That(session.Assets.Select(asset => asset.RelativePath), Has.Some.EndsWith("/resources/prism/prism.js"));
	}

	[TestCase("override {text}")]
	[TestCase("")]
	public void ExistingTemplateFileOverridesEmbeddedResourceWithoutPhysicalConfig(string contents) {
		Write("default/paragraph.html", contents);
		Assert.That(DefaultSession().Tokens["include://paragraph.html"], Is.EqualTo(contents));
	}

	[Test]
	public void TemplateOverridesPreserveExistingSessionAndAssetNamespace() {
		var original = DefaultSession();
		var originalTemplate = original.Tokens["include://paragraph.html"];
		var originalAssetPath = original.Assets[0].RelativePath;
		Write("default/paragraph.html", "override {text}");
		var changed = DefaultSession();
		Assert.That(changed.Tokens["include://paragraph.html"], Is.EqualTo("override {text}"));
		Assert.That(original.Tokens["include://paragraph.html"], Is.EqualTo(originalTemplate), "Existing session is a snapshot");
		Assert.That(changed.Assets[0].RelativePath, Is.EqualTo(originalAssetPath));
	}

	[Test]
	public void RemovingTemplateOverrideRestoresEmbeddedResource() {
		var originalTemplate = DefaultSession().Tokens["include://paragraph.html"];
		Write("default/paragraph.html", "override");
		Assert.That(DefaultSession().Tokens["include://paragraph.html"], Is.EqualTo("override"));
		File.Delete(Path.Combine(_temporaryRoot, "default/paragraph.html"));
		Assert.That(DefaultSession().Tokens["include://paragraph.html"], Is.EqualTo(originalTemplate));
	}

	[Test]
	public void ExpansionTreatsModelTextAsLiteral() {
		Write("default/paragraph.html", "override {text}");
		var html = DefaultSession().Expand("{include://paragraph.html}", new Dictionary<string, object> { ["text"] = "{include://never-expand-source-text}" });
		Assert.That(html, Is.EqualTo("override {include://never-expand-source-text}"));
	}

	[Test]
	public void RootGenericTemplatePrecedesEnvironmentVariant() {
		WriteCustomTheme();
		Write("custom/example.html", "root");
		Write("custom/example.offline.html", "root offline");
		Write("custom/example.online.html", "root online");
		var session = _catalog.CreateSession(["custom"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.GetTemplate("example"), Is.EqualTo("root"));
	}

	[Test]
	public void ModeAndEnvironmentSelectTemplateAndToken([Values] RenderMode mode, [Values] RenderEnvironment environment) {
		WriteCustomTheme();
		Write("custom/example.html", "root");
		Write("custom/example.offline.html", "root offline");
		Write("custom/example.online.html", "root online");
		Write("custom/readonly/example.html", "readonly");
		Write("custom/editable/example.html", "editable");
		Write("custom/readonly/example.offline.html", "readonly offline");
		Write("custom/readonly/example.online.html", "readonly online");
		Write("custom/editable/example.offline.html", "editable offline");
		Write("custom/editable/example.online.html", "editable online");
		var session = _catalog.CreateSession(["custom"], environment, mode);
		Assert.That(session.GetTemplate("example"), Is.EqualTo(mode.ToString().ToLowerInvariant() + " " + environment.ToString().ToLowerInvariant()));
		Assert.That(session.Tokens["Custom"], Is.EqualTo(environment == RenderEnvironment.Offline ? "OFF" : "ON"));
	}

	[TestCase("first", "second", "second")]
	[TestCase("second", "first", "first")]
	public void LaterSelectedThemeOverridesInheritedAndPreviouslySelectedThemes(string firstTheme, string secondTheme, string expected) {
		Write("first/.config.json", """{"type":"html"}""");
		Write("first/color.inc", "first");
		Write("second/.config.json", """{"type":"html","base":"first"}""");
		Write("second/color.inc", "second");
		var session = _catalog.CreateSession([firstTheme, secondTheme], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.Tokens["include://color.inc"], Is.EqualTo(expected));
	}

	[TestCase("{include://nest.inc}", "nested OFF")]
	[TestCase("{{Custom}}", "{Custom}")]
	public void ExpansionHandlesNestedIncludesAndEscapedBraces(string template, string expected) {
		WriteCustomTheme();
		Write("custom/nest.inc", "nested {include://leaf.inc}");
		Write("custom/leaf.inc", "{Custom}");
		var session = _catalog.CreateSession(["custom"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.Expand(template), Is.EqualTo(expected));
	}

	[Test]
	public void ExpansionResolvesLocalTokenWithinIncludePath() {
		WriteCustomTheme();
		Write("custom/leaf.inc", "{Custom}");
		var session = _catalog.CreateSession(["custom"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(session.Expand("{include://{part}.inc}", new Dictionary<string, object> { ["part"] = "leaf" }), Is.EqualTo("OFF"));
	}

	[Test]
	public void ExpansionFormatsLocalValues() {
		var session = DefaultSession();
		Assert.That(session.Expand("{created:yyyy-MM-dd}", new Dictionary<string, object> { ["created"] = new DateTime(2024, 1, 2) }), Is.EqualTo("2024-01-02"));
	}

	[Test]
	public void ExpansionRejectsIncludeCycle() {
		WriteCustomTheme();
		Write("custom/nest.inc", "nested {include://leaf.inc}");
		Write("custom/leaf.inc", "{include://nest.inc}");
		var session = _catalog.CreateSession(["custom"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(() => session.Expand("{include://nest.inc}"), Throws.InstanceOf<ThemeException>());
	}

	[Test]
	public void ExpansionRejectsMissingInclude() {
		var session = DefaultSession();
		Assert.That(() => session.Expand("{include://missing.inc}"), Throws.InstanceOf<ThemeException>());
	}

	[Test]
	public void GetTemplateRejectsMissingTemplate() {
		var session = DefaultSession();
		Assert.That(() => session.GetTemplate("missing-template"), Throws.InstanceOf<ThemeException>());
	}

	[Test]
	public void CreateSessionRejectsInheritanceCycle() {
		Write("cycle/.config.json", """{"type":"html","base":"cycle"}""");
		Assert.That(() => _catalog.CreateSession(["cycle"], RenderEnvironment.Offline, RenderMode.ReadOnly), Throws.InstanceOf<ThemeException>());
	}

	[Test]
	public void CorruptConfigOverrideDoesNotFallBackToEmbeddedConfig() {
		Write("default/.config.json", "{ broken");
		Assert.That(() => DefaultSession(), Throws.InstanceOf<ThemeException>());
	}

	[TestCase("no-such-theme")]
	[TestCase("../escape")]
	public void CreateSessionRejectsUnknownOrInvalidTheme(string theme) {
		Assert.That(() => _catalog.CreateSession([theme], RenderEnvironment.Offline, RenderMode.ReadOnly), Throws.InstanceOf<ThemeException>());
	}

	[Test]
	public void OnlineDefaultsUseCdnWithoutLocalAssetBundle() {
		var session = _catalog.CreateSession(["default"], RenderEnvironment.Online, RenderMode.ReadOnly);
		Assert.That(session.Tokens["theme://resources/local-notion/css/ln.css"].ToString(), Does.StartWith("https://cdn.jsdelivr.net/"));
		Assert.That(session.Assets, Is.Empty);
	}

	[Test]
	public void OfflineAssetOverrideUsesCallerUrlAndDistinctBundleWithoutChangingExistingSession() {
		var original = DefaultSession();
		var originalAssetPath = original.Assets[0].RelativePath;
		Write("default/resources/local-notion/css/ln.css", "body { color: rebeccapurple; }");
		var session = _catalog.CreateSession(["default"], RenderEnvironment.Offline, RenderMode.ReadOnly, "https://example.test/render-assets/");
		Assert.That(session.Assets.Count, Is.GreaterThan(40), "Offline override carries a coherent dependency bundle");
		Assert.That(session.Tokens["theme://resources/local-notion/css/ln.css"].ToString(), Does.StartWith("https://example.test/render-assets/assets/"));
		Assert.That(session.Assets[0].RelativePath, Is.Not.EqualTo(originalAssetPath));
		Assert.That(session.Assets.Select(asset => Encoding.UTF8.GetString(asset.Content.Span)), Does.Contain("body { color: rebeccapurple; }"));
		Assert.That(original.Assets[0].RelativePath, Is.EqualTo(originalAssetPath));
		File.Delete(Path.Combine(_temporaryRoot, "default/resources/local-notion/css/ln.css"));
		var restored = DefaultSession();
		Assert.That(restored.Assets.Count, Is.EqualTo(original.Assets.Count));
		Assert.That(restored.Assets[0].RelativePath, Is.EqualTo(originalAssetPath));
	}

	[TestCase("default", "resources/local-notion/css/ln.css", false)]
	[TestCase("default", "resources/local-notion/css/ln.css", true)]
	[TestCase("cms", "resources/js/feather.min.js", false)]
	[TestCase("cms", "resources/js/feather.min.js", true)]
	public void OnlineAssetOverridesPreserveOwningThemeCdn(string theme, string assetPath, bool changeContents) {
		var original = _catalog.CreateSession(["cms"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		var asset = original.Assets.Single(item => item.RelativePath.EndsWith("/" + assetPath, StringComparison.Ordinal));
		var contents = changeContents ? Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(asset.Content.Span) + "\n/* changed override */") : asset.Content.ToArray();
		Write(theme + "/" + assetPath, contents);
		var session = _catalog.CreateSession(["cms"], RenderEnvironment.Online, RenderMode.ReadOnly, "/.localnotion/render-assets");
		Assert.That(session.Tokens["theme://" + assetPath], Is.EqualTo("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/" + theme + "/" + assetPath));
		Assert.That(session.Tokens["theme://resources/local-notion/js/ln.js"], Is.EqualTo("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/local-notion/js/ln.js"));
		Assert.That(session.Tokens["theme://resources/js/cms_post.js"], Is.EqualTo("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/cms/resources/js/cms_post.js"));
		Assert.That(session.Assets, Is.Empty, "Disk overrides do not replace an explicitly configured online asset location");
	}

	[Test]
	public void OnlineTemplateOverrideIsExpandedInlineWithCdnAssetUrls() {
		Write("default/paragraph.html", "<p class=\"custom\">{text}</p><script src=\"{theme://resources/local-notion/js/ln.js}\"></script>");
		var session = _catalog.CreateSession(["default"], RenderEnvironment.Online, RenderMode.ReadOnly, "/.localnotion/render-assets");
		var html = session.Expand("{include://paragraph.html}", new Dictionary<string, object> { ["text"] = "Overridden template" });
		Assert.That(html, Is.EqualTo("<p class=\"custom\">Overridden template</p><script src=\"https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/local-notion/js/ln.js\"></script>"));
		Assert.That(session.Assets, Is.Empty);
	}

	[Test]
	public void OnlineCmsHtmlUsesOwningThemeCdnWithDiskOverrides() {
		Write("default/resources/local-notion/js/ln.js", "/* default override */");
		Write("cms/resources/js/feather.min.js", "/* cms override */");
		var document = new DocumentBlock { Title = "CMS CDN regression", Themes = ["cms"] };
		var result = new HtmlRenderer(_catalog).Render(document, new RenderOptions { Environment = RenderEnvironment.Online, AssetBaseUrl = "/.localnotion/render-assets" });
		var html = new HtmlParser().ParseDocument(result.Html);
		var scripts = html.QuerySelectorAll("script[src]").Select(script => script.GetAttribute("src")).ToArray();
		Assert.That(scripts, Does.Contain("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/local-notion/js/ln.js"));
		Assert.That(scripts, Does.Contain("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/cms/resources/js/feather.min.js"));
		Assert.That(scripts, Does.Contain("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/cms/resources/js/cms_post.js"));
		Assert.That(result.Html, Does.Not.Contain("/.localnotion/render-assets/"));
		Assert.That(result.Html, Does.Not.Contain("{theme://"));
		Assert.That(result.Assets, Is.Empty);
	}

	[Test]
	public void DiskOnlyAssetsUseConfiguredCustomCdnOnline() {
		Write("custom/.config.json", """{"type":"html","base":"default","online_url":"https://custom.test/theme"}""");
		Write("custom/resources/icon.bin", "custom");
		var session = _catalog.CreateSession(["custom"], RenderEnvironment.Online, RenderMode.ReadOnly, "../render-assets");
		Assert.That(session.Tokens["theme://resources/icon.bin"], Is.EqualTo("https://custom.test/theme/resources/icon.bin"));
		Assert.That(session.Tokens["theme://resources/local-notion/js/ln.js"], Is.EqualTo("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/local-notion/js/ln.js"));
		Assert.That(session.Assets, Is.Empty);
	}

	[TestCase(RenderEnvironment.Offline)]
	[TestCase(RenderEnvironment.Online)]
	public void DiskOnlyAssetsWithoutCdnUseCallerOutput(RenderEnvironment environment) {
		Write("custom/.config.json", """{"type":"html"}""");
		Write("custom/resources/icon.bin", "custom");
		var session = _catalog.CreateSession(["custom"], environment, RenderMode.ReadOnly, "../render-assets");
		Assert.That(session.Assets, Has.Count.EqualTo(1));
		Assert.That(session.Tokens["theme://resources/icon.bin"], Is.EqualTo("../render-assets/" + session.Assets[0].RelativePath));
		Assert.That(Encoding.UTF8.GetString(session.Assets[0].Content.Span), Is.EqualTo("custom"));
	}

	[Test]
	public void OnlineMixedCdnAndLocalThemesKeepCdnUrlsAndCoherentLocalBundle() {
		Write("custom/.config.json", """{"type":"html"}""");
		Write("custom/resources/icon.bin", "custom");
		Write("default/resources/local-notion/css/ln.css", "body { color: rebeccapurple; }");
		var session = _catalog.CreateSession(["default", "custom"], RenderEnvironment.Online, RenderMode.ReadOnly, "../render-assets");
		var icon = session.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/icon.bin", StringComparison.Ordinal));
		Assert.That(session.Tokens["theme://resources/icon.bin"], Is.EqualTo("../render-assets/" + icon.RelativePath));
		Assert.That(session.Tokens["theme://resources/local-notion/css/ln.css"], Is.EqualTo("https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/local-notion/css/ln.css"));
		Assert.That(session.Assets.Count, Is.GreaterThan(40), "Local assets retain the complete dependency tree");
		Assert.That(session.Assets.Select(asset => asset.RelativePath), Has.Some.EndsWith("/MathJax_Main-Regular.woff"));
		Assert.That(session.Assets.Select(asset => Encoding.UTF8.GetString(asset.Content.Span)), Does.Contain("body { color: rebeccapurple; }"));
	}

	[Test]
	public void ConfiguredOnlineUrlAppliesToEmbeddedAssets() {
		Write("default/.config.json", """{"type":"html","base":".root","online_url":"https://custom.test/theme"}""");
		var session = _catalog.CreateSession(["default"], RenderEnvironment.Online, RenderMode.ReadOnly);
		Assert.That(session.Tokens["theme://resources/local-notion/css/ln.css"], Is.EqualTo("https://custom.test/theme/resources/local-notion/css/ln.css"));
	}

	[Test]
	public void SnapshotFreezesTemplateBytesAcrossFragmentThemes() {
		WriteCustomTheme();
		Write("default/paragraph.html", "frozen {contents}");
		var snapshot = _catalog.CreateSnapshot();
		Write("default/paragraph.html", "changed {contents}");
		Assert.That(snapshot.CreateSession(["default"], RenderEnvironment.Offline, RenderMode.ReadOnly).Tokens["include://paragraph.html"], Is.EqualTo("frozen {contents}"));
		Assert.That(snapshot.CreateSession(["custom"], RenderEnvironment.Offline, RenderMode.ReadOnly).Tokens["include://paragraph.html"], Is.EqualTo("frozen {contents}"));
		Assert.That(DefaultSession().Tokens["include://paragraph.html"], Is.EqualTo("changed {contents}"));
	}

	[Test]
	public void ImmutableSnapshotReusesSessionAcrossConcurrentRequests() {
		Write("default/paragraph.html", "frozen {contents}");
		var snapshot = _catalog.CreateSnapshot();
		var sessions = Enumerable.Range(0, 8).AsParallel()
			.Select(_ => snapshot.CreateSession(["default"], RenderEnvironment.Offline, RenderMode.ReadOnly, "/assets")).ToArray();
		Assert.That(sessions, Is.All.SameAs(sessions[0]));
		Assert.That(sessions[0].Expand(sessions[0].GetTemplate("paragraph")), Is.EqualTo("frozen {contents}"));
	}

	[Test]
	public void EmbeddedCatalogReusesImmutableSessionWithoutCreatingOverrideFiles() {
		var catalog = new ThemeCatalog();
		var first = catalog.CreateSession(["default"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(catalog.CreateSession(["default"], RenderEnvironment.Offline, RenderMode.ReadOnly), Is.SameAs(first));
		Assert.That(Directory.Exists(_temporaryRoot), Is.False);
	}

	[Test]
	public void SnapshotSessionCachePreservesOrderedThemeOverrides() {
		Write("first/.config.json", """{"type":"html"}""");
		Write("first/example.html", "first");
		Write("second/.config.json", """{"type":"html"}""");
		Write("second/example.html", "second");
		var snapshot = _catalog.CreateSnapshot();
		var first = snapshot.CreateSession(["first", "second"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		var second = snapshot.CreateSession(["second", "first"], RenderEnvironment.Offline, RenderMode.ReadOnly);
		Assert.That(first.GetTemplate("example"), Is.EqualTo("second"));
		Assert.That(second.GetTemplate("example"), Is.EqualTo("first"));
	}

	[Test]
	public void SnapshotSessionCacheSeparatesRenderSettings([Values] RenderEnvironment environment, [Values] RenderMode mode) {
		WriteCustomTheme();
		Write("custom/resources/test.txt", "asset");
		var snapshot = _catalog.CreateSnapshot();
		var first = snapshot.CreateSession(["custom"], RenderEnvironment.Offline, RenderMode.ReadOnly, "/first");
		var second = snapshot.CreateSession(["custom"], environment, mode, "/second");
		Assert.That(first.Tokens["theme://resources/test.txt"].ToString(), Does.StartWith("/first/"));
		var expectedAssetUrl = environment == RenderEnvironment.Offline
			? "/second/" + second.Assets.Single(asset => asset.RelativePath.EndsWith("/resources/test.txt", StringComparison.Ordinal)).RelativePath
			: "https://cdn.jsdelivr.net/gh/sphere10/cdn/local-notion/themes/default/resources/test.txt";
		Assert.That(second.Tokens["theme://resources/test.txt"], Is.EqualTo(expectedAssetUrl));
		Assert.That(second.Tokens["render_mode"], Is.EqualTo(mode == RenderMode.Editable ? "editable" : "readonly"));
		Assert.That(second.Tokens["Custom"], Is.EqualTo(environment == RenderEnvironment.Offline ? "OFF" : "ON"));
	}

	private ThemeSession DefaultSession() => _catalog.CreateSession(["default"], RenderEnvironment.Offline, RenderMode.ReadOnly);

	private void WriteCustomTheme() {
		Write("custom/.config.json", """{"type":"html","base":"default","tokens":{"Custom":{"offline":"OFF","online":"ON"}}}""");
	}

	private void Write(string path, string contents) => Write(path, Encoding.UTF8.GetBytes(contents));

	private void Write(string path, byte[] contents) {
		var fullPath = Path.Combine(_temporaryRoot, path.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
		File.WriteAllBytes(fullPath, contents);
	}
}