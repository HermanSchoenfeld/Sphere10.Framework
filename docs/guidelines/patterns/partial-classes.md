# Aspect-oriented partial classes

Use partial files when a substantial class has distinct responsibilities that
still share one identity, contract and lifetime. An aspect here is a coherent
responsibility such as binding, selection or preferences; it does not mean
interceptors, code weaving or another runtime abstraction. Keep a small, cohesive
class in one file. A genuinely independent behavior belongs in its existing
component, not in a partial that hides duplicate implementation.

## Decide the owner before the files

Start with [Framework reuse and adoption](framework-reuse.md). Read the public
operations, actual widgets and components, callers, state and cleanup paths.
Distinguish source organization from a contract change: splitting a type keeps
its members and visibility intact, while moving behavior to its correct owner
requires the complete consumer migration. A large file is not a reason to add
an interface, service, wrapper or public helper.

Keep a control's public operations on the control. Its internal mechanisms may
remain separate internal types when they have a concrete responsibility. Do not
export those mechanisms merely so consumers can reach an operation. Conversely,
do not force an existing independently useful component into the control just
to reduce the number of files.

## Partition the whole class

Keep `Type.cs` as the readable entry point: the type declaration, public summary,
construction and shared composition. Name other files `Type.Aspect.cs` using the
responsibility's ordinary name. Place an aspect's fields, properties, operations,
event notifications and private implementation types together. Follow ordinary
[member ordering](../code-styling.md#member-ordering) within each part.

Map every existing member to a responsibility before moving it. Apply the
partition coherently across the whole type; leaving most behavior in a catch-all
file while extracting only new methods does not establish that structure. Avoid
numbered fragments, fixed line-count chunks and `Extensions` or `Helpers` files
that collect unrelated methods. The boundaries should help a reader answer
which part to open for a behavior.

Each partial declaration has a short ordinary comment explaining its
responsibility and its important relationship to other parts. Keep the public
XML summary on the main declaration. For example, the grid's notification part
explains that `Throttle` coalesces requests while the UI owner controls dispatch,
admission and subscription lifetime. This is useful navigation and ownership
information, not a restatement of the filename.

All parts are one class. Do not duplicate state, introduce another disposal
guard, expose private members, or add forwarding methods just to communicate
between files. Preserve complete event families within their aspect, using the
[existing event pattern](type-hierarchies.md#event-notifications-and-template-hooks).
Keep [resource ownership](resource-scopes.md), true asynchronous completion and
thread affinity with their existing owners.

## Preserve semantics and source identity

Imports apply to each file separately. Retain the namespace, generic constraints,
base/interface declarations, attributes, conditional compilation, nullable
context, region structure, comments, license and encoding conventions. Audit
field and static initializers: declaration order across partial files is not an
ordering contract. Keep dependent initializers together or preserve their order
through the existing constructor, without invoking setters or hooks earlier.

Preserve designer-generated and native DLL partitions. Moving a WinForms file
also requires checking `Compile`, `EmbeddedResource`, `DependentUpon`, logical
resource names and designer resource lookup; a successful text move alone does
not establish a correct independent build.

Keep a cohesive family together. VisualGrid uses a flat domain folder; that is
not a requirement to flatten every domain. Choose meaningful ownership boundaries
instead of reproducing `Controls`/`Forms` folders mechanically. For tracked moves,
use `git mv` and retain unrelated working edits. Inspect filename collisions
before moving: merge the actual declarations and references when the destination
already owns the concept, rather than overwriting it or leaving a duplicate.

## Worked example: VisualGrid

The [flat VisualGrid domain](../../../src/Sphere10.Framework.Windows.Forms/VisualGrid/README.md#source-organization)
contains one control with these aspect files:

```text
VisualGrid.cs
VisualGrid.Binding.cs
VisualGrid.Cells.cs
VisualGrid.Editing.cs
VisualGrid.Interaction.cs
VisualGrid.Layout.cs
VisualGrid.Paging.cs
VisualGrid.RowDetails.cs
VisualGrid.Notifications.cs
VisualGrid.Preferences.cs
VisualGrid.Designer.cs
```

The source README is the maintained file-to-responsibility map. Read the
declaration comments in the parts as well as their methods. Binding owns the
read/accept/publish pipeline and its private query records; Cells owns creation
and prepared values; Interaction owns selection and input. Notifications means
source-change presentation, not a dumping ground for all public events.
Preferences captures values, while the containing screen owns persistence.
Designer keeps native construction and disposal rather than copying those into
each aspect. RowDetails coordinates owned detail surfaces without acquiring the
source data.

The associated ownership changes are distinct from the file split. Consumers
configure and refresh `VisualGrid` itself; its data-source controller is internal.
General asynchronous coalescing is implemented by `Throttle`, while the grid's
notification part retains UI admission and borrowed-source lifetime. Detail
classes use the existing disposal base and preserve their awaited nested cleanup.
These choices reduce duplicate behavior without inventing another public layer.

## Review the result

Compare a member inventory before and after a mechanical split, including
attributes, signatures, nested types and executable tokens. Then review imports,
initialization, resource paths, event order, scope lifetime and reentrant calls;
token equality alone does not prove those boundaries. Search all actual
consumers for a moved or changed contract and update their examples and docs.
Build the affected projects and run behavior-focused checks under the active
verification plan. When execution is explicitly paused, report static findings
and the checks still needed; a source review is not a passing build or test.
