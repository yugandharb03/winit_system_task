using Microsoft.EntityFrameworkCore;
using WinitTask.Api.Entities;
using WinitTask.Api.Services;

namespace WinitTask.Api.Data;

/// <summary>
/// Idempotent seeding of reference data (SKU, SKUUOM) and sample load templates.
/// Each table is only populated when it is empty, so restarting the API never duplicates rows.
/// </summary>
public static class SeedData
{
    /// <summary>UOM codes and the names shown in the dropdown, per the task brief.</summary>
    private static readonly (string Code, string Name)[] UomCatalogue =
    [
        ("EA", "Each"),
        ("OU", "Outer"),
        ("CT", "Carton")
    ];

    /// <summary>
    /// Product master. The first seven entries are the exact codes and names visible
    /// in the task screenshots; the rest fill the catalogue out so paging and search
    /// have enough data to demonstrate.
    /// </summary>
    private static readonly (string Code, string Name)[] SkuCatalogue =
    [
        ("3005406", "Anc Fresh Dairy white Milk Blue 3L BOT"),
        ("3005405", "Anc Fresh Dairy white Milk Blue 2L BOT"),
        ("545252",  "Anc Fresh Dairy White Milk Blue 3L BOT"),
        ("458692",  "Anc Fresh Dairy White Milk Lite 3L BOT"),
        ("668458",  "Anc Fresh Dairy White Milk Silver 3L BOT"),
        ("774554",  "Anc Fresh Dairy White Milk Trim 3L BOT"),
        ("564554",  "Anc Fresh Dairy White Milk Calc+ 2L BOT"),

        ("3005407", "Anc Fresh Dairy White Milk Blue 1L BOT"),
        ("3005408", "Anc Fresh Dairy White Milk Lite 2L BOT"),
        ("3005409", "Anc Fresh Dairy White Milk Lite 1L BOT"),
        ("3005410", "Anc Fresh Dairy White Milk Trim 2L BOT"),
        ("3005411", "Anc Fresh Dairy White Milk Trim 1L BOT"),
        ("3005412", "Anc Fresh Dairy White Milk Silver 2L BOT"),
        ("3005413", "Anc Fresh Dairy White Milk Calc+ 1L BOT"),
        ("3005414", "Anc Zero Lacto Milk 2L BOT"),
        ("3005415", "Anc Zero Lacto Milk 1L BOT"),

        ("4001001", "Anc Protein+ Chocolate Milk 250ml BOT"),
        ("4001002", "Anc Protein+ Vanilla Milk 250ml BOT"),
        ("4001003", "Anc Protein+ Coffee Milk 250ml BOT"),
        ("4001004", "Anc Flavoured Milk Strawberry 300ml BOT"),
        ("4001005", "Anc Flavoured Milk Banana 300ml BOT"),

        ("5002001", "Anc Natural Yoghurt 1KG TUB"),
        ("5002002", "Anc Greek Yoghurt Plain 500G TUB"),
        ("5002003", "Anc Greek Yoghurt Honey 500G TUB"),
        ("5002004", "Anc Fruit Yoghurt Mixed Berry 6x100G PK"),
        ("5002005", "Anc Fruit Yoghurt Peach 6x100G PK"),

        ("6003001", "Anc Salted Butter 500G BLK"),
        ("6003002", "Anc Unsalted Butter 500G BLK"),
        ("6003003", "Anc Spreadable Butter 250G TUB"),
        ("6003004", "Anc Cooking Cream 300ml BOT"),
        ("6003005", "Anc Whipping Cream 500ml BOT"),
        ("6003006", "Anc Sour Cream 250G TUB"),

        ("7004001", "Anc Mild Cheddar Cheese 500G BLK"),
        ("7004002", "Anc Tasty Cheddar Cheese 1KG BLK"),
        ("7004003", "Anc Colby Cheese 500G BLK"),
        ("7004004", "Anc Edam Cheese 500G BLK"),
        ("7004005", "Anc Grated Mozzarella 250G PK"),
        ("7004006", "Anc Cream Cheese Spread 250G TUB"),
        ("7004007", "Anc Sliced Cheese Burger 500G PK"),
        ("7004008", "Anc Parmesan Shredded 150G PK")
    ];

    /// <summary>
    /// Sample templates. The first five are the rows from the task screenshot, including
    /// QUICKPOST2 with zero items (the UI renders an empty count as "N/A").
    /// </summary>
    private static readonly (string Name, string Description, int LineCount)[] TemplateCatalogue =
    [
        ("KL0021_CY0000720_LT", "TE AWAMUTU WEST", 39),
        ("KL0021_CY0000724_LT", "TE KUITIE",       31),
        ("130021_CY0000729_LT", "QUICKPOST2",       0),
        ("130021_R500720_LT",   "MARKET MART",     12),
        ("130021_R500720_LT",   "SMALL",           24),

        ("KL0021_CY0000731_LT", "HAMILTON CENTRAL", 18),
        ("KL0021_CY0000732_LT", "HAMILTON EAST",    22),
        ("KL0021_CY0000733_LT", "CAMBRIDGE MAIN",    9),
        ("130021_R500721_LT",   "TAURANGA SOUTH",   15),
        ("130021_R500722_LT",   "ROTORUA CENTRAL",  27),
        ("130021_R500723_LT",   "WHAKATANE MART",    6),
        ("KL0022_CY0000740_LT", "AUCKLAND NORTH",   33),
        ("KL0022_CY0000741_LT", "AUCKLAND WEST",    11),
        ("KL0022_CY0000742_LT", "WELLINGTON CBD",   20),
        ("130022_R500730_LT",   "CHRISTCHURCH HUB",  4)
    ];

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await SeedSkusAsync(db, logger);
        await SeedTemplatesAsync(db, logger);
    }

    private static async Task SeedSkusAsync(AppDbContext db, ILogger logger)
    {
        if (await db.SKUs.AnyAsync())
        {
            logger.LogInformation("SKU table already populated — skipping SKU seed.");
            return;
        }

        var skus = SkuCatalogue
            .Select(s => new SKU
            {
                UID = Guid.NewGuid().ToString(),
                Code = s.Code,
                Name = s.Name
            })
            .ToList();

        db.SKUs.AddRange(skus);

        // Every SKU can be ordered as Each, Outer or Carton.
        var uoms = skus
            .SelectMany(sku => UomCatalogue.Select(u => new SKUUOM
            {
                UID = Guid.NewGuid().ToString(),
                SKUCode = sku.Code,
                Code = u.Code,
                Name = u.Name
            }))
            .ToList();

        db.SKUUOMs.AddRange(uoms);

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {SkuCount} SKUs and {UomCount} SKUUOM rows.", skus.Count, uoms.Count);
    }

    private static async Task SeedTemplatesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.LoadTemplates.AnyAsync())
        {
            logger.LogInformation("LoadTemplate table already populated — skipping template seed.");
            return;
        }

        var skuCodes = await db.SKUs.OrderBy(s => s.Id).Select(s => s.Code).ToListAsync();
        if (skuCodes.Count == 0)
        {
            logger.LogWarning("No SKUs available — skipping template seed.");
            return;
        }

        // Fixed seed so every run produces the same sample quantities.
        var random = new Random(20251109);
        var templates = new List<LoadTemplate>();

        foreach (var (name, description, lineCount) in TemplateCatalogue)
        {
            var templateUid = Guid.NewGuid().ToString();

            // Rotate the starting point so templates do not all hold the same products,
            // and never repeat a SKU inside one template.
            var offset = random.Next(skuCodes.Count);
            var chosen = Enumerable.Range(0, Math.Min(lineCount, skuCodes.Count))
                .Select(i => skuCodes[(offset + i) % skuCodes.Count])
                .ToList();

            var lines = chosen.Select((code, index) => new LoadTemplateLine
            {
                UID = Guid.NewGuid().ToString(),
                LoadTemplateUID = templateUid,
                SKUCode = code,
                UOMCode = UomCatalogue[index % UomCatalogue.Length].Code,
                MonQty = random.Next(0, 25),
                TueQty = random.Next(0, 25),
                WedQty = random.Next(0, 25),
                ThuQty = random.Next(0, 25),
                FriQty = random.Next(0, 25),
                SatQty = random.Next(0, 15),
                SunQty = random.Next(0, 10)
            }).ToList();

            templates.Add(new LoadTemplate
            {
                UID = templateUid,
                TemplateName = name,
                TemplateDescription = description,
                // "No of Items" is the total load quantity, not the row count.
                ItemCount = LoadTemplateService.CalculateItemCount(lines),
                Lines = lines
            });
        }

        db.LoadTemplates.AddRange(templates);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {TemplateCount} load templates with {LineCount} lines.",
            templates.Count, templates.Sum(t => t.Lines.Count));
    }
}
