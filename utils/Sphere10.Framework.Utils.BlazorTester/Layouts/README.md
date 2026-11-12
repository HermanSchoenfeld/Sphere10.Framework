# Layouts

Provides the tester's layout component and the compatibility routes and header search over its registered screens.

`DemoLayout` wraps every page in the theme provider and the two modal hosts, one for each component generation. It adds no header, sidebar or navigation, because the library `ApplicationShell` in [`Workspace`](../Application/README.md) owns the presentation. `DemoNavigation` maps old and alias URLs to a block and screen id and searches the registered application screens for the toolbar search box.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DemoLayout`](DemoLayout.razor) | class | Razor `LayoutComponentBase` that supplies the theme provider and the legacy and current modal hosts |
| [`DemoNavigation`](DemoNavigation.cs) | static class | Route aliases (nested `Alias` record of route, block id and screen id), alias resolution and screen search over an `IApplicationScreenHost` |
