using System.Net;
using System.Text;

namespace applanch.Tests.Infrastructure.Updates.TestDoubles;

internal sealed class JsonHttpMessageHandler(string responseJson) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        };

        return Task.FromResult(response);
    }
}
