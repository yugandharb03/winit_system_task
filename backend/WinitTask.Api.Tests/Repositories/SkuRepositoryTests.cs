using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;
using WinitTask.Api.Repositories;

namespace WinitTask.Api.Tests.Repositories;

public class SkuRepositoryTests
{
    [Fact]
    public async Task Search_MatchesCodeOrName_ExcludesSelectedProducts_AndPages()
    {
        using var db = new SqliteDb();
        await SeedCatalogue(db);
        var repo = new SkuRepository(db.Context);

        var matches = await repo.SearchAsync(new SkuQueryParameters
        {
            Search = " milk ",
            ExcludeCodes = "MILK",
            Page = 1,
            PageSize = 25
        });

        Assert.Equal(1, matches.TotalCount);
        Assert.Equal("BRD", Assert.Single(matches.Items).Code);

        var page = await repo.SearchAsync(new SkuQueryParameters { Page = 2, PageSize = 1 });
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("BRD", Assert.Single(page.Items).Code);
    }

    [Fact]
    public async Task Search_BlankText_ReturnsEverySkuInIdOrder()
    {
        using var db = new SqliteDb();
        await SeedCatalogue(db);
        var repo = new SkuRepository(db.Context);

        var page = await repo.SearchAsync(new SkuQueryParameters { Search = "   ", PageSize = 25 });

        Assert.Equal(new[] { "MILK", "BRD", "JCE" }, page.Items.Select(sku => sku.Code).ToArray());
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task GetUoms_ReturnsAll_OrOnlyTheRequestedSkus_InIdOrder()
    {
        using var db = new SqliteDb();
        await SeedCatalogue(db);
        var repo = new SkuRepository(db.Context);

        var all = await repo.GetUomsAsync(Array.Empty<string>());
        Assert.Equal(new[] { "EA", "OU", "CT", "EA" }, all.Select(uom => uom.Code).ToArray());

        var milk = await repo.GetUomsAsync(new[] { "MILK", "MILK" });
        Assert.Equal(new[] { "EA", "OU", "CT" }, milk.Select(uom => uom.Code).ToArray());
        Assert.All(milk, uom => Assert.Equal("MILK", uom.SKUCode));
    }

    [Fact]
    public async Task GetExistingCodes_ReturnsOnlyRealCodes()
    {
        using var db = new SqliteDb();
        await SeedCatalogue(db);
        var repo = new SkuRepository(db.Context);

        var found = await repo.GetExistingCodesAsync(new[] { "MILK", "NOPE", "JCE", "MILK" });

        Assert.Equal(2, found.Count);
        Assert.Contains("MILK", found);
        Assert.Contains("JCE", found);
        Assert.Empty(await repo.GetExistingCodesAsync(Array.Empty<string>()));
    }

    [Fact]
    public async Task GetNamesByCode_MapsCodeToName_AndSkipsUnknownCodes()
    {
        using var db = new SqliteDb();
        await SeedCatalogue(db);
        var repo = new SkuRepository(db.Context);

        var names = await repo.GetNamesByCodeAsync(new[] { "BRD", "NOPE" });

        Assert.Equal("Milk Bread", Assert.Single(names).Value);
        Assert.Equal("BRD", names.Single().Key);
        Assert.Empty(await repo.GetNamesByCodeAsync(Array.Empty<string>()));
    }

    private static async Task SeedCatalogue(SqliteDb db)
    {
        db.Context.SKUs.AddRange(
            Sku("MILK", "Fresh Milk", ("EA", "Each"), ("OU", "Outer"), ("CT", "Carton")),
            Sku("BRD", "Milk Bread", ("EA", "Each")),
            Sku("JCE", "Juice"));
        await db.Context.SaveChangesAsync();
    }

    private static SKU Sku(string code, string name, params (string Code, string Name)[] uoms)
    {
        var sku = new SKU
        {
            UID = Guid.NewGuid().ToString(),
            Code = code,
            Name = name
        };

        foreach (var (uomCode, uomName) in uoms)
        {
            sku.UOMs.Add(new SKUUOM
            {
                UID = Guid.NewGuid().ToString(),
                SKUCode = code,
                Code = uomCode,
                Name = uomName
            });
        }

        return sku;
    }
}
