using System.Linq;
using InventoryTools.Services;
using InventoryToolsTesting.Tests.Abstract;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace InventoryToolsTesting.Tests;

[TestFixture]
public class UniversalisWorldFilteringTests : BaseTest
{
    [Test]
    public void PriceWorldFilterIncludesChineseWorlds()
    {
        var worldProvider = Host.Services.GetRequiredService<UniversalisWorldProvider>();
        var worldSheet = Host.Services.GetRequiredService<ExcelSheet<World>>();

        var worlds = worldSheet
            .Where(world => worldProvider.SupportedWorldIds.Contains(world.RowId))
            .ToDictionary(world => world.RowId, world => world.Name.ExtractText());

        Assert.That(worlds.ContainsKey(1081), Is.True);
        Assert.That(worlds.ContainsKey(1167), Is.True);
        Assert.That(worlds.ContainsKey(1201), Is.True);
        Assert.That(worlds.Values, Does.Contain("神意之地"));
        Assert.That(worlds.Values, Does.Contain("红玉海"));
    }
}
