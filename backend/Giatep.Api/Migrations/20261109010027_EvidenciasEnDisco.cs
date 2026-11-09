using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class EvidenciasEnDisco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Evidencias_Archivo",
                table: "Evidencias");

            migrationBuilder.AddColumn<string>(
                name: "Ruta",
                table: "EvidenciasMedidas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "EvidenciasMedidas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Tamano",
                table: "EvidenciasMedidas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Ruta",
                table: "Evidencias",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "Evidencias",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Tamano",
                table: "Evidencias",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(@"IF (EXISTS (SELECT 1 FROM [Evidencias]) OR EXISTS (SELECT 1 FROM [EvidenciasMedidas])) AND ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'GIATEP_ARCHIVOS_VERIFICADOS')), 0) <> 1
THROW 51000, 'Antes de migrar deben exportarse y verificarse los archivos mediante MigradorArchivos.', 1;
UPDATE [Evidencias] SET [Ruta]='investigaciones/' + LOWER(REPLACE(CONVERT(varchar(36),[Id]),'-','')) + '.bin', [Tamano]=DATALENGTH([Contenido]), [Sha256]=CONVERT(varchar(64),HASHBYTES('SHA2_256',[Contenido]),2);
UPDATE [EvidenciasMedidas] SET [Ruta]='planes/' + LOWER(REPLACE(CONVERT(varchar(36),[Id]),'-','')) + '.bin', [Tamano]=DATALENGTH([Contenido]), [Sha256]=CONVERT(varchar(64),HASHBYTES('SHA2_256',[Contenido]),2);");
            migrationBuilder.DropColumn(
                name: "Contenido",
                table: "EvidenciasMedidas");

            migrationBuilder.DropColumn(
                name: "Contenido",
                table: "Evidencias");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciasMedidas_Ruta",
                table: "EvidenciasMedidas",
                column: "Ruta",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenciasMedidas_Metadatos",
                table: "EvidenciasMedidas",
                sql: "[Tamano] BETWEEN 8 AND 5242880 AND LEN([Ruta]) > 0 AND LEN([Sha256]) = 64");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_Ruta",
                table: "Evidencias",
                column: "Ruta",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Evidencias_Metadatos",
                table: "Evidencias",
                sql: "[Tamano] BETWEEN 8 AND 5242880 AND LEN([Ruta]) > 0 AND LEN([Sha256]) = 64");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Restaurar el respaldo SQL y storage conjuntamente; el contenido no puede reconstruirse con una migración SQL inversa.");
        }
    }
}
