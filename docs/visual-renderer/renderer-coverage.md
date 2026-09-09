# Renderer source coverage and compatibility

The input to `Sphere10.VisualRenderer` is a finite visual tree. The source mapping lives in `LocalNotion.Core/Rendering/NotionRenderModelBuilder*.cs`; the renderer never queries that source, a repository, or a link resolver.

This inventory maps the reachable branches of the former recursive HTML/text engines. It is an implementation map, not a claim that every possible SDK payload has an individual fixture.

## Documents and recursive blocks

| Source branch | Visual representation | Preparation and rendering behavior |
| --- | --- | --- |
| Root page | `DocumentBlock` | Core supplies title/name, summary, keywords, author, timestamps, cover/icon, themes and title-on-cover presentation. The renderer controls page framing and ordered children. |
| Root database | `DocumentBlock` containing `DatabaseBlock` | Core loads row pages and prepares columns/cells. Database description becomes subtitle/description. Legacy database text omission is an explicit empty text override. |
| Nested page, child-page block | `ReferenceBlock` | Core resolves label, URL, icon and availability. These never recursively expand target pages. Child-page indicators are suppressed. |
| Nested database | `DatabaseBlock` | Core materializes its own stored graph and row values before rendering. |
| Paragraph | `ParagraphBlock` | Rich text, color and explicit owned children. |
| Heading one/two/three | `HeadingBlock` | Level, rich text, color, toggleability and children. Non-toggle HTML headings retain the previous child omission; text retains source children. Heading links select their template from visual ancestry. |
| Bulleted/numbered list item | `ListBlock` and `ListItemBlock` | Core groups adjacent items separately within each parent; intervening content ends a list. Nested groups belong to their item. The visual list owns `Type` (`ListType`), start and color. |
| Quote | `QuoteBlock` | HTML renders owned children. Core supplies the legacy text override that omits quote children. |
| Callout | `CalloutBlock` | Rich text, normalized icon, color and children. |
| To-do | `ToDoBlock` | Rich text, checked state, color and children. Legacy plain text contains the text without a checkbox prefix. |
| Toggle | `ToggleBlock` | Rich title, color, open/closed presentation and children; toggle IDs belong to the render invocation. |
| Code | `CodeBlock` | Code, enum language and caption are separate fields; language descriptions supply Prism identifiers and unknown source languages fall back to text. HTML encoding happens inside the renderer. Core preserves source-specific text behavior. |
| HTML code captioned `{INJECT}` | `RawHtmlBlock` | Core recognizes the existing authoring convention. The renderer only accepts explicit trusted HTML plus a separate text representation. |
| Equation | `EquationBlock` | Raw expression; the HTML renderer encodes it and applies the equation template. |
| Divider | `DividerBlock` | HTML template and legacy text separator. |
| Column list/column | `ColumnListBlock` / `ColumnBlock` | Typed ordered columns and children. Existing HTML support remains zero through twelve columns. |
| Table/table row | `TableBlock`, `TableRowBlock`, `TableCellBlock` | Ordered rich cells and explicit header dimensions. Core maps legacy `HasRowHeader` to first-row headers and `HasColumnHeader` to first-column headers. No source-parent lookup remains in rendering. |
| Link-to-page block | `ReferenceBlock` | Prepared target, icon and indicator; unavailable targets become prepared diagnostic labels. |
| Breadcrumb | `BreadcrumbBlock` | Core computes ancestry and rebases links from the final output directory; the model supplies enabled/current items and icons. Text omission remains explicit. |
| Table of contents | `TableOfContentsBlock` | Existing empty navigation template/browser behavior remains. Other callers can supply explicit entries. Core preserves the old text label. |
| User object | `ParagraphBlock` with `PersonInline` | Display name/email are detached from SDK identity/owner objects. Core preserves the former user text representation. |
| Bookmark, child-database block, synced block, source template block, link-preview block, unsupported/unknown block | `UnsupportedBlock` | Prepared diagnostic string and explicit text fallback. This extraction does not expand synced sources or implement previously unsupported block types. A source template block is unrelated to the renderer's neutral `TemplateBlock` composition extension. |
| Missing graph object, containment cycle, excessive source depth | `UnsupportedBlock` | Projection terminates and records a neutral diagnostic instead of leaving a renderer lookup or infinite recursion. |

## Rich text, icons and media

| Source family | Visual representation | Behavior |
| --- | --- | --- |
| Text rich span | `TextInline`, `TextStyle` | Content and resolved URL; bold, italic, underline, strike-through, code and named colors. Original linked-text URLs can be retained explicitly for keyword extraction. |
| Equation rich span | `EquationInline` | Raw expression remains independent of the SDK. |
| Page/database mention | `ReferenceInline` | Resolved title/URL/icon/indicator. The Core text projection preserves the legacy reference omission. An available empty URL is a valid current-document link. |
| User/date mention | `PersonInline` / `DateInline` | Name/email or date range. Core retains legacy date text while HTML can honor date-only precision. |
| Unknown mention, custom emoji mention, link mention/preview, template mention, known mention with missing payload | `TextInline` fallback | Preserves source plain text; an entirely absent mention payload remains empty. |
| Emoji icon | `Icon.Emoji` | Used by page thumbnails, references and callouts. |
| Uploaded/external/custom-emoji/built-in image icon | `Icon.Url` | Core supplies the final URL; no uploaded-file handle or SDK icon object crosses the boundary. |
| Image/audio/PDF/file block | `MediaBlock` | Final URL, caption, file label and optional alt text. Uploaded file labels use the resolved path. Source text quirks remain explicit overrides. |
| Uploaded/external video | `MediaBlock` | Native video URL or a prepared provider and video ID for YouTube, Rumble, BitChute or Vimeo. |
| X/Twitter embed | `MediaBlock` with Twitter provider | Core prepares the compatible URL and caption. Text retains the original source URL. |
| Video-provider embed | `MediaBlock` with provider | Core prepares provider/ID/caption; legacy text omission is explicit. |
| Blank/unsupported embed or video payload | `UnsupportedBlock` | Preserves the old tolerant blank/fallback behavior without invoking URL helpers on empty data. |

## Database values

Columns are materialized once as an ordered union of row keys; every row aligns its values to those columns. Missing cells become `EmptyValue`. Source database schema/request/response classes are not part of the renderer model.

| Source property values | Visual value |
| --- | --- |
| Title | `ReferenceValue` with block presentation, preserving the existing linked title paragraph |
| Rich text | `TextValue` |
| Checkbox | `BooleanValue` |
| Number | `NumberValue` |
| Date | `DateValue` with start/end, timezone and time precision |
| Created/edited timestamp | `TextValue` retaining the source display convention |
| Created/edited by, people | `PeopleValue` |
| Email and URL | `LinkValue` |
| Phone | `TextValue` |
| Files | `FilesValue` containing detached `MediaBlock` values |
| Select, multi-select and status | `ChoiceValue` with labels/colors |
| Relation | `ReferenceValue` containing prepared target labels/URLs |
| Formula | `ComputedValue` containing evaluated string, number, boolean or date values; the existing array-string fallback remains |
| Rollup | `ComputedValue` containing number, date or recursive `CompoundValue` arrays |
| Null | `EmptyValue` |
| Unknown/unsupported property or computed-result type | `UnsupportedValue` with a prepared diagnostic rendered by the HTML unsupported template |

Boolean formulas, aligned heterogeneous rows, date-only HTML display and resolved relation links are intentional improvements over the former implementation. No formula evaluation or SDK discriminator parsing takes place in the renderer.

## Composition, themes and state

`TemplateBlock` combines trusted scalar presentation tokens and typed node slots. `GroupBlock` groups slot children. Nested `DocumentBlock` values can choose their own themes, retain the page-content wrapper, render only their children, or omit the outer document frame. CMS selection and link preparation stay in Core. The CMS builder HTML-encodes source titles, summaries and attribute values before inserting scalar tokens; explicit internal code stays trusted. Cover and article-feature URLs also escape CSS string delimiters.

The HTML renderer creates one theme catalog snapshot per call, shares it across nested scopes, and restores ambient tokens/themes when each scope exits. Traversal ancestors, generated anchors, toggle numbering and assets belong to that call. Source identity and occurrence anchors are distinct model fields. Theme assets are returned as immutable in-memory content with collision-safe addresses.

## Validation

- `tests/LocalNotion.Core.Tests/Rendering/Characterization`: preserved legacy HTML main-DOM and text goldens from a deterministic 31-object recursive source fixture. Its README documents the deliberate list-ID/default-color/start differences.
- `tests/Sphere10.VisualRenderer.Tests`: independent consumer referencing only the renderer; typed values/media/composition, assets, no theme extraction, literal code and token-like text, self links, title-property wrappers, unknown-property diagnostics, repeated/parallel calls and containment-cycle detection.
- `tests/LocalNotion.Core.Tests/Rendering/Projection`: source projection without repository access for early text, detached rendering after source mutation/removal, recursive ownership, missing/cyclic/deep graph handling, final link resolution, mixed ID representations, embed captions/providers, uploaded file labels and aligned database rows.
- `tests/Sphere10.VisualRenderer.Tests/Themes`: embedded/file overlay behavior, inheritance, modes, includes, immutable snapshots and bundle references.
- `tests/LocalNotion.Core.Tests/Rendering/Integration`: final output allocation, filename conflicts, cross-links, uploaded assets, physical output publication and CMS integration.
- `tests/LocalNotion.Core.Tests/Repository`: existing Windows-style stored path compatibility on the current host.

