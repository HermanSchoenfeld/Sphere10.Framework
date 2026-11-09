# Grid and Wizard Samples

Holds the grid content, sample entities and data sources, grid cell components and the branching new-wallet wizard used by the tester's component gallery.

`UI/Index.razor` is the grid gallery content that `ComponentScreen` in [Application](../Application/README.md) hosts, and it renders `GridDemo` from [Demos](../Demos/README.md). `TestClassDataSource` is a local CRUD `ListDataSource<TestClass>` with search, typed sorting, paging and validation. `GridDemoDataSource` is a read-only capture of 96 rows, and `GridDemoRefreshableDataSource` is the `RefreshableDataSourceBase` that publishes the next revision of every row while keeping their ids. `GridDemo` wraps it in a `PollingDataSource` for the live cadence. The wallet wizard steps derive from `BlazorWizardStep<NewWalletModel>`, and `WalletTypeStep` switches the remaining steps between the password and restore branches.

`TestClass2`, `TestClass3`, the four `BlazorGrid/Components` cell components and `Wizard/ExampleModel` are older scaffolding that no current page uses. `Logic/Wizard` is an empty folder.

## Contents

### UI

| Type | Kind | Purpose |
|---|---|---|

### UI/Controls

| Type | Kind | Purpose |
|---|---|---|
| [`TestClass`](UI/Controls/TestClass.cs) | class | Grid entity with id, name, enum color, date, decimal, flag, details, note and an optional related item, and a nested `TestEnum` |
| [`TestClass2`](UI/Controls/TestClass2.cs) | class | Unused `IColumnDefinition` sample with text, value and locked flag rendered by `ComponentForTestClass2` |
| [`TestClass3`](UI/Controls/TestClass3.cs) | class | Unused `IColumnDefinition` sample with a hash code rendered by `ComponentForTestClass3` |
| [`TestClassDataSource`](UI/Controls/TestClassDataSource.cs) | class | Local CRUD `ListDataSource<TestClass>` with search, typed sorting, paging and validation of create, update and delete actions |

### UI/Controls/BlazorGrid/Components

| Type | Kind | Purpose |
|---|---|---|

### UI/Wizard/Examples/NewWallet

| Type | Kind | Purpose |
|---|---|---|
| [`NewWalletModel`](UI/Wizard/Examples/NewWallet/NewWalletModel.cs) | class | Wizard model with wallet type, password, name and seed |
| [`WalletType`](UI/Wizard/Examples/NewWallet/NewWalletModel.cs) | enum | Wizard branch: `Standard` creates a wallet with a password, `Restore` restores one from a seed |

### Wizard

| Type | Kind | Purpose |
|---|---|---|
| [`ExampleModel`](Wizard/ExampleModel.cs) | class | Unused wizard model with one `EvenNumber` property |
