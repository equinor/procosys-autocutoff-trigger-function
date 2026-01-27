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
   - Update host.json with new configuration paths

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

6. **Clean up legacy files**
   - Delete Startup.cs
   - Remove unused packages
   - Check for and remove BOM (Byte Order Mark) characters from file starts

7. **Validate changes**
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
- **Remove BOM characters**: Check for invisible `﻿` (U+FEFF) at start of files and remove them

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
- Update all configuration files (.csproj, local.settings.json, host.json)
- Create Program.cs with proper service registration
- Convert all function classes to isolated worker patterns
- Update all logging to structured format
- Replace JSON libraries with System.Text.Json
- Delete legacy Startup.cs
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
        
        // Register services as scoped
        services.AddScoped<IMyService, MyService>();
        
        // Register SDK clients as singletons
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
    // Use logger and service directly
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
