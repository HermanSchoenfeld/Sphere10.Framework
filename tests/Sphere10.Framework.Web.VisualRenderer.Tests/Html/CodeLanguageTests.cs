// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CodeLanguageTests {
	[Test]
	public void CodeLanguageDefaultsToText() {
		Assert.That(new CodeBlock().Language, Is.EqualTo(CodeLanguage.Text));
	}

	[TestCase(CodeLanguage.Text, "Plain text")]
	[TestCase(CodeLanguage.CSharp, "C#")]
	[TestCase(CodeLanguage.CPlusPlus, "C++")]
	[TestCase(CodeLanguage.Powershell, "PowerShell")]
	[TestCase(CodeLanguage.Json, "JSON")]
	[TestCase((CodeLanguage)int.MaxValue, "Plain text")]
	public void CodeHeaderPreservesLiteralSourceAndAnchor(CodeLanguage language, string label) {
		const string source = "\n\t<script> & {language_name}  \n  second line\n";
		var block = new CodeBlock { Language = language, Code = source, Metadata = new Metadata { Anchor = "code-example" } };
		var result = new HtmlRenderer().RenderSnippet(block);
		var parsed = new HtmlParser().ParseDocument(result.Html);
		var container = parsed.QuerySelector("#code-example");
		Assert.That(container.QuerySelector(".ln-code-language").TextContent, Is.EqualTo(label));
		Assert.That(container.QuerySelector("pre code").TextContent, Is.EqualTo(source));
		Assert.That(container.QuerySelector("script"), Is.Null);
		Assert.That(container.QuerySelector("button").GetAttribute("type"), Is.EqualTo("button"));
		Assert.That(container.QuerySelector("button").HasAttribute("hidden"), Is.True, "Show the action only when its behavior is initialized.");
		Assert.That(container.QuerySelector("[role=status]"), Is.Not.Null);
	}

	[TestCase("default")]
	[TestCase("cms")]
	public void CodeHeaderIsInheritedWithoutExposingMermaidSource(string theme) {
		var document = new DocumentBlock { Children = [
			new CodeBlock { Language = CodeLanguage.CSharp, Code = "Console.WriteLine(42);" },
			new CodeBlock { Language = CodeLanguage.Mermaid, Code = "flowchart LR\nA --> B" }
		] };
		var result = new HtmlRenderer().Render(document, new RenderOptions { Themes = [theme] });
		var parsed = new HtmlParser().ParseDocument(result.Html);
		Assert.That(parsed.QuerySelectorAll("[data-ln-code-copy]"), Has.Length.EqualTo(1));
		Assert.That(parsed.QuerySelector(".ln-mermaid [data-ln-code-copy], .ln-mermaid details"), Is.Null);
		Assert.That(parsed.QuerySelector(".ln-mermaid-code").HasAttribute("hidden"), Is.True);
	}

	[TestCase(CodeLanguage.Text, "text")]
	[TestCase(CodeLanguage.CSharp, "csharp")]
	[TestCase(CodeLanguage.CPlusPlus, "cpp")]
	[TestCase(CodeLanguage.FSharp, "fsharp")]
	[TestCase(CodeLanguage.Assembly, "armasm")]
	[TestCase(CodeLanguage.ObjectiveC, "objc")]
	[TestCase(CodeLanguage.VbNet, "vbnet")]
	[TestCase(CodeLanguage.VisualBasic, "vb")]
	[TestCase(CodeLanguage.LiveScript, "typescript")]
	[TestCase(CodeLanguage.LlvmIr, "llvm")]
	[TestCase(CodeLanguage.Markup, "markup-templating")]
	[TestCase(CodeLanguage.Shell, "shell-session")]
	[TestCase(CodeLanguage.Ardunio, "arduino")]
	[TestCase(CodeLanguage.Elixer, "elixir")]
	public void CodeLanguageDescriptionDeterminesPrismClass(CodeLanguage language, string prismName) {
		var description = (DescriptionAttribute)Attribute.GetCustomAttribute(typeof(CodeLanguage).GetField(language.ToString()), typeof(DescriptionAttribute));
		Assert.That(description?.Description, Is.EqualTo(prismName));
		var code = "literal <code> " + language;
		DocumentBlock document = new DocumentBlock { Children = [new CodeBlock { Language = language, Code = code }] };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var elements = new HtmlParser().ParseDocument(html).QuerySelectorAll("pre code");
		Assert.That(elements, Has.Length.EqualTo(1));
		Assert.That(elements[0].ClassList, Does.Contain("language-" + prismName));
		Assert.That(elements[0].TextContent.Trim(), Is.EqualTo(code));
	}
}
