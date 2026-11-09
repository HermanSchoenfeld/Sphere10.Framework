# Transactions

Commit and rollback contracts, nested call-context transactional scopes, and file-backed transactional buffers, streams, lists, dictionaries and hash sets.

`ITransactionalObject` is the shared contract, with `Commit`, `Rollback`, their asynchronous pairs and the `Committing`, `Committed`, `RollingBack` and `RolledBack` events. The file-backed types implement it over `TransactionalFileMappedBuffer`, which pages a file into memory and writes changed pages to separate uncommitted page files. Marker files record each page's state, so an interrupted commit or rollback resumes when the file is next loaded. `Commit()` copies the uncommitted pages into the file, and `Rollback()` discards them. Disposing the buffer while changes are still uncommitted rolls them back. `TransactionalStream` exposes that buffer as a seekable stream. `TransactionalList<T>`, `TransactionalDictionary<TKey, TValue>` and `TransactionalHashSet<TItem>` place the stream-mapped collections over a `TransactionalStream`, so their changes become durable only on commit. These types need loading before use, through `Load()` or an auto-load access mode.

`TransactionalScopeBase<TTransaction>` is a [Scopes](../Scopes/README.md) `ContextScope` for repository transactions that span nested calls. The first scope for a context name is the root. The scope that calls `BeginTransaction()` owns the transaction, and nested scopes that call it join the same transaction. A nested `Rollback()` only votes for rollback, and the owner's `Commit()` then rolls back instead. Disposing the owning scope closes the transaction, without calling commit or rollback for you. Derive from `SyncTransactionalScope<TTransaction>` or `AsyncTransactionalScope<TTransaction>` depending on whether the transaction API is synchronous or asynchronous, or pass delegates to `ActionTransactionalScope<TTransaction>` or `TaskTransactionalScope<TTransaction>`. The [Streams](../Streams/README.md) domain provides the `AsTransactional` stream extension.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionTransactionalScope<TTransaction>`](ActionTransactionalScope.cs) | class | Synchronous transactional scope whose begin, commit, rollback and close steps are delegates. |
| [`AsyncTransactionalObjectBase`](AsyncTransactionalObjectBase.cs) | abstract class | `TransactionalObjectBase` whose synchronous `Commit` and `Rollback` start the asynchronous versions on a worker and wait. |
| [`AsyncTransactionalScope<TTransaction>`](AsyncTransactionalScope.cs) | abstract class | Transactional scope implemented through asynchronous hooks, whose synchronous members block on them. |
| [`ITransactionalDictionary<TKey, TValue>`](ITransactionalDictionary.cs) | interface | Stream-mapped dictionary that is also an `ITransactionalObject`. |
| [`ITransactionalFile`](ITransactionalFile.cs) | interface | Transactional object over a file, with its `TransactionalFileDescriptor` and byte-buffer view. |
| [`ITransactionalFilePage<TItem>`](ITransactionalFilePage.cs) | interface | File page that tracks its uncommitted page file and whether it holds uncommitted data. |
| [`ITransactionalHashSet<TItem>`](ITransactionalHashSet.cs) | interface | Disposable stream-mapped hash set that is also an `ITransactionalObject`. |
| [`ITransactionalList<T>`](ITransactionalList.cs) | interface | Stream-mapped list that is also an `ITransactionalObject`. |
| [`ITransactionalObject`](ITransactionalObject.cs) | interface | `Commit`, `Rollback`, `CommitAsync` and `RollbackAsync` with committing, committed, rolling-back and rolled-back events. |
| [`ITransactionalScope`](ITransactionalScope.cs) | interface | Context scope that is a transactional object and can begin a transaction. |
| [`ITransactionalScope<TRootScope>`](ITransactionalScope.cs) | interface | Transactional scope that exposes its root scope as `TRootScope`. |
| [`SyncTransactionalScope<TTransaction>`](SyncTransactionalScope.cs) | abstract class | Transactional scope implemented through synchronous hooks, whose asynchronous members run them on thread-pool tasks. |
| [`TaskTransactionalScope<TTransaction>`](TaskTransactionalScope.cs) | class | Asynchronous transactional scope whose begin, commit, rollback and close steps are task-returning delegates. |
| [`TransactionAction`](TransactionAction.cs) | enum | `Commit` or `Rollback`. |
| [`TransactionalDictionary<TKey, TValue>`](TransactionalDictionary.cs) | class | Stream-mapped dictionary over a `TransactionalStream` whose changes persist on commit. |
| [`TransactionalFileMappedBuffer`](TransactionalFileMappedBuffer.cs) | class | Memory-paged byte buffer over a file whose changed pages persist on commit or are discarded on rollback. |
| [`TransactionalFileMappedListBase<TItem>`](TransactionalFileMappedListBase.cs) | abstract class | File-paged list base that keeps uncommitted pages and marker files beside the file and applies them on commit. |
| [`TransactionalFilePageBase<TItem>`](TransactionalFilePageBase.cs) | abstract class | File page that reads from the committed file or from its uncommitted page file. |
| [`TransactionalHashSet<TItem>`](TransactionalHashSet.cs) | class | Stream-mapped hash set, keyed by item digest, over a transactional dictionary. |
| [`TransactionalList<T>`](TransactionalList.cs) | class | List stored in a file with commit and rollback and in-memory page caching. |
| [`TransactionalObjectBase`](TransactionalObjectBase.cs) | abstract class | Base `ITransactionalObject` that raises the four transaction events through protected notify methods. |
| [`TransactionalScopeBase<TTransaction>`](TransactionalScopeBase.cs) | abstract class | Context scope that owns or joins a transaction, collects rollback votes from nested scopes and closes the transaction when the owner ends. |
| [`TransactionalScopeDecorator<TTransactionalScope>`](TransactionalScopeDecorator.cs) | struct | Struct wrapper that forwards every `ITransactionalScope` member to an inner scope. |
| [`TransactionalStream`](TransactionalStream.cs) | class | Loadable stream over a `TransactionalFileMappedBuffer` for a `TransactionalFileDescriptor`. |
| [`TransactionalStream<TInnerStream>`](TransactionalStream.cs) | class | Stream decorator whose commit and rollback go to a supplied `ITransactionalObject`. |
