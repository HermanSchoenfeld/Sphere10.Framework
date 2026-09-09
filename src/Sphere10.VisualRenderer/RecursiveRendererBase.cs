// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

public abstract class RecursiveRendererBase<TOutput> : IRenderer<TOutput> {

	protected VisualRenderingContext RenderingContext { get; private set; } = null;

	public virtual TOutput Render(DocumentBlock document, RenderOptions options = null) {
		ArgumentNullException.ThrowIfNull(document);
		options ??= new RenderOptions();
		if (!Enum.IsDefined(options.Mode))
			throw new ArgumentOutOfRangeException(nameof(options.Mode));
		if (!Enum.IsDefined(options.Environment))
			throw new ArgumentOutOfRangeException(nameof(options.Environment));
		// Each invocation owns its context, including when an instance is reused concurrently.
		var renderer = (RecursiveRendererBase<TOutput>)MemberwiseClone();
		renderer.RenderingContext = new VisualRenderingContext(document, options);
		renderer.OnRenderingContextCreated();
		return renderer.RenderDocument(document, options);
	}

	public virtual TOutput RenderSnippet(VisualNode node, RenderOptions options = null) {
		ArgumentNullException.ThrowIfNull(node);
		var document = node is DocumentBlock page
			? page with { RenderFrame = false, ShowPageHeader = false }
			: new DocumentBlock { RenderFrame = false, ShowPageHeader = false, Children = [node] };
		return Render(document, options);
	}

	protected abstract TOutput RenderDocument(DocumentBlock document, RenderOptions options);

	protected virtual void OnRenderingContextCreated() {
	}

	protected virtual string Render(VisualNode node, int index = 0) {
		RenderingContext.Enter(node, index);
		try {
			return node switch {
				DocumentBlock item => Render(item),
				TemplateBlock item => Render(item),
				GroupBlock item => Render(item),
				ParagraphBlock item => Render(item),
				HeadingBlock item => Render(item),
				QuoteBlock item => Render(item),
				CalloutBlock item => Render(item),
				ToDoBlock item => Render(item),
				ToggleBlock item => Render(item),
				CodeBlock item => Render(item),
				EquationBlock item => Render(item),
				DividerBlock item => Render(item),
				RawHtmlBlock item => Render(item),
				ColumnListBlock item => Render(item),
				ColumnBlock item => Render(item),
				TableBlock item => Render(item),
				TableRowBlock item => Render(item),
				TableCellBlock item => Render(item),
				MediaBlock item => Render(item),
				ReferenceBlock item => Render(item),
				BreadcrumbBlock item => Render(item),
				TableOfContentsBlock item => Render(item),
				DatabaseBlock item => Render(item),

				ListBlock items => items.Type == ListType.Numbered ? RenderNumberedList(items) : RenderBulletedList(items),
				ListItemBlock item => RenderingContext.GetParentRenderingNode() is ListBlock { Type: ListType.Numbered }
					? RenderNumberedItem(index, item) : RenderBulletedItem(index, item),
				_ => RenderUnsupported(node)
			};
		} finally {
			RenderingContext.Leave(node);
		}
	}

	protected virtual string RenderChildPageItems() {
		var children = RenderingContext.CurrentRenderingNode switch {
			DocumentBlock document => document.Children,
			ContainerVisual container => container.Children,
			TableBlock table => table.Rows,
			ColumnListBlock columns => columns.Columns,
			_ => Enumerable.Empty<VisualNode>()
		};
		return Merge(children.Select((child, index) => Render(child, index)));
	}

	protected virtual string Merge(IEnumerable<string> outputs) => string.Concat(outputs);

	#region Base renderers

	protected virtual string Render(DocumentBlock node) => RenderUnsupported(node);

	protected virtual string Render(TemplateBlock node) => RenderUnsupported(node);

	protected virtual string Render(GroupBlock node) => RenderUnsupported(node);

	protected virtual string Render(ParagraphBlock node) => RenderUnsupported(node);

	protected virtual string Render(HeadingBlock node) => RenderUnsupported(node);

	protected virtual string Render(QuoteBlock node) => RenderUnsupported(node);

	protected virtual string Render(CalloutBlock node) => RenderUnsupported(node);

	protected virtual string Render(ToDoBlock node) => RenderUnsupported(node);

	protected virtual string Render(ToggleBlock node) => RenderUnsupported(node);

	protected virtual string Render(CodeBlock node) => RenderUnsupported(node);

	protected virtual string Render(EquationBlock node) => RenderUnsupported(node);

	protected virtual string Render(DividerBlock node) => RenderUnsupported(node);

	protected virtual string Render(RawHtmlBlock node) => RenderUnsupported(node);

	protected virtual string Render(ColumnListBlock node) => RenderUnsupported(node);

	protected virtual string Render(ColumnBlock node) => RenderUnsupported(node);

	protected virtual string Render(TableBlock node) => RenderUnsupported(node);

	protected virtual string Render(TableRowBlock node) => RenderUnsupported(node);

	protected virtual string Render(TableCellBlock node) => RenderUnsupported(node);

	protected virtual string Render(MediaBlock node) => RenderUnsupported(node);

	protected virtual string Render(ReferenceBlock node) => RenderUnsupported(node);

	protected virtual string Render(BreadcrumbBlock node) => RenderUnsupported(node);

	protected virtual string Render(TableOfContentsBlock node) => RenderUnsupported(node);

	protected virtual string Render(DatabaseBlock node) => RenderUnsupported(node);

	protected virtual string RenderBulletedList(ListBlock items) => RenderUnsupported(items);

	protected virtual string RenderNumberedList(ListBlock items) => RenderUnsupported(items);

	protected virtual string RenderBulletedItem(int number, ListItemBlock item) => RenderUnsupported(item);

	protected virtual string RenderNumberedItem(int number, ListItemBlock item) => RenderUnsupported(item);

	protected virtual string RenderUnsupported(VisualNode node) => throw new NotSupportedException(node.GetType().Name);

	protected virtual string Render(VisualInline inline)
		=> inline switch {
			TextInline item => Render(item),
			EquationInline item => Render(item),
			ReferenceInline item => Render(item),
			PersonInline item => Render(item),
			DateInline item => Render(item),
			BadgeInline item => Render(item),

			_ => throw new NotSupportedException(inline.GetType().Name)
		};

	protected virtual string Render(TextInline inline) => throw new NotSupportedException(inline.GetType().Name);

	protected virtual string Render(EquationInline inline) => throw new NotSupportedException(inline.GetType().Name);

	protected virtual string Render(ReferenceInline inline) => throw new NotSupportedException(inline.GetType().Name);

	protected virtual string Render(PersonInline inline) => throw new NotSupportedException(inline.GetType().Name);

	protected virtual string Render(DateInline inline) => throw new NotSupportedException(inline.GetType().Name);

	protected virtual string Render(BadgeInline inline) => throw new NotSupportedException(inline.GetType().Name);

	protected virtual string Render(IEnumerable<VisualInline> text) => Merge(text.Select(Render));

	protected virtual string Render(VisualValue value) {
		RenderingContext.EnterValue(value);
		try {
			return value switch {
				EmptyValue item => Render(item),
				TextValue item => Render(item),
				NumberValue item => Render(item),
				BooleanValue item => Render(item),
				DateValue item => Render(item),
				PeopleValue item => Render(item),
				FilesValue item => Render(item),
				ChoiceValue item => Render(item),
				ReferenceValue item => Render(item),
				LinkValue item => Render(item),
				CompoundValue item => Render(item),
				ComputedValue item => Render(item),
				UnsupportedValue item => Render(item),

				_ => throw new NotSupportedException(value.GetType().Name)
			};
		} finally {
			RenderingContext.LeaveValue(value);
		}
	}

	protected virtual string Render(EmptyValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(TextValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(NumberValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(BooleanValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(DateValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(PeopleValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(FilesValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(ChoiceValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(ReferenceValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(LinkValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(CompoundValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(ComputedValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(UnsupportedValue value) => throw new NotSupportedException(value.GetType().Name);

	protected virtual string Render(Person person) => person.Name;

	protected virtual string Render(Icon icon) => icon?.Emoji ?? string.Empty;

	protected virtual string Render(Date date) => VisualFormatting.Date(date);

	protected virtual string Render(DateTimeOffset? date) => date?.ToString() ?? string.Empty;

	protected virtual string Render(bool? value) => value?.ToString() ?? string.Empty;

	protected virtual string Render(double? value) => value?.ToString() ?? string.Empty;

	protected virtual string Render(string value) => value ?? string.Empty;

	protected virtual string RenderBadge(string text, Color color) => text;

	protected virtual string RenderText(string content, bool isUrl, bool isBold, bool isItalic, bool isStrikeThrough, bool isUnderline, bool isCode, Color color, (string Url, string Icon, string Indicator) urlInfo = default)
		=> content;

	#endregion
}
