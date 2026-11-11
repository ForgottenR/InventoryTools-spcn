using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InventoryTools.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace InventoryToolsTesting.Tests;

[TestFixture]
public class UniversalisWorldProviderTests
{
    [Test]
    public void FallbackContainsChineseAndInternationalWorlds()
    {
        using var provider = CreateProvider("[]");

        Assert.That(provider.SupportedWorldIds.Contains(21), Is.True);
        Assert.That(provider.SupportedWorldIds.Contains(1081), Is.True);
        Assert.That(provider.SupportedWorldIds.Contains(1167), Is.True);
        Assert.That(provider.SupportedWorldIds.Contains(1201), Is.True);
    }

    [Test]
    public async Task RefreshReplacesFallbackSnapshot()
    {
        using var provider = CreateProvider("[{\"id\":21,\"name\":\"Ravana\"},{\"id\":1081,\"name\":\"神意之地\"},{\"id\":1167,\"name\":\"红玉海\"}]");

        Assert.That(await provider.RefreshAsync(), Is.True);
        CollectionAssert.AreEquivalent(new uint[] { 21, 1081, 1167 }, provider.SupportedWorldIds);
    }

    [Test]
    public async Task FailedRefreshRetainsPreviousSnapshot()
    {
        using var provider = CreateProvider("Unavailable", HttpStatusCode.InternalServerError);
        var previousSnapshot = provider.SupportedWorldIds;

        Assert.That(await provider.RefreshAsync(), Is.False);
        Assert.That(provider.SupportedWorldIds, Is.SameAs(previousSnapshot));
        Assert.That(provider.SupportedWorldIds.Contains(1081), Is.True);
    }

    [Test]
    public async Task EmptyRefreshRetainsPreviousSnapshot()
    {
        using var provider = CreateProvider("[]");
        var previousSnapshot = provider.SupportedWorldIds;

        Assert.That(await provider.RefreshAsync(), Is.False);
        Assert.That(provider.SupportedWorldIds, Is.SameAs(previousSnapshot));
    }

    private static UniversalisWorldProvider CreateProvider(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, content));
        return new UniversalisWorldProvider(httpClient, NullLogger<UniversalisWorldProvider>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content),
            });
        }
    }
}
