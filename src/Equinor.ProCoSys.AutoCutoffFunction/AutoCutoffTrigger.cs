using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Equinor.ProCoSys.AutoCutoffFunction
{
    public class AutoCutoffTrigger
    {
        [FunctionName("AutoCutoffTrigger")]
        public void Run([TimerTrigger("0 0 1 * * Mon")]TimerInfo timerInfo, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
