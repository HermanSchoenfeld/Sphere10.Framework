# Bootstrap base styles

Bootstrap 5.3.3 CSS, copied from the installed .NET 10 Blazor Web App template.
Upstream: https://github.com/twbs/bootstrap/tree/v5.3.3

The demo loads this single Bootstrap base. Sphere10's theme provider sets
`data-bs-theme` alongside `data-sphere10-theme`; application colors come from the
library's `css/themes.css`. Do not load the archived admin or modern theme
stylesheets alongside these assets.

The upstream license and source map are included. No Bootstrap JavaScript is
required: the Sphere10 components own their interaction and modal behavior.
