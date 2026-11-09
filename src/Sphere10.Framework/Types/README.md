# Types

Runtime type helpers for resolving types by name, constructing instances, converting values, setting properties, switching on a value's type and supplying component-model type converters.

`TypeResolver` finds a type from a full or short name, optionally loading an assembly by name or path, and `TypeActivator` builds on it to construct instances by type name or `Type`. `TypeActivator.Activate` needs a constructor whose parameter types exactly match the non-null arguments, while `ActivateWithCompatibleArgs` and `TryActivateWithCompatibleArgs` accept assignable argument types and optional parameters. `TypeChanger.ChangeType` converts a value to a target type, including nullable targets, `IConvertible` values and component-model converters. `PropertySetter` compiles a property's setter into a delegate for repeated reflection-free assignment.

`TypeSwitch`, `TypeSwitch<T>` and `AsyncTypeSwitch` dispatch on the runtime type of a value. Cases are tested in order with `IsAssignableFrom`, so list derived types before their bases, and an unmatched value without a `Default` case throws `InvalidOperationException`. `ForType` passes the `Type` itself to the matched action, so use it with the parameterless `Case<T>` overloads. `CollectionTypeConverter`, `IPAddressTypeConverter` and `TypeDescriptorEx` supply converters for property grids and other component-model consumers. Reflection extension methods live in [Extensions](../Extensions/README.md), and deeper member and assembly inspection lives in [Introspection](../Introspection/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncTypeSwitch`](AsyncTypeSwitch.cs) | static class | Runs the first asynchronous case whose target type is assignable from a value's runtime type, or from a given `Type`. |
| [`CollectionTypeConverter`](CollectionTypeConverter.cs) | class | Collection converter that formats a collection as a culture-aware, separator-joined string, with virtual separator, empty text and item formatting. |
| [`IPAddressTypeConverter`](IPAddressTypeConverter.cs) | class | Type converter that validates and converts between `IPAddress` and its string form. |
| [`PropertySetter`](PropertySetter.cs) | class | Compiles a property's setter into an `Action<object, object>` delegate, assigning `default` for `null` values. |
| [`TypeActivator`](TypeActivator.cs) | static class | Creates instances from a type name or `Type`, by exact constructor match or by finding a constructor compatible with the arguments. |
| [`TypeChanger`](TypeChanger.cs) | static class | Converts values between types through compatibility checks, `Convert.ChangeType` and component-model converters, and widens numeric values to `long` or `double`. |
| [`TypeDescriptorEx`](TypeDescriptorEx.cs) | class | `GetConverter` lookup that returns `IPAddressTypeConverter` for `IPAddress` and `TypeDescriptor.GetConverter` otherwise. |
| [`TypeEquivalenceComparer`](TypeEquivalenceComparer.cs) | class | Equality comparer that treats generic types with the same definition as equal when their unbound type parameters align. |
| [`TypeResolver`](TypeResolver.cs) | static class | Resolves a type from a full or short name across loaded assemblies or a named assembly, loading the assembly when needed and rejecting ambiguous matches. |
| [`TypeSwitch`](TypeSwitch.cs) | static class | Runs the first synchronous case whose target type is assignable from a value's runtime type, or from a given `Type`. |
| [`TypeSwitch<T>`](TypeSwitchT.cs) | static class | Returns the result of the first case whose target type is assignable from a value's runtime type, or from a given `Type`. |
