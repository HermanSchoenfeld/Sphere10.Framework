# C# Source Builder

Builds C# class source text from object models and generates attribute-driven proxy subclasses of existing types.

`ClassBuilder`, `FieldBuilder`, `MethodBuilder` and `ParameterBuilder` describe a class, its fields, methods and parameters, and each `ToString()` emits the matching C# source. `ComponentFactory.Instance.New<T>()` reflects over `T`, builds a `_<Name>Proxy` subclass that overrides every virtual method marked with a `CodeBuilderAttribute`, compiles it in memory and returns an instance. `AutoLog`, `AutoMetrics` and `AutoCache` are the code-injecting attributes, and a method without a `MethodImplementationAttribute` gets a body that calls the base implementation.

This is legacy code in the `Sphere10.Framework.NET` namespace. Compilation goes through CodeDom `CSharpCodeProvider.CompileAssemblyFromSource`, which modern .NET runtimes do not support, and the generated proxy source imports namespaces that no longer exist. Treat the builders as a source-text model only. The math compiler in [MathRuntime](../MathRuntime/README.md) uses the same CodeDom approach.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AutoCache`](ComponentFactory.cs) | class | Method attribute that replaces the proxied body with a per-instance dictionary cache keyed by the call and its arguments |
| [`AutoLog`](ComponentFactory.cs) | class | Method attribute that prepends a `Log.LogInfo` call guarded by `EnableInformationLogging` to the proxied body |
| [`AutoMetrics`](ComponentFactory.cs) | class | Method attribute that wraps the proxied body in an `ElapsedTimeLogger` scope guarded by `EnablePerformanceLogging` |
| [`ClassBuilder`](CodeBuilder.cs) | class | Model of a class (namespace, imports, base type, constructor, fields, methods) whose `ToString()` emits its C# source |
| [`CodeBuilderAttribute`](ComponentFactory.cs) | abstract class | Base attribute whose `InjectCode` rewrites a `MethodBuilder` body when `ComponentFactory` builds a proxy |
| [`ComponentFactory`](ComponentFactory.cs) | class | Singleton that generates, compiles and caches proxy subclasses for types whose virtual methods carry `CodeBuilderAttribute`s |
| [`FieldBuilder`](CodeBuilder.cs) | class | Model of a field declaration (protection level, type name, name) |
| [`MethodBuilder`](CodeBuilder.cs) | class | Model of a method (protection, polymorphic level, return type, parameters, body) with helpers that emit parameter lists and a call to the base method |
| [`MethodImplementationAttribute`](ComponentFactory.cs) | abstract class | Marker base for code-builder attributes that supply the whole method body, so no base call is generated |
| [`ParameterBuilder`](CodeBuilder.cs) | class | Model of a method parameter (type name and parameter name) |
| [`PolymorphicLevel`](CodeBuilder.cs) | enum | Method modifier emitted by `MethodBuilder`: none, static, virtual, override or abstract |
| [`PropertyBuilder`](CodeBuilder.cs) | class | Empty placeholder for a property model, with no members |
| [`ProtectionLevel`](CodeBuilder.cs) | enum | Access modifier emitted for classes, fields and methods: private, public, internal or protected |
