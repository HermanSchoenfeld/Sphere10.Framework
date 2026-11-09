# Binding

A list-binding adapter that exposes a `System.Data.DataView` through the Framework `IBoundList` contract.

`BoundDataView` wraps a `DataView` and implements `IBoundList`, the binding contract that list and grid controls consume. It reports edits, sorting and property descriptors from the underlying view, raises `ListChanged` and `ListCleared`, and raises `ItemDeleted` with the deleted `DataRow` rather than its transient `DataRowView`. The class is disposable and detaches from the view when disposed.

Use it when a control expects `IBoundList` and the data is already in a `DataTable`. The [Extensions](../Extensions/README.md) domain converts readers, entities and sequences into tables that can feed it.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BoundDataView`](BoundDataView.cs) | class | Adapts a `DataView` to `IBoundList` with add, edit, delete, sort and change notifications. |
