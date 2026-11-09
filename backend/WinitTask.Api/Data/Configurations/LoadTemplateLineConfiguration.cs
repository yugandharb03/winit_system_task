using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Data.Configurations;

public class LoadTemplateLineConfiguration : IEntityTypeConfiguration<LoadTemplateLine>
{
    public void Configure(EntityTypeBuilder<LoadTemplateLine> builder)
    {
        builder.ToTable("LoadTemplateLine");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.UID)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.LoadTemplateUID)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.SKUCode)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.UOMCode)
            .HasColumnType("varchar(250)")
            .IsRequired();

        foreach (var qty in new[] { nameof(LoadTemplateLine.MonQty), nameof(LoadTemplateLine.TueQty),
                                    nameof(LoadTemplateLine.WedQty), nameof(LoadTemplateLine.ThuQty),
                                    nameof(LoadTemplateLine.FriQty), nameof(LoadTemplateLine.SatQty),
                                    nameof(LoadTemplateLine.SunQty) })
        {
            builder.Property(qty)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);
        }

        builder.HasIndex(x => x.UID).IsUnique().HasDatabaseName("UX_LoadTemplateLine_UID");
        builder.HasIndex(x => x.LoadTemplateUID).HasDatabaseName("IX_LoadTemplateLine_LoadTemplateUID");

        // A template may not hold the same SKU twice.
        builder.HasIndex(x => new { x.LoadTemplateUID, x.SKUCode })
            .IsUnique()
            .HasDatabaseName("UX_LoadTemplateLine_Template_SKU");

        // Relationships are modelled on the UID / Code business keys, per the task schema.
        builder.HasOne(x => x.LoadTemplate)
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.LoadTemplateUID)
            .HasPrincipalKey(t => t.UID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SKU)
            .WithMany()
            .HasForeignKey(x => x.SKUCode)
            .HasPrincipalKey(s => s.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
