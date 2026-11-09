using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Data.Configurations;

public class SKUUOMConfiguration : IEntityTypeConfiguration<SKUUOM>
{
    public void Configure(EntityTypeBuilder<SKUUOM> builder)
    {
        builder.ToTable("SKUUOM");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.UID)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.SKUCode)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.HasIndex(x => x.UID).IsUnique().HasDatabaseName("UX_SKUUOM_UID");

        // One UOM code may appear only once per SKU.
        builder.HasIndex(x => new { x.SKUCode, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_SKUUOM_SKUCode_Code");

        builder.HasOne(x => x.SKU)
            .WithMany(s => s.UOMs)
            .HasForeignKey(x => x.SKUCode)
            .HasPrincipalKey(s => s.Code)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
