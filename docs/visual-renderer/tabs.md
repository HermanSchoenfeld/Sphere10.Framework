<!-- Copyright (c) Herman Schoenfeld 2018 - Present. Author: Herman Schoenfeld. Distributed under the MIT software license; see LICENSE. -->

# Tabbed content

`TabsBlock` groups ordered `TabBlock` panels. Each panel has a rich-text label (`Text`), optional `Icon` and `Color`, and ordinary visual `Children`. Panels can contain other tab groups. The renderer has no dependency on Notion; a source adapter supplies the completed model.

```csharp
var tabs = new TabsBlock {
    Children = [
        new TabBlock {
            Text = [new TextInline { Text = "Overview" }],
            Children = [new ParagraphBlock { Text = [new TextInline { Text = "Overview content." }] }]
        },
        new TabBlock {
            Text = [new TextInline { Text = "Configuration" }],
            Children = [new ParagraphBlock { Text = [new TextInline { Text = "Configuration content." }] }]
        }
    ]
};
var result = new HtmlRenderer().RenderSnippet(tabs);
```

## HTML, keyboard and fallback behavior

All panels are present in the generated static HTML. Before JavaScript runs, labels and panel contents are readable in order. Successful initialization creates a tab bar, selects the first panel and hides the remaining panels. Plain-text rendering includes every label and panel; print styles reveal every panel as well.

Click a label to select its panel. Left/Right arrows move and activate tabs, respecting text direction; Home/End select the first/last tab. The selected tab is the tab bar's keyboard entry point. Panels use `tabpanel`, labels use `tab`, and the runtime supplies selection and control relationships.

A fragment link targeting an element inside a hidden panel opens that panel and any containing panels before scrolling to the target. This also works on initial navigation. Supply stable, unique `Metadata.Anchor` values for link targets, including repeated occurrences of the same source object.

Tabs change presentation, not publication or access control. Every panel can be read from the HTML. They do not reduce the downloaded page size or defer loading its contents.

## Templates and resources

The standard `default` theme supplies `tabs.html` and `tab.html`; CMS themes inherit them. Overrides must retain:

- `data-ln-tabs` on the group.
- A direct child panel with `data-ln-tab` and a unique `{object_id}`.
- A direct `data-ln-tab-label` child containing `{icon}` and `{title}`.
- `{children}` for the panel body.

Do not author hidden panels in the static template. The runtime hides panels only after controls have been initialized. Interactive links in labels become noninteractive label text in the generated buttons; links in panel content remain ordinary links.

Tab CSS and JavaScript are comment-separated sections of the built-in `default` theme's existing `resources/local-notion/css/ln.css` and `resources/local-notion/js/ln.js`. The normal page frame already loads both files, so tabs add no separate resource requests or runtime injection. CMS inherits them. Online output uses the existing default CDN URLs; offline output exports the shared files in `HtmlRenderResult.Assets`. Publish the returned resource tree with offline HTML. A rendered snippet relies on its host page loading these standard resources. Dynamically inserted fragments can be initialized with `window.localNotionTabs.initialize()`. `InitializeTabs()` is defined under the shared file's Global Functions section and called from Initialization before optional icon/jQuery setup. It does not depend on either library.

The tab bar inherits the surrounding font and text color. Themes can set `--ln-tabs-accent` for the selection indicator and keyboard focus outline. Keep custom styling in theme CSS. The horizontal tab bar scrolls when labels exceed the available width.

## Verification boundary

Tests cover standard/default and CMS output in both environments, nested content, safe label encoding, asset publication and text fallback. Browser checks cover panel selection, nested panels, arrow/Home navigation, fragment reveal and a 390-pixel viewport. This source feature is not evidence that an installed application or public package has been updated.
