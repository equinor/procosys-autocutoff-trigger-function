using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Equinor.ProCoSys.AutoCutoffFunction;

public class AutoCutoffTrigger(
    IConfiguration configuration,
    ILogger<AutoCutoffTrigger> logger)
{
    [Function("AutoCutoffTrigger")]
    public async Task RunAsync([TimerTrigger("%Schedule%")] TimerInfo _)
    {
        logger.LogInformation("Starting AutoCutoffTrigger trigger at: {DateTime}", DateTime.Now);

        var pcsUrl = configuration.GetValue<string>("PCSUrl");
        logger.LogInformation("PCSUrl: {PCSUrl}", pcsUrl);

        var mainSecret = configuration.GetValue<string>("MainSecret");
        logger.LogInformation("MainSecret: {MainSecretPrefix}xxxxxx", mainSecret?.Substring(0, 3));

        var url = $"{pcsUrl?.TrimEnd('/')}/runjob/RunAllCutoffAnonymous?key={mainSecret}";
        var result = await AutoCutoffRunner.RunAsync(url);

        if (result != HttpStatusCode.NoContent)
        {
            throw new Exception($"AutoCutoffTrigger trigger didn't exit with expected code {HttpStatusCode.NoContent}. Got code {result}");
        }

        logger.LogInformation("Finished AutoCutoffTrigger trigger at: {DateTime}", DateTime.Now);
    }
}
