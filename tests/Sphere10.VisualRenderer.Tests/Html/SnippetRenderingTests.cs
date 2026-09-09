// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SnippetRenderingTests {
	[Test]
	public void CalloutSnippetPreservesNestedContentAndEncodesText() {
		IRenderer<HtmlRenderResult> renderer = new HtmlRenderer();
		var callout = new CalloutBlock {
			Metadata = new Metadata { Anchor = "notice" },
			Icon = new Icon { Emoji = "💡" },
			Text = [new TextInline { Text = "Keep <script>literal</script> & useful", Style = new TextStyle { Bold = true } }],
			Children = [new ToDoBlock { Text = [new TextInline { Text = "Nested task" }], IsChecked = true }]
		};

		var result = renderer.RenderSnippet(callout, new RenderOptions { Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelector("#notice")?.TextContent, Does.Contain("Keep <script>literal</script> & useful").And.Contain("💡"));
		Assert.That(parsed.QuerySelector("#notice b")?.TextContent, Is.EqualTo("Keep <script>literal</script> & useful"));
		Assert.That(parsed.QuerySelector("#notice .ln-callout-children input[checked]"), Is.Not.Null);
		Assert.That(parsed.QuerySelector("#notice .ln-callout-children")?.TextContent, Does.Contain("Nested task"));
		Assert.That(parsed.QuerySelectorAll("script"), Is.Empty);
		Assert.That(result.Html, Does.Contain("&lt;script&gt;").And.Not.Contain("<html").And.Not.Contain("<head").And.Not.Contain("<body").And.Not.Contain("ln-page-header"));
		Assert.That(result.Assets, Is.Empty);
	}

	[Test]
	public void ToDoSnippetPreservesCheckedState([Values] bool isChecked) {
		var todo = new ToDoBlock {
			Metadata = new Metadata { Anchor = "task" },
			Text = [new TextInline { Text = "Review <draft> & publish" }],
			IsChecked = isChecked
		};
		var result = new HtmlRenderer().RenderSnippet(todo, new RenderOptions { Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var checkbox = parsed.QuerySelector("#task input[type=checkbox]");
		Assert.That(checkbox, Is.Not.Null);
		Assert.That(checkbox.HasAttribute("checked"), Is.EqualTo(isChecked));
		Assert.That(parsed.QuerySelector("#task")?.TextContent, Does.Contain("Review <draft> & publish"));
		Assert.That(parsed.QuerySelector("draft"), Is.Null);
		Assert.That(result.Html, Does.Not.Contain("<!DOCTYPE").IgnoreCase.And.Not.Contain("<html").And.Not.Contain("<head").And.Not.Contain("<body"));
	}

	[TestCase("default")]
	[TestCase("cms")]
	public void OnlineSnippetUsesEmbeddedThemesWithoutDeployingFiles(string theme) {
		var themesDirectory = Path.Combine(Path.GetTempPath(), "localnotion-snippet-" + Guid.NewGuid().ToString("N"));
		var renderer = new HtmlRenderer(new ThemeCatalog(new ThemeOptions { ThemesDirectory = themesDirectory }));
		var result = renderer.RenderSnippet(
			new CalloutBlock { Text = [new TextInline { Text = "No theme deployment needed" }] },
			new RenderOptions { Environment = RenderEnvironment.Online, Themes = [theme] }
		);

		Assert.That(result.Html, Does.Contain("No theme deployment needed").And.Not.Contain("render-assets").And.Not.Contain("theme://").And.Not.Contain("include://"));
		Assert.That(result.Assets, Is.Empty);
		Assert.That(Directory.Exists(themesDirectory), Is.False);
	}

	[Test]
	public void DocumentSnippetSuppressesPageFrameAndHeaderWithoutMutatingDocument() {
		var document = new DocumentBlock {
			Title = "A full page title",
			CoverUrl = "https://example.test/cover.jpg",
			Icon = new Icon { Emoji = "⭐" },
			RenderFrame = true,
			ShowPageHeader = true,
			Children = [new ToDoBlock { Text = [new TextInline { Text = "Only the child" }] }]
		};
		var options = new RenderOptions { Environment = RenderEnvironment.Online };
		var renderer = new HtmlRenderer();
		var snippet = renderer.RenderSnippet(document, options).Html;
		var complete = renderer.Render(document, options).Html;

		Assert.That(snippet, Does.Contain("Only the child").And.Not.Contain(document.Title).And.Not.Contain(document.CoverUrl).And.Not.Contain("<html"));
		Assert.That(document.RenderFrame, Is.True);
		Assert.That(document.ShowPageHeader, Is.True);
		Assert.That(complete, Does.Contain(document.Title).And.Contain(document.CoverUrl).And.Contain("<html"));
	}

	[Test]
	public void ReusedRendererStartsEachSnippetWithAnIndependentContext() {
		var renderer = new HtmlRenderer();
		var options = new RenderOptions { Environment = RenderEnvironment.Online };
		var first = new ToggleBlock { Text = [new TextInline { Text = "First" }], IsOpen = true };
		var second = new ToDoBlock { Text = [new TextInline { Text = "Second" }] };
		var firstHtml = renderer.RenderSnippet(first, options).Html;
		var secondHtml = renderer.RenderSnippet(second, options).Html;
		var repeatedHtml = renderer.RenderSnippet(first, options).Html;

		Assert.That(secondHtml, Does.Contain("Second").And.Not.Contain("First"));
		Assert.That(repeatedHtml, Is.EqualTo(firstHtml));
	}

	[Test]
	public void TextRendererCanRenderABlockThroughTheSameInterface() {
		IRenderer<string> renderer = new TextRenderer();
		var snippet = renderer.RenderSnippet(new CalloutBlock {
			Text = [new TextInline { Text = "Standalone callout" }],
			Children = [new ToDoBlock { Text = [new TextInline { Text = "Nested task" }] }]
		});
		Assert.That(snippet, Is.EqualTo("Standalone callout" + Environment.NewLine + "Nested task" + Environment.NewLine + Environment.NewLine));
	}

	[Test]
	public void NullSnippetIsRejectedBeforeCreatingARenderingContext() {
		Assert.That(() => new HtmlRenderer().RenderSnippet(null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("node"));
	}
}
