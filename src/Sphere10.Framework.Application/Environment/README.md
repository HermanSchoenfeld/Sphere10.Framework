# Environment

The running processes as a read-only, searchable and sortable data source.

`ProcessDataSource` is a [`ListDataSource<Process>`](../../Sphere10.Framework/DataSource/README.md) whose `Fetch()` returns `Process.GetProcesses()`. It reads, searches, sorts, pages and notifies, but does not create, update or delete. A search matches the process name, ignoring case, or the process ID. `ReadRange` sorts by `PID`, `Name`, `MemoryMB` (the working set), `ThreadCount`, `Priority` or `Responding`, and by ID for any other name. A page past the end is clamped to the last page. It does not dispose the `Process` objects it returns. Wrap it in a refreshable or polling decorator to keep the list current. The type is in the `Sphere10.Framework.Application` namespace. The process-tree helpers `KillTree` and `WaitForExit(TimeSpan)` are in [Extensions](../Extensions/README.md), and `ProcessSentry`, which runs an external command, stays in the core [Environment](../../Sphere10.Framework/Environment/README.md) domain.

```csharp
using System.Diagnostics;
using Sphere10.Framework;
using Sphere10.Framework.Application;

var processes = new PollingDataSource<Process>(new ActionRefreshableDataSource<Process>(() => new ProcessDataSource()));
processes.Enabled = true;
await processes.RefreshAsync();
var page = await processes.ReadRangeAsync("svc", 50, 0, "MemoryMB", SortDirection.Descending);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ProcessDataSource`](ProcessDataSource.cs) | class | The running processes as a read-only, searchable and sortable list data source over the BCL `Process` type; loading fetches them. |
