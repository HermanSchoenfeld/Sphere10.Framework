# Application Workspace

Holds the tester's single routable workspace, the retained application screens it hosts, the shared commands and the scoped demo state.

`Workspace.razor` is the only routed page. It maps every entry URL to a registered block and screen, renders the library `ApplicationShell` with the toolbar search, theme selector and `DemoIdentity`, and keeps opened screens alive while navigating. Each `...Screen.razor` component derives from `ApplicationScreen` and is registered by a plugin definition. Most of them wrap reusable content from [Pages](../Pages/README.md) or [Demos](../Demos/README.md), and the screens in `Legacy` wrap the original Loader and WidgetGallery pages. `WorkspaceCommands` adds the application File, View and Help commands, `ScreenPoliciesPlugin` registers the empty-workspace and permanent-screen profiles, and `WorkspaceStatus` is the scoped state that screens and the [Lifecycle](../Lifecycle/README.md) tasks share.

Razor components compile to public partial classes named after their files. Screen definitions and URLs are described in the [project README](../README.md#routes-and-project-layout).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ComponentScreen`](ComponentScreen.razor) | class | Razor screen hosting the visual grid gallery, with nested settings that persist both gallery grids' layouts |
| [`DemoIdentity`](DemoIdentity.razor) | class | Razor toolbar control that shows a local demo user in `IdentityControl` with profile, sign-out and demo sign-in menu items, without server authentication |
| [`DialogsScreen`](DialogsScreen.razor) | class | Razor screen wrapping the dialogs page |
| [`EditorScreen`](EditorScreen.razor) | class | Razor single-instance screen with an editable tab title, note, counter, Save and Discard commands and a tab-switching guard |
| [`EmptyWorkspaceScreen`](EmptyWorkspaceScreen.razor) | class | Razor tabless screen shown when no other screen is open, used by the empty-workspace lifetime profiles |
| [`OverviewScreen`](OverviewScreen.razor) | class | Razor default screen with the retained counter, block-activation switch and overview page |
| [`PermanentWorkspaceScreen`](PermanentWorkspaceScreen.razor) | class | Razor permanent singleton screen without a close button, used by the optional permanent-screen profile |
| [`RegistrationDetailsScreen`](RegistrationDetailsScreen.razor) | class | Razor screen registered in a block with a bookmark but no navigation command |
| [`RegistrationExampleScreen`](RegistrationExampleScreen.razor) | class | Razor inspector screen registered twice under different names, with nested per-registration display settings |
| [`RegistrationInspectorSettingsEditor`](RegistrationInspectorSettingsEditor.razor) | class | Razor `IApplicationSettingsEditor` that edits the inspector settings inside the Options dialog |
| [`ScratchpadScreen`](ScratchpadScreen.razor) | class | Razor multi-instance screen with an independent tab title, note and counter per instance |
| [`ScreenPoliciesPlugin`](ScreenPoliciesPlugin.cs) | static class | Registers the empty-workspace and optional permanent-screen blocks through the ordinary plugin and block builders |
| [`TablesScreen`](TablesScreen.razor) | class | Razor screen wrapping the tables page |
| [`ThemeScreen`](ThemeScreen.razor) | class | Razor screen wrapping the theme component gallery |
| [`WizardsScreen`](WizardsScreen.razor) | class | Razor screen wrapping the wizards page |
| [`Workspace`](Workspace.razor) | class | Razor routed page for every tester URL that renders the `ApplicationShell` and opens the matching screen in the retained host |
| [`WorkspaceCommands`](WorkspaceCommands.cs) | static class | Configures the application commands that every block shares and the active screen extends, including the scratchpad activator and exit |
| [`WorkspaceStatus`](WorkspaceStatus.cs) | class | Scoped demo state: action counter, block-activation choice and the plugins that have initialized and finalized, with a `Changed` event |

### Legacy

| Type | Kind | Purpose |
|---|---|---|
| [`EndpointScreen`](Legacy/EndpointScreen.razor) | class | Razor screen wrapping the original endpoint `Servers` page |
| [`LegacyDashboardScreen`](Legacy/LegacyDashboardScreen.razor) | class | Razor screen wrapping the original loader `Home` dashboard |
| [`LegacyDialogsScreen`](Legacy/LegacyDialogsScreen.razor) | class | Razor screen wrapping the original `Modals` page |
| [`LegacyGalleryScreen`](Legacy/LegacyGalleryScreen.razor) | class | Razor screen wrapping the original `WidgetGallery` page |
| [`LegacyTablesScreen`](Legacy/LegacyTablesScreen.razor) | class | Razor screen wrapping the original `Tables` page |
| [`LegacyWizardsScreen`](Legacy/LegacyWizardsScreen.razor) | class | Razor screen wrapping the original `Wizards` page |
