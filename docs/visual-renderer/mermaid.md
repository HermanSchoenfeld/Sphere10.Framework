# Mermaid diagrams

Set a `CodeBlock`'s language to `CodeLanguage.Mermaid` to render its source as a diagram in HTML. Other code languages retain their ordinary code presentation. Plain-text rendering returns the original Mermaid source.

```csharp
var document = new DocumentBlock {
	Title = "Deployment flow",
	Children = [
		new CodeBlock {
			Language = CodeLanguage.Mermaid,
			Code = "flowchart LR\n    Author --> Build\n    Build --> Website",
			Caption = [new TextInline { Text = "Authoring and publication" }]
		}
	]
};
var result = new HtmlRenderer().Render(document);
```

The figure contains an SVG display area, an optional caption and hidden encoded source for the runtime. The **Diagram source** disclosure remains commented out in `default/mermaid.html`; uncomment it to restore the optional control. Invalid definitions show an explanatory message while other diagrams continue rendering.

## Runtime and assets

The renderer bundles **Mermaid 11.12.0**, its retained upstream license notices, a small initializer and diagram CSS. It emits their script/stylesheet references once per render invocation, only when Mermaid survives the final HTML composition. Rendering an unused document/template slot therefore cannot consume the runtime or request unused assets. Initialization waits for DOM readiness and processes all generated Mermaid figures. It does not wait for unrelated images or external widgets to finish loading.

These resources belong to an internal runtime theme with no CDN URL. Consequently **Mermaid documents return assets in both online and offline mode**, even when the rest of an online page uses CDN-hosted themes. Serve or export each returned `RenderAsset` at its `RelativePath` beneath `RenderOptions.AssetBaseUrl`, exactly as described in the [theme guide](renderer-themes.md#publishing-the-result). The hashed resource paths change when their effective bytes change. No CDN publication, server-side network request or Node.js installation is required to render a document.

External resources referenced by other document content retain their own requirements. Rendering a diagram in the browser requires JavaScript; without it, the diagram remains unrendered and its source stays hidden.

## Snippets and dynamic hosts

`RenderSnippet` supports a Mermaid `CodeBlock` and returns the same HTML-plus-assets contract. Hosts must load the returned stylesheet and scripts when inserting snippets; assigning an HTML fragment to `innerHTML` does not execute its script tags. Once the initializer is loaded, call:

```javascript
await window.Sphere10Mermaid.render(containerElement);
```

This scans that element and its descendants for pending generated Mermaid figures. Already-rendered and failed figures are not rendered a second time. To replace a diagram, insert a freshly rendered snippet, then scan its container. Multiple separately produced snippets can share the same loaded runtime.

## Source and security

Diagram code is HTML-encoded into an ordinary code element, read through `textContent` and passed to Mermaid as source. Quotes, angle brackets, braces, ampersands and template-like strings do not become renderer tokens or executable HTML. Rich captions use the same typed inline rendering as other content.

The initializer uses Mermaid's **strict** security level, disables HTML labels and protects those settings from diagram-level configuration overrides. Click directives and executable callbacks are not enabled. Source text is retained separately from the SVG; a parse/render failure cannot replace the source with Mermaid's error markup or stop subsequent diagrams.

For an alternative presentation, override `default/mermaid.html` through the normal theme mechanism while retaining the generated figure's class/data hooks, source/display/error elements and **`{runtime}` token**. The renderer resolves that token's marker only after final composition, so discarded slots cannot hide the loader from visible diagrams. The internal runtime files can also be supplied through a theme override directory under `.mermaid`. No additional CMS Theme selection is necessary.

The runtime is intentionally pinned, not a floating "latest" dependency. Its unmodified bundle and complete Mermaid/DOMPurify license files are included with the exported runtime assets and the NuGet package's `third-party/Mermaid` notices. See the [third-party notice](../../src/Sphere10.Framework.Web.VisualRenderer/THIRD-PARTY-NOTICES.md).

Upstream references: [Mermaid usage and rendering API](https://mermaid.js.org/config/usage.html), [configuration](https://mermaid.js.org/config/schema-docs/config.html), and the [pinned 11.12.0 release](https://github.com/mermaid-js/mermaid/releases/tag/mermaid%4011.12.0).
