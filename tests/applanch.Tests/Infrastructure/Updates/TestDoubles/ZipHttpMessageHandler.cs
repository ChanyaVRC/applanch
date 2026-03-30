using System.Net;

namespace applanch.Tests.Infrastructure.Updates.TestDoubles;

internal sealed class ZipHttpMessageHandler(byte[] zipBytes) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(zipBytes),
        };

        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        return Task.FromResult(response);
    }
}
