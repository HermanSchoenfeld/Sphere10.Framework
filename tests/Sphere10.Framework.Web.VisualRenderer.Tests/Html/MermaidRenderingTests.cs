// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MermaidRenderingTests {

	private static readonly ThemeCatalog _themes = new ThemeCatalog().CreateSnapshot();

	[Test]
	public void MermaidRendersEncodedSourceWithItsOwnRuntime(
		[Values("default", "cms")] string theme,
		[Values] RenderEnvironment environment,
		[Values] RenderMode mode,
		[Values] bool snippet
	) {
		const string source = "flowchart LR\n    A[\"<script>alert('literal')</script> & {title}\"] --> B{Ready?}";
		var document = new DocumentBlock { Children = [new CodeBlock { Language = CodeLanguage.Mermaid, Code = source }] };
		var options = new RenderOptions { Themes = [theme], Environment = environment, Mode = mode, AssetBaseUrl = "/render-assets" };
		var renderer = new HtmlRenderer(_themes);
		var result = snippet ? renderer.RenderSnippet(document, options) : renderer.Render(document, options);
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var figure = parsed.QuerySelector("figure[data-ln-mermaid]");

		Assert.That(figure, Is.Not.Null);
		Assert.That(figure.QuerySelector(".ln-mermaid-code code").TextContent, Is.EqualTo(source));
		Assert.That(figure.QuerySelectorAll("script, img"), Is.Empty, "Diagram source must remain text, never executable markup");
		Assert.That(figure.QuerySelector("details"), Is.Null, "The optional source control stays commented out.");
		Assert.That(figure.QuerySelector(".ln-mermaid-code").HasAttribute("hidden"), Is.True);
		Assert.That(figure.InnerHtml, Does.Contain("<!-- Uncomment this source control"));
		Assert.That(parsed.QuerySelector("code.language-mermaid"), Is.Null, "Prism must not modify the Mermaid source");
		var runtimeScripts = parsed.QuerySelectorAll("script[src]")
			.Where(element => element.GetAttribute("src").Contains("/resources/mermaid/", StringComparison.Ordinal)).ToArray();
		Assert.That(runtimeScripts, Has.Length.EqualTo(2));
		Assert.That(runtimeScripts.All(element => element.HasAttribute("defer")), Is.True);
		Assert.That(runtimeScripts[0].GetAttribute("src"), Does.EndWith("/mermaid-11.12.0.min.js"));
		Assert.That(runtimeScripts[1].GetAttribute("src"), Does.EndWith("/mermaid-init.js"));
		foreach (var element in parsed.QuerySelectorAll("script[src], link[href]").Where(element =>
			(element.GetAttribute("src") ?? element.GetAttribute("href")).Contains("/resources/mermaid/", StringComparison.Ordinal))) {
			var url = element.GetAttribute("src") ?? element.GetAttribute("href");
			Assert.That(url, Does.StartWith("/render-assets/assets/"), "New runtime resources are exported in both online and offline mode");
			Assert.That(result.Assets.Any(asset => "/render-assets/" + asset.RelativePath == url), Is.True, url);
		}
		Assert.That(result.Assets.Any(asset => asset.RelativePath.EndsWith("/resources/mermaid/LICENSE.txt", StringComparison.Ordinal)), Is.True);
		Assert.That(result.Assets.Any(asset => asset.RelativePath.EndsWith("/resources/mermaid/DOMPurify-LICENSE.txt", StringComparison.Ordinal)), Is.True);
	}

	[Test]
	public void MultipleAndNestedMermaidBlocksLoadTheRuntimeOnce() {
		var document = new DocumentBlock { Children = [
			new CodeBlock { Language = CodeLanguage.Mermaid, Code = "flowchart LR\nA --> B" },
			new DocumentBlock { RenderFrame = false, Themes = ["cms"], Children = [
				new CodeBlock { Language = CodeLanguage.Mermaid, Code = "sequenceDiagram\nAlice->>Bob: Hello" }
			] }
		] };
		var result = new HtmlRenderer(_themes).Render(document, new RenderOptions { Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelectorAll("[data-ln-mermaid]"), Has.Length.EqualTo(2));
		Assert.That(parsed.QuerySelectorAll("script[src$='/mermaid-11.12.0.min.js']"), Has.Length.EqualTo(1));
		Assert.That(parsed.QuerySelectorAll("script[src$='/mermaid-init.js']"), Has.Length.EqualTo(1));
		Assert.That(result.Assets.Count(asset => asset.RelativePath.EndsWith("/mermaid-11.12.0.min.js", StringComparison.Ordinal)), Is.EqualTo(1));
	}

	[Test]
	public void OnlySurvivingDocumentOrTemplateSlotsRequestTheRuntime([Values] bool templateOwner, [Values] bool visibleDiagram) {
		var unused = new CodeBlock { Language = CodeLanguage.Mermaid, Code = "flowchart LR\nUnused --> Discarded" };
		VisualNode[] visible = visibleDiagram ? [new CodeBlock { Language = CodeLanguage.Mermaid, Code = "flowchart LR\nVisible --> Diagram" }] : [];
		VisualNode owner = templateOwner ? new TemplateBlock {
			Template = "page_content",
			Tokens = new Dictionary<string, string> {
				["page_cover"] = string.Empty, ["page_name"] = string.Empty, ["thumbnail"] = string.Empty,
				["page_title"] = string.Empty, ["page_subtitle"] = string.Empty
			},
			Slots = new Dictionary<string, VisualNode> { ["unused"] = unused, ["children"] = new GroupBlock { Children = visible } }
		} : new DocumentBlock {
			Slots = new Dictionary<string, VisualNode> { ["unused"] = unused },
			Children = visible
		};
		var result = new HtmlRenderer(_themes).RenderSnippet(owner, new RenderOptions { Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelectorAll("[data-ln-mermaid]"), Has.Length.EqualTo(visibleDiagram ? 1 : 0));
		Assert.That(parsed.QuerySelectorAll("script[src$='/mermaid-11.12.0.min.js']"), Has.Length.EqualTo(visibleDiagram ? 1 : 0));
		Assert.That(parsed.QuerySelectorAll("script[src$='/mermaid-init.js']"), Has.Length.EqualTo(visibleDiagram ? 1 : 0));
		Assert.That(result.Assets.Any(asset => asset.RelativePath.Contains("/resources/mermaid/", StringComparison.Ordinal)), Is.EqualTo(visibleDiagram));
		Assert.That(result.Html, Does.Not.Contain("Unused"));
		Assert.That(result.Html, Does.Not.Contain("<!--sphere10-mermaid-runtime-->"));
		if (visibleDiagram)
			Assert.That(parsed.QuerySelector(".ln-mermaid-code code").TextContent, Is.EqualTo("flowchart LR\nVisible --> Diagram"));
	}

	[Test]
	public void RuntimeMarkerInSourceRemainsLiteralText() {
		const string source = "flowchart LR\nA[\"<!--sphere10-mermaid-runtime-->\"] --> B";
		var result = new HtmlRenderer(_themes).RenderSnippet(new CodeBlock { Language = CodeLanguage.Mermaid, Code = source });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelector(".ln-mermaid-code code").TextContent, Is.EqualTo(source));
		Assert.That(parsed.QuerySelectorAll("script[src$='/mermaid-11.12.0.min.js']"), Has.Length.EqualTo(1));
	}

	[Test]
	public void OrdinaryCodeKeepsItsTemplateAndHasNoMermaidResources() {
		const string source = "Console.WriteLine(\"<literal> {title}\");";
		var result = new HtmlRenderer(_themes).RenderSnippet(new CodeBlock { Language = CodeLanguage.CSharp, Code = source },
			new RenderOptions { Environment = RenderEnvironment.Online });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelector("pre code.language-csharp").TextContent.Trim(), Is.EqualTo(source));
		Assert.That(parsed.QuerySelector("[data-ln-mermaid]"), Is.Null);
		Assert.That(parsed.QuerySelector("script"), Is.Null);
		Assert.That(result.Assets, Is.Empty);
	}

	[Test]
	public void MermaidCaptionRetainsRichText() {
		var result = new HtmlRenderer(_themes).RenderSnippet(new CodeBlock {
			Language = CodeLanguage.Mermaid,
			Code = "flowchart LR\nA --> B",
			Caption = [new TextInline { Text = "Diagram <caption>", Style = new TextStyle { Bold = true } }]
		});
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelector("figure > figcaption b").TextContent, Is.EqualTo("Diagram <caption>"));
	}

	[TestCase("not a valid diagram\n{title} <invalid>")]
	[TestCase("")]
	public void InvalidOrEmptyMermaidSourceIsPreservedForBrowserDiagnostics(string source) {
		var result = new HtmlRenderer(_themes).RenderSnippet(new CodeBlock { Language = CodeLanguage.Mermaid, Code = source });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelector(".ln-mermaid-code code").TextContent, Is.EqualTo(source));
		Assert.That(parsed.QuerySelector("[data-ln-mermaid]").GetAttribute("data-ln-mermaid-state"), Is.EqualTo("pending"));
		Assert.That(parsed.QuerySelector(".ln-mermaid-error").HasAttribute("hidden"), Is.True);
	}

	[Test]
	public void PlainTextMermaidContainsOnlyItsSource() {
		const string source = "classDiagram\nVisualNode <|-- DocumentBlock\nDocumentBlock --> VisualNode : Children";
		var text = new TextRenderer().RenderSnippet(new CodeBlock { Language = CodeLanguage.Mermaid, Code = source });
		Assert.That(text, Is.EqualTo(source + Environment.NewLine + Environment.NewLine));
	}

	[Test]
	public void ReusedRendererResetsMermaidRuntimePerInvocation() {
		var renderer = new HtmlRenderer(_themes);
		var document = new DocumentBlock { Children = [new CodeBlock { Language = CodeLanguage.Mermaid, Code = "flowchart LR\nA --> B" }] };
		var first = renderer.Render(document);
		var ordinary = renderer.RenderSnippet(new CodeBlock { Language = CodeLanguage.Text, Code = "ordinary" }, new RenderOptions { Environment = RenderEnvironment.Online });
		var second = renderer.Render(document);
		Assert.That(first.Html, Is.EqualTo(second.Html));
		Assert.That(ordinary.Html, Does.Not.Contain("mermaid"));
		Assert.That(ordinary.Assets, Is.Empty);
	}
}
