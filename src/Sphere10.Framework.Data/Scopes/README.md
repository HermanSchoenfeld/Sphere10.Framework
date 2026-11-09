# Scopes

`DACScope`, the ambient connection and transaction scope that every [DAC](../DAC/README.md) command runs inside.

A `DACScope` is a synchronous transactional context scope keyed by connection string. The root scope creates and owns a `RestrictedConnection` from [ADO.NET](../ADO.NET/README.md), and nested scopes for the same connection string reuse it, so a group of DAC calls shares one connection. `BeginTransaction`, `Commit` and `Rollback` on the scope control the transaction, and a nested scope cannot open a transaction with a stronger isolation level than the current one. The restricted connection and transaction reject direct close, dispose, commit and rollback, so all cleanup goes through the scope.

Create scopes with `IDAC.EnterScope` or `IDAC.BeginDirtyReadScope` rather than the internal constructor. When a `System.Transactions.TransactionScope` is active, the root scope enlists the connection in it, and the scope's own `BeginTransaction`, `Commit` and `Rollback` then throw. `DACScope.GetCurrent(connectionString)` returns the active scope for a connection string.

```csharp
using (var scope = dac.EnterScope()) {
	scope.BeginTransaction();
	dac.ExecuteNonQuery("DELETE FROM AuditLog");
	scope.Commit();
}
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DACScope`](DACScope.cs) | class | Ambient, nestable scope that owns one restricted connection per connection string and manages its transaction or system-transaction enlistment. |
