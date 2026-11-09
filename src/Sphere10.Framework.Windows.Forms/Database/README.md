# Database

Database connection bars and panels that switch their editor by DBMS type and produce an `IDAC`, plus a form that runs a SQL script.

`IDatabaseConnectionProvider` is the common contract. It exposes `ConnectionString`, `DatabaseName`, `GetDAC()` and an asynchronous `TestConnection()`. `ConnectionBarBase` is the compact single-row form and `ConnectionPanelBase` is the full panel. DBMS-specific bars and panels derive from them and live in the provider projects, such as `Sphere10.Framework.Windows.Forms.Sqlite`.

`DatabaseConnectionBar` and `DatabaseConnectionPanel` show a DBMS selector and swap in the matching bar or panel at runtime. They resolve it from `INamedLookup<ConnectionBarBase>` or `INamedLookup<ConnectionPanelBase>`, keyed by the `DBMSType` name. Each provider module registers its implementation with `AddNamedTransient`. A missing registration throws `InvalidOperationException` when that DBMS is selected. Data access types such as `IDAC`, `DBMSType` and `ISQLBuilder` come from [Sphere10.Framework.Data](../../Sphere10.Framework.Data/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IDatabaseConnectionProvider`](IDatabaseConnectionProvider.cs) | interface | Connection editor contract with `ConnectionString`, `DatabaseName`, `GetDAC()` and `TestConnection()` |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ConnectionBarBase`](Controls/ConnectionBarBase.cs) | class | Base single-row connection editor that builds an `IDAC`, optionally loads artificial keys from a file and tests by opening a connection |
| [`ConnectionPanelBase`](Controls/ConnectionPanelBase.cs) | class | Base connection panel that builds an `IDAC` and tests it by opening a connection on a worker |
| [`DatabaseConnectionBar`](Controls/DatabaseConnectionBar.cs) | class | Connection bar with a DBMS selector that hosts the registered `ConnectionBarBase` for the selected `DBMSType` |
| [`DatabaseConnectionPanel`](Controls/DatabaseConnectionPanel.cs) | class | Connection panel with a DBMS selector that hosts the registered `ConnectionPanelBase` and raises `DBMSTypeChanged` |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`ExecuteScriptForm`](Forms/ExecuteScriptForm.cs) | class | Shows an `ISQLBuilder` script with the target connection string and runs it as a batch on a worker, or copies it to the clipboard |
