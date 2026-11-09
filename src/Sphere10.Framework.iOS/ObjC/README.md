# ObjC

Legacy P/Invoke declarations for the Objective-C runtime's associated-object and message-send functions.

`iOSAPI` imports `objc_setAssociatedObject`, `objc_getAssociatedObject` and three typed `objc_msgSend` overloads from the Objective-C runtime library. `AssociationPolicy` mirrors the runtime's association policies. `Tools.iOSTool` in the project root uses them for its runtime-property helpers `AddRuntimeProperty`, `RemoveRuntimeProperty`, `TryGetRuntimeProperty` and `HasRuntimeProperty`. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AssociationPolicy`](AssociationPolicy.cs) | enum | Objective-C association policies for associated objects, such as assign, retain and copy. |
| [`iOSAPI`](iOSAPI.cs) | static class | `DllImport` declarations of `objc_setAssociatedObject`, `objc_getAssociatedObject` and typed `objc_msgSend` variants. |
