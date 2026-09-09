// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DatabaseRenderingTests {
	[Test]
	public void EmptyAndMissingValuesDoNotCreatePropertyWrappers([Values(false, true)] bool missingValue) {
		var parsed = RenderDatabase(missingValue ? [] : [new EmptyValue()]);
		var cell = parsed.QuerySelector("tbody td");
		Assert.That(cell, Is.Not.Null);
		Assert.That(cell.Children, Is.Empty);
		Assert.That(cell.TextContent.Trim(), Is.Empty);
	}

	[TestCaseSource(nameof(EmptyTypedValues))]
	public void EmptyTypedValuesRetainTheirPropertyWrapper(VisualValue value) {
		var cell = RenderDatabase([value]).QuerySelector("tbody td");
		Assert.That(cell.Children.Length, Is.EqualTo(1));
		Assert.That(cell.FirstElementChild.LocalName, Is.EqualTo("div"));
		Assert.That(cell.FirstElementChild.Id, Is.Empty);
		Assert.That(cell.TextContent.Trim(), Is.Empty);
	}

	[Test]
	public void TitleReferencesDoNotInheritTheDatabaseAnchor() {
		var parsed = RenderDatabase([
			new ReferenceValue { IsBlock = true, References = [new Reference { Label = "First", Url = "/first" }] },
			new ReferenceValue { IsBlock = true, References = [new Reference { Label = "Second", Url = "/second" }] }
		], 2);
		var paragraphs = parsed.QuerySelectorAll("tbody td p[class]");
		Assert.That(paragraphs.Length, Is.EqualTo(2));
		foreach (var paragraph in paragraphs) {
			Assert.That(paragraph.Id, Is.Empty);
			Assert.That(paragraph.ClassList, Does.Contain("ln-color-Default"));
		}
		Assert.That(parsed.QuerySelectorAll("#database-anchor").Length, Is.EqualTo(1));
		Assert.That(parsed.QuerySelectorAll("tbody td p a").Select(link => link.GetAttribute("href")), Is.EqualTo(new[] { "/first", "/second" }));
	}

	[Test]
	public void BlockReferencesRetainLegacyParagraphTokens() {
		var document = new DocumentBlock {
			RenderFrame = false,
			Children = [new ReferenceBlock {
				Metadata = new Metadata { SourceId = "reference-source", Anchor = "reference-anchor" },
				Reference = new Reference { Label = "Destination", Url = "/destination" }
			}]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var paragraph = new HtmlParser().ParseDocument(html).QuerySelector("p[class]");
		Assert.That(paragraph.Id, Is.Empty);
		Assert.That(paragraph.ClassList, Does.Contain("ln-color-Default"));
		Assert.That(paragraph.QuerySelector("a").GetAttribute("href"), Is.EqualTo("/destination"));
	}

	[TestCase(Color.Default, "Default")]
	[TestCase(Color.Green, "Green")]
	[TestCase(Color.Gray, "Gray")]
	[TestCase(Color.Orange, "Orange")]
	public void PropertyBadgesRetainEnumColorCasing(Color color, string token) {
		var parsed = RenderDatabase([new ChoiceValue { Choices = [new Choice { Label = "Choice & label", Color = color }] }]);
		var badge = parsed.QuerySelector("td .badge");
		Assert.That(badge.ClassList, Does.Contain("ln-color-" + token + "-background"));
		Assert.That(badge.TextContent, Is.EqualTo("Choice & label"));
	}

	[TestCase(Color.Default, "default")]
	[TestCase(Color.Green, "green")]
	[TestCase(Color.Gray, "gray")]
	public void InlineBadgesRetainLowercaseColorCasing(Color color, string token) {
		var document = new DocumentBlock {
			RenderFrame = false,
			Children = [new ParagraphBlock { Text = [new BadgeInline { Text = "Inline & label", Color = color }] }]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var badge = new HtmlParser().ParseDocument(html).QuerySelector(".badge");
		Assert.That(badge.ClassList, Does.Contain("ln-color-" + token + "-background"));
		Assert.That(badge.TextContent, Is.EqualTo("Inline & label"));
	}

	public static IEnumerable<TestCaseData> EmptyTypedValues() {
		yield return new TestCaseData(new TextValue()).SetName("EmptyTextRetainsPropertyWrapper");
		yield return new TestCaseData(new ChoiceValue()).SetName("EmptyMultiSelectRetainsPropertyWrapper");
		yield return new TestCaseData(new DateValue()).SetName("EmptyDateRetainsPropertyWrapper");
		yield return new TestCaseData(new PeopleValue()).SetName("EmptyPeopleRetainsPropertyWrapper");
		yield return new TestCaseData(new FilesValue()).SetName("EmptyFilesRetainsPropertyWrapper");
	}

	private static IDocument RenderDatabase(VisualValue[] cells, int columnCount = 1) {
		var document = new DocumentBlock {
			RenderFrame = false,
			Children = [new DatabaseBlock {
				Metadata = new Metadata { SourceId = "database-source", Anchor = "database-anchor" },
				Columns = Enumerable.Range(0, columnCount).Select(index => new DatabaseColumn { Label = "Column " + index }).ToArray(),
				Rows = [new DatabaseRow { Cells = cells }]
			}]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		return new HtmlParser().ParseDocument(html);
	}
}
