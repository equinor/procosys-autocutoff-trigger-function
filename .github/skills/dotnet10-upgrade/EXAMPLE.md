# .NET 10 Upgrade - Examples

## Example 1: Core Library Project (.csproj)

### Before (.NET 8)

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<Platforms>AnyCPU;x64</Platforms>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="6.0.0" />
		<PackageReference Include="Microsoft.ApplicationInsights" Version="2.22.0" />
		<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="6.0.1" />
	</ItemGroup>

</Project>
```

### After (.NET 10)

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<Platforms>AnyCPU;x64</Platforms>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="10.0.0" />
		<PackageReference Include="Microsoft.ApplicationInsights" Version="2.23.0" />
		<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
	</ItemGroup>

</Project>
```

**Key Changes:**
- `TargetFramework` updated from `net8.0` to `net10.0`
- `Microsoft.Extensions.Caching.Abstractions` updated to 10.0.0
- `Microsoft.Extensions.DependencyInjection` updated to 10.0.0
- `Microsoft.ApplicationInsights` updated to latest version

---

## Example 2: Infrastructure Project with Entity Framework

### Before (.NET 8)

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Dapper" Version="2.1.15" />
		<PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.24" />
		<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="6.0.0" />
		<PackageReference Include="Oracle.EntityFrameworkCore" Version="6.21.90" />
		<PackageReference Include="System.Text.Encodings.Web" Version="7.0.0" />
	</ItemGroup>

</Project>
```

### After (.NET 10)

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Dapper" Version="2.1.15" />
		<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
		<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />
		<PackageReference Include="Oracle.EntityFrameworkCore" Version="9.23.80" />
		<PackageReference Include="System.Text.Encodings.Web" Version="10.0.0" />
	</ItemGroup>

</Project>
```

**Key Changes:**
- `TargetFramework` updated from `net8.0` to `net10.0`
- `Microsoft.EntityFrameworkCore` updated to 10.0.0
- `Microsoft.Extensions.Logging.Debug` updated to 10.0.0
- `Oracle.EntityFrameworkCore` updated to 9.23.80 (latest available)
- `System.Text.Encodings.Web` updated to 10.0.0

---

## Example 3: Azure Functions Project

### Before (.NET 8)

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<AzureFunctionsVersion>v4</AzureFunctionsVersion>
		<OutputType>Exe</OutputType>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.0.0" />
		<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="2.0.0" />
		<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.2.0" />
		<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="6.0.35" />
		<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.1" />
	</ItemGroup>
</Project>
```

### After (.NET 10)

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<AzureFunctionsVersion>v4</AzureFunctionsVersion>
		<OutputType>Exe</OutputType>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.51.0" />
		<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="2.0.7" />
		<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.3.0" />
		<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.0" />
		<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.0" />
	</ItemGroup>
</Project>
```

**Key Changes:**
- `TargetFramework` updated from `net8.0` to `net10.0`
- `Microsoft.Azure.Functions.Worker` updated to 2.51.0
- `Microsoft.Azure.Functions.Worker.Sdk` updated to 2.0.7 (required for .NET 10 support)
- `Microsoft.Azure.Functions.Worker.Extensions.Http` updated to 3.3.0
- `Microsoft.Extensions.Caching.StackExchangeRedis` updated to 10.0.0
- `Microsoft.Extensions.Configuration.UserSecrets` updated to 10.0.0

---

## Example 4: Test Project

### Before (.NET 8)

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
        <PackageReference Include="xunit" Version="2.6.2" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
        <PackageReference Include="coverlet.collector" Version="3.2.0" />
    </ItemGroup>

</Project>
```

### After (.NET 10)

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
        <PackageReference Include="xunit" Version="2.6.2" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
        <PackageReference Include="coverlet.collector" Version="3.2.0" />
    </ItemGroup>

</Project>
```

**Key Changes:**
- `TargetFramework` updated from `net8.0` to `net10.0`
- `Microsoft.NET.Test.Sdk` updated to 17.12.0

---

## Example 5: Dockerfile

### Before (.NET 8)

```dockerfile
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FamFeederFunction/FamFeederFunction.csproj", "FamFeederFunction/"]
COPY Core/*csproj ./Core/
COPY Infrastructure/*csproj ./Infrastructure/

COPY nuget.config .
COPY . .

WORKDIR "/src/FamFeederFunction"
RUN dotnet build "FamFeederFunction.csproj" -c Release

FROM build AS publish
RUN dotnet publish "FamFeederFunction.csproj" -c Release --no-restore -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
```

### After (.NET 10)

```dockerfile
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["FamFeederFunction/FamFeederFunction.csproj", "FamFeederFunction/"]
COPY Core/*csproj ./Core/
COPY Infrastructure/*csproj ./Infrastructure/

COPY nuget.config .
COPY . .

WORKDIR "/src/FamFeederFunction"
RUN dotnet build "FamFeederFunction.csproj" -c Release

FROM build AS publish
RUN dotnet publish "FamFeederFunction.csproj" -c Release --no-restore -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
```

**Key Changes:**
- Base image: `dotnet-isolated:4-dotnet-isolated8.0` → `dotnet-isolated:4-dotnet-isolated10.0`
- SDK image: `sdk:8.0` → `sdk:10.0`

---

## Example 6: Azure Pipelines (azure-pipelines.yml)

### Before (.NET 8)

```yaml
steps:
  - task: UseDotNet@2
    displayName: 'Use .NET 8 sdk'
    inputs:
      packageType: 'sdk'
      version: '8.0.x'
      includePreviewVersions: true

  - script: dotnet restore --configfile nuget.config
    displayName: 'dotnet restore with feed auth'

  - script: dotnet build --configuration $(buildConfiguration)
    displayName: 'dotnet build $(buildConfiguration)'
```

### After (.NET 10)

```yaml
steps:
  - task: UseDotNet@2
    displayName: 'Use .NET 10 sdk'
    inputs:
      packageType: 'sdk'
      version: '10.0.x'
      includePreviewVersions: true

  - script: dotnet restore --configfile nuget.config
    displayName: 'dotnet restore with feed auth'

  - script: dotnet build --configuration $(buildConfiguration)
    displayName: 'dotnet build $(buildConfiguration)'
```

**Key Changes:**
- Display name updated from `.NET 8` to `.NET 10`
- SDK version updated from `8.0.x` to `10.0.x`

---

## Fixing BOM Character Issues

If you encounter this error after editing .csproj files:

```
error MSB4025: The project file could not be loaded. Data at the root level is invalid. Line 1, position 1.
```

This is caused by duplicate BOM (Byte Order Mark) characters. Fix using PowerShell:

```powershell
# Check for duplicate BOM (bytes 239, 187, 191 appearing twice)
Get-Content "Core\Core.csproj" -Encoding Byte | Select-Object -First 10

# Fix by rewriting without BOM
$content = Get-Content "Core\Core.csproj" -Raw
[System.IO.File]::WriteAllText("Core\Core.csproj", $content, [System.Text.UTF8Encoding]::new($false))
```

---

## Complete Upgrade Sequence

1. Update all `.csproj` files with new TargetFramework and packages
2. Update `Dockerfile` with new base and SDK images
3. Update `azure-pipelines.yml` with new SDK version
4. Fix any BOM character issues
5. Run `dotnet restore`
6. Run `dotnet build`
7. Run `dotnet test`
8. Verify all tests pass
