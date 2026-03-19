---
name: dotnet10-upgrade
description: Upgrading .NET solutions from .NET 8 to .NET 10 with updated packages and configurations
---

# .NET 10 Upgrade Skill

When invoked to upgrade a .NET solution from .NET 8 to .NET 10, perform the following comprehensive steps:

## Upgrade Workflow

1. **Analyze current project structure**
   - Report that the dotnet10-upgrade skill is being used
   - Identify all .csproj files in the solution
   - Examine current TargetFramework and package references
   - Locate Dockerfile, CI/CD pipelines, and other configuration files

2. **Update project files (.csproj)**
   - Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>`
   - Update Microsoft.Extensions.* packages to version 10.0.0
   - Update Microsoft.EntityFrameworkCore to version 10.0.0
   - Update Azure Functions Worker packages to latest versions
   - Update test SDK packages to latest versions

3. **Update Docker configuration**
   - Update base image from .NET 8 to .NET 10
   - Update SDK image from .NET 8 to .NET 10

4. **Update CI/CD pipelines**
   - Update .NET SDK version in pipeline configuration
   - Update any version-specific references

5. **Fix BOM (Byte Order Mark) issues**
   - After edits, check for duplicate BOM characters in project files
   - Remove duplicate BOMs that can cause XML parsing errors

6. **Validate changes**
   - Run `dotnet restore`
   - Run `dotnet build`
   - Run `dotnet test`
   - Fix any compilation errors
   - Report summary of changes made

## Key Upgrade Rules

### Project Configuration (.csproj)

Update TargetFramework in all project files:
```xml
<!-- Before -->
<TargetFramework>net8.0</TargetFramework>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```

### Microsoft.Extensions Packages

Update all Microsoft.Extensions.* packages to 10.0.0:
- `Microsoft.Extensions.Caching.Abstractions` → 10.0.0
- `Microsoft.Extensions.Caching.StackExchangeRedis` → 10.0.0
- `Microsoft.Extensions.Configuration.UserSecrets` → 10.0.0
- `Microsoft.Extensions.DependencyInjection` → 10.0.0
- `Microsoft.Extensions.Logging.Debug` → 10.0.0

### Entity Framework Core

Update EF Core packages to 10.0.0:
```xml
<!-- Before -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.24" />

<!-- After -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
```

Note: Oracle.EntityFrameworkCore may need to use version 9.x until .NET 10 support is released.

### Azure Functions Worker Packages

Update to latest versions that support .NET 10:
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.51.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="2.0.7" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.3.0" />
```

### Test Project Packages

Update test SDK:
```xml
<!-- Before -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />

<!-- After -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

### Docker Configuration

Update Dockerfile images:
```dockerfile
# Before
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# After
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

### Azure Pipelines Configuration

Update SDK version in azure-pipelines.yml:
```yaml
# Before
- task: UseDotNet@2
  displayName: 'Use .NET 8 sdk'
  inputs:
    packageType: 'sdk'
    version: '8.0.x'

# After
- task: UseDotNet@2
  displayName: 'Use .NET 10 sdk'
  inputs:
    packageType: 'sdk'
    version: '10.0.x'
```

### GitHub Actions Configuration

Update SDK version in GitHub Actions workflows:
```yaml
# Before
- uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'

# After
- uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '10.0.x'
```

## Common Issues and Solutions

### BOM Character Errors

**Problem**: After editing .csproj files, you may see:
```
error MSB4025: The project file could not be loaded. Data at the root level is invalid. Line 1, position 1.
```

**Solution**: Remove duplicate BOM characters from the file using PowerShell:
```powershell
$content = Get-Content "path/to/file.csproj" -Raw
[System.IO.File]::WriteAllText("path/to/file.csproj", $content, [System.Text.UTF8Encoding]::new($false))
```

### Package Downgrade Errors

**Problem**: `NU1605: Detected package downgrade`

**Solution**: Ensure all related packages are updated to compatible versions. Check transitive dependencies and update the direct package reference to match or exceed the required version.

### Azure Functions Version Compatibility

**Problem**: `Invalid combination of TargetFramework and AzureFunctionsVersion`

**Solution**: Update `Microsoft.Azure.Functions.Worker.Sdk` to version 2.0.7 or later, which supports .NET 10.

### Oracle.EntityFrameworkCore Compatibility

**Problem**: Oracle.EntityFrameworkCore may not have a .NET 10 version yet.

**Solution**: Use the latest available version (e.g., 9.23.80) which typically supports the next .NET version.

## Validation Checklist

After completing the upgrade:

- [ ] All .csproj files target `net10.0`
- [ ] `dotnet restore` succeeds
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes all tests
- [ ] Dockerfile uses .NET 10 images
- [ ] CI/CD pipelines reference .NET 10 SDK
- [ ] No duplicate BOM characters in project files
