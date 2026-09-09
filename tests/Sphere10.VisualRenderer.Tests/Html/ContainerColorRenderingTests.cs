// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ContainerColorRenderingTests {
	[TestCaseSource(nameof(ColoredParents))]
	public void TableAndListsInheritTheirNearestColoredAncestor(ContainerVisual parent) {
		parent = parent with { Children = [new GroupBlock { Children = Containers() }] };
		var parsed = Render(parent is ListItemBlock item ? new ListBlock { Items = [item] } : parent);
		foreach (var container in parsed.QuerySelectorAll("table, #bullets, #numbered"))
			Assert.That(container.ClassList, Does.Contain("ln-color-blue-background"));
		Assert.That(parsed.QuerySelectorAll("table, #bullets, #numbered").Length, Is.EqualTo(3));
	}

	[TestCase(Color.Default, "default")]
	[TestCase(Color.GreenBackground, "green-background")]
	public void NearestParentColorOverridesTheOuterCallout(Color color, string expectedColor) {
		var parent = new CalloutBlock {
			Color = Color.BlueBackground,
			Children = [new ParagraphBlock { Color = color, Children = Containers() }]
		};
		var parsed = Render(parent);
		foreach (var container in parsed.QuerySelectorAll("table, #bullets, #numbered"))
			Assert.That(container.ClassList, Does.Contain("ln-color-" + expectedColor));
		Assert.That(parsed.QuerySelectorAll("table, #bullets, #numbered").Length, Is.EqualTo(3));
	}

	[Test]
	public void StandaloneContainersHaveDefaultColorInsteadOfAnUnresolvedToken() {
		var parsed = Render(Containers());
		foreach (var container in parsed.QuerySelectorAll("table, #bullets, #numbered")) {
			Assert.That(container.ClassList, Does.Contain("ln-color-default"));
			Assert.That(container.ClassName, Does.Not.Contain("{color}"));
		}
		Assert.That(parsed.QuerySelectorAll("table, #bullets, #numbered").Length, Is.EqualTo(3));
	}

	[Test]
	public void ExplicitListColorOverridesItsAncestor([Values(ListType.Bulleted, ListType.Numbered)] ListType type) {
		var parsed = Render(new CalloutBlock {
			Color = Color.BlueBackground,
			Children = [new ListBlock {
				Metadata = new Metadata { Anchor = "colored-list" },
				Type = type,
				Color = Color.RedBackground,
				Items = [new ListItemBlock { Text = [new TextInline { Text = "Item" }] }]
			}]
		});
		Assert.That(parsed.QuerySelector("#colored-list").ClassList, Does.Contain("ln-color-red-background"));
	}

	[TestCase("default")]
	[TestCase("green-background")]
	[TestCase("custom-theme-color")]
	public void AmbientColorOverridesTheVisualAncestor(string ambientColor) {
		var document = new DocumentBlock {
			RenderFrame = false,
			Tokens = new Dictionary<string, string> { ["color"] = ambientColor },
			Children = [new CalloutBlock { Color = Color.BlueBackground, Children = Containers() }]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var containers = parsed.QuerySelectorAll("table, #bullets, #numbered");
		Assert.That(containers.Length, Is.EqualTo(3));
		foreach (var container in containers)
			Assert.That(container.ClassList, Does.Contain("ln-color-" + ambientColor));
	}

	[Test]
	public void ExplicitListColorOverridesTheAmbientColor([Values(ListType.Bulleted, ListType.Numbered)] ListType type) {
		var document = new DocumentBlock {
			RenderFrame = false,
			Tokens = new Dictionary<string, string> { ["color"] = "default" },
			Children = [new CalloutBlock {
				Color = Color.BlueBackground,
				Children = [new ListBlock {
					Metadata = new Metadata { Anchor = "explicit-color" },
					Color = Color.RedBackground,
					Type = type,
					Items = [new ListItemBlock { Text = [new TextInline { Text = "Item" }] }]
				}]
			}]
		};
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		Assert.That(parsed.QuerySelector("#explicit-color").ClassList, Does.Contain("ln-color-red-background"));
	}

	[Test]
	public void ColorDoesNotLeakFromACompletedSiblingCallout() {
		var parsed = Render(
			new CalloutBlock { Color = Color.BlueBackground, Children = [new TableBlock()] },
			new TableBlock()
		);
		var tables = parsed.QuerySelectorAll("table");
		Assert.That(tables.Length, Is.EqualTo(2));
		Assert.That(tables[0].ClassList, Does.Contain("ln-color-blue-background"));
		Assert.That(tables[1].ClassList, Does.Contain("ln-color-default"));
	}

	public static IEnumerable<TestCaseData> ColoredParents() {
		yield return new TestCaseData(new CalloutBlock { Color = Color.BlueBackground }).SetName("ContainersInheritCalloutColor");
		yield return new TestCaseData(new QuoteBlock { Color = Color.BlueBackground }).SetName("ContainersInheritQuoteColor");
		yield return new TestCaseData(new ParagraphBlock { Color = Color.BlueBackground }).SetName("ContainersInheritParagraphColor");
		yield return new TestCaseData(new HeadingBlock { Color = Color.BlueBackground, IsToggleable = true }).SetName("ContainersInheritHeadingColor");
		yield return new TestCaseData(new ToDoBlock { Color = Color.BlueBackground }).SetName("ContainersInheritToDoColor");
		yield return new TestCaseData(new ToggleBlock { Color = Color.BlueBackground }).SetName("ContainersInheritToggleColor");
		yield return new TestCaseData(new ListItemBlock { Color = Color.BlueBackground }).SetName("ContainersInheritListItemColor");
	}

	private static VisualNode[] Containers() => [
		new TableBlock {
			ColumnCount = 1,
			Rows = [new TableRowBlock { Cells = [new TableCellBlock { Text = [new TextInline { Text = "Cell" }] }] }]
		},
		new ListBlock {
			Metadata = new Metadata { Anchor = "bullets" },
			Type = ListType.Bulleted,
			Items = [new ListItemBlock { Text = [new TextInline { Text = "Bullet" }] }]
		},
		new ListBlock {
			Metadata = new Metadata { Anchor = "numbered" },
			Type = ListType.Numbered,
			Items = [new ListItemBlock { Text = [new TextInline { Text = "Number" }] }]
		}
	];

	private static IDocument Render(params VisualNode[] nodes) {
		var document = new DocumentBlock { RenderFrame = false, Children = nodes };
		var html = new HtmlRenderer().Render(document, RendererTestData.CreateOptions()).Html;
		return new HtmlParser().ParseDocument(html);
	}
}
