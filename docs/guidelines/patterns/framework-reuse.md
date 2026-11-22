# Framework Reuse and Adoption

Sphere10 provides reusable components, widgets, patterns and architecture; its
own libraries and products use those foundations. Before implementing a change,
read the actual owner and its real consumer path. A similarly named type or an
API listing is not enough to establish equivalent behavior.

Inspect existing controls, screens, utilities, base classes and concrete
components before proposing another implementation. Trace configuration,
execution, publication, failure and cleanup. Extend the existing owner for a
demonstrated gap, adopting the capability throughout its relevant consumers in
the same change. Follow the [coding guide](../code-styling.md) and select the
applicable [engineering practices](../patterns-and-practices.md).

## Dogfooding and componentization

Prefer the simplest correct composition of existing owners. Identify the public
operation and trace down to the objects that own its mechanism, state and
lifetime. Each wrapper, coordinator, flag or abstraction must have a concrete
requirement that those owners cannot already express. Derive state from its
actual owner where possible. Judge simplicity over the complete execution path,
including callers, cleanup and error handling; shorter source text alone does
not show simpler behavior.

Keep the user's operation on its natural public owner. For example, source
configuration and refresh belong on `VisualGrid`, while its binding controller
is internal. Introducing a public interface solely to conceal that controller
would retain the same extra concept. Do not hide independently useful components
with real consumers just to reduce the visible type count.

Dogfooding includes the Framework implementation itself. Use the existing
disposal bases and scopes, event notification family, conversion helpers,
controls and execution primitives. When the owner lacks a required capability,
extend it there and migrate both the old and new paths. `Throttle.ExecuteAsync`
owns asynchronous coalescing; existing grid and screen owners retain UI dispatch,
activity, pending presentation and source lifetime. A debounce that waits for a
quiet stream is not a substitute for immediate continuous updates.

Preserve behavioral contracts while adopting a component: cancellation,
completion, reentrancy, event ordering, thread affinity, ownership and failure
policy are part of reuse. Do not add a second lifecycle guard or publication
version when the existing owner already enforces the same invariant. Separate
state is justified only by a distinct lifetime or boundary, such as configured
selection identity versus the identity active after binding.

Keep the change focused on the requirement, but complete it across every
relevant owner and consumer. Remove superseded factories, aliases, hand-written
mechanisms and duplicate registrations. Update real startup paths, applications,
demos, test doubles and documentation. Supporting code that no consumer uses is
not a completed adoption. Keep compatibility code for an identified contract;
do not preserve competing paths for hypothetical consumers.

Componentization gives independently reusable behavior one owner. Before
extracting, determine whether another existing component already owns it. A
substantial class can remain one owner organized into
[aspect-oriented partials](partial-classes.md); moving methods between files does
not require a service, interface or wrapper. When importing code, adapt it to
existing contracts and merge useful behavior, rather than keeping parallel
implementations because a full migration touches more files.

Keep the mechanism at its actual reusable boundary. Native resource operations,
query engines, parsers and general controls can belong in Framework; product
sampling, projections, confirmations and policy stay in the product. Several
screens using a capability is not by itself evidence of Framework ownership.
Apply the boundaries below before moving it.

Review the original and new consumer paths together, including resource and
failure behavior, and verify the actual integration under the active verification
plan. Update the documentation to describe the path that runs. New dependencies,
unrelated redesigns and changes to established policy require their existing
authorization; reuse does not grant it.

## Application and Framework boundaries

Application-specific business logic, presentation logic and all other
application-specific code belong in the app projects. Framework is for generic,
common, reusable capabilities. Inspect both the type's contract and the decisions
it makes; a generic name or several consumers inside one app do not make it a
Framework abstraction.

Keep application screens, product models, command composition, confirmations,
workflow coordination, reporting, defaults and product-specific policies in the
application. Keep general Windows APIs, owned native handles, platform operations,
generic collections, logging, reusable controls and configurable dialogs in the
appropriate Framework library. A generic WinForms dialog is reusable UI machinery;
the app decides when to show it and what business content it presents.

Ask whether another independent application could use the capability without
depending on this product's models, names, workflows or policy. Parameters can
configure a reusable mechanism; they do not justify exporting product decisions
into Framework. General libraries must not reference application assemblies or
require their contracts through an inverted callback dependency.

For example, File Server Migrator's `IActionObserver`, progress dialog, logger
adapter, operation queue and captured screen requests belong in its plugin.
Reusable Windows operations consume the existing Framework `ILogger` or plain
native progress/data contracts. Migration options, results, copiers, traversal
and ordered-host SID resolution stay in the plugin. Native enumeration, raw
SID lookup, descriptor translation and token ownership remain in Windows;
they do not depend on the app's reporting interface.

Before delivery, audit every created or changed file, including abstractions,
models, helper classes and tests. State its owner and reusable purpose, check its
real consumers, and move misplaced code rather than merely renaming it. Preserve
history/provenance and update references, solution entries, tests and docs together.

### Windows abstraction layers

Use three distinct responsibilities:

1. `WinAPI` owns native declarations, native layouts and native handle primitives.
2. Framework.Windows owns domain abstractions for operating-system resources and
   operations: services, registry keys/values, processes, drives/volumes, drivers,
   files and windows. They encapsulate native calls, ownership, native failures
   and reusable operations behind typed contracts, like File/Stream encapsulate
   filesystem IO. Extend existing APIs and folders such as Services, Security
   and Shell; introduce the appropriate domain folder when it does not exist.
3. Applications own diagnostic collectors, sampling intervals/deltas/history,
   metric selection, view models, labels/units, display attributes, action plans,
   confirmations, exports and workflow policy. They compose the Windows domain
   abstractions; moving a collector into the app does not authorize putting
   its raw interop calls into a screen or view model.

A performance collector is an application sampling concern. Framework may
expose raw system/process counter queries through its Windows abstractions;
the app turns successive queries into rates, statistics and charts. Native
models describe Windows contracts, not rows for a diagnostic screen. Keep
`DisplayName`, `Description`, editor/converter attributes and serialization
navigation policy on the app's projections, not on Windows API data types.
Plans for stopping dependencies, restarting services or confirming driver
changes are app orchestration; the reusable layer exposes individual native
operations and configuration/status queries. A class name, several screens,
configurable callbacks or an extra interface alone do not establish reuse.

Audit contract dependencies in both directions: Windows abstractions cannot
depend on app snapshot/plan/report types, and application presentation cannot
bypass its middle abstraction layer by calling Win32 or WMI directly. Tests of
app collectors/projections move with their implementations; Framework tests
exercise the reusable APIs without referencing the application.
