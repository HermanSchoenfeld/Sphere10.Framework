# Use Sphere10.VisualRenderer independently

`Sphere10.VisualRenderer` targets .NET 10 and has no reference to LocalNotion.Core or Notion.Client. Install the `Sphere10.VisualRenderer` NuGet package or use a project reference and construct a visual document directly. The [package README](../../src/Sphere10.VisualRenderer/README.md) includes a complete CDN-backed example for embedded browsers. If distributing built assemblies, include the renderer's runtime dependencies; copying its DLL alone is insufficient:

```csharp
using Sphere10.VisualRenderer;

var document = new DocumentBlock {
	Title = "Independent document",
	Author = "My application",
	Children = [
		new HeadingBlock {
			Level = 1,
			Metadata = new() { Anchor = "intro" },
			Text = [new TextInline { Text = "Introduction" }]
		},
		new ParagraphBlock {
			Text = [
				new TextInline { Text = "Rendered without a Notion repository. " },
				new TextInline { Text = "Documentation", Url = "https://example.com/docs" }
			]
		}
	]
};

var result = new HtmlRenderer().Render(document);
var text = new TextRenderer().Render(document);
```

The renderer does not write files. `result.Html` and `result.Assets` provide HTML and its in-memory theme asset bundle. Content media URLs still refer to resources supplied by the caller. The caller can serve the assets from memory or publish them as static output. Set `RenderOptions.AssetBaseUrl` to the URL prefix under which each `RenderAsset.RelativePath` is served. Preserve asset paths within a bundle, including modules/fonts loaded dynamically by scripts or stylesheets.

The default asset base is empty, so an asset such as `assets/<hash>/resources/bootstrap/bootstrap.min.css` resolves relative to the HTML document. To export a static document, write `result.Html` to its destination and write each asset's bytes beneath the corresponding asset-base directory. The library does not deploy theme source files as part of that operation.

```csharp
var catalog = new ThemeCatalog(new ThemeOptions {
	ThemesDirectory = "/optional/theme-overrides"
});
var renderer = new HtmlRenderer(catalog);
var customizedResult = renderer.Render(document, new RenderOptions {
	Environment = RenderEnvironment.Offline,
	Mode = RenderMode.ReadOnly,
	Themes = ["default"],
	AssetBaseUrl = "shared-render-assets"
});
```

The override directory may be absent. For example, adding only `default/paragraph.html` replaces that template, while all other templates, configuration, includes, and assets remain embedded. An empty file is an intentional override; deleting it restores the embedded default. See [theme and asset behavior](renderer-themes.md).

## Model and rendering contract

`IRenderer<TOutput>.Render(DocumentBlock, RenderOptions)` accepts only renderer-owned data. `HtmlRenderer` returns `HtmlRenderResult`; `TextRenderer` returns a string. A render does not require an output filesystem path. `RenderSnippet(VisualNode, RenderOptions)` accepts an individual block and renders it without a document frame or page header. The result uses the same HTML/assets or plain-text contract; HTML fragments belong in a host page that loads the selected theme's CSS and scripts. The [package README](../../src/Sphere10.VisualRenderer/README.md) includes executable document, callout, and to-do examples alongside their actual HTML output.

The model contains typed paragraphs, headings, lists, tables, columns, media, references, navigation, database values, dates, people, and rich text. Lists contain items explicitly and select their presentation through `ListBlock.Type` (`ListType`). Media uses `MediaBlock.Type` (`MediaType`); database rows align to their column definitions. `Metadata.SourceId` is opaque provenance, while `Anchor` is the occurrence's DOM identity. No Notion ID format is required. Models are records with init properties and arrays for ordered content. Callers should finish populating arrays before rendering and avoid concurrent mutation; tokens and slots remain keyed dictionaries.

Set code languages with the `CodeLanguage` enum, for example `new CodeBlock { Language = CodeLanguage.CSharp, Code = "Console.WriteLine(42);" }`. Each enum member describes its Prism identifier. Notion projection parses language names into the enum and falls back to `CodeLanguage.Text` when parsing fails.

Supply final content URLs and resolved reference labels/icons. The renderer performs no source lookups and has no repository or link-resolution callback. Unknown source types can be represented by `UnsupportedBlock` with a prepared fallback/diagnostic. `RawHtmlBlock` is an explicit trusted-markup node; ordinary text is encoded by the HTML renderer. `TemplateBlock` provides typed node slots and scalar presentation tokens for custom composition without putting source objects into the model. Its scalar tokens, and `DocumentBlock.Tokens`, are trusted output strings inserted literally; use typed content slots for ordinary source text rather than interpolating unencoded content into template markup.

A `DocumentBlock` with `RenderFrame = false` produces a fragment. `ShowPageHeader = false` uses its children directly as page content, which supports composed pages. Nested documents can select their own themes; document `Slots` provide typed header/menu/footer composition. Per-call traversal, theme snapshots, tokens, counters, and assets are isolated, so one renderer can render independent documents repeatedly or concurrently.

The recursive renderer retains the original typed `Render(...)` overloads, `RenderText` recursion, page rendering methods and template calls. The traversal context contains visual nodes; its inputs have already resolved links, media and source-specific decisions. Subclasses can still override block rendering and template hooks.

## LocalNotion integration

Core's `NotionRenderModelBuilder` translates pages/databases before rendering. `CmsRenderModelBuilder` prepares CMS composition. `RenderingManager` reserves batch destinations, projects links relative to those final destinations, calls the standalone engine, and persists HTML/assets. Sync and CLI `render` share that pipeline. They use `RenderingManager.BeginBatch()` to read theme overrides once, share loaded source objects and graphs, and verify each published asset once for the whole batch. Each document still gets its own visual model and resolved links. Complete source updates before opening the batch; a new batch refreshes source data, overrides and asset checks. Asset edits or deletions during a batch are repaired on the next batch. Individual calls outside a batch continue to refresh their inputs. Early keyword extraction uses text projection before downloads and repository registration are complete.

LocalNotion deploys missing built-in theme files to `.localnotion/themes` (or the configured themes path) when creating or opening a repository. Existing files, including empty overrides, are preserved. This keeps physical theme resources available for render types that require them. Generated asset bundles remain under `.localnotion/render-assets`; online URLs continue to use each theme's configured CDN. The standalone renderer itself never deploys theme files and still supports entirely in-memory use.

CMS article and category templates receive `slug` as the sanitized CMS route and `url` as the resolved destination. Existing overrides using `href="/{slug}"` retain their original root-relative link contract. Use `href="{url}"` when updating overrides to honor the configured `base_url` or offline output paths; the embedded templates already use this token. Rendering does not rewrite existing template files.

HTML and plain text are the implemented output formats; LocalNotion does not implement PDF rendering. The extraction preserves current unsupported source blocks and text-specific omissions. Deliberate improvements include explicit database column alignment, evaluated boolean formula values, finite traversal with controlled fallbacks, and unique list wrapper identities with valid default colors.

See [tests/README.md](../tests/README.md) for the standalone, theme, projection, integration, characterization, and path regression commands.

## Distribution and validation scope

The project embeds its theme manifest and assets and depends on Microsoft.Extensions.FileProviders.Embedded. It has no Notion SDK or AngleSharp dependency. Sphere10 Framework owns its version, source, tests, and package tooling under the MIT License. Consumers such as LocalNotion restore its compiled NuGet package. See the [package README](../../src/Sphere10.VisualRenderer/README.md#pack-and-publish-the-package) for build and publication steps.

A web host must serve the returned asset paths under the configured asset base; the library does not register HTTP endpoints. Existing third-party media may still require network access. The tests verify embedded resources after publishing, HTML structure, in-memory asset routes, and output files. They do not replace browser layout and JavaScript execution checks.

## Optional HTML cleanup in LocalNotion

The standalone renderer returns generated HTML directly, preserving template whitespace and markup. It does not parse or pretty-print its output. `HtmlRenderResult.SuppressFormatting` records a theme request to suppress downstream formatting.

LocalNotion Core retains the original AngleSharp formatting pass behind `#if CleanHTML`. The `DefineConstants` line in `LocalNotion.Core.csproj` is commented out, so the default/native build skips cleanup entirely. Uncomment that line to enable cleanup of complete CMS documents; source renders and themes marked `suppress_formatting` retain their original behavior. `RenderOptions.FormatHtml` and the old renderer `Format` method were removed with this dependency. Core includes the AngleSharp package only when the `CleanHTML` compilation symbol is enabled, so the default production application has no AngleSharp dependency. Both test projects reference AngleSharp explicitly for HTML assertions.
