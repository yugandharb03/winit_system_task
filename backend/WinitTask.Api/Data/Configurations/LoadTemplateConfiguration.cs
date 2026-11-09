using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Data.Configurations;

public class LoadTemplateConfiguration : IEntityTypeConfiguration<LoadTemplate>
{
    public void Configure(EntityTypeBuilder<LoadTemplate> builder)
    {
        builder.ToTable("LoadTemplate");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.UID)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.TemplateName)
            .HasColumnType("varchar(250)")
            .IsRequired();

        builder.Property(x => x.TemplateDescription)
            .HasColumnType("varchar(250)");

        builder.Property(x => x.ItemCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(x => x.UID).IsUnique().HasDatabaseName("UX_LoadTemplate_UID");
        builder.HasIndex(x => x.TemplateName).HasDatabaseName("IX_LoadTemplate_TemplateName");
    }
}
