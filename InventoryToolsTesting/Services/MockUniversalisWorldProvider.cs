using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Mediator;
using InventoryTools.Services;
using Microsoft.Extensions.Logging;

namespace InventoryToolsTesting.Services;

public class MockUniversalisWorldProvider : UniversalisWorldProvider
{
    public MockUniversalisWorldProvider(HttpClient httpClient, ILogger<UniversalisWorldProvider> logger, MediatorService? mediator = null) : base(httpClient, logger, mediator)
    {
    }

    public override Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
