# Grid and Wizard Samples

Holds the grid content, sample entities and data sources, grid cell components and the branching new-wallet wizard used by the tester's component gallery.

`UI/Index.razor` is the grid gallery content that `ComponentScreen` in [Application](../Application/README.md) hosts, and it renders `GridDemo` from [Demos](../Demos/README.md). `TestClassDataSource` is a local CRUD `ListDataSource<TestClass>` with search, typed sorting, paging and validation. `GridDemoDataSource` is a read-only capture of 96 rows, and `GridDemoRefreshableDataSource` is the `RefreshableDataSourceBase` that publishes the next revision of every row while keeping their ids. `GridDemo` wraps it in a `PollingDataSource` for the live cadence. The wallet wizard steps derive from `BlazorWizardStep<NewWalletModel>`, and `WalletTypeStep` switches the remaining steps between the password and restore branches.

`TestClass2`, `TestClass3`, the four `BlazorGrid/Components` cell components and `Wizard/ExampleModel` are older scaffolding that no current page uses. `Logic/Wizard` is an empty folder.

## Contents

### UI

| Type | Kind | Purpose |
|---|---|---|
| [`Index`](UI/Index.razor) | class | Razor grid gallery content with related-demo links that passes the two grid layout preferences through to `GridDemo` |

### UI/Controls

| Type | Kind | Purpose |
|---|---|---|
| [`GridDemoDataSource`](UI/Controls/GridDemoDataSource.cs) | class | Read-only, notifying `ListDataSource` over one capture of sample `Row` records (a nested record of id, name, group, revision, usage and details) |
| [`GridDemoRefreshableDataSource`](UI/Controls/GridDemoRefreshableDataSource.cs) | class | `RefreshableDataSourceBase` that serves a new `GridDemoDataSource` capture with the next revision of every row and supports removing a sample |
| [`TestClass`](UI/Controls/TestClass.cs) | class | Grid entity with id, name, enum color, date, decimal, flag, details, note and an optional related item, and a nested `TestEnum` |
| [`TestClass2`](UI/Controls/TestClass2.cs) | class | Unused `IColumnDefinition` sample with text, value and locked flag rendered by `ComponentForTestClass2` |
| [`TestClass3`](UI/Controls/TestClass3.cs) | class | Unused `IColumnDefinition` sample with a hash code rendered by `ComponentForTestClass3` |
| [`TestClassDataSource`](UI/Controls/TestClassDataSource.cs) | class | Local CRUD `ListDataSource<TestClass>` with search, typed sorting, paging and validation of create, update and delete actions |

### UI/Controls/BlazorGrid/Components

| Type | Kind | Purpose |
|---|---|---|
| [`Component1Test`](UI/Controls/BlazorGrid/Components/Component1Test.razor) | class | Razor `IGridComponent<string>` sample cell that renders a fixed select list with the given element id |
| [`Component2Test`](UI/Controls/BlazorGrid/Components/Component2Test.razor) | class | Razor `IGridComponent<bool>` sample cell that binds a checkbox to the value |
| [`ComponentForTestClass2`](UI/Controls/BlazorGrid/Components/ComponentForTestClass2.razor) | class | Razor `IGridComponent<TestClass2>` sample cell that shows the text and value and binds the locked flag |
| [`ComponentForTestClass3`](UI/Controls/BlazorGrid/Components/ComponentForTestClass3.razor) | class | Razor `IGridComponent<TestClass3>` sample cell that shows the hash code |

### UI/Wizard/Examples/NewWallet

| Type | Kind | Purpose |
|---|---|---|
| [`NewWalletModel`](UI/Wizard/Examples/NewWallet/NewWalletModel.cs) | class | Wizard model with wallet type, password, name and seed |
| [`PasswordStep`](UI/Wizard/Examples/NewWallet/PasswordStep.razor) | class | Razor wizard step that collects and validates the wallet password |
| [`RestoreStep`](UI/Wizard/Examples/NewWallet/RestoreStep.razor) | class | Razor wizard step that collects and requires the seed for a restored wallet |
| [`SummaryStep`](UI/Wizard/Examples/NewWallet/SummaryStep.razor) | class | Razor wizard step that summarizes the chosen settings |
| [`WalletNameStep`](UI/Wizard/Examples/NewWallet/WalletNameStep.razor) | class | Razor wizard step that collects and validates the wallet name |
| [`WalletType`](UI/Wizard/Examples/NewWallet/NewWalletModel.cs) | enum | Wizard branch: `Standard` creates a wallet with a password, `Restore` restores one from a seed |
| [`WalletTypeStep`](UI/Wizard/Examples/NewWallet/WalletTypeStep.razor) | class | Razor wizard step that chooses the wallet type and replaces the following steps with password and summary, or restore and summary |

### Wizard

| Type | Kind | Purpose |
|---|---|---|
| [`ExampleModel`](Wizard/ExampleModel.cs) | class | Unused wizard model with one `EvenNumber` property |
