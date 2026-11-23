# NT Object Namespace

Reads NT object-manager directories and symbolic links and decodes object access masks, without changing or following any object.

`Tools.Windows.Objects` lists the entries of an object directory such as `\` or `\Device` with `ReadDirectoryAsync`. The result is a `WindowsObjectDirectory` bounded by an entry count, a retained-character budget (`MaximumDirectoryCharacters` is 2 MiB) and a five-second native query budget. `IsPartial` and `LimitReason` say which limit stopped the read. `ReadSymbolicLinkAsync` returns a symbolic link's target without following it. `DecodeAccessMask` names the standard and type-specific rights of a mask for an object type and keeps every undecoded bit as hexadecimal. Handle-level object information comes from `FileHandleService` in the [Files](../Files/README.md) domain.

## Contents

`Tools.Windows.COM` creates typed COM wrappers for Shell links, Shell namespace
automation, Task Scheduler, BITS, WMI, UI Automation and MSAA/IAccessible2. These explicit wrappers support Native AOT without runtime COM
activation. Dispose returned objects on their owning apartment. A WMI query
owns its connection on the calling thread and its objects must be disposed before
the query. UI Automation uses an MTA and named `PropertyId` values. Its wrapper
owns the cache and walker and its returned elements each own their interface.

`Tools.Windows.COM.ReadAccessibleWindow` reads a window's client accessibility object or returns null when unavailable. `WindowsAccessibleComObject` owns the MSAA interface and optional IAccessible2 interface, with typed roles, states, physical bounds, runtime identity, named attributes and bounded child pages. Each returned child or `Retain` result owns its references independently. Simple numeric MSAA children are omitted because they have no independent IA2 identity. Use the [media session](../Windowing/README.md) for isolated repeated discovery rather than retaining provider objects in a rendering loop.

### Wrapping a COM object

Every COM object the library uses follows the same procedure, so that the code runs on CoreCLR and under Native AOT, where runtime activation, `dynamic` binding and built-in interface marshalling are unavailable.

1. Activate the object in `Tools.Windows.COM`. Class and interface identifiers appear only there or inside the wrapper that owns the interface.
2. Return a typed wrapper that owns the native reference and releases it on disposal. Callers use its named members and never see identifiers, dispatch names or vtable slots.
3. Bind the interface in one of three ways. Declare a `[GeneratedComInterface]` for an interface with a stable vtable, as `ShellLink` and BITS do. Call explicit native slots for a few members of a large interface, as `WinAPI.OLEACC` does. Call `IDispatch` by member name through `WinAPI.OLEAUT32.GetDispatchId` and `InvokeDispatch` for automation objects, as the [Shell automation wrappers](../Shell/ShellAutomationComObject.cs) and the [Task Scheduler wrappers](../ScheduledTasks/TaskSchedulerComObjects.cs) do. Both derive from the internal [`DispatchComObject`](DispatchComObject.cs), which owns one automation interface, releases it on disposal and gives derived wrappers `GetValue`, `GetObject`, `Call` and `SetValue` to read a property or call a method, receive another automation object as a typed wrapper, call a method whose result is unused and assign a property. The internal `Tools.Windows.COM.CreateShellApplication` and `CreateTaskService` activate their roots.
4. Convert values with `WindowsVariantMarshaller` and free every variant and string the call returns.
5. State the apartment the caller must provide, and verify the affected operations in a Native AOT executable as well as in tests.

Do not use `Activator.CreateInstance` on a COM type, `Type.GetTypeFromCLSID`, `[ComImport]`, `dynamic` or an `object` parameter marshalled as an interface.

The wrappers pass values as `ComVariant` and convert them with `ComVariantMarshaller`, which .NET 9 added. On `net8.0`, [ComVariantCompatibility.cs](ComVariantCompatibility.cs) (compiled under `#if !NET9_0_OR_GREATER`) supplies public stand-ins of the same names in `System.Runtime.InteropServices.Marshalling`: the same VARIANT layout, converted by the classic COM marshaller (`Marshal.GetNativeVariantForObject` and `GetObjectForNativeVariant`), which needs the JIT. The wrappers keep one API on every target, so every COM feature exists in the `net8.0` build, but only the `net9.0` and later builds are Native AOT safe.

| Type | Kind | Purpose |
|---|---|---|
| [`ComVariant`](ComVariantCompatibility.cs) | struct | `net8.0` only: stand-in for the .NET 9 VARIANT type, in `System.Runtime.InteropServices.Marshalling`, with `VarType` and `GetRawDataRef<T>` |
| [`ComVariantMarshaller`](ComVariantCompatibility.cs) | static class | `net8.0` only: stand-in for the .NET 9 marshaller that converts through the classic COM marshaller, so it is not Native AOT safe |
| [`Tools.Windows.COM`](COM.cs) | static class | Creates explicit COM wrappers for Native AOT, centralizing activation and generated-wrapper ownership |
| [`WindowsInt32SafeArrayMarshaller`](WindowsInt32SafeArrayMarshaller.cs) | static class | Custom marshaller between `int[]` and a native SAFEARRAY of 32-bit integers |
| [`WindowsAccessibleComObject`](WindowsAccessibleComObject.cs) | class | Owns typed MSAA/IAccessible2 provider queries, bounded child pages and independent retained references on one apartment |
| [`WindowsUiAutomationComObject`](WindowsUiAutomationComObject.cs) | class | Owns UI Automation, cache and walker interfaces and returns disposable typed elements with named property IDs |
| [`WindowsVariantMarshaller`](WindowsVariantMarshaller.cs) | static class | Marshals scalar, byte-array and one-dimensional native SAFEARRAY values without runtime-generated COM marshalling |
| [`WindowsObjectDirectory`](WindowsObjectDirectory.cs) | record | Entries of one object directory with its path, partial flag and the reason a limit stopped the read |
| [`WindowsObjectEntry`](WindowsObjectEntry.cs) | record | One object directory entry with name, object type and full path |
| [`Tools.Windows.Objects`](WindowsObjectUtil.cs) | static class | Bounded object-directory listing, symbolic-link reading, access-mask decoding and retained-size accounting |
