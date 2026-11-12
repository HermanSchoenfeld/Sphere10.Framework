# Page Content

Holds the Razor content components that the tester's overview, tables, dialogs and wizards screens render.

These components carry no `@page` route. The single routed [`Workspace`](../Application/README.md) opens a registered screen, and that screen renders one of these components. Each page adds a heading and introduction around the matching [demo](../Demos/README.md), and `WizardsPage` also shows the guided workspace wizard from [Showcase](../Showcase/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DialogsPage`](DialogsPage.razor) | class | Razor content that hosts `DialogsDemo` |
| [`OverviewPage`](OverviewPage.razor) | class | Razor tour of linked cards for the application patterns, the component reference and the screen-hosting and legacy examples, shown by the default screen |
| [`TablesPage`](TablesPage.razor) | class | Razor content that hosts `TablesDemo` |
| [`WizardsPage`](WizardsPage.razor) | class | Razor content that hosts `GuidedWorkspaceDemo` followed by the original `WizardsDemo` |
