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
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RenderingIsolationTests {
	[Test]
	public void RepeatedRenderingIsDeterministic() {
		var renderer = new HtmlRenderer();
		DocumentBlock document = RendererTestData.CreateDocument();
		var options = RendererTestData.CreateOptions();
		var first = renderer.Render(document, options).Html;
		Assert.That(renderer.Render(document, options).Html, Is.EqualTo(first));
	}

	[Test]
	public void MissingThemeDirectoryUsesEmbeddedAssetsWithoutCreatingFiles() {
		var missingThemes = Path.Combine(Path.GetTempPath(), "renderer-missing-" + Guid.NewGuid().ToString("N"));
		try {
			var renderer = new HtmlRenderer(new ThemeCatalog(new ThemeOptions { ThemesDirectory = missingThemes }));
			var result = renderer.Render(RendererTestData.CreateDocument(), RendererTestData.CreateOptions());
			Assert.That(result.Assets, Is.Not.Empty);
			Assert.That(result.Assets.Select(asset => asset.RelativePath), Is.Unique);
			Assert.That(Directory.Exists(missingThemes), Is.False);
			var assetPaths = result.Assets.Select(asset => "/render-assets/" + asset.RelativePath).ToHashSet(StringComparer.Ordinal);
			var document = new HtmlParser().ParseDocument(result.Html);
			var referencedAssets = document.QuerySelectorAll("script[src],link[href]")
				.Select(element => element.GetAttribute("src") ?? element.GetAttribute("href"))
				.Where(source => source.StartsWith("/render-assets/", StringComparison.Ordinal));
			Assert.That(referencedAssets, Is.SubsetOf(assetPaths), "Each generated resource URL must have a matching asset.");
		} finally {
			var fullPath = Path.GetFullPath(missingThemes);
			if (!fullPath.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Fixture path escaped its temporary parent.");
			if (Directory.Exists(fullPath))
				Directory.Delete(fullPath, true);
		}
	}

	[Test]
	public void TypedTemplateSlotsRenderWithoutLeakingThemesOrTokensToNextCall() {
		var renderer = new HtmlRenderer();
		DocumentBlock document = RendererTestData.CreateDocument();
		var options = RendererTestData.CreateOptions();
		var originalHtml = renderer.Render(document, options).Html;
		DocumentBlock scoped = new DocumentBlock {
			Title = "Composition",
			ShowPageHeader = false,
			Slots = new Dictionary<string, VisualNode> {
				["page_header"] = new DocumentBlock {
					RenderFrame = false,
					ShowPageHeader = false,
					Themes = ["embedded"],
					Children = [new RawHtmlBlock { Html = "<header>Scoped fragment</header>" }]
				}
			},
			Children = [
				new TemplateBlock {
					Template = "paragraph",
					Tokens = new Dictionary<string, string> { ["color"] = "default", ["children"] = "" },
					Slots = new Dictionary<string, VisualNode> {
						["contents"] = new GroupBlock { Children = [new ParagraphBlock { Text = [new TextInline { Text = "Typed slot" }] }] }
					}
				}
			]
		};
		Assert.That(renderer.Render(scoped, options).Html, Does.Contain("Typed slot"));
		Assert.That(renderer.Render(document, options).Html, Is.EqualTo(originalHtml));
	}

	[Test]
	public async Task ConcurrentCallsUseIsolatedRenderingState() {
		var renderer = new HtmlRenderer();
		DocumentBlock document = RendererTestData.CreateDocument();
		var options = RendererTestData.CreateOptions();
		var expectedHtml = renderer.Render(document, options).Html;
		var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => renderer.Render(document, options).Html)));
		Assert.That(results, Is.All.EqualTo(expectedHtml));
	}

	[Test]
	public void ContainmentCyclesFailClearly([Values(false, true)] bool htmlRenderer) {
		var children = new VisualNode[1];
		GroupBlock cycle = new GroupBlock { Children = children };
		children[0] = cycle;
		DocumentBlock document = new DocumentBlock { Children = [cycle] };
		Action render = htmlRenderer
			? () => new HtmlRenderer().Render(document, RendererTestData.CreateOptions())
			: () => new TextRenderer().Render(document);
		Assert.That(render, Throws.TypeOf<InvalidOperationException>().With.Message.Contains("cycle").IgnoreCase);
	}
}
