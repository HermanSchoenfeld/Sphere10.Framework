# External libraries

Typed models for two client-side libraries, the Animate.css class names and the obsolete jQuery DataTables server-side protocol.

`Animation` and `AnimationDelay` give each Animate.css animation and delay class a C# name, and each member's `[Description]` attribute holds the CSS class text. `Tools.Web.Html.AnimationClass` in the project root reads those descriptions to build an `animated <animation> <delay>` class string. The library ships no CSS, so the application supplies Animate.css and defines the `animation-delay-<n>` classes, where `n` counts tenths of a second.

`JDataTable` and `JDataTableParameters` carry the legacy DataTables request and response fields, such as `sEcho`, `iDisplayStart` and `aaData`. `JDataTable.Parse` sorts the rows on the first sort column, counts them and returns the requested page. Both types are marked `[Obsolete]` and remain only for existing integrations. The [Presentation](../Presentation/README.md) and [Sitemap](../Sitemap/README.md) domains hold the library's other web models.

## Contents

The domain folder itself declares no types. Its types are grouped by subfolder below.

### AnimateCss

| Type | Kind | Purpose |
|---|---|---|
| [`Animation`](AnimateCss/Animation.cs) | enum | Animate.css animation names, such as `bounceIn` and `fadeOut`, whose `[Description]` attribute holds the CSS class. |
| [`AnimationDelay`](AnimateCss/AnimationDelay.cs) | enum | Delays from 0 to 3 seconds in tenths, whose `[Description]` attribute holds the `animation-delay-<n>` class, or an empty string for no delay. |

### JQuery

| Type | Kind | Purpose |
|---|---|---|
| [`JDataTable`](JQuery/JDataTable.cs) | class | Obsolete DataTables response with the echo value, record counts, row data and column names, and a `Parse` method that sorts and pages string rows. |
| [`JDataTableParameters`](JQuery/JDataTableParameters.cs) | class | Obsolete DataTables request with the echo value, search text, page start and length, column count and first sort column and direction. |
