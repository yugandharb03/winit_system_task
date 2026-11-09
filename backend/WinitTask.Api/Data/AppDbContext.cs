using Microsoft.EntityFrameworkCore;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<LoadTemplate> LoadTemplates => Set<LoadTemplate>();
    public DbSet<LoadTemplateLine> LoadTemplateLines => Set<LoadTemplateLine>();
    public DbSet<SKU> SKUs => Set<SKU>();
    public DbSet<SKUUOM> SKUUOMs => Set<SKUUOM>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
