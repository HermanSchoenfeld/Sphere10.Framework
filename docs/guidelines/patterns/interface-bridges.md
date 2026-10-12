# Interface Bridges

## Default interface projections

Execution-direction adapters are separate from default interface projections.
A specialized interface can expose its typed property through a base contract,
as the [generic IApplicationPlugin](../../../src/Sphere10.Framework.Application/UI/IApplicationPlugin.cs)
does for `Blocks`. Keep new or changed projections inline
in the interface under `#region Base Interface Bridges`, without helper classes.
They project the same value; they do not select scheduling or lifecycle
execution. Do not put sync/async execution adapters into default interface
members.

[IApplication](../../../src/Sphere10.Framework.Application/UI/IApplication.cs)
and [IApplicationMenu](../../../src/Sphere10.Framework.Application/UI/IApplicationMenu.cs)
show the region-organized typed property bridges. The original plugin `Blocks`
projection demonstrates the same forwarding behavior without that region.

Compatible method results and mutable properties can also bridge in place.
[ISynchronizedObject](../../../src/Sphere10.Framework/Threading/ISynchronizedObject.cs)
forwards typed scopes through the untyped methods, while
[IWinFormsApplicationMenu](../../../src/Sphere10.Framework.Windows.Forms/Application/IWinFormsApplicationMenu.cs)
and [IBlazorApplicationMenu](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/Application/IBlazorApplicationMenu.cs)
bridge their typed `Parent` property. Preserve the specialized setter's null
and type-validation semantics instead of adding separate ownership storage.

For paired execution operations, use the
[sync/async implementation hierarchy](sync-async.md).
