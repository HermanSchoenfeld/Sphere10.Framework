# Sphere10.Framework.Generators

The `AutoDirty` incremental source generator implements partial properties and sets a named Boolean property when their values change.

## NuGet usage

Reference the package as a development dependency:

```xml
<PackageReference Include="Sphere10.Framework.Generators" Version="3.1.3" PrivateAssets="all" />
```

Use the .NET 10 SDK (or a compiler supporting Roslyn 5.9 and C# partial properties). The package supplies a compiler analyzer under `analyzers/dotnet/cs`; it adds no runtime assembly or Roslyn package dependencies to the consuming application.

```csharp
using Sphere10.Framework.Generators;

namespace Example;

[AutoDirty(nameof(IsDirty))]
public partial class Person {
	public bool IsDirty { get; set; }

	public partial string Name { get; set; }
}
```

The generator injects the `AutoDirtyAttribute` declaration into the compilation. Keep the containing class and each generated property `partial`, and provide a writable Boolean dirty property. Assigning the same value leaves the flag unchanged; assigning a different value sets it to `true`.

Run `validate-packages.ps1` from the repository root to compile an external consumer of the packed analyzer along with the framework packages.

## License

MIT (see [LICENSE](../../LICENSE) in the repository root).
