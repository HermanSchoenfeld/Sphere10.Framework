# Processing

Typed query-string parameter reading and a static accessor for the current `HttpContext`.

`PageParameterProcessor` names a query parameter with an enum member. The member's name is the parameter name unless a `ParameterAttribute` from Sphere10.Framework.Web overrides it. `GetParameter<T>` parses the unescaped value with `Tools.Parser` and throws `SoftwareException` when the parameter is absent, so call `ContainsParameter` first for an optional parameter. The `HttpRequest` extensions in [Extensions](../Extensions/README.md) create a processor for each call.

`HttpContextEx.Current` returns the `HttpContext` of a static `IHttpContextAccessor`. Register the accessor with `AddHttpContextAccessor()` and pass it to `HttpContextEx.Configure` at startup before reading `Current`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HttpContextEx`](HttpContextEx.cs) | static class | Holds a configured `IHttpContextAccessor` and exposes its current `HttpContext` statically. |
| [`PageParameterProcessor`](PageParameterProcessor.cs) | class | Checks for and parses query-string values of a request whose names come from enum members or their `ParameterAttribute`. |
