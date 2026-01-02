# Publishing Sphere10 Framework to NuGet

## Prerequisites

- NuGet.org account with API key
- .NET 8.0 SDK
- PowerShell 7+

## Setup

Set your NuGet API key:
```powershell
$env:NUGET_API_KEY = "your-api-key-here"
```

Or persist in PowerShell profile:
```powershell
[Environment]::SetEnvironmentVariable('NUGET_API_KEY', 'your-api-key', 'User')
```

## Local Publishing

### Pack

Generate NuGet packages:
```powershell
./scripts/pack.ps1
```

Options:
```powershell
./scripts/pack.ps1 -Configuration Release -Clean
./scripts/pack.ps1 -OutputPath ./my-packages
```

Output: `./nuget-packages/` contains `.nupkg` and `.snupkg` files for 18 projects.

### Publish

Push to nuget.org:
```powershell
./scripts/publish.ps1
```

Preview without uploading:
```powershell
./scripts/publish.ps1 -DryRun
```

Options:
```powershell
./scripts/publish.ps1 -PackagesPath ./my-packages
./scripts/publish.ps1 -SkipSymbols
```

Verify on [nuget.org](https://www.nuget.org/) (may take a few minutes).

## GitHub Actions CI/CD

1. Add `NUGET_API_KEY` secret in repository Settings â†’ Secrets
2. Push version tag to trigger workflow:
   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. GitHub Actions will pack and publish automatically

## Version Management

All projects use version from `Directory.Build.props`:
```xml
<Version>1.0.0</Version>
```

Update version, commit, tag, and push to publish:
```powershell
# Edit Directory.Build.props with new version
git add Directory.Build.props
git commit -m "Bump version to 1.0.0"
git tag v1.0.0
git push origin main
git push origin v1.0.0
```

## Troubleshooting

**Pack fails: "DLL not found"** - Build first: `dotnet build -c Release`

**Pack fails: "Nullable value error"** - Legacy projects (iOS, Android) have `<Nullable>disable</Nullable>`. Add to new projects if needed.

**Publish fails: "401 Unauthorized"** - Invalid or expired API key. Regenerate on nuget.org.

**Publish fails: "Package already exists"** - Version already published. Increment version and republish.

**Publish fails: "Symbol package validation failed"** - Use `-SkipSymbols` flag.

**Package on nuget.org but dependencies not found** - Inspect `.nupkg` contents (rename to `.zip` and check `.nuspec` for `<dependencies>`).

## Scripts Reference

### pack.ps1

Builds and packages projects.

Parameters:

- `-Configuration`: Build config (default: Release)
- `-OutputPath`: Output directory (default: ./nuget-packages)
- `-VerifyNugetDependencies`: Verifies packed nuspec dependency metadata matches `ProjectReference` graphs
- `-Clean`: Remove old artifacts before packing

Example:
```powershell
./scripts/pack.ps1 -Configuration Release -Clean -Verbose
```

### publish.ps1

Publishes packages to nuget.org.

Parameters:

Example:
```powershell
./scripts/publish.ps1 -DryRun
./scripts/publish.ps1 -ApiKey "oy2..."
```

## Development vs Production Mode

The framework uses standard SDK-style `<ProjectReference>` during development, and `dotnet pack` automatically emits proper NuGet dependencies when producing packages.

To validate dependency metadata before publishing:
```powershell
./scripts/pack.ps1 -VerifyNugetDependencies
```

## Best Practices

1. **Always test locally first**
   ```powershell
   dotnet build "src\Sphere10 Framework (CrossPlatform).sln" -c Release
   ./scripts/pack.ps1 -Clean
   ./scripts/publish.ps1 -DryRun
   ```

2. **Use semantic versioning** (MAJOR.MINOR.PATCH)
   - Breaking changes â†’ increment MAJOR
   - New features â†’ increment MINOR
   - Bug fixes â†’ increment PATCH

3. **Keep versions in sync** - All packages use same version from `Directory.Build.props`

4. **Tag releases**
   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

5. **Verify dependencies** before publishing to catch missing dependencies:
   ```powershell
   ./scripts/pack.ps1 -VerifyNugetDependencies
   ```

