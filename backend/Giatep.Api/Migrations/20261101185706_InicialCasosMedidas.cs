using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class InicialCasosMedidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Casos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Establecimiento = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Relato = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Casos", x => x.Id);
                    table.CheckConstraint("CK_Casos_Establecimiento", "LEN(LTRIM(RTRIM([Establecimiento]))) >= 3");
                    table.CheckConstraint("CK_Casos_Estado", "[Estado] IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_Casos_Relato", "LEN(LTRIM(RTRIM([Relato]))) >= 20");
                    table.CheckConstraint("CK_Casos_Tipo", "[Tipo] IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "Medidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CasoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Plazo = table.Column<DateOnly>(type: "date", nullable: false),
                    Completada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medidas", x => x.Id);
                    table.CheckConstraint("CK_Medidas_Descripcion", "LEN(LTRIM(RTRIM([Descripcion]))) >= 10");
                    table.CheckConstraint("CK_Medidas_Responsable", "LEN(LTRIM(RTRIM([Responsable]))) >= 3");
                    table.ForeignKey(
                        name: "FK_Medidas_Casos_CasoId",
                        column: x => x.CasoId,
                        principalTable: "Casos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Casos_Codigo",
                table: "Casos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Casos_Estado_Fecha",
                table: "Casos",
                columns: new[] { "Estado", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Medidas_CasoId",
                table: "Medidas",
                column: "CasoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Medidas");

            migrationBuilder.DropTable(
                name: "Casos");
        }
    }
}
