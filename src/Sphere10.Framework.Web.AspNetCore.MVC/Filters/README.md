# Filters

MVC action filter attributes for per-action cleanup, service-resolved filters and form-tag flags.

`CleanupAttribute` gives an action a `ViewBag.Cleanup` list of `Action` delegates and runs them after the result executes, for example to delete a temporary file that a file result streamed. A failing delegate is logged through `SystemLog`, and the next one still runs. `FilterFactoryBase<TFilter>` is the base for an attribute that resolves `TFilter` from the request services on every request, so `TFilter` must be registered in the service collection. `FormActionAttribute` sets `ViewData[FormActionAttribute.OmitFormTag]` to `true` for a view to read, and nothing in the library reads the flag. The [Forms](../Forms/README.md) domain has its own `FormScopeOptions.OmitFormTag` option for form scopes.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CleanupAttribute`](CleanupAttribute.cs) | class | Action filter that collects cleanup delegates in `ViewBag.Cleanup` and runs each after the result executes, logging failures. |
| [`FilterFactoryBase<TFilter>`](FilterFactoryBase.cs) | abstract class | `IFilterFactory` attribute that resolves a non-reusable `TFilter` instance with `GetRequiredService` on each request. |
| [`FormActionAttribute`](FormAction.cs) | class | Action filter that sets the `OmitFormElement` view-data flag to `true` before the action runs. |
