# Application patterns

This block demonstrates complete, generic application interactions composed from the public Sphere10 Blazor APIs. It sits beside the component reference; the original grid, tables, dialogs, wallet wizard and legacy examples remain available. All sample data belongs to the current service scope and resets on browser refresh.

## Suggested tour

1. **Cards & collections:** filter or search the nine creative collections, select a cover, switch to the native table, then open two different collections. Reopening one returns to its existing tab. Edit its name, try to leave with an unsaved draft, then use Save collection or Discard changes. Saving refreshes the shared catalogue.
2. **Menus & commands:** choose a sample document and inspect the active File, Tools and toolbar commands. Export contains nested Documents and Structured data groups. Clear the document selection, disable review actions or reveal diagnostics to see enabled and hidden command states change. Prepared exports remain a readable local preview.
3. **Guided workflows:** start Workspace setup inline or in a dialog. Required fields validate on Next. Choose Team to add Reviewers, go Back to change to Solo, and take the preset chooser detour from Settings. Applying or backing out of a preset returns to Settings. Review the values before creating a workspace; cancelling retains the last completed summary.
4. **Theme studio:** browse every ThemeCatalog preset, filter by name or light/dark scheme, and inspect a complete release-board layout. Edit the note, select a chart bar by pointer or keyboard, then change the preset. The same components and draft remain in place. Open a preview dialog and compare the exhaustive color/component gallery through its link.

The Overview also has an **Activate the block's default screen** checkbox. It switches the native ApplicationShell behavior between browsing a block's menus and activating its default screen. Ordinary screen guards apply in both modes.

## Source and public APIs

| Example | Source | Library foundations |
| --- | --- | --- |
| Plugin and block composition | [ShowcasePlugin.cs](ShowcasePlugin.cs) | ConfigurePlugin, ConfigureBlock, ConfigureMenu, screen registrations and scoped services |
| Card/table catalogue | [CardsScreen.razor](Cards/CardsScreen.razor), [CollectionDataSource.cs](Cards/CollectionDataSource.cs) | BulkFetchDataSource, BlazorGridController, BlazorGrid, PageSizeSelector, ExpandsToFit |
| Keyed editor | [CollectionDetailScreen.razor](Cards/CollectionDetailScreen.razor) | BlazorApplicationScreenActivation parameters and instance key; HasUnsavedChanges, CanDeactivateAsync, NotifyScreenChanged |
| Native commands | [CommandsScreen.razor](Commands/CommandsScreen.razor) | Menus/ToolBarItems, merge by ID, WithChildren, EnabledWhen, WithVisibility and ViewService |
| Guided wizard | [GuidedWorkspaceDemo.razor](Wizards/GuidedWorkspaceDemo.razor), [WorkspaceSetupFlow.cs](Wizards/WorkspaceSetupFlow.cs) | BlazorWizardBuilder, conditional UpdateSteps, BlazorWizardStep, embedded BlazorWizardHost and ViewService.WizardDialogAsync |
| Theme studio | [ThemeStudioScreen.razor](Themes/ThemeStudioScreen.razor) | ThemeCatalog, scoped IThemeService, ThemeProvider, semantic CSS tokens and themed dialogs |

The cards are an application-specific view over the existing data-source/controller APIs; no parallel card data framework is introduced. SVG artwork is deterministic and theme-aware. Collection IDs are passed into retained screens rather than copying mutable catalogue entities into registration metadata. Catalogue read and write scopes return detached copies.

Command definitions are shared between the native menu and toolbar. Availability is checked by the host as well as presented by its renderers. Screen-specific Tools and File contributions follow the active screen, and the common application menus remain available.

Both wizard hosts run the same steps and completion configuration. Their draft is separate from the copied completion snapshot. The existing wallet and legacy widget wizards continue to demonstrate their own cancellation policies and earlier component generation.

## Verification

Use the commands in the [tester guide](../README.md#verify-changes) and [Blazor test guide](../../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md). `ShowcaseBrowserTests` checks the actual menus, keyboard navigation, card/grid state, retained editors, conditional wizard flows, theme state and mobile containment. `CollectionShowcaseTests` and `GuidedWorkspaceWizardTests` cover detached data, filtering/paging/invalidation and wizard mutation/validation. Existing shell, grid, theme and wizard browser fixtures remain part of the regression walkthrough.
