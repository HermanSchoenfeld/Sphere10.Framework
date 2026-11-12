# Component Demos

Holds reusable Razor examples of the Blazor grid, tables, dialogs, wizards and themes that the tester's screens render.

Each demo is a self-contained component that injects `ViewService` and exercises one part of the library. `GridDemo` combines an editable CRUD and reference grid over `TestClassDataSource` with a live-capture grid over `GridDemoRefreshableDataSource`, both from [Modern](../Modern/README.md). `WizardsDemo` launches the branching wallet wizard whose steps also live in Modern. The demos are hosted by the [Pages](../Pages/README.md) content components and the [Application](../Application/README.md) screens.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DialogsDemo`](DialogsDemo.razor) | class | Razor demo of awaited information, confirmation and custom dialogs through `ViewService` |
| [`GridDemo`](GridDemo.razor) | class | Razor demo of buffered editing, validation, reference selection and row actions in `BlazorGrid`, plus a live grid with paging, retained layout, refresh, removal and export |
| [`TablesDemo`](TablesDemo.razor) | class | Razor demo of `PagedTable`, `VirtualPagedTable` and `RapidTable` |
| [`ThemeDemo`](ThemeDemo.razor) | class | Razor gallery of buttons, semantic colors, alerts, forms, navigation, tables and dialogs under the current theme |
| [`WizardsDemo`](WizardsDemo.razor) | class | Razor launcher of the branching new-wallet wizard and its step validation |
