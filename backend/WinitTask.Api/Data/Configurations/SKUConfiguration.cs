using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Data.Configurations;

public class SKUConfiguration : IEntityTypeConfiguration<SKU>
{
    public void Configure(EntityTypeBuilder<SKU> builder)
    {
        builder.ToTable("SKU");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.UID)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.HasIndex(x => x.UID).IsUnique().HasDatabaseName("UX_SKU_UID");

        // Required so LoadTemplateLine.SKUCode and SKUUOM.SKUCode can reference it.
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_SKU_Code");

        builder.HasIndex(x => x.Name).HasDatabaseName("IX_SKU_Name");
    }
}
