# Web bookmarks

`BookmarkBlock` represents a prepared website preview. It supplies `Url`, `Title`,
`Description`, `ImageUrl`, `IconUrl` and rich-text `Caption`. The renderer makes no
network requests and owns no metadata cache. Source adapters supply these values.

The built-in `default/bookmark.html` template produces one linked card with a
preview image on the left, an icon, title, description and destination on the
right. The caption is outside the link. CMS inherits this template. Missing images
are omitted; missing titles fall back to the URL. Text is encoded and image/link
destinations accept HTTP and HTTPS. Styling lives in the shared `ln.css` bundle.

```csharp
var bookmark = new BookmarkBlock {
    Url = "https://example.com/documentation",
    Title = "Documentation",
    Description = "Installation and usage reference",
    ImageUrl = "https://example.com/preview.webp",
    IconUrl = "https://example.com/favicon.ico"
};
var result = new HtmlRenderer().RenderSnippet(bookmark);
```

LocalNotion uses HtmlAgilityPack in `WebBookmarkBuilder` during pull. It prefers
Open Graph metadata, then Twitter Card metadata, then ordinary HTML metadata.
The first usable value wins. Title falls back to `<title>` and the authored URL;
description falls back to `meta[name=description]`; images fall back to
`link[rel=image_src]`. Icons use `rel=icon`, then `apple-touch-icon`, then the
destination origin's `/favicon.ico`. Relative URLs resolve against the final
response URL and any valid HTML `<base>` element. Metadata never replaces the
authored bookmark destination with `og:url`.

One pull deduplicates destination requests by URL. The next pull fetches again,
including when the Notion page itself is unchanged. The resulting static fields
are persisted in `LocalNotionBookmark` on the page resource. Rendering a reopened
repository maps that object into `BookmarkBlock` without refetching. Image and icon
URLs still point to their destination servers; persisting metadata does not imply
that remote images are available offline.

The fetch has a 15-second deadline and a 2 MiB HTML limit. Caller cancellation
propagates. LocalNotion's existing fault-tolerant pull option governs fetch
failures: tolerant pulls log the failure and store a URL-only preview; strict
pulls report the failure. No destination JavaScript is executed.

References: [Open Graph](https://ogp.me/), [HtmlAgilityPack](https://html-agility-pack.net/).
