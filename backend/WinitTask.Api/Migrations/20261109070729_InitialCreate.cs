using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WinitTask.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoadTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UID = table.Column<string>(type: "varchar(250)", nullable: false),
                    TemplateName = table.Column<string>(type: "varchar(250)", nullable: false),
                    TemplateDescription = table.Column<string>(type: "varchar(250)", nullable: true),
                    ItemCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadTemplate", x => x.Id);
                    table.UniqueConstraint("AK_LoadTemplate_UID", x => x.UID);
                });

            migrationBuilder.CreateTable(
                name: "SKU",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UID = table.Column<string>(type: "varchar(250)", nullable: false),
                    Code = table.Column<string>(type: "varchar(250)", nullable: false),
                    Name = table.Column<string>(type: "varchar(250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKU", x => x.Id);
                    table.UniqueConstraint("AK_SKU_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "LoadTemplateLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UID = table.Column<string>(type: "varchar(250)", nullable: false),
                    LoadTemplateUID = table.Column<string>(type: "varchar(250)", nullable: false),
                    SKUCode = table.Column<string>(type: "varchar(250)", nullable: false),
                    UOMCode = table.Column<string>(type: "varchar(250)", nullable: false),
                    MonQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TueQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    WedQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ThuQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    FriQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SatQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SunQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadTemplateLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoadTemplateLine_LoadTemplate_LoadTemplateUID",
                        column: x => x.LoadTemplateUID,
                        principalTable: "LoadTemplate",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoadTemplateLine_SKU_SKUCode",
                        column: x => x.SKUCode,
                        principalTable: "SKU",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SKUUOM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UID = table.Column<string>(type: "varchar(250)", nullable: false),
                    SKUCode = table.Column<string>(type: "varchar(250)", nullable: false),
                    Code = table.Column<string>(type: "varchar(250)", nullable: false),
                    Name = table.Column<string>(type: "varchar(250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKUUOM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SKUUOM_SKU_SKUCode",
                        column: x => x.SKUCode,
                        principalTable: "SKU",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoadTemplate_TemplateName",
                table: "LoadTemplate",
                column: "TemplateName");

            migrationBuilder.CreateIndex(
                name: "UX_LoadTemplate_UID",
                table: "LoadTemplate",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoadTemplateLine_LoadTemplateUID",
                table: "LoadTemplateLine",
                column: "LoadTemplateUID");

            migrationBuilder.CreateIndex(
                name: "IX_LoadTemplateLine_SKUCode",
                table: "LoadTemplateLine",
                column: "SKUCode");

            migrationBuilder.CreateIndex(
                name: "UX_LoadTemplateLine_Template_SKU",
                table: "LoadTemplateLine",
                columns: new[] { "LoadTemplateUID", "SKUCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LoadTemplateLine_UID",
                table: "LoadTemplateLine",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SKU_Name",
                table: "SKU",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UX_SKU_Code",
                table: "SKU",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SKU_UID",
                table: "SKU",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SKUUOM_SKUCode_Code",
                table: "SKUUOM",
                columns: new[] { "SKUCode", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SKUUOM_UID",
                table: "SKUUOM",
                column: "UID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoadTemplateLine");

            migrationBuilder.DropTable(
                name: "SKUUOM");

            migrationBuilder.DropTable(
                name: "LoadTemplate");

            migrationBuilder.DropTable(
                name: "SKU");
        }
    }
}
