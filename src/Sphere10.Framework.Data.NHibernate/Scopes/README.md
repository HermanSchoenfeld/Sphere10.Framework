# Scopes

Provides `DataAccessScope`, a nestable transactional scope that shares one NHibernate session and transaction across nested scopes.

`DataAccessScope` derives from `TransactionalScopeBase<ITransaction>` in the core library. The root scope opens a session from `INHDatabaseManager.OpenDatabase(connectionString)` and owns it. Nested scopes for the same DBMS type and connection string reuse the root's session. `BeginTransaction()` uses `ReadCommitted`, and an overload takes another `IsolationLevel`. Beginning, committing and rolling back each have synchronous and asynchronous forms. When the root scope ends it flushes, closes and disposes the session.

The constructor throws when an ambient `System.Transactions` transaction exists, and beginning a transaction inside one is rejected. Each root scope calls `OpenDatabase`, which builds a new session factory, as [Database](../Database/README.md) describes.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DataAccessScope`](DataAccessScope.cs) | class | Nestable transactional scope keyed by DBMS type and connection string that owns an NHibernate `ISession` and its `ITransaction` |
