---
name: state-machines
description: Build application workflows and session lifecycles with the Stateless C# library, using typed triggers, hierarchical states, guarded transitions, and async actions. Use for multi-step interaction or process orchestration; unrelated to stateless cryptography.
---

# State Machines Skill

Use `Stateless.StateMachine<TState, TTrigger>` for application workflows. Keep input routing, workflow transitions, and domain operations separate. Apply [code-style](../code-style/SKILL.md) to implementation and the existing application abstractions to dependencies.

## Library and API baseline

- The library/package is **Stateless** (`using Stateless;`). The APIs below were checked against source version **5.13.0**; this is an evidence baseline, not a required version or a claim about the latest release. Inspect the target project's package or project reference before choosing overloads or changing dependencies.
- Consult the [upstream documentation](https://github.com/dotnet-state-machine/stateless) and the source for the referenced version when adapting the configuration. Do not assume application extension methods ship with the library.
- `OnExitWhen`, `OnExitAsyncWhen`, `OnExitIf`, and `OnExitAsyncIf` can be application helpers. Stock `OnExitAsync(Transition => ...)` plus a trigger predicate expresses the same conditional exit. Some one-argument typed internal-action overloads likewise come from helpers; stock typed async actions accept the payload **and** transition.

## Shape the workflow

- Define separate state and trigger enums. States describe stages or modes; triggers describe events such as input received, next, back, finished, or restart. Store draft data in the owning session/model rather than adding a state for every value.
- Let a session/workflow object own one machine, registered typed triggers, and draft context. A coordinator locates sessions by stable entity/session ID; configure and activate a new session before normal dispatch. Keep I/O and domain work in named handlers or injected services so the configuration remains readable.
- Register `SetTriggerParameters<T>(Trigger.Input)` once and keep its `TriggerWithParameters<T>`. Use the matching typed `FireAsync` overload at dispatch. Use multiple typed parameters when an event needs both an input envelope and parsed arguments.
- Configure a bootstrap state with `OnActivateAsync(() => Machine.FireAsync(InitialTrigger))` and its allowed destination(s). Finish configuring and choosing the initial trigger before `ActivateAsync()`. Do not perform async startup in the constructor or treat activation as a per-input operation.
- Use `SubstateOf` to inherit shared navigation only where it belongs. Parent transitions remain available to descendants, so avoid placing privileged commands or completion effects on a parent shared with unrelated modes. Check membership with `IsInState` when descendants count.

## Choose the transition deliberately

| Intent | Configuration |
|---|---|
| Move to another stage | `Permit` / `PermitIf` |
| Handle input while keeping the current stage and its entry/exit actions untouched | `InternalTransition` / `InternalTransitionAsyncIf` |
| Reset the current stage and rerun exit/entry actions | `PermitReentry` / `PermitReentryIf` |
| Deliberately accept an event without work | `Ignore` |
| Show a prompt or prepare a stage | `OnEntryAsync` |
| Perform work only on a particular exit | `OnExitAsync` with a predicate on `Transition.Trigger` |

Guard alternatives for the same trigger must be mutually exclusive and side effect free. A useful input pattern pairs `IsValid(Input)` with `!IsValid(Input)`: the invalid branch reports the problem without leaving the stage, while the valid branch records the value and fires an internal next/finished trigger. Put mutations and I/O in actions, not guards; use `Guard.*` for handler arguments and invariants.

Completion and cancellation often leave the same state. Check the exit trigger before saving/submitting so Back does not accidentally complete the operation. An exit handler returning normally does **not** veto a transition; if completion can be declined, represent that outcome in a guard or explicit processing/result stages.

## Configuration example

This configuration fragment assumes `WorkflowState`/`WorkflowTrigger` enums, an owned `Draft`, and the named task-returning handlers. It uses stock overloads and can sit inside the workflow's configuration method.

```csharp
var Machine = new StateMachine<WorkflowState, WorkflowTrigger>(WorkflowState.Started, FiringMode.Queued);
var InputTrigger = Machine.SetTriggerParameters<string>(WorkflowTrigger.Input);

Machine.Configure(WorkflowState.Started)
	.OnActivateAsync(() => Machine.FireAsync(WorkflowTrigger.Begin))
	.Permit(WorkflowTrigger.Begin, WorkflowState.Collecting);

Machine.Configure(WorkflowState.Collecting)
	.SubstateOf(WorkflowState.Started)
	.OnEntryAsync(PromptAsync)
	.InternalTransitionAsyncIf(InputTrigger, Value => !IsValid(Value), (Value, Transition) => RejectAsync())
	.InternalTransitionAsyncIf(InputTrigger, IsValid, async (Value, Transition) => {
		Draft.Value = Value;
		await Machine.FireAsync(WorkflowTrigger.Next);
	})
	.Permit(WorkflowTrigger.Next, WorkflowState.Review);

Machine.Configure(WorkflowState.Review)
	.SubstateOf(WorkflowState.Started)
	.OnEntryAsync(PreviewAsync)
	.OnExitAsync(async Transition => {
		if (Transition.Trigger == WorkflowTrigger.Finish)
			await CompleteAsync();
	})
	.Permit(WorkflowTrigger.Back, WorkflowState.Collecting)
	.Permit(WorkflowTrigger.Finish, WorkflowState.Completed);
```

The owner retains `Machine` and `InputTrigger`, awaits `Machine.ActivateAsync()` during startup, and later awaits `Machine.FireAsync(InputTrigger, Input)`. Keep Begin/Next as internal events rather than accepting arbitrary trigger enum values from external input. `CompleteAsync` in this fragment represents successful completion or a propagated failure, not a boolean success decision.

## Async execution, failures, and state lifetime

- Register task-returning handlers with the `*Async` methods and await `FireAsync` end to end. An async lambda passed to a synchronous action can become `async void`; avoid blocking wrappers and unobserved tasks.
- Queued firing supports handlers that fire Next/Finished: a nested fire enqueues the event for the outer dispatch to drain. Do not assume the state changed immediately after the nested await. Queuing is **not** thread safety: serialize external dispatch per machine, including activation and session creation. An async gate or per-session queue belongs at the outer boundary; handlers must not reacquire that gate when firing an internal trigger.
- `RetainSynchronizationContext` is a host requirement, not a lock. Set it only when the execution host needs its context. Framework read/write scopes use thread-affine locks: finish synchronous database work and release the scope before awaiting network calls. See [disposable-scopes](../disposable-scopes/SKILL.md).
- Handle unsupported input at the dispatch boundary or through an intentional unhandled-trigger policy. Keep configuration errors and action failures visible through `ILogger`; do not classify every exception as invalid user input. Record state/trigger/session identity without dumping sensitive payloads. See [logging](../logging/SKILL.md).
- Decide explicitly whether sessions restart or resume. A machine created with an initial enum holds its state in memory; saving domain records does not persist the machine or draft automatically. For required resume, use the external-state constructor `new StateMachine<State, Trigger>(() => Model.State, Value => Model.State = Value)` and persist the associated draft as well. Rebuild configuration on restore; serialization of delegates/machine internals is unnecessary.
- Stateless does not make transitions, database commits, and remote effects atomic. Define recovery for an action that fails after a commit, and protect completion from duplicate delivery where it causes durable effects. Do not assume an exception restores state or undoes earlier work.

## Verify behavior

Use [unit-testing](../unit-testing/SKILL.md) for application tests. Cover the workflow's actual decisions: startup enters the intended mode once; invalid input preserves state/data; valid input advances with its typed payload; Back skips completion; Finish completes once; internal transitions do not rerun prompts; reentry does; inherited commands have the intended reach. Await the outer fire when checking chained transitions. Exercise action failure and concurrent delivery when those paths exist, and a restore-with-draft scenario only when resume is supported. Prefer fake domain/I/O services and observable effects over tests that merely enumerate configuration calls.
