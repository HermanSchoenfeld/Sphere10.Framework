# UI

The Razor components of the current generation, from the application shell and CRUD grid to dialogs, tables, layout pieces, theme controls and the wizard host.

`Application.ApplicationShell` renders the circuit's workspace from [Application](../Application/README.md). It shows block navigation, merged command bars, screen tabs and the retained screen components, and it maps the selected block, screen and instance to the URL. Pass the containing page's query parameters to its `BlockId`, `ScreenId` and `InstanceId` parameters to support bookmarks and browser history. `Controls.BlazorGrid.BlazorGrid<TItem>` is the CRUD grid over `IDataSource<TItem>`, and it creates and disposes its controller from [Grids](../Grids/README.md). The grid does not own its data source, so the source's creator or DI scope disposes it.

The `Dialogs` components render through `Dialogs.ModalHost`, which the [Modal](../Modal/README.md) services need in the same interactive scope. The `Theming` components read the scoped service from [Theming](../Theming/README.md). The `Wizard` components render wizards built with the [Wizard](../Wizard/README.md) builder. The display tables in `Grids` are the current counterparts of the original tables in [Components](../Components/README.md). Each Razor component's namespace follows its folder, such as `Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs`, except that `IdentityControl` is declared in `UI.Controls`. The `UI` folder itself contains only `_Imports.razor`.

## Contents

### Application

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationSettingsDialog`](Application/ApplicationSettingsDialog.razor.cs) | class | Options dialog that edits the application's plugin, block and screen settings owners and keeps serialized backups only to undo Cancel. |
| [`ApplicationSettingsEditor`](Application/ApplicationSettingsEditor.razor) | class | Default `IApplicationSettingsEditor` that edits one settings object through `ApplicationSettingsValueEditor` and reports its input errors. |
| [`ApplicationSettingsValueEditor`](Application/ApplicationSettingsValueEditor.razor.cs) | class | Edits the public values of a detached settings object using their component-model names, descriptions and read-only metadata. |
| [`ApplicationShell`](Application/ApplicationShell.razor.cs) | class | Circuit-scoped application workspace with header, sidebar, block menus, command bar, screen tabs, screen host view and URL navigation. |

### Controls/BlazorGrid

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorGrid<TItem>`](Controls/BlazorGrid/BlazorGrid.razor.cs) | class | CRUD grid over `IDataSource<TItem>` with typed columns, buffered row editing, search, sort, paging modes, row actions and optional saved preferences. |
| [`BlazorGridReferencePicker<TItem>`](Controls/BlazorGrid/BlazorGridReferencePicker.razor.cs) | class | Cell editor that chooses an existing record from a data source through a dropdown containing a read-only grid. |
| [`Cell`](Controls/BlazorGrid/Cell.cs) | class | Legacy cell model with header, row, type information and display text, kept for cell-based consumers. |
| [`DataEnd<TInItem, TOutItem>`](Controls/BlazorGrid/DataEnd.cs) | class | Legacy column that extracts an `IColumnDefinition` from an item and renders it with the `IGridComponent` that the definition names. |
| [`GridAction<TItem>`](Controls/BlazorGrid/GridAction.cs) | class | Row command with a name, icon URL, availability predicate and synchronous or awaited work. |
| [`HeaderData`](Controls/BlazorGrid/HeaderData.cs) | class | Legacy header name and width, with a default width of 100. |
| [`IBlazorGridLegacyColumn`](Controls/BlazorGrid/IBlazorGridLegacyColumn.cs) | interface | Compatibility contract for the original `DataEnd` columns, with a name and a render method. |
| [`IColumnDefinition`](Controls/BlazorGrid/IColumnDefinition.cs) | interface | Legacy column value that names the component type that renders it. |
| [`IGridComponent<TItem>`](Controls/BlazorGrid/IGridComponent.cs) | interface | Legacy renderer that writes an item into a `RenderTreeBuilder`. |
| [`ObjectTypeInfo`](Controls/BlazorGrid/ObjectTypeInfo.cs) | class | Legacy property type information with the underlying type, its names and enum values. |
| [`RowData`](Controls/BlazorGrid/RowData.cs) | class | Legacy row height. |
| [`Utility`](Controls/BlazorGrid/Utility.cs) | static class | Legacy string helpers `IsNullOrEmpty` and `AddSpacesForCamelCase`. |

### Controls/IdentityControl

| Type | Kind | Purpose |
|---|---|---|
| [`IdentityControl`](Controls/IdentityControl/IdentityControl.razor.cs) | class | Shows the current user's avatar, display name and secondary text and opens a keyboard-accessible menu of application commands. |

### Dialogs

| Type | Kind | Purpose |
|---|---|---|
| [`Confirm`](Dialogs/Confirm.razor) | class | Confirmation modal with a title, message and confirm and cancel buttons. |
| [`ModalHost`](Dialogs/ModalHost.razor.cs) | class | Hosts one `Dialogs.ModalComponent` at a time for `Modal.ModalService` until its interaction finishes. |
| [`Severity`](Dialogs/Severity.cs) | enum | `Info`, `Warning` or `Error`. |

### Dialogs/Content

| Type | Kind | Purpose |
|---|---|---|

### Grids

| Type | Kind | Purpose |
|---|---|---|
| [`ItemRequest`](Grids/ItemsRequest.cs) | record | Requested item index, count, sort column and sort direction for `VirtualPagedTable<TItem>`. |
| [`ItemsResponse<TItem>`](Grids/ItemsResponse.cs) | record | One page of items and the total item count returned to `VirtualPagedTable<TItem>`. |
| [`PagedTable<TItem>`](Grids/PagedTable.razor.cs) | class | Table that pages a local collection with item and header templates and a row-select callback. |
| [`RapidTable<TItem>`](Grids/RapidTable.razor.cs) | class | Table that renders items from an `IAsyncEnumerable<TItem>` up to `ItemLimit` and restarts when the source changes. |
| [`VirtualPagedTable<TItem>`](Grids/VirtualPagedTable.razor.cs) | class | Table that requests pages from an asynchronous items provider and ignores stale responses. |

### MainFrame

| Type | Kind | Purpose |
|---|---|---|
| [`SearchInput`](MainFrame/SearchInput.razor.cs) | class | Search box that calls a provider returning matching strings, with a minimum interval between searches. |
| [`Sidebar`](MainFrame/Sidebar.razor) | class | Sidebar with a brand link, block menu and the selected block's menus. |

### Theming

| Type | Kind | Purpose |
|---|---|---|
| [`ThemeProvider`](Theming/ThemeProvider.razor.cs) | class | Wrapper that loads the active theme's stylesheet and applies its theme attributes to descendants without recreating them. |
| [`ThemeSelector`](Theming/ThemeSelector.razor.cs) | class | Dropdown of every catalogue theme that changes the scoped theme without changing the route. |
| [`ThemeToggle`](Theming/ThemeToggle.razor.cs) | class | Toggle button whose pressed state shows that a dark theme is selected. |
| [`ThemedComponentBase`](Theming/ThemedComponentBase.cs) | abstract class | Base component that subscribes to `IThemeService.Changed`, exposes the current theme and Bootstrap theme names and unsubscribes on disposal. |

### Wizard

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorWizardHost`](Wizard/BlazorWizardHost.razor.cs) | class | Renders the current step of a `Wizard.IBlazorWizard`, applies step validation and cancellation policies and raises finished, cancelled and step-change callbacks. |
| [`BlazorWizardStep<TModel>`](Wizard/BlazorWizardStep.razor.cs) | abstract class | Base step component with typed access to its wizard and model. |
| [`BlazorWizardStepBase`](Wizard/BlazorWizardStep.razor.cs) | abstract class | Non-generic step base that implements the shared `IWizardStep` traits, title, button texts, cancellation flag and next and previous callbacks. |
