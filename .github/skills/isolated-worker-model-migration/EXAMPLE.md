# Azure Functions Isolated Model Migration - Examples

## Example 1: Timer Trigger Function

### Before (In-Process)

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MyApp
{
    public class AutoCutoffTrigger
    {
        private readonly ILogger _logger;
        private readonly IAutoCutoffRunner _runner;

        public AutoCutoffTrigger(
            ILogger<AutoCutoffTrigger> logger,
            IAutoCutoffRunner runner)
        {
            _logger = logger;
            _runner = runner;
        }

        [FunctionName("AutoCutoff")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timer)
        {
            _logger.LogInformation($"AutoCutoff execution started at {DateTime.Now}");
            
            try
            {
                await _runner.ExecuteAsync();
                _logger.LogInformation($"AutoCutoff completed successfully at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"AutoCutoff failed: {ex.Message}");
                throw;
            }
        }
    }
}
```

### After (Isolated Worker)

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MyApp
{
    public class AutoCutoffTrigger(
        ILogger<AutoCutoffTrigger> logger,
        IAutoCutoffRunner runner)
    {
        [FixedDelayRetry(3, "01:00:00")]
        [Function("AutoCutoff")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timer)
        {
            logger.LogInformation("AutoCutoff execution started at {ExecutionTime}", DateTime.Now);
            
            try
            {
                await runner.ExecuteAsync();
                logger.LogInformation("AutoCutoff completed successfully at {CompletionTime}", DateTime.Now);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AutoCutoff failed");
                throw;
            }
        }
    }
}
```

**Key Changes:**
- Primary constructor syntax
- `[FunctionName]` → `[Function]`
- `Microsoft.Azure.WebJobs` → `Microsoft.Azure.Functions.Worker`
- Field references (`_logger`, `_runner`) → Parameter references (`logger`, `runner`)
- String interpolation → Structured logging with named placeholders
- Exception logged with proper exception parameter

---

## Example 2: Service Bus Trigger with Output

### Before (In-Process)

```csharp
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyApp
{
    public class MessageProcessor
    {
        private readonly ILogger _logger;
        private readonly IProcessingService _service;

        public MessageProcessor(
            ILogger<MessageProcessor> logger,
            IProcessingService service)
        {
            _logger = logger;
            _service = service;
        }

        [FunctionName("ProcessMessage")]
        public async Task Run(
            [ServiceBusTrigger("%InputQueue%", Connection = "ServiceBusConnection")] 
            string message,
            [ServiceBus("%OutputQueue%", Connection = "ServiceBusConnection")] 
            IAsyncCollector<ProcessedMessage> output)
        {
            var data = JsonConvert.DeserializeObject<InputMessage>(message);
            _logger.LogInformation($"Processing message {data.Id}");
            
            var result = await _service.ProcessAsync(data);
            
            await output.AddAsync(new ProcessedMessage { Id = result.Id, Status = "Completed" });
        }
    }
}
```

### After (Isolated Worker)

```csharp
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyApp
{
    public class MessageProcessor(
        ILogger<MessageProcessor> logger,
        IProcessingService service,
        ServiceBusClient serviceBusClient,
        IConfiguration configuration)
    {
        [Function("ProcessMessage")]
        public async Task Run(
            [ServiceBusTrigger("%InputQueue%", Connection = "ServiceBusConnection")] 
            ServiceBusReceivedMessage message)
        {
            var data = JsonSerializer.Deserialize<InputMessage>(message.Body.ToString());
            logger.LogInformation("Processing message {MessageId}", data.Id);
            
            var result = await service.ProcessAsync(data);
            
            var outputQueue = configuration["OutputQueue"];
            var sender = serviceBusClient.CreateSender(outputQueue);
            var outMessage = new ServiceBusMessage(
                JsonSerializer.Serialize(new ProcessedMessage 
                { 
                    Id = result.Id, 
                    Status = "Completed" 
                }));
            await sender.SendMessageAsync(outMessage);
        }
    }
}
```

**Key Changes:**
- Primary constructor with additional dependencies (ServiceBusClient, IConfiguration)
- `string message` → `ServiceBusReceivedMessage message`
- `IAsyncCollector<T>` → Direct ServiceBusClient usage
- `Newtonsoft.Json` → `System.Text.Json`
- String interpolation → Structured logging

---

## Example 3: Program.cs (Entry Point)

### Before (Startup.cs in In-Process)

```csharp
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MyApp;

[assembly: FunctionsStartup(typeof(Startup))]

namespace MyApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddScoped<IMyService, MyService>();
            builder.Services.AddScoped<IProcessingService, ProcessingService>();
            
            builder.Services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.example.com");
            });
        }
    }
}
```

### After (Program.cs in Isolated Worker)

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Register scoped services
        services.AddScoped<IMyService, MyService>();
        services.AddScoped<IProcessingService, ProcessingService>();
        
        // Register SDK clients as singletons
        services.AddSingleton(sp =>
        {
            var connectionString = context.Configuration["ServiceBusConnection"];
            return new ServiceBusClient(connectionString);
        });
        
        // HTTP clients
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.example.com");
        });
    })
    .Build();

host.Run();
```

**Key Changes:**
- `FunctionsStartup` class → Top-level Program.cs with HostBuilder
- `IFunctionsHostBuilder` → `HostBuilder` with `ConfigureFunctionsWorkerDefaults()`
- Added Application Insights configuration
- SDK clients (ServiceBusClient) registered as singletons
- Access to IConfiguration via context parameter

---

## Example 4: HTTP Trigger Function

### Before (In-Process)

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyApp
{
    public class HttpEndpoint
    {
        private readonly ILogger _logger;
        private readonly IDataService _dataService;

        public HttpEndpoint(ILogger<HttpEndpoint> logger, IDataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        [FunctionName("GetData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] 
            HttpRequest req)
        {
            var id = req.Query["id"];
            _logger.LogInformation($"Received request for id: {id}");
            
            var data = await _dataService.GetAsync(id);
            
            return new OkObjectResult(data);
        }
    }
}
```

### After (Isolated Worker)

```csharp
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MyApp
{
    public class HttpEndpoint(
        ILogger<HttpEndpoint> logger,
        IDataService dataService)
    {
        [Function("GetData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] 
            HttpRequestData req)
        {
            var id = req.Query["id"];
            logger.LogInformation("Received request for id: {RequestId}", id);
            
            var data = await dataService.GetAsync(id);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(data);
            return response;
        }
    }
}
```

**Key Changes:**
- Primary constructor
- `HttpRequest` → `HttpRequestData`
- `IActionResult` → `HttpResponseData`
- Manual response creation with `CreateResponse` and `WriteAsJsonAsync`
- String interpolation → Structured logging

---

## Example 5: Configuration Files

### local.settings.json

**Before:**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "FUNCTIONS_INPROC_NET8_ENABLED": "1",
    "ServiceBusConnection": "Endpoint=sb://...",
    "MyConfig": {
      "Timeout": 30,
      "RetryCount": 3
    }
  }
}
```

**After:**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "Endpoint=sb://...",
    "MyConfig:Timeout": "30",
    "MyConfig:RetryCount": "3"
  }
}
```

**Changes:**
- Removed `FUNCTIONS_INPROC_NET8_ENABLED`
- Changed runtime to `"dotnet-isolated"`
- Flattened nested configuration using colon notation

### host.json

**Before:**
```json
{
  "version": "2.0",
  "extensions": {
    "serviceBus": {
      "messageHandlerOptions": {
        "maxAutoRenewDuration": "00:05:00"
      }
    }
  }
}
```

**After:**
```json
{
  "version": "2.0",
  "logging": {
    "logLevel": {
      "default": "Information"
    }
  },
  "extensions": {
    "serviceBus": {
      "maxAutoLockRenewalDuration": "00:05:00"
    }
  }
}
```

**Changes:**
- Updated Service Bus configuration path
- Added logging configuration section
