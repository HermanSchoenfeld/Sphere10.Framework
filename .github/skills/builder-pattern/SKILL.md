---
name: builder-pattern
description: Create fluent FooBuilder classes with chainable With*/Add*/Configure* methods and a terminal Build(). Trigger when designing a new complex configuration API (wizards, serializers, protocols, jobs, app blocks).
---

# Builder Facade Skill

The codebase favors fluent builders for configuring complex objects. Follow the existing builders exactly:
`SerializerBuilder` (`src/Sphere10.Framework/Serialization/Builder/SerializerBuilder.cs`),
`ProtocolBuilder` (`src/Sphere10.Framework/Protocol/Builder/ProtocolBuilder.cs`),
`WinFormsApplicationBlockBuilder` (`src/Sphere10.Framework.Windows.Forms/Application/Builder/WinFormsApplicationBlockBuilder.cs`),
`WizardBuilder<T>` (`src/Sphere10.Framework.Windows.Forms/Wizard/WizardBuilder.cs`),
`JobBuilder` (`src/Sphere10.Framework/Scheduler/JobBuilder.cs`).

## Recipe
1. Create `FooBuilder` (generic `FooBuilder<T>` when configuring a model type).
2. Accumulate state in private fields via chainable methods that `return this`:
   - `WithX(...)` for scalar configuration (e.g. `WithTitle`, `WithModel`)
   - `AddX(...)` for collection items (e.g. `AddScreen`, `AddMenu(mb => ...)`)
   - `ConfigureX(...)` / `OnX(...)` for callbacks (e.g. `OnFinished`, `OnCancelled`)
3. Validate each argument immediately with `Guard.ArgumentNotNull` etc.
4. Terminal `.Build()` validates the accumulated state with `Guard.Ensure(...)` and constructs the product:
   ```csharp
   public ActionWizard<T> Build() {
	   Guard.Ensure(!string.IsNullOrEmpty(_title), "Wizard title is required");
	   Guard.Ensure(_screens.Count > 0, "At least one screen is required");
	   return new ActionWizard<T>(_title, _model, _screens, _finishFunc, _cancelFunc);
   }
   ```
5. For class hierarchies, use a non-generic abstract base plus a generic derived builder that re-exposes methods with `new` and covariant casts (see `SerializerBuilder` / `SerializerBuilder<TItem>`).
6. Provide a static entry point where useful (`SerializerBuilder.For<T>()`, `JobBuilder.For(action)`).

## Anti-patterns
- No settable public properties on builders; mutation only through fluent methods.
- Don't construct the product before `.Build()`.
- Don't swallow missing configuration — `Guard.Ensure` in `Build()`.
