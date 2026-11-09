# Application Workspace

Holds the tester's single routable workspace, the retained application screens it hosts, the shared commands and the scoped demo state.

`Workspace.razor` is the only routed page. It maps every entry URL to a registered block and screen, renders the library `ApplicationShell` with the toolbar search, theme selector and `DemoIdentity`, and keeps opened screens alive while navigating. Each `...Screen.razor` component derives from `ApplicationScreen` and is registered by a plugin definition. Most of them wrap reusable content from [Pages](../Pages/README.md) or [Demos](../Demos/README.md), and the screens in `Legacy` wrap the original Loader and WidgetGallery pages. `WorkspaceCommands` adds the application File, View and Help commands, `ScreenPoliciesPlugin` registers the empty-workspace and permanent-screen profiles, and `WorkspaceStatus` is the scoped state that screens and the [Lifecycle](../Lifecycle/README.md) tasks share.

Razor components compile to public partial classes named after their files. Screen definitions and URLs are described in the [project README](../README.md#routes-and-project-layout).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ComponentScreen`](ComponentScreen.razor) | class | Razor screen hosting the visual grid gallery, with nested settings that persist both gallery grids' layouts |
| [`RegistrationExampleScreen`](RegistrationExampleScreen.razor) | class | Razor inspector screen registered twice under different names, with nested per-registration display settings |
| [`ScreenPoliciesPlugin`](ScreenPoliciesPlugin.cs) | static class | Registers the empty-workspace and optional permanent-screen blocks through the ordinary plugin and block builders |
| [`Workspace`](Workspace.razor) | class | Razor routed page for every tester URL that renders the `ApplicationShell` and opens the matching screen in the retained host |
| [`WorkspaceCommands`](WorkspaceCommands.cs) | static class | Configures the application commands that every block shares and the active screen extends, including the scratchpad activator and exit |
| [`WorkspaceStatus`](WorkspaceStatus.cs) | class | Scoped demo state: action counter, block-activation choice and the plugins that have initialized and finalized, with a `Changed` event |

### Legacy

| Type | Kind | Purpose |
|---|---|---|
