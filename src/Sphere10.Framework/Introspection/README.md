# Introspection

Compiled-expression reflection accessors with process-wide caches, and the `Tools.Reflection` helpers built on them.

The `Sphere10.Framework.FastReflection` namespace replaces late-bound `MethodInfo.Invoke`, `PropertyInfo.GetValue` and similar calls with delegates compiled from expression trees. Each `ConstructorInvoker`, `MethodInvoker`, `PropertyAccessor` and `FieldAccessor` compiles its delegate once in its constructor. `FastReflectionCaches` keeps one accessor per member, and `FastReflectionExtensions` exposes them as `FastInvoke`, `FastGetValue`, `FastSetValue`, `FastGetEnumNames` and `FastIsSubTypeOf` on the reflection types. Call the extensions when the same member is accessed repeatedly, because the first call pays the compilation cost.

`Tools.Reflection` reads and writes properties and fields by name and invokes methods by name, converting results with `Tools.Object.ChangeType` from [Objects](../Objects/README.md). The [Mapping](../Mapping/README.md) domain builds member models from lambda expressions and uses these accessors for value access.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ConstructorInvoker`](ConstructorInvoker.cs) | class | Compiles a `ConstructorInfo` into a delegate that creates an instance from an `object[]` of arguments. |
| [`FastReflectionCaches`](FastReflectionCaches.cs) | static class | Process-wide replaceable caches of enum names, method, property, field and constructor accessors, and subtype checks. |
| [`FastReflectionExtensions`](FastReflectionExtensions.cs) | static class | Extends `MethodInfo`, `ConstructorInfo`, `PropertyInfo`, `FieldInfo` and `Type` with cached `FastInvoke`, `FastGetValue`, `FastSetValue`, `FastGetEnumNames` and `FastIsSubTypeOf`. |
| [`FieldAccessor`](FieldAccessor.cs) | class | Compiles a getter delegate for an instance or static field. |
| [`MethodInvoker`](MethodInvoker.cs) | class | Compiles a `MethodInfo` into a delegate taking the instance and an `object[]` of arguments, returning `null` for `void` methods. |
| [`PropertyAccessor`](PropertyAccessor.cs) | class | Compiles getter and setter delegates for a property and throws `NotSupportedException` when the accessor does not exist. |
| [`Tools.Reflection`](ReflectionTool.cs) | tools | Gets and sets property and field values by name, converting to a requested type, and invokes public or non-public methods by name. |
