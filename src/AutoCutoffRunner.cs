using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Equinor.ProCoSys.AutoCutoffFunction;

internal static class AutoCutoffRunner
{
    public static async Task<HttpStatusCode> RunAsync(string url, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url, cancellationToken);
        return response.StatusCode;
    }
}
