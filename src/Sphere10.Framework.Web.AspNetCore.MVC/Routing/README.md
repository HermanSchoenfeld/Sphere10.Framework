# Routing

A controller attribute that declares conventional routes and registers them from an assembly.

`CustomRouteAttribute` can be applied several times to a controller class and is not inherited. `CustomRouteAttribute.RegisterCustomRoutesFromAssembly(endpoints, assembly)` maps each attribute of every controller in the assembly with `MapControllerRoute`, ordered by `Priority`, using the controller name and the `DefaultAction` and `Defaults` values as route defaults. A route URL may not contain `{controller}`, and the constructor throws when it does. Attribute routing with `[Route]` and `[HttpGet("...")]` remains the usual ASP.NET Core choice.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CustomRouteAttribute`](CustomRouteAttribute.cs) | class | Sealed attribute holding a route URL, default action, query-string style defaults and priority, with static methods that map these routes for an assembly, a controller type or one attribute. |
