---
name: builder-pattern
description: Create fluent FooBuilder classes with chainable With*/Add*/Configure* methods and a terminal Build(). Trigger when designing a new complex configuration API (wizards, serializers, protocols, jobs, app blocks).
---

# Builder Facade Skill

Read [fluent builder guidance](../../../docs/guidelines/patterns/fluent-builders.md) before changing configuration composition.

The codebase favors fluent builders for complex configuration. Choose the existing family for the product being built:

- [SerializerBuilder](../../../src/Sphere10.Framework/Serialization/Builder/SerializerBuilder.cs) for serializers.
- [ProtocolBuilder](../../../src/Sphere10.Framework/Protocol/Builder/ProtocolBuilder.cs) for protocol composition.
- [ApplicationPluginBuilderBase](../../../src/Sphere10.Framework.Application/UI/ApplicationPluginBuilderBase.cs) and the block/menu/item builder bases for shared UI definitions.
- [WinFormsApplicationBlockBuilder](../../../src/Sphere10.Framework.Windows.Forms/Application/Builder/WinFormsApplicationBlockBuilder.cs) for native block definitions.
- [WizardBuilderBase](../../../src/Sphere10.Framework.Application/UI/Wizard/WizardBuilderBase.cs) and [WinFormsWizardBuilder<T>](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs) for shared configuration and native wizard creation.
- [JobBuilder](../../../src/Sphere10.Framework/Scheduler/JobBuilder.cs) for scheduled jobs.

Reuse shared storage, validation and product factories before introducing parallel platform implementations. Existing builders differ in lifetime and construction strategy; copy the relevant contract, not every incidental detail of an unrelated builder.

## Recipe

1. Create `FooBuilder` (generic `FooBuilder<T>` when configuring a model type).
2. Accumulate state in private fields via chainable methods that `return this`:
   - `WithX(...)` for scalar configuration (e.g. `WithTitle`, `WithModel`)
   - `AddX(...)` for existing collection items or direct construction from arguments (e.g. `AddMenu(menu)`, `AddActionItem(text, action)`). These methods never create or invoke a child builder.
   - `ConfigureX(Action<XBuilder>)` for child-builder composition (e.g. `ConfigureMenu(mb => ...)`, `ConfigureItem(item => ...)`). Follow `ProtocolBuilder`: create the child builder, invoke the callback, call `Build()` and register its result through `AddX(...)`.
   - `OnX(...)` for lifecycle callbacks (e.g. `OnFinished`, `OnCancelled`). Action handlers supplied to `AddActionItem` are runtime callbacks, not builder callbacks.
3. Format each invocation as one continuous indented block: no blank lines inside the chain, one configuration call per line, nested child calls one tab deeper than their owner, and multiline closing parentheses aligned with the owning call. Validate required arguments immediately with the appropriate `Guard` helper; preserve optional values allowed by the contract.
4. Terminal `.Build()` validates the accumulated state before publishing the configured product. Shared UI builders expose product factories such as `CreateWizard` so platforms supply presentation types without repeating configuration validation.
5. Follow the family's fluent typing. `SerializerBuilder<TItem>` re-exposes methods from its non-generic base; shared UI builders use a neutral storage base plus a `TSelf` fluent layer. Do not require a non-generic base or repeated `new` methods when a self-typed existing base already preserves the chain.
6. Provide a static entry point where useful (`SerializerBuilder.For<T>()`, `JobBuilder.For(action)`).

The current shared wizard base uses:

```csharp
public virtual TWizard Build() {
	Guard.Ensure(!string.IsNullOrWhiteSpace(Title), "Wizard title is required.");
	Guard.Ensure(_steps.Count > 0, "At least one wizard step is required.");
	return CreateWizard(_steps.ToArray());
}
```

## Anti-patterns

- New configuration should use fluent methods rather than public setters; keep required existing API contracts intact.
- Do not expose `AddX(Action<XBuilder>)` overloads or route `AddX` convenience methods through `ConfigureX`; reserve child builders for `ConfigureX`.
- Do not publish incomplete products or execute their runtime lifecycle while configuring them. `ProtocolBuilder`, `SerializerBuilder` and `JobBuilder` can allocate or retain products before `Build`; that is an existing implementation strategy, not a reason to redesign them.
- Do not assume `Build()` is a fresh deep snapshot: some builders retain the same product. Preserve the family's reuse and ownership semantics, including array membership capture and parent links.
- Do not swallow missing configuration. Validate arguments before mutation and cross-field requirements at `Build`; keep any required domain-specific validation result or exception contract.

## Plugin lifecycle registrations

Call `WithName` before `RegisterInitializer<T>` or `RegisterFinalizer<T>`, including their factory overloads. Unnamed registration throws immediately before changing builder state. The builder defers service registration until load and uses the built plugin's actual `Name` as its key; loaded plugin names must be unique. Task factories resolve dependencies from the executing application's provider. Do not replace these helpers with `typeof(IApplication)` keys or introduce a separate plugin ID.

See [the ownership guide](../../../docs/guidelines/patterns/application-ownership.md) and the [WinForms component plugin](../../../utils/Sphere10.Framework.Utils.WinFormsTester/WinFormsComponentDemoPlugin.cs) for the active registration path. Ordinary dependencies still use `ConfigureServices`; unkeyed initializers and finalizers belong to the existing process lifecycle.
