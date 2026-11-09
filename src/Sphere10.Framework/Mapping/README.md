# Mapping

Member models resolved from lambda expressions, with accessors that read and write a single member or a chain of members.

`Member` wraps a property, field or method behind one abstract API that reports its name, type, visibility and auto-property status, gets and sets values, finds a backing field and converts itself to a delegate. `MemberExtensions.ToMember` creates one from any `MemberInfo`, and `ReflectionExtensions.ToMember` creates one from a lambda such as `x => x.Email`. `Tools.Mapping.GetMember` and `GetAccessor` do the same for model expressions. An accessor is a `SingleMember` for `x => x.Name` and a `PropertyChain` for nested access such as `x => x.Address.City`.

The concrete member classes are internal, so callers work with `Member` and `IAccessor`. The [ObjectSpaces](../ObjectSpaces/README.md) builders take `Member` values for identifiers, indexes and change tracking. Compiled reflection accessors are in [Introspection](../Introspection/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DummyPropertyInfo`](DummyPropertyInfo.cs) | class | Sealed placeholder `PropertyInfo` with a name and type only, created for dynamic-component expressions such as indexer calls with a constant key. |
| [`ExpressionBuilder`](ExpressionBuilder.cs) | class | Static methods that build an `entity => (object)entity.Property` lambda from a property `Member` or `PropertyInfo`. |
| [`IAccessor`](IAccessor.cs) | interface | Gets and sets a member value on a target and creates child accessors, exposing the inner `Member`. |
| [`Member`](Member.cs) | abstract class | Uniform description and value access for a property, field or method, with equality by metadata token and module, and conversion to typed delegates. |
| [`MemberEqualityComparer`](MemberEqualityComparer.cs) | class | `IEqualityComparer<Member>` that matches members by metadata token and module. |
| [`MemberExtensions`](MemberExtensions.cs) | static class | Converts `PropertyInfo`, `FieldInfo`, `MethodInfo` and `MemberInfo` to `Member`, and lists a type's instance fields, methods, properties and members. |
| [`PropertyChain`](PropertyChain.cs) | class | `IAccessor` over a chain of members that walks to the innermost target before reading or writing. |
| [`ReflectionExtensions`](ReflectionExtensions.cs) | static class | Extends `Expression<Func<TMapping, TReturn>>` with `ToMember`. |
| [`SingleMember`](SingleMember.cs) | class | `IAccessor` over one member, with `Build` from an expression or a property name. |
| [`Tools.Mapping`](ReflectionHelper.cs) | tools | Resolves a `Member` or an `IAccessor` from a model lambda expression, including method calls and nested member access. |
