using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;
using WinitTask.Api.Repositories;

namespace WinitTask.Api.Tests.Repositories;

public class LoadTemplateRepositoryTests
{
    [Fact]
    public async Task GetPaged_FiltersByName_Sorts_AndCountsBeforePaging()
    {
        using var db = new SqliteDb();
        var repo = new LoadTemplateRepository(db.Context);
        db.Context.LoadTemplates.AddRange(
            Template("Alpha", "Mango", 1),
            Template("Alpine", "Apple", 5),
            Template("Beta", "Zebra", 5),
            Template("Other", "Pear", 2));
        await db.Context.SaveChangesAsync();

        var filtered = await repo.GetPagedAsync(new LoadTemplateQueryParameters
        {
            Name = "  alp ",
            SortBy = "templateName",
            SortDir = "asc",
            Page = 2,
            PageSize = 1
        });

        Assert.Equal(2, filtered.TotalCount);
        Assert.Equal("Alpine", Assert.Single(filtered.Items).TemplateName);

        var byNameDesc = await repo.GetPagedAsync(new LoadTemplateQueryParameters
        {
            SortBy = "TEMPLATENAME",
            SortDir = "DESC",
            PageSize = 10
        });
        Assert.Equal(new[] { "Other", "Beta", "Alpine", "Alpha" }, byNameDesc.Items.Select(t => t.TemplateName).ToArray());

        var byDescription = await repo.GetPagedAsync(new LoadTemplateQueryParameters
        {
            SortBy = "templateDescription",
            SortDir = "asc",
            PageSize = 10
        });
        Assert.Equal(new[] { "Alpine", "Alpha", "Other", "Beta" }, byDescription.Items.Select(t => t.TemplateName).ToArray());

        var byCount = await repo.GetPagedAsync(new LoadTemplateQueryParameters
        {
            SortBy = "itemCount",
            SortDir = "desc",
            PageSize = 10
        });
        Assert.Equal(new[] { "Alpine", "Beta", "Other", "Alpha" }, byCount.Items.Select(t => t.TemplateName).ToArray());
    }

    [Fact]
    public async Task GetPaged_BlankName_DoesNotFilter_AndUnknownSortFallsBackToId()
    {
        using var db = new SqliteDb();
        var repo = new LoadTemplateRepository(db.Context);
        db.Context.LoadTemplates.AddRange(
            Template("Alpha", "A", 1),
            Template("Beta", "B", 1),
            Template("Gamma", "C", 1));
        await db.Context.SaveChangesAsync();

        var all = await repo.GetPagedAsync(new LoadTemplateQueryParameters
        {
            Name = "   ",
            SortBy = "drop table",
            SortDir = "asc",
            PageSize = 10
        });
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, all.Items.Select(t => t.TemplateName).ToArray());

        var reversed = await repo.GetPagedAsync(new LoadTemplateQueryParameters
        {
            SortBy = "not-a-column",
            SortDir = "desc",
            PageSize = 10
        });
        Assert.Equal(new[] { "Gamma", "Beta", "Alpha" }, reversed.Items.Select(t => t.TemplateName).ToArray());
    }

    [Fact]
    public async Task GetByUid_LoadsLinesOnlyWhenAsked()
    {
        using var db = new SqliteDb();
        var repo = new LoadTemplateRepository(db.Context);
        db.Context.SKUs.Add(Sku("MILK", "Fresh Milk"));
        await db.Context.SaveChangesAsync();

        var template = Template("Weekly", "Standard", 1);
        template.Lines.Add(new LoadTemplateLine
        {
            UID = "line-1",
            LoadTemplateUID = template.UID,
            SKUCode = "MILK",
            UOMCode = "EA",
            MonQty = 1.25m,
            SunQty = 2
        });
        await repo.AddAsync(template);
        await repo.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var withoutLines = await repo.GetByUidAsync(template.UID, includeLines: false);
        Assert.NotNull(withoutLines);
        Assert.Empty(withoutLines.Lines);

        var withLines = await repo.GetByUidAsync(template.UID, includeLines: true);
        Assert.NotNull(withLines);
        var line = Assert.Single(withLines.Lines);
        Assert.Equal("MILK", line.SKUCode);
        Assert.Equal("EA", line.UOMCode);
        Assert.Equal(1.25m, line.MonQty);
        Assert.Equal(2, line.SunQty);

        Assert.Null(await repo.GetByUidAsync("missing", includeLines: true));
    }

    [Fact]
    public async Task RemoveLines_ThenRemove_DeletesTheTemplate()
    {
        using var db = new SqliteDb();
        var repo = new LoadTemplateRepository(db.Context);
        db.Context.SKUs.Add(Sku("MILK", "Fresh Milk"));
        await db.Context.SaveChangesAsync();

        var template = Template("Weekly", null, 1);
        template.Lines.Add(new LoadTemplateLine
        {
            UID = "line-1",
            LoadTemplateUID = template.UID,
            SKUCode = "MILK",
            UOMCode = "EA"
        });
        await repo.AddAsync(template);
        await repo.SaveChangesAsync();

        repo.RemoveLines(template.Lines.ToList());
        await repo.SaveChangesAsync();
        Assert.Empty(db.Context.LoadTemplateLines);
        Assert.NotNull(await repo.GetByUidAsync(template.UID, includeLines: false));

        repo.Remove(template);
        await repo.SaveChangesAsync();
        Assert.Null(await repo.GetByUidAsync(template.UID, includeLines: false));
    }

    private static LoadTemplate Template(string name, string? description, int itemCount) => new()
    {
        UID = Guid.NewGuid().ToString(),
        TemplateName = name,
        TemplateDescription = description,
        ItemCount = itemCount
    };

    private static SKU Sku(string code, string name) => new()
    {
        UID = Guid.NewGuid().ToString(),
        Code = code,
        Name = name
    };
}
