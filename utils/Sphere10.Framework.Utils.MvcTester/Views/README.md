# Views

The Razor views of the tester host, currently the landing page of the home controller.

Views follow the MVC convention `Views/<ControllerName>/<ActionName>.cshtml`, and the [Controllers](../Controllers/README.md) domain holds the matching controllers. `Home/Index.cshtml` sets `Layout = null` and renders a complete HTML page with the heading "Sphere10 Framework MVC Tester". The host defines no shared layout, view imports or static assets.

## Contents

This folder declares no public types. The views compile to generated view classes that are not part of any public API.

### Home

| File | Purpose |
|---|---|
| [`Index.cshtml`](Home/Index.cshtml) | Self-contained landing page returned by `HomeController.Index`. |
