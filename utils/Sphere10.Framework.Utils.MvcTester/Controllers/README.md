# Controllers

The MVC controllers of the tester host, currently a single landing-page controller.

`Program.cs` maps the conventional route `{controller=Home}/{action=Index}/{id?}`, so both `/` and `/Home/Index` reach `HomeController.Index`. That action returns the view in [Views/Home](../Views/README.md). Add a controller here, with its view under `Views/<ControllerName>`, to demonstrate a feature of the MVC library.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HomeController`](HomeController.cs) | class | MVC `Controller` whose `Index` action returns the landing view. |
