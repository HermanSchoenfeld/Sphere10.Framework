// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NUnit.Framework;

namespace Sphere10.Framework.Web.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BulletCheckRenderingTests {
	[Test]
	public void RichTextLinksShareOneShrinkableFlow([Values] RenderEnvironment environment, [Values] RenderMode mode) {
		// Multiple rich-text runs must be inline siblings inside the text column, not independent flex items.
		var item = new ListItemBlock {
			Metadata = new Metadata { Anchor = "linked-item" },
			Text = [
				new TextInline { Text = "Use " },
				new TextInline { Text = "CCIP", Url = "https://example.test/ccip" },
				new TextInline { Text = " with " },
				new TextInline { Text = "IBC", Url = "https://example.test/ibc" },
				new TextInline { Text = " and HTTP/2." }
			]
		};
		var html = new HtmlRenderer().RenderSnippet(
			new ListBlock { Type = ListType.Bulleted, Items = [item] },
			new RenderOptions { Themes = ["default", "bullet_check"], Environment = environment, Mode = mode }
		).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var row = parsed.QuerySelector("#linked-item");
		var content = row.QuerySelector(":scope > .flex-grow-1");
		Assert.That(row.Children.Length, Is.EqualTo(2), "Only the check badge and one content column participate in flex layout.");
		Assert.That(row.QuerySelectorAll(":scope > a"), Is.Empty, "Hyperlinks must not become separate flex items.");
		Assert.That(Regex.Replace(content.TextContent, @"\s+", " ").Trim(), Is.EqualTo("Use CCIP with IBC and HTTP/2."));
		Assert.That(content.QuerySelectorAll("a").Select(link => link.GetAttribute("href")), Is.EqualTo(new[] { "https://example.test/ccip", "https://example.test/ibc" }));
		Assert.That(content.GetAttribute("style"), Does.Contain("min-width:0").And.Contain("overflow-wrap:anywhere"));
		Assert.That(row.QuerySelector(":scope > .badge").ClassList, Does.Contain("flex-shrink-0").And.Contain("align-self-start"));
	}

	[Test]
	public void NestedChildrenRemainBelowTheParentTextInTheContentColumn([Values] RenderEnvironment environment, [Values] RenderMode mode) {
		// Keep nested content in the text column while retaining source anchors and the existing children hook.
		var item = new ListItemBlock {
			Metadata = new Metadata { Anchor = "parent-item" },
			Text = [new TextInline { Text = "Parent item" }],
			Children = [new ListBlock {
				Type = ListType.Bulleted,
				Items = [new ListItemBlock {
					Metadata = new Metadata { Anchor = "nested-item" },
					Text = [new TextInline { Text = "Nested link", Url = "https://example.test/nested" }]
				}]
			}]
		};
		var html = new HtmlRenderer().RenderSnippet(
			new ListBlock { Type = ListType.Bulleted, Items = [item] },
			new RenderOptions { Themes = ["default", "bullet_check"], Environment = environment, Mode = mode }
		).Html;
		var parsed = new HtmlParser().ParseDocument(html);
		var parent = parsed.QuerySelector("#parent-item");
		var content = parent.QuerySelector(":scope > .flex-grow-1");
		var children = content.QuerySelector(":scope > .ln-bulleted-list-item-children");
		Assert.That(parent.Children.Length, Is.EqualTo(2));
		Assert.That(children, Is.Not.Null);
		Assert.That(children.QuerySelector("#nested-item a")?.GetAttribute("href"), Is.EqualTo("https://example.test/nested"));
		Assert.That(content.TextContent.IndexOf("Parent item"), Is.LessThan(content.TextContent.IndexOf("Nested link")));
		Assert.That(parent.QuerySelector(":scope > .ln-bulleted-list-item-children"), Is.Null, "Nested children must not become a third flex column.");
	}
}
