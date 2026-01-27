---
name: isolated-worker-model-migration
description: Migrates Azure Functions from in-process to isolated worker model with .NET 8+ patterns
---

# Azure Functions Isolated Model Migration Skill

When invoked to migrate an Azure Function project from in-process to isolated worker model, perform the following comprehensive steps:

## Migration Workflow

1. **Analyze current project structure**
   - Report that the isolated-model-migration skill is being used
   - Identify the .csproj file and examine package references
   - Locate the Startup.cs (if present) and function classes
   - Review local.settings.json and host.json configurations

2. **Update project configuration**
   - Modify .csproj to use isolated worker packages
   - Update local.settings.json with isolated worker runtime settings
   - Update host.json ONLY if it contains extension-specific configuration (e.g., Service Bus, Durable Functions)

3. **Create or update Program.cs entry point**
   - If Program.cs exists, update it to use isolated worker HostBuilder pattern
   - If Program.cs doesn't exist, create it and replace Startup.cs
   - Migrate all service registrations to the new DI container

4. **Convert function classes**
   - Update to primary constructors (C# 12)
   - Change attributes from `[FunctionName]` to `[Function]`
   - Update trigger bindings to new types
   - Replace output bindings with SDK clients

5. **Update code patterns**
   - Convert to structured logging
   - Replace Newtonsoft.Json with System.Text.Json
   - Update all using statements

6. **Update Docker configuration (if present)**
   - Check if Dockerfile exists in the project
   - Update base image from in-process to isolated worker image
   - Ensure proper image tags are used

7. **Clean up legacy files**
   - Delete Startup.cs
   - Remove unused packages

8. **Remove BOM (Byte Order Mark) characters**
   - After ALL file edits are complete, scan all C# files for BOM characters
   - Remove UTF-8 BOM (EF BB BF) from the start of any files
   - This prevents invisible characters that can cause issues with some tools
   - Do this LAST, after all other file modifications

9. **Validate changes**
   - Build the project
   - Fix any remaining compilation errors
   - Report summary of changes made

## Key Migration Rules

### Project Configuration
- Add `<OutputType>Exe</OutputType>` to .csproj PropertyGroup
- Replace in-process packages with isolated worker equivalents:
  - `Microsoft.Azure.WebJobs` → `Microsoft.Azure.Functions.Worker`
  - `Microsoft.Azure.WebJobs.Extensions.*` → `Microsoft.Azure.Functions.Worker.Extensions.*`
  - Add `Microsoft.Azure.Functions.Worker.Sdk`
- Remove `FUNCTIONS_INPROC_NET8_ENABLED` from local.settings.json
- Set `FUNCTIONS_WORKER_RUNTIME: "dotnet-isolated"`

### Code Patterns
- Use **primary constructors** for dependency injection
- Use **structured logging** (named placeholders, never string interpolation)
- Register SDK clients (ServiceBusClient, BlobServiceClient) as **singletons**
- Replace `IAsyncCollector<T>` with direct SDK client usage
- Replace `string message` with `ServiceBusReceivedMessage message`
- Replace `HttpRequest` with `HttpRequestData`
- **Do NOT add redundant comments** - Code should be self-explanatory (e.g., don't add "// Register HttpClient" above `services.AddHttpClient()`)

### BOM Character Prevention
- **CRITICAL**: After completing ALL file edits, check for and remove BOM characters
- BOMs (Byte Order Mark: EF BB BF or U+FEFF) can be invisibly added by editors
- Use `replace_string_in_file` to replace file content, matching the BOM character `﻿` at the start
- Check: Program.cs, all function .cs files, and any newly created files
- The BOM appears as an invisible `﻿` character before the first `using` statement
- Modern C# source files should be UTF-8 without BOM

### Docker Configuration
- Update Dockerfile base image:
  - From: `mcr.microsoft.com/azure-functions/dotnet:4-dotnet8.0`
  - To: `mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0`
- For .NET 6: Use `dotnet-isolated:4-dotnet-isolated6.0`
- For .NET 7: Use `dotnet-isolated:4-dotnet-isolated7.0`

### Host Configuration (host.json)
- **Most host.json files do NOT need changes** for isolated worker migration
- **Only update if** the file contains extension-specific configuration that changed between models:
  - Service Bus: `messageHandlerOptions.maxAutoRenewDuration` → `maxAutoLockRenewalDuration`
  - Durable Functions: Configuration path changes
- **Do NOT add** `logLevel` or other logging configuration unless it was already present
- Basic Application Insights sampling configuration remains unchanged

### Namespace Changes
| In-Process | Isolated Worker |
|------------|-----------------|
| `Microsoft.Azure.WebJobs` | `Microsoft.Azure.Functions.Worker` |
| `[FunctionName("name")]` | `[Function("name")]` |
| `Newtonsoft.Json` | `System.Text.Json` |

## Usage

Invoke this skill when asked to:
- "Migrate to isolated worker model"
- "Convert to .NET 8 isolated functions"
- "Update Azure Functions to isolated model"
- "Migrate from in-process to isolated"

## Expected Output

The skill should:
- Update all necessary configuration files (.csproj, local.settings.json)
- Update host.json ONLY if it contains extension-specific configuration that needs migration
- Update Dockerfile (if present) to use isolated worker base image
- Create Program.cs with proper service registration
- Convert all function classes to isolated worker patterns
- Update all logging to structured format
- Replace JSON libraries with System.Text.Json
- Delete legacy Startup.cs
- **Remove BOM characters from all C# files** (do this after all other edits)
- Provide a summary of all changes made

See [EXAMPLE.md](EXAMPLE.md) for detailed before/after code examples.

## Quick Reference Patterns

### Program.cs Entry Point
```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        services.AddScoped<IMyService, MyService>();
        
        services.AddSingleton(sp => 
            new ServiceBusClient(context.Configuration["AzureWebJobsServiceBus"]));
    })
    .Build();

host.Run();
```

### Primary Constructor Pattern
```csharp
// BEFORE: Traditional constructor
public class MyFunction
{
    private readonly ILogger _logger;
    private readonly IMyService _service;
    
    public MyFunction(ILogger<MyFunction> logger, IMyService service)
    {
        _logger = logger;
        _service = service;
    }
}

// AFTER: Primary constructor
public class MyFunction(
    ILogger<MyFunction> logger,
    IMyService service)
{
}
```

### Service Bus Trigger
```csharp
[Function("my-function")]
public async Task Run(
    [ServiceBusTrigger("%QueueName%", Connection = "AzureWebJobsServiceBus")] 
    ServiceBusReceivedMessage message)
{
    var data = JsonSerializer.Deserialize<MyType>(message.Body.ToString());
    logger.LogInformation("Processing {Id}", data.Id);
    await service.ProcessAsync(data);
}
```

### Structured Logging
```csharp
// BAD - String interpolation
logger.LogInformation($"Processing {id}");

// GOOD - Structured logging
logger.LogInformation("Processing {ItemId}", id);

// BAD - Exception as string
logger.LogError($"Failed: {ex.Message}");

// GOOD - Exception as first parameter
logger.LogError(ex, "Failed to process {ItemId}", id);
```

### Output Bindings
```csharp
// BEFORE: IAsyncCollector
[Function("process")]
public async Task Run(
    [ServiceBusTrigger("%Input%")] string message,
    [ServiceBus("%Output%")] IAsyncCollector<MyMessage> output)
{
    await output.AddAsync(new MyMessage());
}

// AFTER: ServiceBusClient
public class MyFunction(
    ServiceBusClient serviceBusClient,
    IConfiguration configuration)
{
    [Function("process")]
    public async Task Run(
        [ServiceBusTrigger("%Input%", Connection = "AzureWebJobsServiceBus")] 
        ServiceBusReceivedMessage message)
    {
        var queueName = configuration["Output"];
        var sender = serviceBusClient.CreateSender(queueName);
        await sender.SendMessageAsync(
            new ServiceBusMessage(JsonSerializer.Serialize(new MyMessage())));
    }
}
```

### Dockerfile
```dockerfile
# BEFORE: In-process base image
FROM mcr.microsoft.com/azure-functions/dotnet:4-dotnet8.0 AS base

# AFTER: Isolated worker base image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
```

### BOM Removal (Final Step)
After all file edits are complete, remove BOM characters from C# files:

```markdown
IMPORTANT: The BOM appears as an invisible `﻿` character (U+FEFF) before the first line.

Use replace_string_in_file to match and remove it:

OLD STRING (with BOM):
﻿using System;
using System.Threading.Tasks;
...rest of file...

NEW STRING (without BOM):
using System;
using System.Threading.Tasks;
...rest of file...

Check these files after editing:
- Program.cs (newly created)
- All function .cs files that were modified
- Any other .cs files that were touched during migration
```

