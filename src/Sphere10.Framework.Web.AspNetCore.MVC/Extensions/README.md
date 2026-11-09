# Extensions

Extension methods for MVC controllers, Razor HTML helpers, URL helpers, select lists and model state.

`HtmlHelperExtensions` is the main entry point for views. Its `BeginBootstrapForm` overloads open a [`BootstrapFormScope<TModel>`](../Forms/README.md), and its `Bootstrap*For` helpers wrap the MVC `*For` helpers with Bootstrap 5 classes, `[DisplayName]` labels and placeholders, a ` *` suffix for `[Required]` members, and an `is-invalid` tooltip that lists the member's model-state errors. `BootstrapFormButton` renders the submit button of a form scope.

`ControllerExtensions.RenderViewAsync` renders a view to a string, for example as the `content` of a `ReplaceForm` [form result](../Forms/README.md). When the view engine finds no view, it returns the text `A view with the name <viewName> could not be found` instead of throwing. `SelectExtensions` builds `SelectListItem` lists from any sequence and renders enum drop-downs through `Tools.Web.Mvc.ToSelectList`. `ModelStateDictionaryExtensions` adds the error messages of a Framework `Result` to `ModelState`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ControllerExtensions`](ControllerExtensions.cs) | static class | Extends `Controller` with `MapUrl`, which maps an application file path to a relative or absolute URL, and `RenderViewAsync`, which renders a view or partial view to a string. |
| [`EnumExtensions`](EnumExtensions.cs) | static class | Empty placeholder whose former enum select-list methods are commented out. Use `Tools.Web.Mvc.ToSelectList` instead. |
| [`HtmlHelperExtensions`](IHtmlHelperExtensions.cs) | static class | Extends `IHtmlHelper` with `BeginBootstrapForm`, `BootstrapFormButton`, `ValidationMessageForEx` and the Bootstrap label, text box, password, text area, drop-down and check box helpers. |
| [`ModelStateDictionaryExtensions`](ModelStateDictionaryExtensions.cs) | static class | Extends `ModelStateDictionary` to add a `Result`'s error messages or a formatted message as model errors, and to remove several keys at once. |
| [`SelectExtensions`](SelectExtensions.cs) | static class | Builds `SelectListItem` lists from a sequence with text, value and selection delegates, renders `EnumDropDownListFor`, and returns the selected item of a list. |
| [`UrlExtensions`](UrlExtensions.cs) | static class | Extends `IUrlHelper` with `Content(path, toAbsolute)`, which resolves a content path and optionally makes it absolute for the current request. |
