# Sitemap

An XML sitemap model that serializes to the standard sitemaps.org `urlset` document.

`SitemapXml` keeps its entries in a dictionary keyed by URL. `Add(url, lastModified, frequency, priority)` adds an entry and throws `ArgumentException` for a URL that is already present, so call `HasNode(url)` first when URLs can repeat. `XmlSerializer` writes each entry as a `url` element with `loc` and the optional `lastmod`, `changefreq` and `priority` elements. It omits the optional elements that were not set and writes `lastmod` as a date without a time. The model does not crawl a site or serve an endpoint. The [Presentation](../Presentation/README.md) domain holds the library's navigation model.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`SitemapXml`](SitemapXml.cs) | class | XML-serializable sitemap whose nested `Node` entries carry a URL, an optional last-modified date, a `Frequency` value and a priority. |
