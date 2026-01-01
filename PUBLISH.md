# Publishing Guide - Sphere10 Framework

This guide covers how to pack and publish Sphere10 Framework NuGet packages using the unified publishing script.

## Quick Start

### Pack and Publish to Local Feed (Development Testing)

```powershell
.\scripts\publish.ps1 -Local
```

This will:
1. Pack all publishable projects from the solution
2. Copy packages to your local NuGet feed at `~/.nuget/local-feed`

### Publish to NuGet.org (Production)

```powershell
.\scripts\publish.ps1 -NuGetOrg -ApiKey $env:NUGET_API_KEY
```

This will:
1. Pack all publishable projects
2. Push packages to NuGet.org

### Pack Only (No Publishing)

```powershell
.\scripts\publish.ps1 -PackOnly
```

Useful for inspecting packages before deciding to publish.

---

## Script Overview

The unified publishing script (`scripts/publish.ps1`) handles the complete workflow:

```
┌─────────────────────────┐
│  PHASE 1: PACK          │  dotnet pack all projects
├─────────────────────────┤
│  PHASE 2: PUBLISH       │  → Local feed OR NuGet.org
└─────────────────────────┘
```

### Key Features

- ✅ **Automatic project discovery** from solution file
- ✅ **Excludes test projects** automatically
- ✅ **Symbol packages** (.snupkg) included by default
- ✅ **Dry-run mode** for safe testing
- ✅ **Clean option** to remove old packages
- ✅ **Selective packing** by project pattern
- ✅ **Formatted output** with clear progress indication

---

## Common Usage Patterns

### 1. Development Workflow: Test Locally First

```powershell
# First, do a dry-run to see what would happen
.\scripts\publish.ps1 -Local -DryRun

# Then publish to local feed
.\scripts\publish.ps1 -Local

# Use in another project
cd ..\MyProject
dotnet add package Sphere10.Framework -s sphere10-local
```

### 2. Publish Specific Projects Only

```powershell
# Publish only Data-related packages
.\scripts\publish.ps1 -Local -ProjectPattern "Sphere10.Framework.Data*"

# Publish only Windows packages
.\scripts\publish.ps1 -Local -ProjectPattern "Sphere10.Framework.Windows*"
```

### 3. Clean Rebuild and Publish

```powershell
# Remove old packages and rebuild
.\scripts\publish.ps1 -Local -Clean

# Or for NuGet.org
.\scripts\publish.ps1 -NuGetOrg -ApiKey $env:NUGET_API_KEY -Clean
```

### 4. Production Release to NuGet.org

```powershell
# Dry run first
.\scripts\publish.ps1 -NuGetOrg -ApiKey $env:NUGET_API_KEY -DryRun

# Then publish for real
.\scripts\publish.ps1 -NuGetOrg -ApiKey $env:NUGET_API_KEY
```

### 5. Only Pack, Don't Publish

```powershell
# Useful for CI/CD pipelines where pack and publish are separate steps
.\scripts\publish.ps1 -PackOnly

# Inspect the packages in ./nuget-packages/
# Then publish separately if desired
```

---

## Command Reference

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Local` | Switch | false | Publish to local NuGet feed |
| `-NuGetOrg` | Switch | false | Publish to NuGet.org |
| `-PackOnly` | Switch | false | Pack projects but don't publish |
| `-ApiKey` | String | `$env:NUGET_API_KEY` | NuGet.org API key for publishing |
| `-ProjectPattern` | String | `*` | Filter projects to pack (e.g., `Sphere10.Framework.Data*`) |
| `-SolutionFile` | String | `src/Sphere10 Framework (Win).sln` | Solution file to use |
| `-LocalFeedPath` | String | `~/.nuget/local-feed` | Path to local NuGet feed |
| `-OutputPath` | String | `./nuget-packages` | Directory for packed packages |
| `-NuGetSource` | String | `https://api.nuget.org/v3/index.json` | NuGet.org source URL |
| `-DryRun` | Switch | false | Preview what would happen without making changes |
| `-Clean` | Switch | false | Delete old packages before packing |
| `-SkipSymbols` | Switch | false | Don't include symbol packages (.snupkg) |

### Examples

#### Pack and publish to local feed
```powershell
.\scripts\publish.ps1 -Local
```

#### Pack specific projects to local feed
```powershell
.\scripts\publish.ps1 -Local -ProjectPattern "Sphere10.Framework.Data*"
```

#### Dry-run before publishing to NuGet.org
```powershell
.\scripts\publish.ps1 -NuGetOrg -ApiKey $env:NUGET_API_KEY -DryRun
```

#### Clean and rebuild everything locally
```powershell
.\scripts\publish.ps1 -Local -Clean
```

#### Pack only (for CI/CD)
```powershell
.\scripts\publish.ps1 -PackOnly -Clean
```

#### Use custom local feed path
```powershell
.\scripts\publish.ps1 -Local -LocalFeedPath "D:\MyNuGetFeed"
```

---

## Setup: Local NuGet Feed

### First-Time Setup

The script automatically creates the local feed directory if it doesn't exist. On first run:

```powershell
.\scripts\publish.ps1 -Local
```

This will:
1. Create `~/.nuget/local-feed` directory
2. Pack all projects
3. Copy packages to the feed
4. Show you how to register it

### Register Local Feed (Manual)

If the script doesn't automatically register it:

```powershell
dotnet nuget add source "C:\Users\<YourUsername>\.nuget\local-feed" -n sphere10-local
```

### Verify Local Feed is Registered

```powershell
dotnet nuget list source

# Output should show:
# sphere10-local [Enabled]
#   https://api.nuget.org/v3/index.json
```

### Using Packages from Local Feed

In your project:

```powershell
dotnet add package Sphere10.Framework -s sphere10-local
dotnet add package Sphere10.Framework.Data -s sphere10-local
```

Or in `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="Sphere10.Framework" Version="1.0.0" />
    <PackageReference Include="Sphere10.Framework.Data" Version="1.0.0" />
</ItemGroup>
```

---

## Setup: NuGet.org Publishing

### Prerequisites

1. **NuGet.org Account**: Create one at https://www.nuget.org/
2. **API Key**: Generate at https://www.nuget.org/account/apikeys
3. **Set Environment Variable**:

```powershell
# PowerShell (persistent)
[Environment]::SetEnvironmentVariable("NUGET_API_KEY", "your-api-key-here", "User")

# Or temporarily for this session
$env:NUGET_API_KEY = "your-api-key-here"
```

### Publishing to NuGet.org

```powershell
# Requires NUGET_API_KEY environment variable
.\scripts\publish.ps1 -NuGetOrg

# Or pass key directly
.\scripts\publish.ps1 -NuGetOrg -ApiKey "your-api-key-here"
```

### Security Notes

⚠️ **Never commit API keys to source control**

- Use environment variables
- Use secure secret managers (Azure Key Vault, GitHub Secrets, etc.)
- Rotate API keys regularly
- Limit API key scope in NuGet.org settings

---

## Package Output Structure

### Location

Packages are placed in: `./nuget-packages/`

### Contents

After packing, you'll see:

```
nuget-packages/
├── Sphere10.Framework.1.0.0.nupkg
├── Sphere10.Framework.1.0.0.snupkg          (symbols)
├── Sphere10.Framework.Data.1.0.0.nupkg
├── Sphere10.Framework.Data.1.0.0.snupkg
├── Sphere10.Framework.CryptoEx.1.0.0.nupkg
├── ... (other packages)
└── HashLib4CSharp.1.5.0.nupkg
```

**Note**: Symbol packages (.snupkg) are only published when using `-Local` or when explicitly requested for NuGet.org.

---

## Troubleshooting

### Issue: "No packable projects found"

**Cause**: The solution file path is incorrect or has no publishable projects.

**Solution**:
```powershell
# Check the solution file exists
Test-Path "src/Sphere10 Framework (Win).sln"

# Specify correct solution
.\scripts\publish.ps1 -Local -SolutionFile "src/Sphere10 Framework (CrossPlatform).sln"
```

### Issue: "API key is required"

**Cause**: Missing NUGET_API_KEY environment variable when publishing to NuGet.org.

**Solution**:
```powershell
# Set environment variable
$env:NUGET_API_KEY = "your-api-key-here"

# Or pass directly
.\scripts\publish.ps1 -NuGetOrg -ApiKey "your-api-key-here"
```

### Issue: "Failed to pack project X"

**Cause**: Project has compilation errors or missing dependencies.

**Solution**:
1. Build the solution first: `dotnet build src/Sphere10\ Framework\ \(Win\).sln`
2. Check for build errors
3. Run restore: `dotnet restore`
4. Try packing again

### Issue: Local feed packages not found

**Cause**: Local feed source not registered or path issue.

**Solution**:
```powershell
# Verify source is registered
dotnet nuget list source

# If missing, add it
dotnet nuget add source "C:\Users\Developer\.nuget\local-feed" -n sphere10-local

# Clear NuGet cache to force refresh
dotnet nuget locals all --clear
```

### Issue: "Skip duplicate" errors on NuGet.org

**Cause**: Version already published to NuGet.org.

**Solution**:
- Increment version in `Directory.Build.props`
- Clean and rebuild
- Republish with new version

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Publish Packages

on:
  workflow_dispatch:
    inputs:
      target:
        description: 'Publish target'
        required: true
        default: 'local'
        type: choice
        options:
          - local
          - nuget

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Publish to Local Feed
        if: github.event.inputs.target == 'local'
        run: ./scripts/publish.ps1 -Local
        shell: pwsh
      
      - name: Publish to NuGet.org
        if: github.event.inputs.target == 'nuget'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: ./scripts/publish.ps1 -NuGetOrg
        shell: pwsh
```

---

## Versioning

The framework uses semantic versioning configured in:

**File**: `Directory.Build.props`

```xml
<PropertyGroup>
    <Version>3.0.0</Version>
    <Authors>Sphere10</Authors>
    <Company>Sphere10</Company>
</PropertyGroup>
```

### Updating Version for Release

1. Edit `Directory.Build.props`
2. Update the `<Version>` tag
3. Commit changes
4. Run publish script

Example:

```powershell
# Update version
# Edit Directory.Build.props: <Version>3.1.0</Version>

# Pack and publish
.\scripts\publish.ps1 -Local -Clean
```

---

## Best Practices

### Development

1. **Test locally first** before publishing to NuGet.org
2. **Use dry-run** to preview before actual publish
3. **Keep incremental versions** during development (1.0.0-dev, 1.0.0-beta)
4. **Document changes** in CHANGELOG

### Production

1. **Version bump** before every NuGet.org release
2. **Run full test suite** before publishing
3. **Tag releases** in git: `git tag v3.0.0`
4. **Create GitHub release** with notes
5. **Clean packages** periodically from local feed

### Security

1. ✅ Use environment variables for API keys
2. ✅ Rotate API keys quarterly
3. ✅ Limit API key scope in NuGet.org
4. ✅ Never commit secrets to git
5. ✅ Use Azure Key Vault for production

---

## Getting Help

- 📖 See [docs/](./docs/) for architecture documentation
- 🔧 Check [scripts/](./scripts/) for other utilities
- 🐛 Report issues on GitHub
- 💬 Discuss in project discussions

---

**Last Updated**: January 1, 2026  
**Framework Version**: 3.0.0  
**Target Framework**: .NET 8.0
