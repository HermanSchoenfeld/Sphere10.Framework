// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DocumentStandardsRenderingTests {
	[Test]
	public void FullDocumentUsesStandardsMode([Values("default", "cms")] string theme, [Values] RenderEnvironment environment, [Values] RenderMode mode) {
		var document = new DocumentBlock {
			Title = "Standards document",
			Children = [new ParagraphBlock { Text = [new TextInline { Text = "Preserved content" }] }]
		};
		var result = new HtmlRenderer().Render(document, new RenderOptions { Themes = [theme], Environment = environment, Mode = mode });
		var parsed = new HtmlParser().ParseDocument(result.Html);

		Assert.That(result.Html.TrimStart('\uFEFF'), Does.StartWith("<!DOCTYPE html>"));
		Assert.That(Regex.Matches(result.Html, "<!doctype", RegexOptions.IgnoreCase).Count, Is.EqualTo(1));
		Assert.That(parsed.Doctype?.Name, Is.EqualTo("html"));
		Assert.That(parsed.CompatMode, Is.EqualTo("CSS1Compat"));
		Assert.That(parsed.Body.TextContent, Does.Contain("Preserved content"));
	}

	[Test]
	public void DatabaseFrameUsesStandardsMode([Values] RenderEnvironment environment) {
		var session = new ThemeCatalog().CreateSession(["default"], environment, RenderMode.ReadOnly);
		var innerSlot = environment == RenderEnvironment.Online ? "include://page_database.online.html" : "include://page_database.offline.html";
		var result = session.Expand(session.GetTemplate("page_database"), new Dictionary<string, object> {
			[innerSlot] = "<head><title>Database</title></head><body><p>Preserved database content</p></body>"
		});
		var parsed = new HtmlParser().ParseDocument(result);

		Assert.That(result, Does.StartWith("<!DOCTYPE html>"));
		Assert.That(Regex.Matches(result, "<!doctype", RegexOptions.IgnoreCase).Count, Is.EqualTo(1));
		Assert.That(parsed.Doctype?.Name, Is.EqualTo("html"));
		Assert.That(parsed.CompatMode, Is.EqualTo("CSS1Compat"));
		Assert.That(parsed.QuerySelector("p")?.TextContent, Is.EqualTo("Preserved database content"));
	}

	[Test]
	public void FragmentsKeepTheirExistingNoFrameContract([Values] RenderEnvironment environment, [Values] RenderMode mode, [Values] bool snippet) {
		var document = new DocumentBlock {
			RenderFrame = snippet,
			ShowPageHeader = false,
			Children = [new ParagraphBlock { Text = [new TextInline { Text = "Fragment content" }] }]
		};
		var renderer = new HtmlRenderer();
		var options = new RenderOptions { Environment = environment, Mode = mode };
		var result = snippet ? renderer.RenderSnippet(document, options) : renderer.Render(document, options);

		Assert.That(result.Html, Does.Contain("Fragment content"));
		Assert.That(result.Html, Does.Not.Contain("<!DOCTYPE").IgnoreCase.And.Not.Contain("<html").And.Not.Contain("<head").And.Not.Contain("<body"));
	}

	[Test]
	public void SelectedFrameModifierPreservesInnerCmsComposition([Values] RenderEnvironment environment) {
		var directory = Path.Combine(Path.GetTempPath(), "visual-renderer-standards-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(directory, "standards", "readonly"));
		try {
			File.WriteAllText(Path.Combine(directory, "standards", ".config.json"), """{"type":"html"}""");
			var variant = environment == RenderEnvironment.Online ? "online" : "offline";
			File.WriteAllText(Path.Combine(directory, "standards", "readonly", "page." + variant + ".html"),
				"<!DOCTYPE html><html lang=\"en\" data-frame-owner=\"modifier\">{include://page." + variant + ".html}</html>");
			var document = new DocumentBlock {
				Title = "Selected Article",
				Tokens = new Dictionary<string, string> { ["html_head_start"] = "", ["google_tag"] = "", ["html_head_end"] = "<meta name=\"preserved-head-slot\" content=\"existing-composition\">" },
				Children = [new ParagraphBlock { Text = [new TextInline { Text = "Preserved Article body" }] }]
			};
			var result = new HtmlRenderer(new ThemeCatalog(new ThemeOptions { ThemesDirectory = directory })).Render(document,
				new RenderOptions { Themes = ["cms", "standards"], Environment = environment });
			var parsed = new HtmlParser().ParseDocument(result.Html);

			Assert.That(parsed.CompatMode, Is.EqualTo("CSS1Compat"));
			Assert.That(parsed.DocumentElement.GetAttribute("data-frame-owner"), Is.EqualTo("modifier"));
			Assert.That(parsed.Head.QuerySelector("meta[name=preserved-head-slot]")?.GetAttribute("content"), Is.EqualTo("existing-composition"));
			Assert.That(parsed.QuerySelector("main")?.TextContent, Does.Contain("Preserved Article body"));
			Assert.That(Regex.Matches(result.Html, "<!doctype", RegexOptions.IgnoreCase).Count, Is.EqualTo(1));
		} finally {
			Directory.Delete(directory, recursive: true);
		}
	}
}
