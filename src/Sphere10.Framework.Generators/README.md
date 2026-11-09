# Sphere10.Framework.Generators

Sphere10.Framework.Generators is a C# source generator package. Its `AutoDirty` generator implements the `partial` properties of a class marked `[AutoDirty]`, so that assigning a different value also sets the class's dirty flag. Use it for models whose persistence layer saves only changed objects, such as ObjectSpace dimensions that track changes through a dirty flag.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Generators` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`AutoDirtyGenerator`](AutoDirtyGenerator.cs) | class | Incremental source generator that adds an internal `[AutoDirty(dirtyPropertyName)]` attribute and implements each `partial` property of a marked class with a backing field that sets the named dirty flag when the value changes. |
<!-- domains:end -->

## Reference the package

Reference the package as a development dependency, with the same version as your other Sphere10 Framework packages:

```xml
<PackageReference Include="Sphere10.Framework.Generators" Version="3.1.3" PrivateAssets="all" />
```

The package delivers the generator only as a compiler analyzer, under `analyzers/dotnet/cs`. It adds no runtime assembly and no package dependencies to the consuming application.

Projects inside this repository reference the generator project as an analyzer instead:

```xml
<ProjectReference Include="..\..\src\Sphere10.Framework.Generators\Sphere10.Framework.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

The generator is compiled against `Microsoft.CodeAnalysis.CSharp` 5.9.0, so the consuming compiler must be Roslyn 5.9 or later, such as the compiler in .NET SDK 10.0.403. An older compiler cannot load the generator, which leaves the partial properties unimplemented. Partial properties also require C# 13 or later.

## Example

```csharp
using Sphere10.Framework.Generators;

namespace Example;

[AutoDirty(nameof(IsDirty))]
public partial class Person {
	public bool IsDirty { get; set; }

	public partial string Name { get; set; }

	public partial int Age { get; set; }
}
```

For `Name`, the generator emits a backing field and an implementing declaration equivalent to this code:

```csharp
private string _autoDirty_name;

public partial string Name {
	get => _autoDirty_name;
	set {
		if (!System.Collections.Generic.EqualityComparer<string>.Default.Equals(_autoDirty_name, value)) {
			_autoDirty_name = value;
			IsDirty = true;
		}
	}
}
```

Assigning a value that `EqualityComparer<T>.Default` treats as equal leaves `IsDirty` unchanged. The generator never clears the flag, so the code that saves the object resets it. ObjectSpace `Save` does this for the member configured with `WithChangeTrackingVia`, as the [ObjectSpace guide](../Sphere10.Framework/ObjectSpaces/README.md) describes.

## Rules

- The generator adds the `AutoDirtyAttribute` declaration, in the `Sphere10.Framework.Generators` namespace, to each consuming compilation as an `internal` type, so assemblies that reference each other do not see conflicting copies. A class still inherits tracking from a marked base class in another assembly.
- Mark the class or one of its base classes with `[AutoDirty(nameof(IsDirty))]`. Partial properties declared in derived classes are implemented too, as the `SafeBoxObject` hierarchy in [SafeBoxModel.cs](../../tests/Sphere10.Framework.Tests/TestObjects/SafeBoxModel.cs) shows.
- Declare the dirty flag as an ordinary, non-partial `bool` property that the class can set.
- Declare each tracked property as a `partial` instance property with `get` and `set` or `init` accessors and no bodies. The implementation repeats the declared modifiers and accessors, so the property can be `internal` or `protected`, can be `virtual`, and an accessor may be more restrictive, such as `private set`. An `init` accessor also sets the dirty flag when it assigns a different value.
- The generator skips static properties and properties without both accessors, so those need their own implementing declaration.
- Classes and record classes are processed, including generic classes and classes nested in other types. Structs cannot carry the attribute.

## Validation

`./scripts/validate-packages.ps1` packs the generator with the other Framework packages, checks that it contains the analyzer and no `lib` assets, and compiles an external consumer that declares an `[AutoDirty]` partial property. The [package consumer validation](../../scripts/package-consumers/README.md) page describes that consumer. Within the repository, `tests/Sphere10.Framework.Tests` builds its SafeBox ObjectSpace model with the generator.

## License

MIT (see [LICENSE](../../LICENSE) in the repository root).
