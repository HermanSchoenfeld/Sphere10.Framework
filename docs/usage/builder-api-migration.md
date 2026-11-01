# Builder API migration

Framework builders use `AddX(existingX)` for existing definitions and
`AddX(Action<XBuilder> configure)` to configure and add one child. Child callbacks
run immediately and return the parent. Use `WithX` for scalar/replacement values
and explicit runtime callback names for handlers. The callback's argument type
and execution timing remain part of its contract.

## Renamed public APIs

| Previous builder API | Current API | Preserved behavior |
|---|---|---|
| `ConfigurePlugin` | `AddPlugin` | Build and add one plugin immediately. |
| `ConfigureBlock` | `AddBlock` | Build and add one block immediately. |
| `ConfigureMenu` | `AddMenu` | Build and add one menu immediately. |
| `ConfigureItem` | `AddItem` | Build and add one item immediately. |
| `ConfigureToolBarItem` | `AddToolBarItem` | Build and add one toolbar item immediately. |
| Builder `ConfigureServices` | `AddServices` | Accumulate callbacks for the existing plugin/framework service-registration phase. |
| Builder `RegisterInitializer` / `RegisterFinalizer` | `AddInitializer` / `AddFinalizer` | Validate the plugin name immediately; retain keyed task registration, factory resolution and lifecycle ordering. |
| `ConfigureApplication` | `OnApplicationCreated` | Compose deferred callbacks over the created application; no child builder is involved. |
| `UseModule` | `AddModule` | Append an existing or typed module registration. |
| Protocol `ConfigureHandshake` | `WithHandshake` | Immediately build and replace the handshake. |
| Protocol `ConfigureRequest` | `AddRequest` | Immediately build and add one request/response pair. |
| Protocol `ConfigureCommand` | `AddCommand` | Immediately build and add one command handler. |
| Protocol `AddCommand` delegate overloads | `OnCommand` | Register runtime command handlers without executing them during composition. |
| Protocol `AddRequestResponse` delegate overloads | `OnRequestResponse` | Register runtime request/response handlers. Existing-handler overloads retain `AddRequestResponse`. |
| Protocol `ConfigureSerialization` | `AddSerializers` | Immediately invoke the callback over the existing serializer-factory context. |
| ObjectSpace `Configure<T>()` | `GetDimension<T>()` | Look up a dimension already added; missing dimensions still fail. |

Runtime APIs such as a host's `RegisterBlock`, `SerializerFactory.Register` and
`Sphere10FrameworkBuilder.RegisterModules(IServiceCollection)` retain their
names. The latter applies already collected registrations to a service
collection; it is not a fluent registration method. Third-party host and options
configuration APIs are unchanged.

## Screens and navigation

Register a screen on its owning block with `AddScreen<TScreen>(screen => ...)`,
then refer to that registration from `AddShowScreenItem<TScreen>()` or an exact
registration reference. Menu and toolbar presentation no longer defines screen
activation policy. Menu type conveniences select the first matching registration
in their owning block; an exact reference selects a particular registration.
Defaults, programmatic type lookup and legacy startup restoration still require
an unambiguous registration.

The ordinary parent-returning overload does not require an unused reference.
Optional reference overloads place the configuration callback first and the
`out` reference second. Existing-object forms place the object first. Closing a
callback completes its scope; `.Done()` belongs only to a separately exposed
direct child-builder scope.

Protocol's optional child-reference overloads expose the actual `ICommandHandler`
or named `(IRequestHandler RequestHandler, IResponseHandler ResponseHandler)`
pair. Existing-handler overloads retain those same products. `AddModule<T>(out
T registration)` exposes the immediately constructed module; adding an existing
module can also return that identity. These references do not introduce a second
registry or cause runtime handlers and module lifecycles to execute during
configuration.

Blazor toolbar composition also retains both ordinary overloads and optional
references: `AddToolBarItem(configure, out registration)` captures the configured
command definition, and `AddToolBarItem(existing, out registration)` captures the
supplied definition. Building independent block graphs clones those commands;
the source reference is not retargeted to a later graph.

Repeated `Build()` retains each builder family's existing contract: native
builders may retain their product, while Blazor graph builders create independent
snapshots. Registration references identify the corresponding registration in
each graph, rather than changing a source reference to point at the last graph.

## Consumer inventory

The coordinated migration includes Framework source, tests, demonstrations,
recipes and package-consumer samples; System Expert and File Server Migrator;
AutoMouse; Dewatering Pipeline Designer; Enterprise's native and Blazor hosts;
the legacy sphere10.com desktop blocks; and Local Notion's module registration.
Lumina has no Framework reference and uses none of these APIs. The registered
historical AutoMouse QA checkout is not an independently active product.

Source builds and package builds are distinct checks. Consumers must use mutually
compatible candidate Framework packages when verifying the new source API in
package mode; an older package pin does not acquire these APIs from the source
checkout automatically.

This naming migration does not change Protocol envelope or handshake behavior,
or repair its previously documented factory wiring limitations. In particular,
the renamed factory callback keeps the existing supplied context and timing.
