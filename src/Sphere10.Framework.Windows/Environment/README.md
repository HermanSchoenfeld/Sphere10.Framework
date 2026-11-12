# Environment Variables

Reads, validates and changes process, user and machine environment variables, and serves them as an editable data source.

`Tools.Windows.Environment` reads one scope with `ReadAll` or a single variable with `ReadAsync`. `SetAsync` and `DeleteAsync` take the value the caller last saw and refuse to write when the current value differs. User and machine values are written to the registry with their existing `REG_SZ` or `REG_EXPAND_SZ` type, so `PATH`-style expansion is preserved. After a persistent write the tool broadcasts `WM_SETTINGCHANGE` and returns a warning text when some windows did not acknowledge it. Writes through the tool are serialized, and the synchronous reads assert in debug builds when called on the main thread.

`EnvironmentVariableDataSourceBase` is a [`PollingDataSource`](../../Sphere10.Framework/DataSource/README.md) over the three scopes with create, update and delete support. Each refresh swaps in a fresh capture, and edits write through `Tools.Windows.Environment` and update the rows in place. Subclasses implement `ConfirmAsync` to approve persistent changes and may override `Logger`. `ActionEnvironmentVariableDataSource` takes the confirmation and logger as delegates for clients that compose rather than derive. Rows are `EnvironmentVariableEntry` objects, which keep the original name, value and scope as the edit baseline. Per-process environment blocks of other processes are read through `WindowsProcess` in the [Processes](../Processes/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionEnvironmentVariableDataSource`](ActionEnvironmentVariableDataSource.cs) | class | Environment variable data source whose change confirmation and logger are supplied as delegates |
| [`Tools.Windows.Environment`](EnvironmentUtil.cs) | static class | Expands, reads, validates, sets and deletes variables per scope with an expected-original-value check and a settings-change broadcast |
| [`EnvironmentVariableDataSourceBase`](EnvironmentVariableDataSourceBase.cs) | abstract class | Template CRUD data source over process, user and machine variables, with nested `Inventories` and `Inventory` captures and an abstract `ConfirmAsync` |
| [`EnvironmentVariableEntry`](EnvironmentVariableEntry.cs) | class | Editable row with name, value and scope plus the original values and owner used to validate and apply changes |
