# Forms

Bootstrap AJAX form models, rendering scope, options and JSON results for the embedded forms script.

A view model derives from `FormModelBase`, which gives it a random DOM-safe `ID` and a `SubmitCount`. A view opens `BootstrapFormScope<TModel>`, usually through `Html.BeginBootstrapForm` in [Extensions](../Extensions/README.md), and disposes it to close the form. The scope writes the form tag, hidden fields, an optional antiforgery token and loading overlay, a message marker and the `F_Init` call of the embedded `hydrogen-bootstrap-forms-1.0.0.js` script. `FormScopeOptions` flags control those parts and the script's behavior. The scope implements both `Dispose` and `DisposeAsync`.

The action answers with a `FormResult` serialized by Newtonsoft.Json, whose `FormResultType` tells the script to show a message, redirect, or replace the page or the form. ASP.NET Core's default System.Text.Json output names the type field differently and writes it as a number, so the action must serialize the result with `JsonConvert`. The script is an embedded resource of the assembly, not a static web asset, so the application serves it itself. The [project README](../README.md#bootstrap-ajax-forms) walks through a complete form.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BootstrapFormScope<TModel>`](BootstrapFormScope.cs) | class | Disposable scope that writes a Bootstrap AJAX form's opening markup on construction and its message marker, `F_Init` call and closing tag on synchronous or asynchronous disposal. |
| [`FormModelBase`](FormModelBase.cs) | abstract class | Base form model with a required hidden `ID`, generated as a DOM-safe identifier, and a `SubmitCount` that the script increments. |
| [`FormResult`](FormResult.cs) | class | JSON result of a form action with `result`, `message`, `type`, `url` and `content` fields for the forms script. |
| [`FormResultType`](FormResultType.cs) | enum | Whether the script shows a message, redirects, or replaces the page or the form for a result. |
| [`FormScopeOptions`](FormScopeOptions.cs) | enum | Flags for a form scope's loading overlay, clearing on success, form tag, antiforgery token, bot protection, GET method and overlay size, with `Default` and `DefaultRetainInput` combinations. |
