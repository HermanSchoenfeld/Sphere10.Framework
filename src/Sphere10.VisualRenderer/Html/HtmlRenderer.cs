// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

public class HtmlRenderer : RecursiveRendererBase<HtmlRenderResult> {

	private readonly ThemeCatalog _themes;

	private int _toggleCount;

	private ThemeCatalog _snapshot = null;

	private ThemeSession _theme = null;

	private Dictionary<string, object> _tokens = null;

	private Dictionary<string, ThemeSession> _sessions = null;

	private Dictionary<string, RenderAsset> _assets = null;

	public HtmlRenderer(ThemeCatalog themes = null) {
		_themes = themes ?? new ThemeCatalog();
	}

	protected bool SuppressFormatting { get; private set; }

	protected override void OnRenderingContextCreated() {
		_toggleCount = 0;
		_snapshot = _themes.CreateSnapshot();
		_tokens = new Dictionary<string, object>(StringComparer.Ordinal);
		_sessions = new Dictionary<string, ThemeSession>(StringComparer.Ordinal);
		_assets = new Dictionary<string, RenderAsset>(StringComparer.Ordinal);
		SuppressFormatting = false;
		_theme = LoadThemes(RenderingContext.Options.Themes);
	}

	protected override HtmlRenderResult RenderDocument(DocumentBlock document, RenderOptions options) {
		var html = Render((VisualNode)document);
		return new HtmlRenderResult(html, _assets.Values.OrderBy(asset => asset.RelativePath, StringComparer.Ordinal).ToArray()) {
			SuppressFormatting = SuppressFormatting
		};
	}

	protected override string Merge(IEnumerable<string> outputs)
		=> string.Concat(outputs);

	#region Values

	protected override string Render(Date date)
		=> VisualFormatting.Date(date);

	protected override string Render(DateTimeOffset? date)
		=> date?.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture) ?? "Empty";

	protected override string Render(bool? value)
		=> value == null ? string.Empty : value.Value ? "[X]" : "[ ]";

	protected override string Render(double? value)
		=> value?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty;

	protected override string Render(string value)
		=> Encode(value, true, false, true);

	#endregion

	#region Users

	protected override string Render(Person person)
		=> RenderEmailLink(person.Name, person.Email ?? string.Empty);

	protected virtual string RenderEmailLink(string name, string email) {
		var mailTo = "mailto:" + email;
		var encodedEmail = Convert.ToBase64String(Encoding.UTF8.GetBytes(mailTo));
		return RenderTemplate(
			"user",
			new RenderTokens {
				["name"] = Render(name),
				["mailto"] = Encode(mailTo, true, false, false),
				["base64_mailto_string_exp"] = string.Join(" + ", encodedEmail.Select(character => "'" + character + "'"))
			}
		);
	}

	#endregion

	#region Database

	protected override string Render(DatabaseBlock database) {
		return RenderTemplate(
			"database",
			new RenderTokens(database) {
				["header"] = Merge(database.Columns.Select(column => RenderTemplate(
					"database_header_cell",
					new RenderTokens {
						["contents"] = Render(column.Label),
						["property_name"] = Render(column.Label),
						["property_type"] = Render(column.Type)
					}
				))),
				["contents"] = Merge(database.Rows.Select(row => RenderTemplate(
					"database_row",
					new RenderTokens(row.Metadata) {
						["contents"] = Merge(database.Columns.Select((column, index) => RenderTemplate(
							"database_row_cell",
							new RenderTokens {
								["contents"] = index >= row.Cells.Length || row.Cells[index] is null or EmptyValue
									? string.Empty
									: RenderTemplate(
										"property_value",
										new RenderTokens {
											["contents"] = Render(row.Cells[index])
										}
									)
							}
						)))
					}
				)))
			}
		);
	}

	protected override string Render(EmptyValue value) => string.Empty;

	protected override string Render(TextValue value) => Render(value.Text);

	protected override string Render(NumberValue value) => Render(value.Number);

	protected override string Render(BooleanValue value)
		=> value.Boolean == null ? string.Empty : RenderTemplate(
					"to_do",
					new RenderTokens {
						["text"] = string.Empty,
						["checked"] = value.Boolean.Value ? "checked" : string.Empty,
						["color"] = "default",
						["children"] = string.Empty
					}
				);

	protected override string Render(DateValue value) => Render(value.Date);

	protected override string Render(PeopleValue value) => Merge(value.People.Select(Render));

	protected override string Render(FilesValue value) => Merge(value.Files.Select(file => Render((VisualNode)file)));

	protected override string Render(ChoiceValue value)
		=> Merge(value.Choices.Select(choice => RenderTemplate(
			"text_badge",
			new RenderTokens {
				["text"] = Render(choice.Label),
				["color"] = choice.Color
			}
		)));

	protected override string Render(ReferenceValue value) => Merge(value.References.Select(reference => RenderReference(reference, !value.IsBlock)));

	protected override string Render(LinkValue value) => value.Url == null ? Render(value.Label) : RenderLink(value.Url, Render(value.Label), string.Empty, string.Empty);

	protected override string Render(CompoundValue value) => Merge(value.Values.Select(Render));

	protected override string Render(ComputedValue value) => Render(value.Value);

	protected override string Render(UnsupportedValue value)
		=> RenderTemplate("unsupported", new RenderTokens { ["json"] = Encode(value.Diagnostic ?? value.Text, true, false, false), ["text"] = Render(value.Text) });

	#endregion

	#region Text

	protected override string Render(IEnumerable<VisualInline> text)
		=> Merge(text.Select(Render));

	protected override string Render(EquationInline text)
		=> RenderTemplate(
						"equation_inline",
						new RenderTokens {
							["expression"] = Encode(text.Expression, true, true, false),
						}
					);

	// Mentions degrade to plain text rather than throwing. Notion introduces mention
	// types without notice (link_mention is undocumented but live), and this renderer also
	// runs during the download phase for keyword extraction -- so a throw here aborts an
	// entire scheduled mirror rather than spoiling one span of text.

	protected override string Render(TextInline text) {
		var isUrl = text.Url is not null;
		var urlInfo = isUrl ? (Url: text.Url, Icon: string.Empty, Indicator: string.Empty) : default;
		return RenderText(text.Text, isUrl, text.Style.Bold, text.Style.Italic, text.Style.StrikeThrough, text.Style.Underline, text.Style.Code, text.Style.Color, urlInfo);
	}

	protected override string Render(ReferenceInline text) => RenderReference(text.Reference, true);

	protected override string Render(PersonInline text) => Render(text.Person);

	protected override string Render(DateInline text) => Render(text.Date);

	protected override string Render(BadgeInline text) => RenderBadge(text.Text, text.Color);

	protected override string RenderBadge(string text, Color color)
		=> RenderTemplate(
					"text_badge",
					new RenderTokens {
						["color"] = ToColorString(color),
						["text"] = Render(text)
					}
				);

	protected virtual string RenderLink(string url, string text, string icon, string indicator) {
		return RenderTemplate(
			RenderingContext.RenderingStack.Any(node => node is HeadingBlock) ? "header_link" : "text_link",
			new RenderTokens {
				["url"] = Encode(url, true, false, false),
				["text"] = text,
				["icon"] = icon,
				["indicator"] = indicator
			}
		);
	}

	#endregion

	#region Page

	protected override string Render(DocumentBlock page) {
		var previousTheme = _theme;
		var previousTokens = _tokens;
		if (page.Themes.Length > 0)
			_theme = LoadThemes(page.Themes);
		_tokens = new Dictionary<string, object>(_tokens, StringComparer.Ordinal);
		foreach (var token in page.Tokens)
			_tokens[token.Key] = token.Value;
		try {
			foreach (var slot in page.Slots)
				_tokens[slot.Key] = Render(slot.Value);
			var contents = page.ShowPageHeader ? RenderPageContent(page) : RenderChildPageItems();
			return page.RenderFrame ? RenderPageInternal(page, contents) : contents;
		} finally {
			_theme = previousTheme;
			_tokens = previousTokens;
		}
	}

	protected virtual string RenderPageInternal(DocumentBlock page, string contents) {
		var framingTokens = new RenderTokens(page) {
			["object-id"] = Encode(page.Metadata.SourceId ?? RenderingContext.GetAnchor(page), true, false, false),
			["object-type"] = Render(page.Metadata.ObjectType),
			["created_time"] = page.CreatedTime,
			["last_updated_time"] = page.UpdatedTime,
			["type"] = "page",
			["id"] = Encode(page.Metadata.SourceId ?? RenderingContext.GetAnchor(page), true, false, false),
			["style"] = Render(page.PageStyle),
			["title"] = Render(page.Title),
			["description"] = Render(page.Description),
			["keywords"] = Render(string.Join(", ", page.Keywords)),
			["author"] = Render(page.Author)
		};
		return RenderTemplate(
			"page",
			new RenderTokens(framingTokens) {
				["page_content"] = contents
			}
		);
	}

	protected virtual string RenderPageContent(DocumentBlock page) {
		var useCoverTitle = page.TitleOnCover;
		return RenderTemplate(
			"page_content",
			new RenderTokens(page) {
				["id"] = Encode(RenderingContext.GetAnchor(page), true, false, false),
				["title"] = Render(page.Title),
				["page_name"] = Render(page.Name),
				["page_title"] = RenderTemplate("page_title", new() { ["text"] = !useCoverTitle ? Render(page.Title) : string.Empty }),
				["page_subtitle"] = page.Subtitle.Length > 0 ? RenderTemplate("page_subtitle", new() { ["subtitle"] = Render(page.Subtitle), ["description"] = Render(page.Subtitle) }) : string.Empty,
				["page_cover"] = string.IsNullOrEmpty(page.CoverUrl) ? string.Empty : RenderTemplate(
					"page_cover",
					new RenderTokens {
						["cover_url"] = Encode(CssUrl(page.CoverUrl), true, false, false),
						["cover_title"] = useCoverTitle ? RenderTemplate("page_cover_title", new() { ["text"] = Render(page.Title) }) : string.Empty
					}
				),
				["thumbnail"] = page.Icon switch {
					{ Emoji.Length: > 0 } => RenderTemplate(
						!string.IsNullOrEmpty(page.CoverUrl) ? "thumbnail_emoji_on_cover" : "thumbnail_emoji",
						new RenderTokens { ["thumbnail_emoji"] = Render(page.Icon.Emoji) }
					),
					{ Url.Length: > 0 } => RenderTemplate(
						!string.IsNullOrEmpty(page.CoverUrl) ? "thumbnail_image_on_cover" : "thumbnail_image",
						new RenderTokens { ["thumbnail_url"] = Encode(page.Icon.Url, true, false, false) }
					),
					_ => string.Empty
				},
				["children"] = RenderChildPageItems(),
				["created_time"] = page.CreatedTime,
				["last_updated_time"] = page.UpdatedTime
			}
		);
	}

	protected override string Render(TemplateBlock template) {
		var previousTheme = _theme;
		var previousTokens = _tokens;
		if (template.Themes.Length > 0)
			_theme = LoadThemes(template.Themes);
		_tokens = new Dictionary<string, object>(_tokens, StringComparer.Ordinal);
		foreach (var token in template.Tokens)
			_tokens[token.Key] = token.Value;
		try {
			var tokens = new RenderTokens(template);
			foreach (var slot in template.Slots)
				tokens[slot.Key] = Render(slot.Value);
			return RenderTemplate(template.Template, tokens);
		} finally {
			_theme = previousTheme;
			_tokens = previousTokens;
		}
	}

	protected override string Render(GroupBlock group) => RenderChildPageItems();

	protected override string Render(TableBlock block)
		=> RenderTemplate(
					"table",
					new RenderTokens(block) {
						["color"] = GetContainerColor(),
						["column_count"] = block.ColumnCount,
						["table_rows"] = RenderChildPageItems(),
					}
				);

	protected override string Render(TableRowBlock block) {
		TableBlock table = RenderingContext.GetParentRenderingNode() as TableBlock;
		var rowIndex = RenderingContext.CurrentIndex;
		var hasRowHeader = table?.FirstRowIsHeader == true;
		var hasColHeader = table?.FirstColumnIsHeader == true;
		return RenderTemplate(
			hasRowHeader && rowIndex == 0 ? "table_header_row" : "table_row",
			new RenderTokens(block) {
				["row_index"] = rowIndex,
				["table_row_cells"] = Merge(block.Cells.Select((cell, columnIndex) => RenderTemplate(
					hasRowHeader && rowIndex == 0 || hasColHeader && columnIndex == 0 ? "table_header_cell" : "table_cell",
					new RenderTokens(cell) {
						["row_id"] = Encode(RenderingContext.GetAnchor(block), true, false, false),
						["row_index"] = rowIndex,
						["col_index"] = columnIndex,
						["contents"] = Render(cell.Text)
					}
				)))
			}
		);
	}

	protected override string Render(TableCellBlock cell) => Render(cell.Text);

	protected override string Render(BreadcrumbBlock block) {
		return RenderTemplate(
			"breadcrumb",
			new RenderTokens(block) {
				["breadcrumb_items"] = Merge(block.Items.Select(item => RenderTemplate(
					!item.Reference.IsAvailable || string.IsNullOrWhiteSpace(item.Reference.Url) || item.IsCurrent ? "breadcrumb_item_disabled" : "breadcrumb_item",
					new RenderTokens {
						["type"] = "reference",
						["data"] = Render(item.Reference.Icon?.Emoji ?? item.Reference.Icon?.Url),
						["text"] = Render(item.Reference.Label),
						["url"] = Encode(item.Reference.Url, true, false, false),
						["icon"] = Render(item.Reference.Icon)
					}
				)))
			}
		);
	}

	protected override string RenderBulletedList(ListBlock bullets)
		=> RenderTemplate(
					"bulleted_list",
					new RenderTokens(bullets) {
						["contents"] = Merge(bullets.Items.Select((bullet, index) => Render((VisualNode)bullet, bullets.Start + index))),
						["start"] = bullets.Start,
						["color"] = GetContainerColor(bullets.Color)
					}
				);

	protected override string RenderBulletedItem(int number, ListItemBlock block) {

		return RenderTemplate(
			"bulleted_list_item",
			new RenderTokens(block) {
				["number"] = number,
				["contents"] = Render(block.Text),
				["color"] = ToColorString(block.Color),
				["children"] = RenderChildPageItems(),
			}
		);
	}

	protected override string Render(CalloutBlock block)
		=> RenderTemplate(
					"callout",
					new RenderTokens(block) {
						["icon"] = Render(block.Icon),
						["text"] = Render(block.Text),
						["color"] = ToColorString(block.Color),
						["children"] = RenderChildPageItems()
					}
				);

	protected override string Render(ReferenceBlock block) => RenderReference(block.Reference, false);

	protected override string Render(RawHtmlBlock block) => block.Html;

	protected override string Render(CodeBlock block) {
		var rawCode = block.Code;
		var lang = typeof(CodeLanguage).GetField(block.Language.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "text";

		return RenderTemplate(
			"code",
			new RenderTokens(block) {
				["language"] = lang,
				["code"] = Encode(rawCode, true, true, false),
				["raw_code"] = rawCode
			}
		);
	}

	protected override string Render(ColumnBlock block)
		=> RenderChildPageItems();

	protected override string Render(ColumnListBlock block)
		=> block.Columns.Length switch {
			0 => RenderTemplate(
					"column_list_1",
					new RenderTokens(block) {
						["column_1"] = string.Empty,
					}
				),
			var item and > 0 and <= 12 => RenderTemplate(
					$"column_list_{item}",
					new RenderTokens(
						RenderTokens
							.ExtractTokens(block)
							.Concat(
								Enumerable.Range(0, item).Select(
										index => new KeyValuePair<string, object>($"column_{index + 1}", (object)Render((VisualNode)block.Columns[index]))
								)
						)
					)
				),
			var item => throw new InvalidOperationException($"Unable to render pages with {item} or more columns")
		};

	protected override string Render(DividerBlock block)
		=> RenderTemplate("divider");

	protected override string Render(EquationBlock block)
		=> RenderTemplate(
						"equation_block",
						new RenderTokens(block) {
							["expression"] = Encode(block.Expression, true, true, false),
						}
					);

	protected override string Render(HeadingBlock block)
		=> block.Level switch {
			1 => RenderHeadingOne(block),
			2 => RenderHeadingTwo(block),
			3 => RenderHeadingThree(block),
			_ => throw new ArgumentOutOfRangeException(nameof(block.Level))
		};

	protected virtual string RenderHeadingOne(HeadingBlock block)
		=> block.IsToggleable switch {

			true => RenderTemplate(
				block.IsOpen ? "toggle_open" : "toggle_closed",
				new RenderTokens(block) {
					["toggle_id"] = $"toggle_{++_toggleCount}",
					["title"] = RenderTemplate(
						"heading_1",
						new RenderTokens(block) {
							["text"] = Render(block.Text),
							["color"] = ToColorString(block.Color)
						}
					),
					["color"] = ToColorString(block.Color),
					["children"] = RenderChildPageItems(),
				}
			),

			false => RenderTemplate(
					"heading_1",
					new RenderTokens(block) {
						["text"] = Render(block.Text),
						["color"] = ToColorString(block.Color)
					}
				)
		};

	protected virtual string RenderHeadingTwo(HeadingBlock block)
		=> block.IsToggleable switch {
			true => RenderTemplate(
				block.IsOpen ? "toggle_open" : "toggle_closed",
				new RenderTokens(block) {
					["toggle_id"] = $"toggle_{++_toggleCount}",
					["title"] = RenderTemplate(
						"heading_2",
						new RenderTokens(block) {
							["text"] = Render(block.Text),
							["color"] = ToColorString(block.Color)
						}
					),
					["color"] = ToColorString(block.Color),
					["children"] = RenderChildPageItems(),
				}
			),
			false => RenderTemplate(
				"heading_2",
				new RenderTokens(block) {
					["text"] = Render(block.Text),
					["color"] = ToColorString(block.Color)
				}
			)
		};

	protected virtual string RenderHeadingThree(HeadingBlock block)
		=> block.IsToggleable switch {
			true => RenderTemplate(
				block.IsOpen ? "toggle_open" : "toggle_closed",
				new RenderTokens(block) {
					["toggle_id"] = $"toggle_{++_toggleCount}",
					["title"] = RenderTemplate(
						"heading_3",
						new RenderTokens(block) {
							["text"] = Render(block.Text),
							["color"] = ToColorString(block.Color)
						}
					),
					["color"] = ToColorString(block.Color),
					["children"] = RenderChildPageItems(),
				}
			),
			false => RenderTemplate(
				"heading_3",
				new RenderTokens(block) {
					["text"] = Render(block.Text),
					["color"] = ToColorString(block.Color)
				}
			)
		};

	protected override string RenderNumberedItem(int number, ListItemBlock block)
		=> RenderTemplate(
					"numbered_list_item",
					new RenderTokens(block) {
						["number"] = number,
						["contents"] = Render(block.Text),
						["color"] = ToColorString(block.Color),
						["children"] = RenderChildPageItems(),
					}
				);

	protected override string RenderNumberedList(ListBlock items)
		=> RenderTemplate(
					"numbered_list",
					new RenderTokens(items) {
						["contents"] = Merge(items.Items.Select((item, index) => Render((VisualNode)item, items.Start + index))),
						["start"] = items.Start,
						["color"] = GetContainerColor(items.Color)
					}
				);

	protected override string Render(ParagraphBlock block) {

		return RenderTemplate(
				"paragraph",
				new RenderTokens(block) {
					["contents"] = Render(block.Text),
					["color"] = ToColorString(block.Color),
					["children"] = RenderChildPageItems(),
				}
			);
	}

	protected override string Render(QuoteBlock block)
		=> RenderTemplate(
					"quote",
					new RenderTokens(block) {
						["text"] = Render(block.Text),
						["color"] = ToColorString(block.Color),
						["children"] = RenderChildPageItems(),
					}
				);

	protected override string Render(TableOfContentsBlock block)
		=> RenderTemplate(
					"table_of_contents",
					new RenderTokens(block) {
						["color"] = ToColorString(block.Color)
					}
				);

	protected override string Render(ToDoBlock block)
		=> RenderTemplate(
					"to_do",
					new RenderTokens(block) {
						["text"] = Render(block.Text),
						["checked"] = block.IsChecked ? "checked" : string.Empty,
						["color"] = ToColorString(block.Color),
						["children"] = RenderChildPageItems(),
					}
				);

	protected override string Render(ToggleBlock block)
		=> RenderTemplate(
					block.IsOpen ? "toggle_open" : "toggle_closed",
					new RenderTokens(block) {
						["toggle_id"] = $"toggle_{++_toggleCount}",
						["title"] = Render(block.Text),
						["color"] = ToColorString(block.Color),
						["children"] = RenderChildPageItems(),
					}
				);

	protected override string RenderText(string content, bool isUrl, bool isBold, bool isItalic, bool isStrikeThrough, bool isUnderline, bool isCode, Color color, (string Url, string Icon, string Indicator) urlInfo = default) {
		if (isUrl)

			return RenderLink(
				urlInfo.Url ?? string.Empty,
				RenderText(content, false, isBold, isItalic, isStrikeThrough, isUnderline, isCode, color),
				urlInfo.Icon ?? string.Empty,
				urlInfo.Indicator ?? string.Empty
			);

		if (isBold)
			return RenderTemplate(
				"text_bold",
				new RenderTokens {
					["text"] = RenderText(content, false, false, isItalic, isStrikeThrough, isUnderline, isCode, color)
				}
			);

		if (isItalic)
			return RenderTemplate(
				"text_italic",
				new RenderTokens {
					["text"] = RenderText(content, false, false, false, isStrikeThrough, isUnderline, isCode, color)
				}
			);

		if (isStrikeThrough)
			return RenderTemplate(
				"text_strikethrough",
				new RenderTokens {
					["text"] = RenderText(content, false, false, false, false, isUnderline, isCode, color)
				}
			);

		if (isUnderline)
			return RenderTemplate(
				"text_underline",
				new RenderTokens {
					["text"] = RenderText(content, false, false, false, false, false, isCode, color)
				}
			);

		if (isCode)
			return RenderTemplate(
				"text_code",
				new RenderTokens {
					["text"] = Render(content),
				}
			);

		if (color != Color.Default)
			return RenderTemplate(
				"text_colored",
				new RenderTokens {
					["color"] = ToColorString(color),
					["text"] = RenderText(content, false, false, false, false, false, false, Color.Default)
				}
			);

		return RenderTemplate(
			"text",
			new RenderTokens {
				["text"] = Render(content),
			}
		);
	}

	protected virtual string RenderReference(Reference reference, bool isInline) {
		var text = !reference.IsAvailable || reference.Url == null
			? Render(reference.Label)
			: RenderText(
				reference.Label, true, false, false, false, false, false, Color.Default,
				(reference.Url, Render(reference.Icon), reference.ShowIndicator && reference.Icon != null ? RenderTemplate("indicator_link") : string.Empty)
			);
		return isInline ? text : RenderTemplate(
			"paragraph",
			new RenderTokens {
				["contents"] = text,
				["color"] = Color.Default,
				["children"] = string.Empty
			}
		);
	}

	protected override string Render(Icon icon)
		=> icon switch {
					{ Emoji.Length: > 0 } => RenderTemplate("icon_emoji", new RenderTokens { ["emoji"] = Render(icon.Emoji) }),
					{ Url.Length: > 0 } => RenderTemplate("icon_image", new RenderTokens {
						["url"] = Encode(icon.Url, true, false, false),
						["alt"] = Encode(icon.AltText, true, false, false)
					}),
			_ => string.Empty
		};

	protected override string Render(MediaBlock block)
		=> block.Type switch {
			MediaType.Audio => RenderAudio(block),
			MediaType.Image => RenderImage(block),
			MediaType.Pdf => RenderPdf(block),
			MediaType.File => RenderFile(block),
			MediaType.Video => RenderVideo(block),
			MediaType.Embed => RenderEmbed(block),
			_ => RenderUnsupported(block)
		};

	protected virtual string RenderAudio(MediaBlock block)
		=> RenderTemplate(
					"audio",
					new RenderTokens(block) {
						["caption"] = Render(block.Caption),
						["url"] = GetFileUrl(block, out _)
					}
				);

	protected virtual string RenderImage(MediaBlock block)
		=> RenderTemplate(
						"image",
						new RenderTokens(block) {
							["url"] = GetFileUrl(block, out _),
							["caption"] = Render(block.Caption)
						}
					);

	protected virtual string RenderPdf(MediaBlock block)
		=> RenderTemplate(
					"pdf",
					new RenderTokens(block) {
						["caption"] = Render(block.Caption),
						["url"] = GetFileUrl(block, out _)
					}
				);

	protected virtual string RenderFile(MediaBlock block) {
		var url = GetFileUrl(block, out var filename);
		return RenderTemplate(
			"file",
			new RenderTokens(block) {
				["filename"] = Render(filename),
				["caption"] = Render(block.Caption),
				["url"] = url,
				["size"] = Render(block.Size),
			}
		);
	}

	protected virtual string RenderVideo(MediaBlock block) {
		if (block.Provider != EmbedProvider.None)
			return RenderSocialMediaVideo(block, block.Provider, block.ProviderId ?? string.Empty, Render(block.Caption));
		return RenderTemplate(
			"embed_video",
			new RenderTokens(block) {
				["caption"] = Render(block.Caption),
				["url"] = GetFileUrl(block, out _)
			}
		);
	}

	protected virtual string RenderEmbed(MediaBlock block) {
		if (string.IsNullOrWhiteSpace(block.Url) && string.IsNullOrWhiteSpace(block.ProviderId))
			return RenderUnsupported(block);
		if (block.Provider == EmbedProvider.Twitter)
			return RenderTemplate(
				"embed_x",
				new RenderTokens(block) {
					["url"] = GetFileUrl(block, out _),
					["caption"] = Render(block.Caption)
				}
			);
		if (block.Provider != EmbedProvider.None)
			return RenderSocialMediaVideo(block, block.Provider, block.ProviderId ?? string.Empty, Render(block.Caption));
		return RenderUnsupported(block);
	}

	protected virtual string RenderSocialMediaVideo(MediaBlock block, EmbedProvider platform, string videoID, string caption) {
		return platform switch {
			EmbedProvider.YouTube => RenderTemplate(
				"embed_youtube",
				new RenderTokens(block) {
					["caption"] = caption,
					["video_id"] = Encode(videoID, true, false, false),
				}),
			EmbedProvider.Rumble => RenderTemplate(
				"embed_rumble",
				new RenderTokens(block) {
					["caption"] = caption,
					["video_id"] = Encode(videoID, true, false, false),
				}),
			EmbedProvider.BitChute => RenderTemplate(
				"embed_bitchute",
				new RenderTokens(block) {
					["caption"] = caption,
					["video_id"] = Encode(videoID, true, false, false),
				}),
			EmbedProvider.Vimeo => RenderTemplate(
				"embed_vimeo",
				new RenderTokens(block) {
					["caption"] = caption,
					["video_id"] = Encode(videoID, true, false, false),
				}),
			_ => throw new NotSupportedException(platform.ToString())
		};
	}

	protected override string RenderUnsupported(VisualNode node)
		=> RenderTemplate(
					"unsupported",
					new RenderTokens(node) {
						["json"] = Encode(node is UnsupportedBlock unsupported ? unsupported.Diagnostic ?? unsupported.FallbackText : "Unsupported " + node.GetType().Name, true, false, false),
						["text"] = node is UnsupportedBlock fallback ? Render(fallback.FallbackText) : string.Empty
					}
				);

	#endregion

	#region Aux

	protected virtual string GetFileUrl(MediaBlock file, out string filename) {
		filename = file.FileName;
		return Encode(file.Url, true, false, false);
	}

	protected virtual string RenderTemplate(string widgetType)
		=> RenderTemplate(widgetType, new RenderTokens());

	protected virtual string RenderTemplate(string widget, RenderTokens tokens) {
		var previousTokens = _tokens;
		_tokens = new Dictionary<string, object>(_tokens, StringComparer.Ordinal);
		foreach (var token in tokens)
			_tokens[token.Key] = token.Value ?? string.Empty;
		if (tokens.Node != null)
			_tokens["object_id"] = Encode(RenderingContext.GetAnchor(tokens.Node), true, false, false);
		if (!_tokens.ContainsKey("color") && !_theme.Tokens.ContainsKey("color")) {
			var color = GetContainerColor();
			// A missing color must not activate background:inherit and reset legacy cover sizing/position.
			_tokens["color"] = color == ToColorString(Color.Default) ? string.Empty : color;
		}
		try {
			return _theme.Expand(FetchTemplate(widget, ".html"), _tokens);
		} finally {
			_tokens = previousTokens;
		}
	}

	protected virtual string FetchTemplate(string widgetName, string fileExt)
		=> _theme.GetTemplate(widgetName, fileExt);

	protected string GetContainerColor(Color color = Color.Default) {
		if (color != Color.Default)
			return ToColorString(color);
		if (_tokens.TryGetValue("color", out var ambientColor) || _theme.Tokens.TryGetValue("color", out ambientColor))
			return Convert.ToString(ambientColor, CultureInfo.InvariantCulture) ?? string.Empty;
		// Synthetic containers inherit the nearest source block's color, including its default.
		for (var index = RenderingContext.RenderingStack.Count - 2; index >= 0; index--)
			switch (RenderingContext.RenderingStack[index]) {
				case CalloutBlock block:
					return ToColorString(block.Color);
				case QuoteBlock block:
					return ToColorString(block.Color);
				case ParagraphBlock block:
					return ToColorString(block.Color);
				case HeadingBlock block:
					return ToColorString(block.Color);
				case ToDoBlock block:
					return ToColorString(block.Color);
				case ToggleBlock block:
					return ToColorString(block.Color);
				case ListItemBlock block:
					return ToColorString(block.Color);
			}
		return ToColorString(Color.Default);
	}

	protected string ToColorString(Color color) => VisualFormatting.Color(color);

	protected string Encode(string text, bool htmlEncode, bool escapeBraces, bool convertNewlinesToBreaks) {
		text ??= string.Empty;
		if (htmlEncode)
			text = WebUtility.HtmlEncode(text);
		// ThemeSession inserts render tokens literally; braces need no escaping.
		if (convertNewlinesToBreaks)
			text = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br />");
		return text;
	}

	private ThemeSession LoadThemes(IReadOnlyList<string> themes) {
		var key = string.Join("\0", themes);
		if (_sessions.TryGetValue(key, out var session))
			return session;
		session = _snapshot.CreateSession(themes, RenderingContext.Options.Environment, RenderingContext.Options.Mode, RenderingContext.Options.AssetBaseUrl);
		_sessions.Add(key, session);
		foreach (var asset in session.Assets)
			_assets.TryAdd(asset.RelativePath, asset);
		SuppressFormatting |= session.SuppressFormatting;
		return session;
	}

	private static string CssUrl(string value)
		=> (value ?? string.Empty).Replace("\\", "%5C").Replace("'", "%27").Replace("\r", "%0D").Replace("\n", "%0A").Replace("\f", "%0C");

	#endregion

	#region Inner Classes

	protected class RenderTokens : Dictionary<string, object> {

		public RenderTokens() {
			this["object_id"] = string.Empty;
			this["object_type"] = "misc";
			this["type"] = string.Empty;
		}

		public RenderTokens(VisualNode node)
			: this(ExtractTokens(node)) {
			this.Node = node;
		}

		public RenderTokens(Metadata metadata)
			: this(ExtractTokens(metadata)) {
		}

		public RenderTokens(IEnumerable<KeyValuePair<string, object>> values)
			: base(values, StringComparer.Ordinal) {
		}

		public VisualNode Node { get; }

		private static string GetNodeType(VisualNode node) => node switch {
			DocumentBlock => "page",
			ParagraphBlock => "paragraph",
			HeadingBlock item => "heading-" + item.Level,
			QuoteBlock => "quote",
			CalloutBlock => "callout",
			ToDoBlock => "to-do",
			ToggleBlock => "toggle",
			CodeBlock => "code",
			EquationBlock => "equation",
			DividerBlock => "divider",
			ListBlock item => item.Type == ListType.Numbered ? "numbered-list" : "bulleted-list",
			ListItemBlock => "list-item",
			ColumnListBlock => "column-list",
			ColumnBlock => "column",
			TableBlock => "table",
			TableRowBlock => "table-row",
			TableCellBlock => "table-cell",
			MediaBlock item => item.Type.ToString().ToLowerInvariant(),
			DatabaseBlock => "database",
			ReferenceBlock => "reference",
			BreadcrumbBlock => "breadcrumb",
			TableOfContentsBlock => "table-of-contents",
			UnsupportedBlock item => item.Type,
			RawHtmlBlock => "html",
			TemplateBlock => "template",
			_ => "group"
		};

		public static IEnumerable<KeyValuePair<string, object>> ExtractTokens(VisualNode node) {
			foreach (var token in ExtractTokens(node.Metadata))
				yield return token.Key == "type" ? new("type", WebUtility.HtmlEncode(node.Metadata.Type ?? GetNodeType(node))) : token;
		}

		public static IEnumerable<KeyValuePair<string, object>> ExtractTokens(Metadata metadata) {
			yield return new("object_id", WebUtility.HtmlEncode(metadata.Anchor ?? metadata.SourceId ?? string.Empty));
			yield return new("source_id", WebUtility.HtmlEncode(metadata.SourceId ?? string.Empty));
			yield return new("object_type", WebUtility.HtmlEncode(metadata.ObjectType));
			yield return new("type", WebUtility.HtmlEncode(metadata.Type ?? string.Empty));
		}
	}

	#endregion
}
