# Processing

An attribute that names the request parameter an enum member stands for.

Put `[Parameter(Name = "...")]` on an enum member to map it to a query or form parameter name. `PageParameterProcessor` and `HttpRequest.GetParameter<T>(Enum)` in the ASP.NET Core layer read the attribute, and a member without it uses its own name. The attribute lives in the `Sphere10.Framework.Web.Processing` namespace rather than `Sphere10.Framework.Web`. A Blazor component that imports `Sphere10.Framework.Web` therefore still resolves `[Parameter]` to the component parameter attribute. The [Presentation](../Presentation/README.md) domain holds the library's other request-independent models.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ParameterAttribute`](ParameterAttribute.cs) | class | Attribute whose `Name` property gives the request parameter name for an enum member. |
