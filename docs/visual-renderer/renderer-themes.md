# Embedded themes and output assets

`Sphere10.Framework.Web.VisualRenderer` embeds one ZIP containing the built-in theme templates, includes, configuration, and assets. Rendering decompresses the selected theme entries in memory and does not create or populate a theme directory.

The editable source files remain under the renderer project's `Themes/` folder,
excluded from Visual Studio's ordinary project items. The build target packages
them directly from disk, including hidden theme directories and `.config.json`
files, while excluding C# source. It preserves entry paths and content and keeps
the generated archive's timestamp when its bytes are unchanged. Design-time
builds skip archive generation. `EmbeddedThemes.OpenArchive()` opens a fresh
read-only .NET `ZipArchive`; each caller owns and disposes its archive and entry
streams, so concurrent readers do not share a stream position.

```csharp
var renderer = new HtmlRenderer(new ThemeCatalog(new ThemeOptions {
    ThemesDirectory = optionalOverrideDirectory
}));
var result = renderer.Render(document, new RenderOptions {
    Themes = ["default"],
    Environment = RenderEnvironment.Offline,
    AssetBaseUrl = "../render-assets"
});
```

Omit `ThemeCatalog` or `ThemesDirectory` to use the embedded defaults. A configured directory need not exist. LocalNotion additionally deploys missing built-in files to its configured themes path, normally `.localnotion/themes`, on repository creation and opening. Existing files are preserved. This deployment belongs to Core; the standalone renderer does not perform it.

To override one file, create the corresponding named-theme path, for example `.localnotion/themes/default/paragraph.html`. The remaining files, including `.config.json`, fall back to the assembly. Empty files count as overrides. In the standalone renderer, removing an override reveals the embedded version on the next independent render or render batch. LocalNotion also restores that missing built-in file on its next repository open. A session keeps the bytes it read, so editing files during later renders cannot change an earlier result. `HtmlRenderer` calls `ThemeCatalog.CreateSnapshot()` per render; a catalog that is already a snapshot returns itself and reuses sessions for the same ordered themes, environment, mode and asset base URL. Pass `catalog.CreateSnapshot()` to the renderer when rendering a group of documents against one consistent set of overrides. Direct `CreateSession` calls on a catalog with a mutable override directory refresh independently.

A custom theme needs its own `.config.json` and can inherit a built-in theme:

```json
{
  "type": "html",
  "base": "default",
  "tokens": {
    "BrandName": { "offline": "Example", "online": "Example" }
  }
}
```

For each named theme, disk files override the matching embedded files. The first selected theme contributes its complete base chain, applied from the oldest base through the selected theme. Each later selected theme overlays only its own files and tokens, in selection order; its base chain is not applied again. An explicitly selected base later in the list therefore overrides earlier values with its own definitions. This preserves the original renderer's cascade: decorative themes can replace their specific includes without resetting earlier carousel, column, or typography templates. File origin never outranks these theme priorities.

The existing template selection order remains: mode folder plus environment variant, mode folder generic, root generic, root environment variant. In particular, `readonly/paragraph.html` is selected before `paragraph.html`.

Missing required templates, invalid configuration, unknown themes, and inheritance/include cycles report `ThemeException`; invalid override files are not silently ignored. File names and token names use ordinal comparison. Preserve their spelling across operating systems.

Previously extracted theme copies remain ordinary overrides. They can mask newer embedded defaults and corrected built-in template references. The library never deletes or rewrites them automatically.

Escape literal CSS and JavaScript braces inside templates and includes as `{{`
and `}}`. Single braces belong to template expressions. For example, a CSS
declaration block beginning with `color:` can otherwise be interpreted as the
existing `color` token with a format suffix. Check the rendered stylesheet and
script rather than validating only their unexpanded source. Ordinary resource
files are emitted as assets and do not need this template escaping.

Full-document root Page and database wrappers begin with `<!DOCTYPE html>` so HTML parsers select standards mode. Fragments (`RenderFrame = false` or `RenderSnippet`) do not receive that document preamble. Existing extracted wrappers may predate this correction and continue to select quirks mode. A selected modifier can override `readonly/page.online.html` or `readonly/page.offline.html` through the normal template cascade while retaining the matching `{include://page.online.html}` or `{include://page.offline.html}` for the existing inner head/body composition; changing a shared `.root` override affects all documents using it.

## Page covers and header heights

The default cover keeps its existing `35vh` height, `200px` minimum, centered background image and full-height Bootstrap title wrapper. Its native document subtitle retains the normal outside `page_subtitle.html` location. Cover presentation can be changed through the same ordered Theme composition used by other native headers; it does not require rewriting the shared cover stylesheet.

When a native Page has both a cover and an icon, `thumbnail_image_on_cover.html`
or `thumbnail_emoji_on_cover.html` centres the icon on the cover's bottom edge.
Half of the image's 124.32px box, or the emoji's 5rem line box, overlaps the cover;
the lower half remains in document flow to keep the following title and body
clear. No-cover icons use their ordinary fragments, and navigation themes retain
their separate icon composition. A modifier that inherits the stock overlap must
not redirect these on-cover slots to the ordinary no-cover fragments.

Select `header_cover` for a white, naturally sized title/optional Subtitle group centered with Bootstrap flex utilities. Its cover template owns a definite viewport height through `cover_height.inc`, defaulting to `35vh` to match the existing stock cover stylesheet. Select an existing size modifier after it: `header_tiny` supplies `15vh`, `header_small` supplies `25vh`, `header_large` supplies `75vh`, and `header_full` supplies `100vh`. The normal cover minimum height still applies. For example, `Themes = ["cms", "header_cover", "header_large"]` composes the CMS page layout with a large centered cover. The size modifiers retain their original `header_height.inc` classes for existing carousel Headers; the new cover include does not change that composition.

`header_cover` uses the supported configuration token `page_subtitle_placement` with offline/online values of `cover`. When an actual native cover and `TitleOnCover` are present, this opts the native document Subtitle into the cover group. The renderer supplies `{text}` and optional `{cover_subtitle}` to `page_cover_title.html`; the fragment `page_cover_subtitle.html` receives the rendered rich text as both `{subtitle}` and `{description}`. A missing or empty native Subtitle emits no fragment. The title stays centered by itself, while a title/Subtitle pair centers as one natural-height group and moves the title upward. Preserve `page-cover-title`, its `data-toc-skip` attribute and the Theme's cover-only lettering. Body headings and Typed content stay in the body; they are not inferred banner subtitles.

The renderer does not inspect rendered HTML or identify a Theme by name to decide this placement. Default and legacy Themes without the explicit token keep their outside subtitle. A Theme opting into cover placement owns the matching title/Subtitle fragments. Without an actual cover or when `TitleOnCover` is false, native subtitles remain outside even if that Theme is selected. `TitleOnCover` continues to suppress the normal body title without a native cover, because a parent CMS or custom frame may already own it.

Previously extracted files remain ordinary overrides and can mask newer built-in Theme files. Adopt the selected `header_cover` files and matching size include through the normal Theme installer when refreshing a deployment. Its markup and Bootstrap utilities work with the existing shared stylesheet; the default cover stylesheet and carousel height behavior remain unchanged. A local renderer update does not itself update an independently hosted asset URL.

## Presentation Themes and document behavior

Themes select ordered template, token and resource overrides. They do not interpret source metadata or CMS Tags. A source adapter translates behavior controls into explicit visual-model properties before rendering. For example, Local Notion's `@HideEmbeddedPages` Tag sets `DocumentBlock.HideEmbeddedPages`; it does not add a Theme of the same name.

```csharp
var document = new DocumentBlock {
	Themes = ["default"],
	HideEmbeddedPages = true,
	Children = [
		new ReferenceBlock {
			IsEmbeddedPage = true,
			Reference = new Reference { Url = "/manual", Label = "Contained manual" }
		},
		new ParagraphBlock { Text = [new TextInline { Text = "Read a chapter", Url = "/manual#chapter" }] }
	]
};
```

The automatic contained-manual widget is omitted from HTML while the authored chapter link remains visible. Render the destination independently with its normal content and presentation Themes. The flag does not remove model/source nodes, change publication or access policy, suppress other child content or hide the destination's document body. It defaults to `false` and leaves plain-text output unchanged.

The nearest containing `DocumentBlock` determines suppression, independently of Theme selection. A nested document owns its own flag even if it inherits its parent's presentation Themes. The behavior works in online/offline and editable/read-only HTML without JavaScript removal, route-specific conditions or `display:none` rules.

Visible embedded references still pass normal reference HTML through `embedded_page.html`. The default slot is `{contents}`; a custom Theme may style it through the normal cascade. This presentation hook is separate from the document's visibility control. An earlier development build supplied a `hide_embedded_pages` suppression Theme; consumers of that build must explicitly move the behavioral selection to the canonical `@HideEmbeddedPages` source Tag or `HideEmbeddedPages` model flag and retire only that obsolete Theme override. The renderer does not silently reinterpret a Theme name as a Tag.

## Checklist rich text

The `bullet_check` modifier keeps its check badge separate from a single shrinkable content wrapper. Rich-text runs and hyperlinks stay in normal inline flow, and nested `.ln-bulleted-list-item-children` remain inside that wrapper below the item text. The wrapper uses `min-width:0` and `overflow-wrap:anywhere` so narrow layouts can wrap long linked labels and technical tokens without clipping them or forcing an unbroken flex row.

Existing extracted `bullet_check/bulleted_list_item.html` files remain ordinary project overrides and can mask this correction. Update that one override through the normal theme workflow when adopting the corrected layout; installing a newer renderer preserves an existing override. A template-only project override does not update the installed renderer binary.

## Article listing pages

The stock `cms_articles` templates render one already selected page of article
summaries. The host owns the complete catalogue, category membership, visibility,
ordering, page size and query/file routing. Supply only the current page's
`summaries` slot; the renderer does not load or filter a catalogue in JavaScript.
Each article's existing destination remains independent of its listing page.

The stock presentation uses a left topic hierarchy beside consistent article
summary rows. On desktop the hierarchy stays within the article section as the
reader scrolls, and its height is bounded so long category trees remain usable.
On narrow screens the same navigation becomes a native **Browse topics**
disclosure. Article titles, creation dates, summaries and existing feature
images remain host-supplied data. Each complete summary is one native link to its
independent article page, including the title, image, description and surrounding
space. It supports normal keyboard activation, modifier-click and link context menus.

Each title and creation date spans the full article header. The image and
summary row sits underneath, with `articles_summary` placing the feature on the
left and `articles_summary_alt` placing it on the right. The host retains its
existing variant sequence. Dates display without a label. A palette-derived
background and title color indicate summary hover; keyboard focus also has a
visible outline. `--ln-articles-summary-hover-bg` can override the hover background.
Hierarchy links use body-color text at normal weight, with indentation and guide
lines identifying nesting. Only the current destination is bold and has a selected
background; ancestor destinations can retain the primary-color cue without bold.

Presentation and runtime live in `cms_articles/resources/css/articles.css` and
`resources/js/articles.js`. The template references them through normal
`theme://` tokens, with a deferred external script rather than page-local CSS
or initializer blocks. The theme's own `online_url` resolves these files from
the Sphere10 CDN online; offline output returns their bytes in the normal asset
bundle. Inherited CMS/default resources retain their existing online owners.

This component styles the article-listing section, including its hierarchy,
summary entries and pagination. A consumer's page banner, global navigation,
footer and unrelated content keep their existing presentation. Scope article
styles to the component rather than applying global heading or page rules.
Collection titles and hierarchy labels come from the supplied model; they need
not be named "Articles" or belong to a particular website.

The article component's own wrapper, hierarchy layout, local headings, summary
composition and pagination framing belong to these templates. They can be
reworked without replacing the surrounding page frame.

Extend these built-in templates through the existing `default`/CMS theme
cascade and typed slots. Keep shared template changes scoped to the article
component so adopting the stock article presentation does not restyle the
consumer's other sections or require a site-specific theme.

Listing and article URLs are opaque, host-resolved destinations. A listing may
be at `/fixed/fixed2/knowledge/security?page=2`, while an article remains at
`/fixed/fixed2/knowledge/security/signing-messages`. Templates must not assume
an `/articles` prefix, a fixed segment depth or a site-specific domain, and must
not derive article destinations from pagination links.

The optional `pagination` slot on `articles` accepts an `articles_pagination`
`TemplateBlock`. Its `links` slot contains `articles_page_link` blocks with
prepared `url` and `label` tokens, an `articles_page_active` block with `label`
for the current page, and optional `articles_page_gap` blocks for omitted ranges.
Page links are native anchors; the active page and gaps are noninteractive
markers. Supply the final resolved URL, including the host's query or offline
file routing, rather than asking a template to reconstruct it from a slug.

Pass these scalar tokens on the containing `articles` block; its pagination
child inherits them through the existing template scope:

| Token | Meaning |
| --- | --- |
| `range_start`, `range_end` | Inclusive positions of the visible summaries; both zero for an empty listing. |
| `total_count` | Total articles in this listing's permitted catalogue/category scope. |
| `page_number`, `page_count` | Current and total listing pages, starting at one. |

A single-page or empty listing may supply the pagination wrapper with an empty
`links` slot. Its range remains visible, while the navigation is hidden when
`page_count` is one. Existing callers that omit `pagination` keep their prior
summary-only output; the stock configuration defaults the optional slot and its
range tokens to empty. An incomplete wrapper with no total is hidden rather
than presenting a dangling count.

`TemplateBlock` scalar tokens remain trusted output strings. Encode ordinary
labels and URL attributes before supplying them, as with the other article
templates. Pagination uses native links and the article theme's shared stylesheet
for its compact, wrapping controls and single-page state. The CMS loading frame and loading-circle assets
are unchanged. Refresh the changed extracted `cms_articles` templates and
configuration when adopting this built-in update; existing independent
overrides continue to take precedence.

## Publishing the result

### Code block headers

The built-in `default/code.html` places a language label and Copy button above
ordinary code blocks. CMS themes inherit this template. `{language}` remains the
Prism identifier; `{language_name}` is the readable label, such as `C#`, `JSON`
or `Plain text`. The source block's anchor belongs to the outer `.ln-code-block`.
The template inserts `{code}` directly inside `<code>` without adding whitespace.

The shared `ln.css` owns the header, focus, print and scrolling styles. The shared
`ln.js` initializes the buttons and copies the code's text without the header or
syntax-highlighting markup. Successful copies show **Copied** briefly; failures
show **Copy failed** with an accessible manual-copy instruction. Clipboard writes
use the browser Clipboard API in secure contexts, with a selection-based fallback
where that API is unavailable. The fallback depends on browser permission and
support. The button stays hidden without JavaScript; the code and language remain
readable. Mermaid uses its separate template and retains hidden source.

Keep `data-ln-code-block`, `data-ln-code-copy`, `.ln-code-status` and the `pre > code`
structure when overriding this template. Refresh the template and both shared
resources together when adopting this update. Online output needs the corresponding
CDN assets published; changing local copies alone does not update online visitors.

### Native tabs and output resources

Native tab groups use the standard `default` theme's `tabs.html` and `tab.html`, inherited by CMS themes. Tab styles and behavior are comment-separated sections in the existing `ln.css` and `ln.js`; the page frame loads them without extra requests. Resource ownership stays CDN online and bundled files offline. No separate theme selection is required. Preserve the group and panel DOM hooks when overriding these templates; use `--ln-tabs-accent` for the active indicator and focus color. See [tabbed content](tabs.md) for the full contract.

An `HtmlRenderResult` contains `Html` and `Assets`. Each asset has:

- `RelativePath`: a path such as `assets/{sha256}/resources/local-notion/css/ln.css`.
- `ContentType`: the media type to return when serving it.
- `Content`: the in-memory bytes of that render's resource snapshot.

The HTML's asset URLs are `AssetBaseUrl` plus those relative paths; when no base is supplied, they are relative to the rendered document. A web host can serve `Assets` from memory using their paths and content types. A static exporter can write them under its chosen output asset directory. LocalNotion's generated asset output is separate from its theme override directory.

The bundle hash covers the effective resource paths and bytes. Different overrides cannot overwrite resources used by an earlier document. Place custom assets and their relative dependencies under the theme's `resources/` tree. Each bundle preserves complete effective resource subtrees, including MathJax fonts and modules and Prism's bundled language grammars. Reuse identical bundle paths when publishing several documents.

`RenderEnvironment.Online` uses each asset's owning theme `online_url`, whether that file comes from disk or the assembly. Existing extracted theme folders and local file overrides do not disable CDN links. Inherited files retain their owning theme's URL: CMS scripts use the CMS CDN directory, while the inherited LocalNotion script uses the default theme directory. Template and include contents still prefer disk overrides. To change a remotely served asset, publish it at the configured URL or configure the theme to use your own hosted URL. Offline rendering, and assets whose owning theme has no online URL, use the emitted resource bundle and `AssetBaseUrl`.

The no-disk library contract is HTML plus in-memory resources. An offline static page requires the emitted resource files beside its output, or a host that serves the same bytes. It is not a promise of self-contained single-file HTML. External media and social embeds retain their own network requirements.

Mermaid code blocks load a separate, pinned internal runtime bundle with no CDN owner. Their scripts, stylesheet and license files are returned in `Assets` in **both** environments and referenced once per render. Serve these assets even when the other online theme dependencies use CDNs. Ordinary code and documents without Mermaid do not load that runtime. See [Mermaid diagrams](mermaid.md).

Run theme regressions with:

```powershell
dotnet test tests/Sphere10.Framework.Web.VisualRenderer.Tests/Sphere10.Framework.Web.VisualRenderer.Tests.csproj
```
