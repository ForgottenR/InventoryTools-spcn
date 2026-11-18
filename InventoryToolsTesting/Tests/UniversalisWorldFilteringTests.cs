using System;
using System.Linq;
using System.Net.Http;
using InventoryTools.Services;
using Lumina;
using Lumina.Data;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace InventoryToolsTesting.Tests;

[TestFixture]
public class UniversalisWorldFilteringTests
{
    [Test]
    public void PriceWorldFilterIncludesChineseWorlds()
    {
        var gameDataDirectory = Environment.GetEnvironmentVariable("EXD_DATA_DIR");
        Assert.That(gameDataDirectory, Is.Not.Null.And.Not.Empty, "EXD_DATA_DIR must point to the game's sqpack directory.");

        using var gameData = new GameData(gameDataDirectory!, new LuminaOptions
        {
            DefaultExcelLanguage = Language.ChineseSimplified,
            PanicOnSheetChecksumMismatch = false,
        });
        using var httpClient = new HttpClient();
        using var worldProvider = new UniversalisWorldProvider(httpClient, NullLogger<UniversalisWorldProvider>.Instance);

        var worldSheet = gameData.GetExcelSheet<Lumina.Excel.Sheets.World>()
            ?? throw new InvalidOperationException("The World sheet is unavailable.");
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
