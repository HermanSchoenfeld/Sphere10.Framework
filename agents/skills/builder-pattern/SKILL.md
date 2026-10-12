---
name: builder-pattern
description: Create fluent FooBuilder classes with chainable With*/Add*/Configure* methods and a terminal Build(). Trigger when designing a new complex configuration API (wizards, serializers, protocols, jobs, app blocks).
---

# Builder Facade Skill

The codebase favors fluent builders for configuring complex objects. Follow the existing builders exactly:
`SerializerBuilder` (`src/Sphere10.Framework/Serialization/Builder/SerializerBuilder.cs`),
`ProtocolBuilder` (`src/Sphere10.Framework/Protocol/Builder/ProtocolBuilder.cs`),
`WinFormsApplicationBlockBuilder` (`src/Sphere10.Framework.Windows.Forms/Application/Builder/WinFormsApplicationBlockBuilder.cs`),
`WinFormsWizardBuilder<T>` (`src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs`),
`JobBuilder` (`src/Sphere10.Framework/Scheduler/JobBuilder.cs`).

## Recipe
1. Create `FooBuilder` (generic `FooBuilder<T>` when configuring a model type).
2. Accumulate state in private fields via chainable methods that `return this`:
   - `WithX(...)` for scalar configuration (e.g. `WithTitle`, `WithModel`)
   - `AddX(...)` for existing collection items or direct construction from arguments (e.g. `AddMenu(menu)`, `AddActionItem(text, action)`). These methods never create or invoke a child builder.
   - `ConfigureX(Action<XBuilder>)` for child-builder composition (e.g. `ConfigureMenu(mb => ...)`, `ConfigureItem(item => ...)`). Follow `ProtocolBuilder`: create the child builder, invoke the callback, call `Build()` and register its result through `AddX(...)`.
   - `OnX(...)` for lifecycle callbacks (e.g. `OnFinished`, `OnCancelled`). Action handlers supplied to `AddActionItem` are runtime callbacks, not builder callbacks.
3. Format each invocation as one continuous indented block: no blank lines inside the chain, one configuration call per line, nested child calls one tab deeper than their owner, and multiline closing parentheses aligned with the owning call. Validate each argument immediately with `Guard.ArgumentNotNull` etc.
4. Terminal `.Build()` validates the accumulated state with `Guard.Ensure(...)` and constructs the product:
   ```csharp
   public WinFormsActionWizard<T> Build() {
	   Guard.Ensure(!string.IsNullOrEmpty(_title), "Wizard title is required");
	   Guard.Ensure(_screens.Count > 0, "At least one screen is required");
	   return new WinFormsActionWizard<T>(_title, _model, _screens, _finishFunc, _cancelFunc);
   }
   ```
5. For class hierarchies, use a non-generic abstract base plus a generic derived builder that re-exposes methods with `new` and covariant casts (see `SerializerBuilder` / `SerializerBuilder<TItem>`).
6. Provide a static entry point where useful (`SerializerBuilder.For<T>()`, `JobBuilder.For(action)`).

## Anti-patterns
- No settable public properties on builders; mutation only through fluent methods.
- Do not expose `AddX(Action<XBuilder>)` overloads or route `AddX` convenience methods through `ConfigureX`; reserve child builders for `ConfigureX`.
- Don't construct the product before `.Build()`.
- Don't swallow missing configuration — `Guard.Ensure` in `Build()`.
