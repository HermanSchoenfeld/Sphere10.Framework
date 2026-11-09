# Events

A legacy event-arguments type carried over from the original Blazor presentation code.

`DataSourceChangedEventArgs` takes a server `Uri` in its constructor and keeps it in a private property, so handlers cannot read it. No type in this library raises or consumes it. New code should use the change notifications of `IDataSource<T>`, which the [Grids](../Grids/README.md) controller already observes.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DataSourceChangedEventArgs`](DataSourceChangedEventArgs.cs) | class | `EventArgs` constructed with a non-null server `Uri` that it stores privately. |
