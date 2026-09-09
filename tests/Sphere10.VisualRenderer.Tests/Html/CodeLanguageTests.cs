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

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CodeLanguageTests {
	[Test]
	public void CodeLanguageDefaultsToText() {
		Assert.That(new CodeBlock().Language, Is.EqualTo(CodeLanguage.Text));
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
