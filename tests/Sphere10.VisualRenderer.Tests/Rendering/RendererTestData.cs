// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.VisualRenderer;

namespace Sphere10.VisualRenderer.Tests;

internal static class RendererTestData {
	public static RenderOptions CreateOptions() => new() { AssetBaseUrl = "/render-assets" };

	public static DocumentBlock CreateDocument() => new() {
		Metadata = new Metadata { SourceId = "source-object", Anchor = "explicit-anchor", ObjectType = "page", Type = "page" },
		Title = "Standalone <renderer>",
		Name = "manual-page",
		Description = "An independent renderer fixture",
		Author = "Another application",
		Keywords = ["render", "independent"],
		CoverUrl = "https://example.test/cover.png",
		Icon = new Icon { Emoji = "⭐" },
		TitleOnCover = true,
		CreatedTime = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero),
		Subtitle = [new TextInline { Text = "A subtitle" }],
		Children = [
			new HeadingBlock {
				Level = 2, IsToggleable = true, Text = [new TextInline { Text = "Heading link", Url = "https://example.test/page#destination" }],
				Children = [new ParagraphBlock { Text = [new TextInline { Text = "Inside the heading" }] }]
			},
			new ParagraphBlock {
				Text = [
					new TextInline { Text = "Literal {title} & <markup>", Style = new TextStyle { Bold = true, Italic = true, Underline = true, StrikeThrough = true, Color = Color.BlueBackground } },
					new TextInline { Text = "Link label", Url = "https://example.test/resolved", PlainTextOverride = "https://example.test/original" },
					new EquationInline { Expression = "x^{2}" },
					new PersonInline { Person = new Person { Name = "Example user", Email = "person@example.test" } },
					new DateInline { Date = new Date { Start = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), IncludeTime = false } },
					new ReferenceInline { Reference = new Reference { Label = "A reference", Url = "https://example.test/page#destination", Icon = new Icon { Emoji = "📄" }, ShowIndicator = true } }
				]
			},
			new ListBlock { Type = ListType.Numbered, Start = 4, Items = [
				new ListItemBlock { Text = [new TextInline { Text = "Numbered" }], Children = [
					new ListBlock { Items = [new ListItemBlock { Text = [new TextInline { Text = "Nested bullet" }] }] }
				] },
				new ListItemBlock { Text = [new TextInline { Text = "Next numbered" }] }
			] },
			new ColumnListBlock { Columns = [
				new ColumnBlock { Children = [new QuoteBlock { Text = [new TextInline { Text = "Quoted" }] }] },
				new ColumnBlock { Children = [new CalloutBlock { Text = [new TextInline { Text = "Callout" }], Icon = new Icon { Url = "https://example.test/icon.png" } }] }
			] },
			new TableBlock { ColumnCount = 2, FirstRowIsHeader = true, FirstColumnIsHeader = true, Rows = [
				new TableRowBlock { Cells = [new TableCellBlock { Text = [new TextInline { Text = "Header A" }] }, new TableCellBlock { Text = [new TextInline { Text = "Header B" }] }] },
				new TableRowBlock { Cells = [new TableCellBlock { Text = [new TextInline { Text = "Row header" }] }, new TableCellBlock { Text = [new TextInline { Text = "Data cell" }] }] }
			] },
			new DatabaseBlock {
				Columns = [new DatabaseColumn { Label = "Title" }, new DatabaseColumn { Label = "Checked" }, new DatabaseColumn { Label = "Computed" }, new DatabaseColumn { Label = "Unknown" }, new DatabaseColumn { Label = "Missing" }],
				Rows = [new DatabaseRow { Cells = [
					new ReferenceValue { IsBlock = true, References = [new Reference { Label = "Row page", Url = "https://example.test/row" }] },
					new BooleanValue { Boolean = true },
					new ComputedValue { Value = new CompoundValue { Values = [new NumberValue { Number = 42 }, new TextValue { Text = [new TextInline { Text = "Nested value" }] }] } },
					new UnsupportedValue { Diagnostic = "Unknown property diagnostic" }
				] }]
			},
			new ToDoBlock { Text = [new TextInline { Text = "Done" }], IsChecked = true },
			new ToggleBlock { Text = [new TextInline { Text = "Toggle" }], IsOpen = true, Children = [new DividerBlock()] },
			new CodeBlock { Code = "<script>alert('literal')</script>\n{title}", Language = CodeLanguage.Html },
			new EquationBlock { Expression = "a_{1} + b_{2}" },
			new MediaBlock { Type = MediaType.Image, Url = "https://example.test/image.png", AltText = "An image" },
			new MediaBlock { Type = MediaType.Audio, Url = "https://example.test/audio.mp3" },
			new MediaBlock { Type = MediaType.Video, Provider = EmbedProvider.YouTube, ProviderId = "video-123", Url = "https://example.test/video", Caption = [new TextInline { Text = "Video caption" }] },
			new MediaBlock { Type = MediaType.Pdf, Url = "https://example.test/document.pdf" },
			new MediaBlock { Type = MediaType.File, Url = "https://example.test/file.bin", FileName = "file.bin" },
			new MediaBlock { Type = MediaType.Embed, Provider = EmbedProvider.Twitter, Url = "https://twitter.com/example/status/1" },
			new ReferenceBlock { Reference = new Reference { Label = "Missing reference", IsAvailable = false } },
			new ReferenceBlock { Reference = new Reference { Label = "Self reference", Url = "" } },
			new BreadcrumbBlock { Items = [new BreadcrumbItem { Reference = new Reference { Label = "Home", Url = "https://example.test" } }, new BreadcrumbItem { IsCurrent = true, Reference = new Reference { Label = "Current" } }] },
			new TableOfContentsBlock { Entries = [new TocEntry { Reference = new Reference { Label = "Contents entry", Url = "#explicit-anchor" } }] },
			new UnsupportedBlock { Type = "unknown", FallbackText = "Unknown visual", Diagnostic = "{\"type\":\"unknown\"}" },
			new RawHtmlBlock { Html = "<b id=\"trusted-content\">Trusted</b>", Text = "code text fallback" },
			new ParagraphBlock { Text = [new TextInline { Text = "Ignored HTML-only content" }], PlainTextOverride = "" }
		]
	};
}
