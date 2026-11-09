# Sphere10.Framework.Runtime

Internal assembly with a legacy C# source builder and a single-variable math-expression compiler, built with the Framework but never packed.

Sphere10.Framework.Runtime holds two legacy code generators. [CSharpBuilder](CSharpBuilder/README.md) models C# classes as source text and builds attribute-driven proxy subclasses. [MathRuntime](MathRuntime/README.md) scans, parses, type-checks and compiles math expressions into `IFunction` objects. Both compile their generated source through CodeDom `CSharpCodeProvider.CompileAssemblyFromSource`, which modern .NET runtimes do not support. The source builders, the scanner and the parser work without that compilation step.

The project sets `IsPackable` to `false`, so the Framework publishes no package for it. It targets `$(FrameworkTargetFrameworks)`, references the core `Sphere10.Framework` project and uses the `System.CodeDom` package. Both `src/Sphere10.Framework (Win).sln` and `src/Sphere10.Framework (CrossPlatform).sln` build it. No library or tool in the repository references it.

Its only consumer is [Sphere10.Framework.NET.Tests](../../tests/Sphere10.Framework.NET.Tests/README.md), which tests the math scanner and parser. No solution includes that test project, so run it through its project file as its README shows. Code inside the repository references the assembly with a project reference:

```xml
<ProjectReference Include="..\..\src\Sphere10.Framework.Runtime\Sphere10.Framework.Runtime.csproj" />
```

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Runtime` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [CSharpBuilder](CSharpBuilder/README.md) | Builds C# class source text from object models and generates attribute-driven proxy subclasses of existing types. | [source](CSharpBuilder/) |
| [MathRuntime](MathRuntime/README.md) | Scans, parses, type-checks and compiles single-variable math expressions into `IFunction` objects that evaluate a `double`. | [source](MathRuntime/) |
<!-- domains:end -->

## License

MIT (see [LICENSE](../../LICENSE) in the repository root).
