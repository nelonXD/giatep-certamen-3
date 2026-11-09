using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class PermitirWordEnEvidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Evidencias_Archivo' AND parent_object_id = OBJECT_ID('dbo.Evidencias')) ALTER TABLE [dbo].[Evidencias] DROP CONSTRAINT [CK_Evidencias_Archivo];");
            migrationBuilder.AddCheckConstraint(
                name: "CK_Evidencias_Archivo",
                table: "Evidencias",
                sql: "DATALENGTH([Contenido]) >= 8 AND DATALENGTH([Contenido]) <= 5242880 AND [Tipo] IN ('application/pdf', 'image/png', 'image/jpeg', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Evidencias_Archivo",
                table: "Evidencias");
        }
    }
}
