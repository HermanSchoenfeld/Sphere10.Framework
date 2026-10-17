# Designer-editable WinForms UI

Read this practice whenever creating or changing a WinForms form, user control,
hosted screen or dialog. Every visual type must be editable in Visual Studio's
WinForms designer, including its real layout and event wiring.

## Files and construction

Use a matching `Foo.cs` / `Foo.Designer.cs` partial class pair and a public
parameterless constructor that calls `InitializeComponent()`. The designer file
declares control/component fields and initializes their real properties,
collections, bounds/docking/layout and named event handlers using the normal
designer serialization style. Use `SuspendLayout`/`ResumeLayout` around layout,
including container initialization as needed. Preserve source history when moving
existing UI code into these files.

An empty designer method does not make a programmatically constructed UI
designer-editable. Do not create the actual static layout in a constructor,
helper method, lambda or runtime loop. Runtime code owns behavior, bindings,
data-dependent rows/menu items and state transitions; designer code owns the
static visual structure. Dynamic content must remain genuinely data-dependent,
rather than being used to avoid serializing a fixed layout.

Dialogs also need their own concrete visual class, parameterless constructor
and designer file. A static show helper can create/configure that class and keep
it in a `using` scope across the async modal operation. Do not construct an
anonymous `Form` and its entire layout inside the helper.

## Concrete nongeneric visual bases

The WinForms designer must instantiate the base surface. Do not put an abstract
or generic visual class directly in a designer-edited inheritance chain. The
existing [WinFormsApplicationScreen](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/WinFormsApplicationScreen.cs),
[ApplicationControl](../../../src/Sphere10.Framework.Windows.Forms/Application/Controls/ApplicationControl.cs)
and [BlockMainForm](../../../src/Sphere10.Framework.Windows.Forms/Application/Forms/BlockMainForm.cs)
show the framework's concrete native surface and design-mode guards.

Keep reusable domain/services/controller foundations abstract where appropriate;
compose them behind a concrete nongeneric visual surface. Typed grid behavior
can live in a nonvisual controller, leaving the designer to instantiate an
ordinary control. Use the framework's
[CrudGridSnapshotController<T>](../../../src/Sphere10.Framework.Windows.Forms/Crud/CrudGridSnapshotController.cs)
behind a concrete designer-owned `CrudGrid`/`WinFormsApplicationScreen` surface.
This visual-design requirement is an explicit exception
to inheritance-only abstract-base naming; do not apply it to nonvisual classes.

Protected fields permit editing many inherited controls, but inherited
`ToolStrip`, `TableLayoutPanel`, `FlowLayoutPanel` and `DataGridView` surfaces
remain read-only in the designer. See Microsoft's
[visual inheritance constraints](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/walkthrough-demonstrating-visual-inheritance).
Declare each screen's editable command toolbar in that screen's designer;
compose shared commands into it at runtime while preserving the static command
layout on the concrete screen's design surface. The existing application host
composes that screen-owned toolbar with block/application commands.
Edit reusable common controls through their own
design surface rather than relying on protected modifiers to bypass these limits.

## Screen actions and tools

Put a screen's primary workflow action beside its inputs in the screen body:
Copy, Generate Report, Search, Send and Start Monitoring are ordinary buttons.
Keep those buttons with the screen when the host lifts or composes its toolbar.
Use the toolbar for tools such as Refresh, Export, Review, inspection and selected
row utilities; applicable row tools also belong in the row context menu. Bind
each command to one behavior path, retaining its validation, busy state and
permissions when its presentation moves.

When modernizing an imported application, retain its original designer layout,
grouping, labels, images, default choices, tab order and primary action placement
except where the user requests a change. Replacing obsolete framework controls
or native services is an implementation update, not permission to invent a new
screen. Compare the actual rendered screen against the original designer and
assets, including normal-size and resized views.

## No runtime work on a design surface

Use `Tools.Runtime.IsDesignMode` before DI/framework resolution, provider reads,
monitoring, file/registry IO or timer-driven collection. Initialize controls
first, then return from design-mode construction before runtime binding/guards
that need services. Do not start a watcher or perform native inventory merely
because the designer constructed or showed a control. Preserve real runtime
activation, retention and disposal through the application host.

Verify parameterless construction without an active application/service provider,
real designer-file content, actual menu activation, UI geometry and disposal.
Opening a file in an editor or successfully compiling it does not by itself
prove Visual Studio rendered its design surface.
