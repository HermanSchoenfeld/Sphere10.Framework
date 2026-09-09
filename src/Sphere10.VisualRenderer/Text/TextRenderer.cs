// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

public class TextRenderer : RecursiveRendererBase<string> {

	protected override string RenderDocument(DocumentBlock document, RenderOptions options)
		=> Render((VisualNode)document);

	protected override string Render(VisualNode node, int index = 0)
		=> node.PlainTextOverride ?? base.Render(node, index);

	protected override string Merge(IEnumerable<string> outputs)
		=> string.Concat(outputs);

	#region Values

	protected override string Render(bool? value)
		=> value == null ? string.Empty : value.Value ? "[X]" : "[ ]";

	protected override string Render(double? value)
		=> value?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty;

	protected override string Render(Person person)
		=> !string.IsNullOrWhiteSpace(person.Name) ? person.Name + " <" + person.Email + ">" : person.Email ?? string.Empty;

	protected override string Render(Date date)
		=> VisualFormatting.Date(date);

	protected override string Render(EmptyValue value) => string.Empty;

	protected override string Render(TextValue value) => Render(value.Text);

	protected override string Render(NumberValue value) => Render(value.Number);

	protected override string Render(BooleanValue value) => Render(value.Boolean);

	protected override string Render(DateValue value) => Render(value.Date);

	protected override string Render(PeopleValue value) => Merge(value.People.Select(Render));

	protected override string Render(FilesValue value) => Merge(value.Files.Select(file => file.FileName + " (" + file.Url + ")"));

	protected override string Render(ChoiceValue value) => string.Join(", ", value.Choices.Select(choice => choice.Label));

	protected override string Render(ReferenceValue value) => Merge(value.References.Select(RenderReference));

	protected override string Render(LinkValue value) => value.Url ?? value.Label;

	protected override string Render(CompoundValue value) => Merge(value.Values.Select(Render));

	protected override string Render(ComputedValue value) => Render(value.Value);

	protected override string Render(UnsupportedValue value) => value.Text;

	#endregion

	#region Text

	protected override string Render(VisualInline text)
		=> text.PlainTextOverride ?? base.Render(text);

	protected override string Render(IEnumerable<VisualInline> text)
		=> Merge(text.Select(Render));

	protected override string Render(EquationInline text) => text.Expression;

	protected override string Render(TextInline text) {
		var isUrl = !string.IsNullOrWhiteSpace(text.Url);
		var urlInfo = isUrl ? (Url: text.Url, Icon: string.Empty, Indicator: string.Empty) : default;
		return RenderText(text.Text, isUrl, text.Style.Bold, text.Style.Italic, text.Style.StrikeThrough, text.Style.Underline, text.Style.Code, text.Style.Color, urlInfo);
	}

	protected override string Render(ReferenceInline text) => RenderReference(text.Reference);

	protected override string Render(PersonInline text) => Render(text.Person);

	protected override string Render(DateInline text) => Render(text.Date);

	protected override string Render(BadgeInline text) => text.Text;

	protected override string RenderText(string content, bool isUrl, bool isBold, bool isItalic, bool isStrikeThrough, bool isUnderline, bool isCode, Color color, (string Url, string Icon, string Indicator) urlInfo = default)
		=> (isUrl ? urlInfo.Url : content) ?? string.Empty;

	#endregion

	#region Page

	protected override string Render(DocumentBlock page)
		=> (page.ShowPageHeader ? page.Title + Environment.NewLine + page.Name + Environment.NewLine : string.Empty) +
		   RenderChildPageItems() + Environment.NewLine;

	protected override string Render(TemplateBlock template) => Merge(template.Slots.Values.Select(node => Render(node)));

	protected override string Render(GroupBlock group) => RenderChildPageItems();

	protected override string Render(TableBlock table) => RenderChildPageItems();

	protected override string Render(TableRowBlock row)
		=> Merge(row.Cells.Select(cell => Render(cell.Text))) + Environment.NewLine;

	protected override string Render(TableCellBlock cell) => Render(cell.Text);

	protected override string Render(BreadcrumbBlock breadcrumb)
		=> Merge(breadcrumb.Items.Select(item => $"{item.Reference.Label} ({item.Reference.Url})")) + Environment.NewLine;

	protected override string RenderBulletedList(ListBlock bullets)
		=> Merge(bullets.Items.Select((bullet, index) => Render((VisualNode)bullet, bullets.Start + index))) + Environment.NewLine;

	protected override string RenderBulletedItem(int number, ListItemBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string RenderNumberedList(ListBlock items)
		=> Merge(items.Items.Select((item, index) => Render((VisualNode)item, items.Start + index)));

	protected override string RenderNumberedItem(int number, ListItemBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string Render(CalloutBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string Render(HeadingBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string Render(ParagraphBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string Render(ToDoBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string Render(ToggleBlock block)
		=> Render(block.Text) + Environment.NewLine + RenderChildPageItems();

	protected override string Render(CodeBlock block) => block.Code + Environment.NewLine;

	protected override string Render(ColumnBlock block) => RenderChildPageItems();

	protected override string Render(ColumnListBlock block) => RenderChildPageItems() + Environment.NewLine;

	protected override string Render(DividerBlock block) => "================================================" + Environment.NewLine;

	protected override string Render(EquationBlock block) => block.Expression + Environment.NewLine;

	protected override string Render(QuoteBlock block) => "\"" + Render(block.Text) + "\"" + Environment.NewLine + RenderChildPageItems();

	protected override string Render(TableOfContentsBlock block)
		=> "Table of Contents" + Environment.NewLine + Merge(block.Entries.Select(entry => RenderReference(entry.Reference) + Environment.NewLine));

	protected override string Render(ReferenceBlock block) => RenderReference(block.Reference) + Environment.NewLine;

	protected virtual string RenderReference(Reference reference) => reference.PlainTextOverride ?? reference.Label;

	protected override string Render(RawHtmlBlock block) => block.Text + Environment.NewLine;

	protected override string RenderUnsupported(VisualNode node)
		=> (node is UnsupportedBlock unsupported ? unsupported.FallbackText : string.Empty) + Environment.NewLine;

	protected override string Render(MediaBlock block)
		=> block.Type switch {
			MediaType.File => Render(block.Caption) + Environment.NewLine,
			MediaType.Image => $"Image ({block.Url}) {Render(block.Caption)}" + Environment.NewLine,
			MediaType.Audio => $"Audio ({block.Url})" + Environment.NewLine,
			MediaType.Pdf => Render(block.Caption) + $"PDF: {block.Url}",
			MediaType.Video => $"[{(block.Provider == EmbedProvider.None ? "Video" : block.Provider)}]({block.ProviderId ?? block.Url}) {Render(block.Caption)}" + Environment.NewLine,
			MediaType.Embed => block.Url + Environment.NewLine,
			_ => RenderUnsupported(block)
		};

	protected override string Render(DatabaseBlock database)
		=> string.Join("\t", database.Columns.Select(column => column.Label)) + Environment.NewLine +
				   Merge(database.Rows.Select(row => string.Join(
					   "\t",
					   database.Columns.Select((column, index) => index < row.Cells.Length ? Render(row.Cells[index]) : string.Empty)
				   ) + Environment.NewLine));

	#endregion
}
