# Component Demos

Holds reusable Razor examples of the Blazor grid, tables, dialogs, wizards and themes that the tester's screens render.

Each demo is a self-contained component that injects `ViewService` and exercises one part of the library. `GridDemo` combines an editable CRUD and reference grid over `TestClassDataSource` with a live-capture grid over `GridDemoRefreshableDataSource`, both from [Modern](../Modern/README.md). `WizardsDemo` launches the branching wallet wizard whose steps also live in Modern. The demos are hosted by the [Pages](../Pages/README.md) content components and the [Application](../Application/README.md) screens.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`GridDemo`](GridDemo.razor) | class | Razor demo of buffered editing, validation, reference selection and row actions in `BlazorGrid`, plus a live grid with paging, retained layout, refresh, removal and export |
