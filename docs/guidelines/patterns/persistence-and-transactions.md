# Persistence, Repositories and Transactions

Read this topic when choosing or implementing entity repositories, load/save
lifecycle, transactional collections or backend transaction scopes. Inspect the
concrete storage owner and its consumers before selecting a contract. Read
[sync/async hierarchies](sync-async.md) for execution direction,
[resource scopes](resource-scopes.md) for cleanup, and
[collection selection](collections.md) for the available families, and
[collections and storage](collections-and-storage.md) for storage composition.

## Choose the capability the consumer needs

These are separate capabilities. A repository is not inherently durable, a
loadable object is not inherently disposable, and a transaction interface does
not supply a transaction backend.

| Need | Existing contract and responsibility |
|---|---|
| Entity CRUD by business identity | [IRepository<TEntity, TIdentity>](../../../src/Sphere10.Framework/Repository/IRepository.cs): contains/get, create/update/delete/clear, notifications and both disposal forms. |
| Prepare a stored representation for use | [ILoadable](../../../src/Sphere10.Framework/Loadable/ILoadable.cs): `RequiresLoad`, `Load`/`LoadAsync`, `Loading`/`Loaded`. |
| Save an object's own state | [ISaveable](../../../src/Sphere10.Framework/Saveable/ISaveable.cs): `RequiresSave`, `Save`/`SaveAsync`, `Saving`/`Saved`. |
| Both load and save lifecycle | [IPersistable](../../../src/Sphere10.Framework/Persistable/IPersistable.cs): composes `ILoadable` and `ISaveable`. |
| Complete or discard pending backend changes | [ITransactionalObject](../../../src/Sphere10.Framework/Transactions/ITransactionalObject.cs): commit/rollback pairs and notifications. |
| Begin a transaction and participate in logical nesting | [ITransactionalScope](../../../src/Sphere10.Framework/Transactions/ITransactionalScope.cs): adds explicit begin and transactional root access to the context/scope contracts. |
| Durable list, dictionary or set with rollback | `ITransactionalList<T>`, `ITransactionalDictionary<TKey, TValue>` and `ITransactionalHashSet<T>` compose stream-mapped collection capabilities with `ITransactionalObject`. |

The current lifecycle names are **`ILoadable` and `ISaveable`**; `Loading` and
`Saving` are events. The repository contract is generic in entity and identity.
It has no `Save()` method, transaction member, query language or required
enumeration API. Choose [IDataSource<T>](data-sources.md) for UI CRUD with paging,
sorting and validation instead of inventing those capabilities on a repository.

The older [IPersistedDictionary<TKey, TValue>](../../../src/Sphere10.Framework/Collections/Dictionaries/IPersistedDictionary.cs)
extends `IDictionary` with synchronous `Load`, `Save` and `Delete` only. It does
not compose `IPersistable` or `ITransactionalObject`, expose readiness/dirty
state/events, or promise async operations. It is used as the file registry
contract by [SimpleFileStore](../../../src/Sphere10.Framework.Data/FileStore/SimpleFileStore.cs).
[NotPersistedDictionary](../../../src/Sphere10.Framework/Collections/Dictionaries/NotPersistedDictionary.cs)
implements its lifecycle as no-ops; the interface alone is not proof of durable
storage. [XmlFileDictionary](../../../src/Sphere10.Framework.Data/XML/XmlFileDictionary.cs)
loads/saves XML and deletes its backing file. Its load recovery can delete and
recreate an unreadable file, so do not assume non-destructive validation or
transactional replacement from this older contract.

## Repository identity and composition

Use `TryGet` for an expected miss. Its async counterpart returns
`(bool, TEntity)`; [Get/GetAsync extensions](../../../src/Sphere10.Framework/Repository/IRepositoryExtensions.cs)
throw when the entity is absent. `Contains` can be derived from `TryGet`, as in
[RepositoryBase](../../../src/Sphere10.Framework/Repository/RepositoryBase.cs),
but a concrete backend can supply a cheaper identity lookup.

[DictionaryRepositoryAdapter](../../../src/Sphere10.Framework/Repository/DictionaryRepositoryAdapter.cs)
adapts an existing `IDictionary<TIdentity, TEntity>` and an entity-to-identity
function. Creation rejects a duplicate identity; update and deletion reject a
missing identity. An update obtains its target identity from the supplied
entity, so changing that identity does not automatically move an old dictionary
entry. Keep business identity stable, or perform an explicit supported rekeying
operation at the owning layer.

```csharp
using Sphere10.Framework;

public sealed class Customer {
	public int Id { get; set; }

	public string Name { get; set; }
}

public static class RepositoryExample {
	public static string CreateAndRead() {
		using var repository = new DictionaryRepositoryAdapter<Customer, int>(item => item.Id);
		repository.Create(new Customer { Id = 1, Name = "Alice" });
		var customer = repository.Get(1);
		customer.Name = "Alice Smith";
		repository.Update(customer);
		return repository.Get(1).Name;
	}
}
```

This example stores references in a dictionary; it does not persist a file or
clone entities. The adapter disposes a supplied dictionary if it implements
`IDisposable`. Decide that ownership before adapting a shared dictionary.
[AsRepository extensions](../../../src/Sphere10.Framework/Extensions/DecoratorExtensions.cs)
construct the same adapter; they do not alter that ownership.

Use [RepositoryDecorator](../../../src/Sphere10.Framework/Repository/RepositoryDecorator.cs)
for behavior on the same contract. It forwards events and both disposal forms
to the inner repository. [SynchronizedRepository](../../../src/Sphere10.Framework/Repository/SynchronizedRepository.cs)
adds read/write scopes; its async operations use a blocking same-thread pump,
not nonblocking asynchronous locks. See [synchronization](synchronization.md)
before combining that wrapper with asynchronous consumers.

Repository event pairs are `Changing`/`Changed`, `Saving`/`Saved`,
`Adding`/`Added`, `Updating`/`Updated` and `Clearing`/`Cleared`. The common base
supplies `On...` hooks and `Notify...` helpers, but does not wrap the abstract CRUD
operations automatically. Concrete implementations must invoke the appropriate
notifications. The current dictionary adapter does not invoke them; a consumer
must not assume every repository mutation raises every declared event.

The file named [TransactionalRepository.cs](../../../src/Sphere10.Framework/Repository/TransactionalRepository.cs)
contains commented-out scaffolding, not an available implementation. To adapt a
transactional dictionary as a repository, retain its actual transaction owner
and explicitly commit/rollback there. `IRepository` alone does not expose that
capability and wrapping an ordinary dictionary does not add rollback.

## Load/save lifecycle and state

[LoadableBase](../../../src/Sphere10.Framework/Loadable/LoadableBase.cs) owns the
sequence `OnLoading`/`Loading`, load primitive, `OnLoaded`/`Loaded`.
[SaveableBase](../../../src/Sphere10.Framework/Saveable/SaveableBase.cs) owns the
matching save sequence; [PersistableBase](../../../src/Sphere10.Framework/Persistable/PersistableBase.cs)
combines both. An unsuccessful primitive does not reach its after-notification;
an exception from a notification also interrupts the sequence.

The bases do **not** automatically check `RequiresLoad`/`RequiresSave`, coalesce
concurrent calls or update those properties after work. The concrete backend
owns its readiness and dirty state. `LoadableBase.RequiresLoad` defaults to
false; `SaveableBase` and `PersistableBase` supply a settable `RequiresSave` that
also defaults to false. Implement meaningful state rather than treating the
base as a once-only initializer or automatic dirty tracker.

Consume the contract's state when deciding whether work is needed:

```csharp
using System.Threading.Tasks;
using Sphere10.Framework;

public static class PersistenceExample {
	public static async Task PrepareAsync(ILoadable storage) {
		Guard.ArgumentNotNull(storage, nameof(storage));
		if (storage.RequiresLoad)
			await storage.LoadAsync();
	}

	public static async Task SaveChangesAsync(ISaveable storage) {
		Guard.ArgumentNotNull(storage, nameof(storage));
		if (storage.RequiresSave)
			await storage.SaveAsync();
	}
}
```

These checks do not make concurrent use safe; use the backend's synchronization
and ownership discipline. `ILoadable`, `ISaveable` and `IPersistable` do not
declare disposal or cancellation. Dispose through a separately established
owner. Their task-returning methods have no `CancellationToken` parameter;
cancelling a caller's wait does not cancel the ongoing load/save or roll it back.

Async lifecycle notifications in the current bases run their event delegates
through `Task.Run`. Do not update UI controls directly from those handlers or
assume they run on the invoking thread. Decorators forward event subscriptions
to the original object and preserve its event sender and timing.

Follow the established directional hierarchy when implementing these APIs:

| Family | Shared layer | Synchronous primitive | Asynchronous primitive |
|---|---|---|---|
| Repository | `RepositoryBase<TEntity, TIdentity>` | `SyncRepositoryBase<TEntity, TIdentity>` | `AsyncRepositoryBase<TEntity, TIdentity>` |
| Load | `LoadableBase` | `SyncLoadableBase` | `AsyncLoadableBase` |
| Save | `SaveableBase` | `SyncSaveableBase` | `AsyncSaveableBase` |
| Load and save | `PersistableBase` | `SyncPersistableBase` | `AsyncPeristableBase` (current public spelling) |
| Transaction scope | `TransactionalScopeBase<TTransaction>` | `SyncTransactionalScope<TTransaction>` | `AsyncTransactionalScope<TTransaction>` |

The common layer owns shared sequencing/state/hooks; the directional layer
adapts the opposite API; concrete implementations supply their chosen primitive.
Do not make the neutral base choose a direction, make both forms call each other,
or repeat bridges in every concrete implementation. The
[sync/async guide](sync-async.md#adapter-completion-and-cancellation) owns the
completion and context requirements. Several older async-first bases above use
`WaitSafe`, which currently schedules a wait without joining it. Their
synchronous void operations and synchronous cleanup are not evidence that the
asynchronous work has finished; prefer their awaited APIs and do not copy the
legacy bridge into new implementations.

[LoadableDecorator](../../../src/Sphere10.Framework/Loadable/LoadableDecorator.cs)
and [SaveableDecorator](../../../src/Sphere10.Framework/Saveable/SaveableDecorator.cs)
forward their respective contracts. The current
[PersistableDecorator<T>](../../../src/Sphere10.Framework/Persistable/PersistableDecorator.cs)
has save members but only declares `ILoadable` through its base; it does not
declare `IPersistable`. Do not assume it is assignable to that combined contract
without a concrete implementation explicitly supplying it.

## Saving, flushing and committing are different operations

A method name does not define a durability boundary. Establish what the storage
backend actually writes and which owner completes its transaction.

| Operation | Meaning in the current implementation |
|---|---|
| Repository `Create`/`Update` | The repository's mutation policy; a dictionary adapter only changes dictionary entries. |
| `ISaveable.Save` | The concrete object's save primitive; the interface promises no rollback or particular storage medium. |
| `ObjectSpace.Save(item)` | Serialize a tracked instance into its dimension and update indexes. |
| `ObjectSpace.Flush()` | Save configured dirty tracked objects and flush indexes/backing stream. |
| `TransactionalStream.Flush()` / file-page flush | Stage pending bytes/pages; this is not publication of the committed base file. |
| `ITransactionalObject.Commit()` | Complete the concrete transaction; scope participation may defer physical completion to another owner. |
| `ITransactionalObject.Rollback()` | Discard pending backend changes; derived in-memory views may need rebuilding. |

[TransactionalStream](../../../src/Sphere10.Framework/Transactions/TransactionalStream.cs)
flushes its backing stream before delegating commit. Transactional lists and
dictionaries compose stream-mapped storage with that backend and reinitialize
their clustered-stream view after rollback. They supply file-backed transactions,
not an undo log for an arbitrary in-memory list.

For [FileObjectSpace](../../../src/Sphere10.Framework/ObjectSpaces/FileObjectSpace.cs),
`Save`/`Flush` affect pending transactional pages; explicit `Commit()` publishes
them. Rollback unloads and reloads dimensions/identity tracking. Discard old
tracked entities, grid rows and query snapshots after rollback. Memory spaces
have no file transaction. Their flush makes a backing-memory snapshot current.
Follow the [ObjectSpace durability and ownership guide](../../../src/Sphere10.Framework/ObjectSpaces/README.md#durability-scopes-and-ownership)
for autosave, original layout configuration and owner-thread lifetime.

## Transaction scopes and nested ownership

[ContextScope](../../../src/Sphere10.Framework/Scopes/ContextScope/ContextScope.cs)
uses a `ContextID` and [CallContext's AsyncLocal storage](../../../src/Sphere10.Framework/Scopes/ContextScope/CallContext.cs)
to identify a root within the logical call context. `MustBeRoot` rejects an
existing matching root, `MustBeNested` requires one, and `None` permits either.
This is context participation, not locking or an independent transaction per
scope. Child tasks can inherit the same root reference; do not use that to run
concurrent operations on a shared transaction/session.

[TransactionalScopeBase<TTransaction>](../../../src/Sphere10.Framework/Transactions/TransactionalScopeBase.cs)
separates the context root from the transaction owner. A nested scope can create
the transaction when the root has none. Begin the intended owning transaction
before constructing its participants, and finish/dispose participants before
the owner completes. Only the creating scope owns backend commit/rollback/close.

| Explicit operation | Owning scope | Nested participant |
|---|---|---|
| `BeginTransaction` | Creates the backend transaction when absent; repeating begin while still owning it throws. | Uses the transaction captured from the root. |
| `Commit` | Commits unless a participant voted rollback, in which case it rolls back; then closes the backend transaction. | Closes local participation without physically committing. |
| `Rollback` | Rolls back and closes the backend transaction. | Records a rollback vote on the owner; does not create an independent savepoint. |
| Disposal with unfinished work | Calls the owned close hook; the backend determines what closing means. | Does not automatically cast a rollback vote. |

There are no independent nested savepoints in this common pattern. Inner success
can still be rolled back by the owner, and an explicit inner rollback affects
the whole shared transaction. The owner can begin another transaction after
completion; do not retain old child scopes/views as participants in that new
transaction.

The current `Commit` orchestration raises `Committing`/`Committed` even when the
owner honors a rollback vote by calling the backend rollback primitive. A
participant also raises those events although it performs no backend commit.
Consequently `Committed` is a scope-operation notification, not proof that the
database/file changes became durable. Backend outcome and an owning consumer's
success decision remain authoritative.

The common scope disposal path does not explicitly call `Rollback`; unfinished
child disposal is not a veto. Explicitly roll back abandoned work according to
the concrete backend contract. Close/notification failures can interrupt the
completion path; do not assume disposal or a success event repairs a failed
commit. Reuse concrete recovery mechanisms and preserve the original failure.

## Choose a concrete backend

| Backend | Existing implementation and important ownership |
|---|---|
| ADO.NET database work | [IDAC.BeginScope()](../../../src/Sphere10.Framework.Data/DAC/IDACExtensions.cs) returns [DACScope](../../../src/Sphere10.Framework.Data/Scopes/DACScope.cs). The root owns a connection, and the creating scope owns a local `IDbTransaction`. |
| NHibernate work | [DataAccessScope](../../../src/Sphere10.Framework.Data.NHibernate/Scopes/DataAccessScope.cs) shares the root session and transaction for its database/context key. The session owner closes/disposes it; ambient `System.Transactions` is rejected. |
| Several transactional files | [FileTransactionScope](../../../src/Sphere10.Framework/IO/FileTransactionScope.cs) coordinates [FileTransaction](../../../src/Sphere10.Framework/IO/FileTransaction.cs) and enlisted page-backed files. |
| A single serialized list/dictionary/set | [TransactionalList](../../../src/Sphere10.Framework/Transactions/TransactionalList.cs), [TransactionalDictionary](../../../src/Sphere10.Framework/Transactions/TransactionalDictionary.cs), [TransactionalHashSet](../../../src/Sphere10.Framework/Transactions/TransactionalHashSet.cs) compose collection storage with a transactional stream. |
| Serialized bytes / tree storage | [TransactionalFileMappedBuffer](../../../src/Sphere10.Framework/Transactions/TransactionalFileMappedBuffer.cs) and `TransactionalStream`; persistent trees can share that byte backend instead of adding an unrelated transaction implementation. |
| Typed entities with identity/member indexes | `FileObjectSpace`, using its existing construction and transaction lifecycle. |
| An application-owned backend with existing begin/commit/rollback/close operations | [ActionTransactionalScope<T>](../../../src/Sphere10.Framework/Transactions/ActionTransactionalScope.cs) or [TaskTransactionalScope<T>](../../../src/Sphere10.Framework/Transactions/TaskTransactionalScope.cs) supplies delegate-backed integration. Delegates must perform the real backend work. |

`DACScope` shares connection/transaction through matching context keys.
`UseScopeOsmosis = true` permits sharing across DAC instances with the same
connection string; false separates different instances, but does not prevent
nesting on the same instance. Ambient system transactions and local scope
transactions have different owners: follow the [DAC guide](../../../src/Sphere10.Framework.Data/README.md#dacscope---connection-and-transaction-management)
for provider support, isolation and enlistment instead of mixing their completion
methods. The concrete core scopes are `DACScope` and NHibernate `DataAccessScope`;
both require an explicit transaction begin. An application's `DataScope` is its
DAL facade/convention. The existing [NHibernate DAL skill](../../../agents/skills/nhibernate-dal/SKILL.md#datascope-and-business-operations)
defines a runtime scaffold whose default constructor obtains the application
session and starts a transaction. It needs no second `BeginTransaction()` call
and is not a drop-in alias for core `DataAccessScope`. Follow that skill for
factory/session ownership and the existing application's nesting behavior.

## File and collection ownership

For a file transaction, enlist the file, mutate its buffer and complete the
scope. Enlisted files must use the same uncommitted-page directory. This example
expects an existing absolute staging directory and caller-selected file path:

```csharp
using System;
using System.Collections.Generic;
using Sphere10.Framework;

public static class FileTransactionExample {
	public static void Append(string transactionDirectory, string filePath, byte[] data) {
		Guard.ArgumentNotNull(data, nameof(data));
		using var scope = new FileTransactionScope(transactionDirectory, ContextScopePolicy.MustBeRoot);
		scope.BeginTransaction();
		var file = scope.EnlistFile(filePath, checked((int)Sphere10FrameworkDefaults.TransactionalPageSize), Sphere10FrameworkDefaults.MaxMemoryPerCollection);
		try {
			file.AsBuffer.AddRange(data);
		} catch (Exception failure) {
			var errors = new List<Exception> { failure };
			Tools.Exceptions.ExecuteCapturingException(scope.Rollback, errors);
			if (errors.Count > 1)
				throw new AggregateException("Append and rollback failed.", errors);
			throw;
		}
		scope.Commit();
	}
}
```

The scope owns this newly enlisted buffer. The overload accepting an existing
`ITransactionalFile` takes `ownsFile` explicitly. Commit/rollback enlisted files
through their scope; direct completion is rejected while they participate in an
active file scope. Unrelated files can still complete independently.

The example explicitly rolls back a mutation failure and preserves both errors
if rollback also fails. Commit occurs outside that catch: a commit failure can
leave the backend in a recovery state, so it propagates to the owner rather than
triggering a blind second completion operation. Apply the concrete backend's
recovery policy before retrying or attempting rollback after a failed commit.

[TransactionalFileMappedListBase](../../../src/Sphere10.Framework/Transactions/TransactionalFileMappedListBase.cs)
stages page/file markers, publishes pending pages at commit and rebuilds from
committed bytes at rollback. Its disposal can roll back outstanding markers;
this backend behavior does not make every `ITransactionalScope` an automatic
rollback scope, especially for borrowed enlisted files. `FileTransaction`
supports resuming a known journal in its constructor, while
`FileTransactionScope.ProcessDanglingTransactions` is currently an empty TODO;
do not advertise automatic directory-wide recovery through that method.

Transactional collection constructors that create their list/dictionary set
`OwnsList`/`OwnsDictionary` and wire stream ownership. Overloads accepting an
existing collection borrow it by default. Their wrapper disposal does not
universally dispose a separately supplied `ITransactionalObject`; trace
`OwnsList`/`OwnsDictionary`, `OwnsContainer`, `OwnsStreams` and `OwnsStream`
through the actual composition. Avoid disposing a shared backend twice or
discarding the only owner of pending transactions.

The interface's async member is not proof of backend support or freedom from
thread affinity. `FileObjectSpace.CommitAsync` and `RollbackAsync` currently
throw `NotSupportedException`; create/use/dispose the space on its owner thread.
Other file collections expose worker adapters, but shared streams and lock
scopes still need serialized, same-thread ownership where required. Existing
transaction/repository/load/save pairs do not accept cancellation tokens. Stop
new work at a safe boundary and let the actual owner complete or discard pending
work; abandoning an await does neither.

## Verify the chosen lifecycle

Verify entity identity, missing/duplicate handling, notification behavior and
ownership against the concrete implementation. For persisted storage, reload or
reopen after commit; a cached entity or collection entry alone is not durability
evidence. Cover explicit rollback, nested rollback votes, unfinished disposal,
fresh transactions after completion and backend failures as applicable. For
async work, verify completion/fault propagation and actual cleanup rather than
merely observing a task was returned.

Useful current behavioral references are
[TransactionalScopeInterfaceTests](../../../tests/Sphere10.Framework.Tests/Scopes/TransactionalScopeInterfaceTests.cs),
[FileTransactionScopeTests](../../../tests/Sphere10.Framework.Tests/Collections/Transactional/FileTransactionScopeTests.cs),
[TransactionalListTests](../../../tests/Sphere10.Framework.Tests/Collections/Transactional/TransactionalListTests.cs),
[TransactionalDictionaryTestsBase](../../../tests/Sphere10.Framework.Tests/Collections/Transactional/TransactionalDictionaryTestsBase.cs),
[DACScopeTests](../../../tests/Sphere10.Framework.Data.Tests/DACScopeTests.cs),
and [TransactionScopeCompatibilityTests](../../../tests/Sphere10.Framework.Data.Tests/TransactionScopeCompatibilityTests.cs).
The current [RepositoryDemoTest.cs](../../../tests/Sphere10.Framework.Tests/Repository/RepositoryDemoTest.cs)
is commented-out scaffolding; it is not executed repository coverage or a usable
transactional-repository example.
