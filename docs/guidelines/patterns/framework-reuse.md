# Framework Reuse and Adoption

This applies to all development in this repository. Sphere10 provides reusable
code, patterns and architecture, and its own development must use those foundations.

Use existing Sphere10 abstractions and utilities before introducing another
implementation. Trace the real consumers and registration path, then extend
the layer that owns the behavior. Apply the [coding guide](../code-styling.md)
and the relevant [engineering practices](../patterns-and-practices.md).

- Before implementing anything, find and understand the existing code,
  abstractions, utilities, patterns and architecture that address the problem.
  Reuse them rather than creating parallel implementations or local substitutes.
- Extend or compose the existing framework where it falls short. Add a new
  abstraction only when the existing architecture cannot reasonably support the
  requirement; explain that gap and keep the addition consistent with the framework.
- Extend the existing class in its owning source file. Do not introduce partial
  extension files merely to separate a new feature's methods. Retain the existing
  native DLL declaration partition and designer-generated visual partials.
- When adding or improving a capability, adopt it in the relevant repository
  consumers in the same change. Supporting code alone is not a completed
  implementation.
- Trace the real startup, registration and user-facing execution paths. Update
  existing applications, demos and examples to consume the new APIs.
- Replace the superseded manual configuration or duplicated logic in those
  consumers. Do not leave the old path active alongside an unused replacement.
- Keep compatibility code only where it has an identified consumer or purpose,
  and clearly distinguish it from the recommended, actively demonstrated path.
- Verify the integration through the actual consumer's configuration and
  behavior, in addition to testing the supporting API in isolation.
- Update documentation to match the code that runs. Before claiming completion,
  audit the whole change for new capabilities that were added but never adopted.

For concrete utility choices, read [Framework Utilities](framework-utilities.md).
For design responsibilities, select the applicable topics from the
[Patterns and Practices index](../patterns-and-practices.md).

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
