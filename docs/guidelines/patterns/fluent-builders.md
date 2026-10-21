# Fluent Builders

Read the [fluent formatting guide](../code-styling.md#fluent-configuration-hierarchy). Reuse the builder family for the product being configured; see [framework reuse](framework-reuse.md).

## Builder Facade Pattern

The codebase favors **fluent builder classes** for configuring complex objects. Builders accumulate state via chainable `With*`/`Add*`/`Configure*` methods and finalize with `.Build()`. Key examples:

- **[SerializerBuilder](../../../src/Sphere10.Framework/Serialization/Builder/SerializerBuilder.cs)** builds `IItemSerializer<T>` through `For<T>()`, member `Serialize(...)` calls and `Build()`. The parameterless `For<T>()` requires `T : new()`; supply `For<T>(Func<T>)` for another activation strategy. Use `FactoryAssemble<T>(factory)` for factory-based assembly; consult the [serialization practice](serialization.md#automatic-assembly-and-reflection) before selecting automatic configuration paths.
- **[ProtocolBuilder](../../../src/Sphere10.Framework/Protocol/Builder/ProtocolBuilder.cs)** builds communication `Protocol` objects using `ConfigureHandshake`, `ConfigureRequest`, `ConfigureCommand`, direct handler registrations and `SetMode`.
- **[WinFormsApplicationBlockBuilder](../../../src/Sphere10.Framework.Windows.Forms/Application/Builder/WinFormsApplicationBlockBuilder.cs)** builds native blocks through `WithName(string)`, optional `WithDefaultScreen<TScreen>(...)`, `ConfigureMenu(...)` and `Build()`. The screen type must derive from `WinFormsApplicationScreen`.
- **[WinFormsWizardBuilder](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs)** configures a native wizard through `WithTitle(string)`, `WithModel(T)`, `AddScreen(WinFormsWizardScreen<T>)` and `OnFinished(...)`. Its shared [WizardBuilderBase](../../../src/Sphere10.Framework.Application/UI/Wizard/WizardBuilderBase.cs) also exposes `AddStep`; native `AddScreen` delegates to it. `Build()` requires a nonblank title, at least one step and a finish callback, then creates [WinFormsActionWizard](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsActionWizard.cs).

When creating new complex configuration APIs, follow this pattern: create a `FooBuilder` class with chainable methods and a terminal `.Build()`. Format each builder invocation as a continuous indented block with no blank lines: one configuration call per line, child calls one tab deeper than their owning call, and multiline closing parentheses aligned with that call. Follow `ProtocolBuilder` for composition: `ConfigureX(Action<XBuilder>)` creates and builds a child, then registers its result through `AddX`. `AddX` registers an existing definition or constructs one directly from arguments; it must never create or invoke a child builder.

For example, this native block uses the existing direct action-item overload;
the action is a runtime callback rather than a child-builder callback:

```csharp
using Sphere10.Framework.Windows.Forms;

var block = new WinFormsApplicationBlockBuilder()
	.WithName("Operations")
	.ConfigureMenu(menu => menu
		.WithText("Actions")
		.AddActionItem("Refresh", () => { })
	)
	.Build();
```

The concrete composition paths are
[ApplicationMenuBuilderBase.ConfigureItem](../../../src/Sphere10.Framework.Application/UI/ApplicationMenuBuilderBase.cs)
and [ApplicationPluginBuilderBase.ConfigureBlock](../../../src/Sphere10.Framework.Application/UI/ApplicationPluginBuilderBase.cs).
`ProtocolBuilder.ConfigureRequest` and `ConfigureCommand` follow the same
build-then-add sequence. The existing handshake setter is a specialized path,
rather than an `AddHandshake` API to invent.

## WinForms UI Pattern

- [WinFormsApplicationBlock](../../../src/Sphere10.Framework.Windows.Forms/Application/WinFormsApplicationBlock.cs) and its builder register navigation. `WithDefaultScreen<TScreen>()` supplies the default; `ConfigureMenu(...)` uses the native menu builder's `AddScreenItem<TScreen>(string text, ...)` for labeled screen commands.
- [WinFormsApplicationScreen](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/WinFormsApplicationScreen.cs) is the existing concrete, inheritable native control type required by registration. Reuse that public family when defining screens; it is not a template for introducing new concrete classes whose only purpose is inheritance. Follow [type hierarchies](type-hierarchies.md) for new foundations.
- [CrudGrid](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGrid.cs) binds grid screens through `SetDataSource<TEntity>(IDataSource<TEntity>, ...)` and the existing [IDataSource](../../../src/Sphere10.Framework/DataSource/IDataSource.cs) contract.

The [WinForms project guide](../../../src/Sphere10.Framework.Windows.Forms/README.md) cover native screens, grids and wizards. Shared application builders and their platform specializations are described in the [application guide](../../../src/Sphere10.Framework.Application/README.md#shared-ui-framework) and [Blazor workspace guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#register-a-workspace).

A builder's existing construction and reuse semantics remain authoritative: `Build()` does not universally promise a fresh deep snapshot. Validate required arguments before mutation and complete configuration before publishing the product. Plugin lifecycle registration and names belong to [application ownership](application-ownership.md); the [serialization](serialization.md) and [scheduling](scheduling.md) topics cover their domain-specific builders.

`ProtocolBuilder`, `SerializerBuilder` and
[JobBuilder](../../../src/Sphere10.Framework/Scheduler/JobBuilder.cs)
retain their configured product. Native block and
[menu builders](../../../src/Sphere10.Framework.Windows.Forms/Application/Builder/WinFormsApplicationMenuBuilder.cs)
also return the same product after their first build, and later fluent calls can
update it. In contrast, the
[Blazor block](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/Application/BlazorApplicationBlockBuilder.cs)
and [menu builders](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/Application/BlazorApplicationMenuBuilder.cs)
create independent ownership snapshots. Shared builder bases do not themselves
promise either strategy; their platform product factory owns that choice.
The [shared builder tests](../../../tests/Sphere10.Framework.Application.Tests/UI/SharedFluentBuilderTests.cs)
and [Blazor graph tests](../../../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/ApplicationGraphOwnershipTests.cs)
exercise repeated builds. The current
[native wizard demo](../../../utils/Sphere10.Framework.Utils.WinFormsTester/WinFormsDemoPlugin.cs)
supplies actual screen instances and awaits `Start(owner)` after construction.
