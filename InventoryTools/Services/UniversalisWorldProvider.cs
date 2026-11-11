using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Mediator;
using InventoryTools.Mediator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InventoryTools.Services;

public class UniversalisWorldProvider : BackgroundService
{
    private const string WorldsUrl = "https://universalis.app/api/v2/worlds";
    private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(10);
    private static readonly IReadOnlySet<uint> FallbackWorldIds = new HashSet<uint>
    {
        21, 22, 23, 24, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72,
        73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 85, 86, 87, 88, 90, 91, 92, 93, 94, 95, 96, 97, 98,
        99, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 3000, 3001,
        1042, 1043, 1044, 1045, 1060, 1076, 1081, 1106, 1113, 1121, 1166, 1167, 1169, 1170, 1171, 1172,
        1173, 1174, 1175, 1176, 1177, 1178, 1179, 1180, 1183, 1186, 1192, 1201,
        2075, 2076, 2077, 2078, 2080, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<UniversalisWorldProvider> _logger;
    private readonly MediatorService? _mediator;
    private IReadOnlySet<uint> _supportedWorldIds = FallbackWorldIds;

    public UniversalisWorldProvider(HttpClient httpClient, ILogger<UniversalisWorldProvider> logger, MediatorService? mediator = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mediator = mediator;
    }

    public IReadOnlySet<uint> SupportedWorldIds => Volatile.Read(ref _supportedWorldIds);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return RefreshAsync(stoppingToken);
    }

    public virtual async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(RefreshTimeout);
            using var response = await _httpClient.GetAsync(WorldsUrl, timeoutSource.Token);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(timeoutSource.Token);
            var worlds = JsonConvert.DeserializeObject<List<UniversalisWorld>>(json);
            var supportedWorldIds = (worlds ?? new List<UniversalisWorld>())
                .Select(world => world.Id)
                .Where(worldId => worldId != 0)
                .ToHashSet();
            if (supportedWorldIds.Count == 0)
            {
                _logger.LogWarning("Universalis returned no supported worlds; retaining the previous world list.");
                return false;
            }

            Volatile.Write(ref _supportedWorldIds, supportedWorldIds);
            _logger.LogInformation("Loaded {Count} supported worlds from Universalis.", supportedWorldIds.Count);
            _mediator?.Publish(new UniversalisWorldsUpdatedMessage());
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to refresh supported worlds from Universalis; retaining the previous world list.");
            return false;
        }
    }

    private sealed class UniversalisWorld
    {
        public uint Id { get; set; }
    }
}
