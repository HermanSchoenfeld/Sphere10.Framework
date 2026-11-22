# UI

The Razor components of the current generation, from the application shell and BlazorGrid to dialogs, tables, layout pieces, theme controls and the wizard host.

`Application.ApplicationShell` renders the circuit's workspace from [Application](../Application/README.md). It shows block navigation, merged command bars, screen tabs and the retained screen components, and it maps the selected block, screen and instance to the URL. Pass the containing page's query parameters to its `BlockId`, `ScreenId` and `InstanceId` parameters to support bookmarks and browser history. `Controls.BlazorGrid.BlazorGrid<TItem>` is the editable grid over `IDataSource<TItem>`, and it creates and disposes its controller from [Grids](../Grids/README.md). The grid does not own its data source, so the source's creator or DI scope disposes it.

The `Dialogs` components render through `Dialogs.ModalHost`, which the [Modal](../Modal/README.md) services need in the same interactive scope. The `Theming` components read the scoped service from [Theming](../Theming/README.md). The `Wizard` components render wizards built with the [Wizard](../Wizard/README.md) builder. The display tables in `Grids` are the current counterparts of the original tables in [Components](../Components/README.md). Each Razor component's namespace follows its folder, such as `Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs`, except that `IdentityControl`, `FieldGrid`, `FieldRow` and `SplitContainer` are declared in `UI.Controls`. `SplitContainer` holds a master grid and its detail panel, and `FieldGrid` lays out the detail panel's values and the inputs of wizard steps and dialogs as label and value rows, as the [library guide](../README.md#master-detail-layouts-and-field-rows) shows. The `UI` folder itself contains only `_Imports.razor`.

## Contents

### Application

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationScreenHostView`](Application/ApplicationScreenHostView.razor) | class | Renders every retained screen session as a `DynamicComponent`, showing only the active one, once screen preferences are ready. |
| [`ApplicationScreenTabs`](Application/ApplicationScreenTabs.razor) | class | Tab strip for open screen sessions with select, close, reorder and make-startup callbacks. |
| [`ApplicationSettingsDialog`](Application/ApplicationSettingsDialog.razor.cs) | class | Options dialog that edits the application's plugin, block and screen settings owners and keeps serialized backups only to undo Cancel. |
| [`ApplicationSettingsEditor`](Application/ApplicationSettingsEditor.razor) | class | Default `IApplicationSettingsEditor` that edits one settings object through `ApplicationSettingsValueEditor` and reports its input errors. |
| [`ApplicationSettingsValueEditor`](Application/ApplicationSettingsValueEditor.razor.cs) | class | Edits the public values of a detached settings object using their component-model names, descriptions and read-only metadata. |
| [`ApplicationShell`](Application/ApplicationShell.razor.cs) | class | Circuit-scoped application workspace with header, sidebar, block menus, command bar, screen tabs, screen host view and URL navigation. |

### Controls/BlazorGrid

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorGrid<TItem>`](Controls/BlazorGrid/BlazorGrid.razor.cs) | class | Editable grid over `IDataSource<TItem>` with typed columns, buffered row editing, search, sort, paging, source-owned hierarchical child views, custom detail templates, optional saved preferences and opt-in power-user presentation: compact density, icon row commands, a header reload icon, omitted caption, toolbar and footer, bound `SelectedItem`, `ItemOpened` and rows that open on a click (`RowClick`). Enum cells show their display text. |
| [`BlazorGridCellEditor<TItem>`](Controls/BlazorGrid/BlazorGridCellEditor.razor) | class | Default cell editor that renders a select (enum options show their display text), checkbox or typed HTML input for a `BlazorGridCellEditContext<TItem>`. |
| [`BlazorGridReferencePicker<TItem>`](Controls/BlazorGrid/BlazorGridReferencePicker.razor.cs) | class | Cell editor that chooses an existing record from a data source through a dropdown containing a read-only grid. |
| [`Cell`](Controls/BlazorGrid/Cell.cs) | class | Legacy cell model with header, row, type information and display text, kept for cell-based consumers. |
| [`DataEnd<TInItem, TOutItem>`](Controls/BlazorGrid/DataEnd.cs) | class | Legacy column that extracts an `IColumnDefinition` from an item and renders it with the `IGridComponent` that the definition names. |
| [`GridAction<TItem>`](Controls/BlazorGrid/GridAction.cs) | class | Row command with a name, icon CSS class (`Icon`) or icon URL, availability predicate and synchronous or awaited work. |
| [`HeaderData`](Controls/BlazorGrid/HeaderData.cs) | class | Legacy header name and width, with a default width of 100. |
| [`IBlazorGridLegacyColumn`](Controls/BlazorGrid/IBlazorGridLegacyColumn.cs) | interface | Compatibility contract for the original `DataEnd` columns, with a name and a render method. |
| [`IColumnDefinition`](Controls/BlazorGrid/IColumnDefinition.cs) | interface | Legacy column value that names the component type that renders it. |
| [`IGridComponent<TItem>`](Controls/BlazorGrid/IGridComponent.cs) | interface | Legacy renderer that writes an item into a `RenderTreeBuilder`. |
| [`ObjectTypeInfo`](Controls/BlazorGrid/ObjectTypeInfo.cs) | class | Legacy property type information with the underlying type, its names and enum values. |
| [`RowData`](Controls/BlazorGrid/RowData.cs) | class | Legacy row height. |
| [`Utility`](Controls/BlazorGrid/Utility.cs) | static class | Legacy string helpers `IsNullOrEmpty` and `AddSpacesForCamelCase`. |

### Controls/FieldGrid

| Type | Kind | Purpose |
|---|---|---|
| [`FieldGrid`](Controls/FieldGrid/FieldGrid.razor) | class | Field layout of `FieldRow` children: `FieldLayout.Stacked` (default, label above control, fields across `Columns` columns) or `FieldLayout.Inline` ("Label:" in an aligned left column), compact by default (`Compact`), with an optional group `Label`. |
| [`FieldLayout`](Controls/FieldGrid/FieldLayout.cs) | enum | Where a `FieldGrid` puts each label: `Stacked` above its control or `Inline` beside it. |
| [`FieldRow`](Controls/FieldGrid/FieldRow.razor) | class | One field: a `Label` naming the child control given by `For`, or a read-only `Value` labelled by the row; `Wide` spans every stacked column or stretches the inline control. |

### Controls/GroupBoxCaption

| Type | Kind | Purpose |
|---|---|---|
| [`GroupBoxCaption`](Controls/GroupBoxCaption/GroupBoxCaption.razor) | class | A group box's heading (`Text`, `Level` 1 to 6) with its state icons (`ChildContent`) and its screen `Commands` as icons on the right, never taller than the heading; the caption carries the heading's bottom spacing (`--sphere10-group-caption-spacing`). |
| [`ScreenCommandIcon`](Controls/GroupBoxCaption/ScreenCommandIcon.razor) | class | One `ScreenCommand` as a small icon button: the toolbar's icon and action, disabled exactly when the toolbar item is, the caption naming its blocker as tooltip and the command's text as accessible name; refreshes with its screen and on `NotifyScreenChanged`. |
| [`StateIcon`](Controls/GroupBoxCaption/StateIcon.razor) | class | A state such as Locked shown as an icon (`Icon`, `Text`, `Shown`) in a fixed slot that keeps its size while the state is off, so showing it never shifts the layout. |

### Controls/IdentityControl

| Type | Kind | Purpose |
|---|---|---|
| [`IdentityControl`](Controls/IdentityControl/IdentityControl.razor.cs) | class | Shows the current user's avatar, display name and secondary text and opens a keyboard-accessible menu of application commands. |

### Controls/SplitContainer

| Type | Kind | Purpose |
|---|---|---|
| [`SplitContainer`](Controls/SplitContainer/SplitContainer.razor.cs) | class | Two panels separated by a draggable, keyboard-operable splitter, with a two-way `SplitterPosition` in percent, minimum panel sizes and an optional remembered position under a `SettingsKey`. |
| [`SplitContainerSettings`](Controls/SplitContainer/SplitContainerSettings.cs) | class | `SettingsObject` holding a split container's remembered `SplitterPosition`. |
| [`SplitOrientation`](Controls/SplitContainer/SplitOrientation.cs) | enum | `Horizontal` places the first panel above the second and `Vertical` to its left, as the WinForms SplitContainer's orientation does. |

### Dialogs

| Type | Kind | Purpose |
|---|---|---|
| [`Confirm`](Dialogs/Confirm.razor) | class | Confirmation modal with a title, message and confirm and cancel buttons. |
| [`Dialog`](Dialogs/Dialog.razor) | class | Information modal with a title, message or body fragment and optional custom buttons. |
| [`ModalComponent`](Dialogs/ModalComponent.razor) | class | Base modal content of this generation with `ModalOptions` and `ShowAsync`, `Ok`, `OkData`, `Cancel` and `RequestCloseAsync`. |
| [`ModalHost`](Dialogs/ModalHost.razor.cs) | class | Hosts one `Dialogs.ModalComponent` at a time for `Modal.ModalService` until its interaction finishes, and shows the close confirmation of `ConfirmCloseAsync` above it in a second host whose `Id` ends in `-confirmation`. |
| [`ModalTemplate`](Dialogs/ModalTemplate.razor) | class | Modal frame with header, body and footer templates, an optional close button and `ModalOptions` sizing. `RequestDismissAsync` applies the `ModalOptions.BackdropDismissal` policy, or the dialog's `DefaultBackdropDismissal`, to a click outside the dialog or the Escape key. |
| [`Severity`](Dialogs/Severity.cs) | enum | `Info`, `Warning` or `Error`. |

### Dialogs/Content

| Type | Kind | Purpose |
|---|---|---|
| [`ExceptionContent`](Dialogs/Content/ExceptionContent.razor) | class | Shows an exception's display text with collapsible diagnostic details and a copy-to-clipboard button. |
| [`MessageContent`](Dialogs/Content/MessageContent.razor) | class | Renders a cascaded message string. |

### Grids

| Type | Kind | Purpose |
|---|---|---|
| [`ItemRequest`](Grids/ItemsRequest.cs) | record | Requested item index, count, sort column and sort direction for `VirtualPagedTable<TItem>`. |
| [`ItemsResponse<TItem>`](Grids/ItemsResponse.cs) | record | One page of items and the total item count returned to `VirtualPagedTable<TItem>`. |
| [`PageSizeSelector`](Grids/PageSizeSelector.razor) | class | Two-way bound page-size dropdown that always includes the current value among its options. |
| [`PagedTable<TItem>`](Grids/PagedTable.razor.cs) | class | Table that pages a local collection with item and header templates and a row-select callback. |
| [`RapidTable<TItem>`](Grids/RapidTable.razor.cs) | class | Table that renders items from an `IAsyncEnumerable<TItem>` up to `ItemLimit` and restarts when the source changes. |
| [`VirtualPagedTable<TItem>`](Grids/VirtualPagedTable.razor.cs) | class | Table that requests pages from an asynchronous items provider and ignores stale responses. |

### MainFrame

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationBlockMenu`](MainFrame/ApplicationBlockMenu.razor) | class | Block list rendered as buttons or links, with an optional compact icon dock. |
| [`ApplicationCommandBar`](MainFrame/ApplicationCommandBar.razor) | class | Menu bar and toolbar for arrays of application menus and toolbar items with a select callback; icon-bearing toolbar commands show only their icon with the caption as tooltip unless `ShowToolBarText` or the application's `ApplicationOptions.ToolBarText` is set, when they show `ToolBarCaption` beside the icon. |
| [`ApplicationMenus`](MainFrame/ApplicationMenus.razor) | class | Renders a block's menus and marks the active menu item. |
| [`Footer`](MainFrame/Footer.razor) | class | Footer element rendered only when it has child content. |
| [`MainLayout`](MainFrame/MainLayout.razor) | class | Minimal layout with a header, the page body and a `Dialogs.ModalHost` that it passes to `Modal.ModalService`. |
| [`SearchInput`](MainFrame/SearchInput.razor.cs) | class | Search box that calls a provider returning matching strings, with a minimum interval between searches. |
| [`Sidebar`](MainFrame/Sidebar.razor) | class | Sidebar with a brand link, block menu and the selected block's menus. |
| [`SidebarBrand`](MainFrame/SidebarBrand.razor) | class | Sidebar title link to the home URL with optional child content. |
| [`TopMenu`](MainFrame/TopMenu.razor) | class | Renders a block's menus through `ApplicationMenus`. |

### Theming

| Type | Kind | Purpose |
|---|---|---|
| [`ThemeProvider`](Theming/ThemeProvider.razor.cs) | class | Wrapper that loads the active theme's stylesheet and applies its theme attributes to descendants without recreating them. |
| [`ThemeSelector`](Theming/ThemeSelector.razor.cs) | class | Dropdown of every catalogue theme that changes the scoped theme without changing the route. |
| [`ThemedComponentBase`](Theming/ThemedComponentBase.cs) | abstract class | Base component that subscribes to `IThemeService.Changed`, exposes the current theme and Bootstrap theme names and unsubscribes on disposal. |

### Wizard

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorWizardHost`](Wizard/BlazorWizardHost.razor.cs) | class | Renders the current step of a `Wizard.IBlazorWizard`, applies step validation and cancellation policies and raises finished, cancelled and step-change callbacks. |
| [`BlazorWizardModal`](Wizard/BlazorWizardModal.razor) | class | Modal that hosts a `BlazorWizardHost` and allows closing only when the wizard can be cancelled. Its close button cancels the wizard directly, while a click outside it or the Escape key asks first unless the caller's `ModalOptions.BackdropDismissal` says otherwise. |
| [`BlazorWizardStep<TModel>`](Wizard/BlazorWizardStep.razor.cs) | abstract class | Base step component with typed access to its wizard and model. |
| [`BlazorWizardStepBase`](Wizard/BlazorWizardStep.razor.cs) | abstract class | Non-generic step base that implements the shared `IWizardStep` traits, title, button texts, cancellation flag and next and previous callbacks. |
