---
name: state-machines
description: Configure or extend workflows that already use the Stateless C# library, or where the user explicitly authorizes its adoption. Covers typed triggers, hierarchical states, guarded transitions, and async actions; application lifecycle work alone does not select this skill. Unrelated to stateless cryptography.
---

# State Machines Skill

Apply this skill to existing Stateless consumers or user-authorized adoption. For those workflows, use `Stateless.StateMachine<TState, TTrigger>` and keep input routing, workflow transitions, and domain operations separate. Apply [code-style](../code-style/SKILL.md) to implementation and the existing application abstractions to dependencies.

## Scope and application lifecycle

- Inspect the target project's references and actual consumers before selecting this skill. A Stateless dependency elsewhere in the repository does not authorize adding it to the target project. Selecting this skill does not authorize a new dependency or replacement of established orchestration.
- Preserve `IApplication`'s existing lifecycle implementation without introducing Stateless into the shared application project. Its paired initialization, finalization and exit APIs use a neutral `ApplicationBase` and the existing `SyncApplicationBase` / `AsyncApplicationBase` directional adapters. Follow [the sync/async guide](../../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies) for execution direction and the [application lifecycle documentation](../../../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown) for task registration, ownership and shutdown behavior.
- A separate workflow that already uses Stateless may consume application services; that does not make the application lifecycle itself a state machine or change its sync/async implementation direction.

## Library and API baseline

- The library/package is **Stateless** (`using Stateless;`). Inspect the target project's actual package or project reference before choosing overloads; dependency changes require user authorization. The current Framework source has no `using Stateless` workflow consumers or package reference; its `StatelessDigitalSignatureScheme` classes are unrelated cryptographic types.
- Consult the [upstream documentation](https://github.com/dotnet-state-machine/stateless) and the source for the referenced version when adapting the configuration. Do not assume application extension methods ship with the library.
- `OnExitWhen`, `OnExitAsyncWhen`, `OnExitIf`, and `OnExitAsyncIf` can be application helpers. Stock `OnExitAsync(transition => ...)` plus a trigger predicate expresses the same conditional exit. Some one-argument typed internal-action overloads likewise come from helpers; stock typed async actions accept the payload **and** transition. The example's overload shapes and queued firing were rechecked against upstream [async configuration](https://github.com/dotnet-state-machine/stateless/blob/dev/src/Stateless/StateConfiguration.Async.cs) and [async dispatch](https://github.com/dotnet-state-machine/stateless/blob/dev/src/Stateless/StateMachine.Async.cs); verify the installed version rather than assuming the development branch is its release API.

## Shape the workflow

- Define separate state and trigger enums. States describe stages or modes; triggers describe events such as input received, next, back, finished, or restart. Store draft data in the owning session/model rather than adding a state for every value.
- Let a session/workflow object own one machine, registered typed triggers, and draft context. A coordinator locates sessions by stable entity/session ID; configure and activate a new session before normal dispatch. Keep I/O and domain work in named handlers or injected services so the configuration remains readable.
- Register `SetTriggerParameters<T>(Trigger.Input)` once and keep its `TriggerWithParameters<T>`. Use the matching typed `FireAsync` overload at dispatch. Use multiple typed parameters when an event needs both an input envelope and parsed arguments.
- Configure a bootstrap state with `OnActivateAsync(() => machine.FireAsync(initialTrigger))` and its allowed destination(s). Finish configuring and choosing the initial trigger before `ActivateAsync()`. Do not perform async startup in the constructor or treat activation as a per-input operation.
- Use `SubstateOf` to inherit shared navigation only where it belongs. Parent transitions remain available to descendants, so avoid placing privileged commands or completion effects on a parent shared with unrelated modes. Check membership with `IsInState` when descendants count.

## Choose the transition deliberately

| Intent | Configuration |
|---|---|
| Move to another stage | `Permit` / `PermitIf` |
| Handle input while keeping the current stage and its entry/exit actions untouched | `InternalTransition` / `InternalTransitionAsyncIf` |
| Reset the current stage and rerun exit/entry actions | `PermitReentry` / `PermitReentryIf` |
| Deliberately accept an event without work | `Ignore` |
| Show a prompt or prepare a stage | `OnEntryAsync` |
| Perform work only on a particular exit | `OnExitAsync` with a predicate on `transition.Trigger` |

Guard alternatives for the same trigger must be mutually exclusive and side effect free. A useful input pattern pairs `IsValid(input)` with `!IsValid(input)`: the invalid branch reports the problem without leaving the stage, while the valid branch records the value and fires an internal next/finished trigger. Put mutations and I/O in actions, not guards; use `Guard.*` for handler arguments and invariants.

Completion and cancellation often leave the same state. Check the exit trigger before saving/submitting so Back does not accidentally complete the operation. An exit handler returning normally does **not** veto a transition; if completion can be declined, represent that outcome in a guard or explicit processing/result stages.

## Configuration example

This configuration fragment assumes `WorkflowState`/`WorkflowTrigger` enums, an owned `draft`, and the named task-returning handlers. It uses stock overloads and can sit inside the workflow's configuration method.

```csharp
using Stateless;

var machine = new StateMachine<WorkflowState, WorkflowTrigger>(WorkflowState.Started, FiringMode.Queued);
var inputTrigger = machine.SetTriggerParameters<string>(WorkflowTrigger.Input);

machine.Configure(WorkflowState.Started)
	.OnActivateAsync(() => machine.FireAsync(WorkflowTrigger.Begin))
	.Permit(WorkflowTrigger.Begin, WorkflowState.Collecting);

machine.Configure(WorkflowState.Collecting)
	.SubstateOf(WorkflowState.Started)
	.OnEntryAsync(PromptAsync)
	.InternalTransitionAsyncIf(inputTrigger, value => !IsValid(value), (value, transition) => RejectAsync())
	.InternalTransitionAsyncIf(inputTrigger, IsValid, async (value, transition) => {
		draft.Value = value;
		await machine.FireAsync(WorkflowTrigger.Next);
	})
	.Permit(WorkflowTrigger.Next, WorkflowState.Review);

machine.Configure(WorkflowState.Review)
	.SubstateOf(WorkflowState.Started)
	.OnEntryAsync(PreviewAsync)
	.OnExitAsync(async transition => {
		if (transition.Trigger == WorkflowTrigger.Finish)
			await CompleteAsync();
	})
	.Permit(WorkflowTrigger.Back, WorkflowState.Collecting)
	.Permit(WorkflowTrigger.Finish, WorkflowState.Completed);
```

The owner retains `machine` and `inputTrigger`, awaits `machine.ActivateAsync()` during startup, and later awaits `machine.FireAsync(inputTrigger, input)`. Keep Begin/Next as internal events rather than accepting arbitrary trigger enum values from external input. `CompleteAsync` in this fragment represents successful completion or a propagated failure, not a boolean success decision.

## Async execution, failures, and state lifetime

- Register task-returning handlers with the `*Async` methods and await `FireAsync` end to end. An async lambda passed to a synchronous action can become `async void`; avoid blocking wrappers and unobserved tasks.
- Queued firing supports handlers that fire Next/Finished: a nested fire enqueues the event for the outer dispatch to drain. Do not assume the state changed immediately after the nested await. Queuing is **not** thread safety: serialize external dispatch per machine, including activation and session creation. An async gate or per-session queue belongs at the outer boundary; handlers must not reacquire that gate when firing an internal trigger.
- `RetainSynchronizationContext` is a host requirement, not a lock. Set it only when the execution host needs its context. Framework read/write scopes use thread-affine locks: finish synchronous database work and release the scope before awaiting network calls. See [disposable-scopes](../disposable-scopes/SKILL.md).
- Handle unsupported input at the dispatch boundary or through an intentional unhandled-trigger policy. Keep configuration errors and action failures visible through `ILogger`; do not classify every exception as invalid user input. Record state/trigger/session identity without dumping sensitive payloads. See [logging](../logging/SKILL.md).
- Decide explicitly whether sessions restart or resume. A machine created with an initial enum holds its state in memory; saving domain records does not persist the machine or draft automatically. For required resume, use the external-state constructor `new StateMachine<State, Trigger>(() => model.State, value => model.State = value)` and persist the associated draft as well. Rebuild configuration on restore; serialization of delegates/machine internals is unnecessary.
- Stateless does not make transitions, database commits, and remote effects atomic. Define recovery for an action that fails after a commit, and protect completion from duplicate delivery where it causes durable effects. Do not assume an exception restores state or undoes earlier work.

## Verify behavior

Use [unit-testing](../unit-testing/SKILL.md) for application tests. Cover the workflow's actual decisions: startup enters the intended mode once; invalid input preserves state/data; valid input advances with its typed payload; Back skips completion; Finish completes once; internal transitions do not rerun prompts; reentry does; inherited commands have the intended reach. Await the outer fire when checking chained transitions. Exercise action failure and concurrent delivery when those paths exist, and a restore-with-draft scenario only when resume is supported. Prefer fake domain/I/O services and observable effects over tests that merely enumerate configuration calls.
