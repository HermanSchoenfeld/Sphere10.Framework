# Loader Layouts

Holds the original loader shell's main layout, which composes its sidebar, top bar, modal host and footer.

`MainLayout` renders `Sidebar` and `Topbar` from [Components](../Components/README.md) around the page body, adds the logout dialog and a `ModalHost`, and initializes the injected `IModalService` with that host after rendering. The current tester uses its own `DemoLayout` and the library `ApplicationShell` instead, so this layout is a migration reference used only by the retained standalone entry point.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`MainLayout`](MainLayout.razor) | class | Razor `LayoutComponentBase` of the original shell that hosts the sidebar, top bar, footer and modal host and registers the host with `IModalService` |
