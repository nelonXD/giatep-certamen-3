using Giatep.Api.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace Giatep.Api.Migrations;
[DbContext(typeof(GiatepDbContext))]
[Migration("20261101194500_VistasReportes")]
public class VistasReportes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE VIEW dbo.v_CasosOperativos AS SELECT c.Id, c.Codigo, c.Fecha, c.Tipo, c.Estado, c.EstablecimientoId, c.Establecimiento, c.TrabajadorId, c.CerradoEn, COUNT(m.Id) AS TotalMedidas FROM dbo.Casos c LEFT JOIN dbo.Medidas m ON m.CasoId=c.Id GROUP BY c.Id,c.Codigo,c.Fecha,c.Tipo,c.Estado,c.EstablecimientoId,c.Establecimiento,c.TrabajadorId,c.CerradoEn");
        migrationBuilder.Sql("CREATE VIEW dbo.v_ResumenEstablecimiento AS SELECT EstablecimientoId, Establecimiento, YEAR(Fecha) AS Anio, MONTH(Fecha) AS Mes, COUNT(*) AS Casos, SUM(CASE WHEN Estado=3 THEN 1 ELSE 0 END) AS Cerrados FROM dbo.Casos GROUP BY EstablecimientoId,Establecimiento,YEAR(Fecha),MONTH(Fecha)");
        migrationBuilder.Sql("CREATE VIEW dbo.v_SeguimientoMedidas AS SELECT m.Id, m.CasoId, c.Codigo, c.Fecha AS FechaCaso, c.EstablecimientoId, c.Establecimiento, m.Descripcion, m.Responsable, m.UsuarioResponsableId, m.Plazo, m.Completada FROM dbo.Medidas m INNER JOIN dbo.Casos c ON m.CasoId=c.Id");
        migrationBuilder.Sql("ALTER TABLE dbo.Casos ADD CONSTRAINT CK_Casos_RelatoMaximo CHECK (LEN(Relato)<=5000)");
        migrationBuilder.Sql("ALTER TABLE dbo.Casos ADD CONSTRAINT CK_Casos_Fecha CHECK (Fecha <= CONVERT(date, SYSDATETIMEOFFSET() AT TIME ZONE 'Pacific SA Standard Time'))");
        migrationBuilder.Sql("ALTER TABLE dbo.Trabajadores ADD CONSTRAINT CK_Trabajadores_Textos CHECK (LEN(LTRIM(RTRIM(Nombre)))>=3 AND LEN(LTRIM(RTRIM(Codigo)))>=3 AND LEN(LTRIM(RTRIM(Cargo)))>=3)");
        migrationBuilder.Sql("ALTER TABLE dbo.Establecimientos ADD CONSTRAINT CK_Establecimientos_Textos CHECK (LEN(LTRIM(RTRIM(Nombre)))>=3 AND LEN(LTRIM(RTRIM(Direccion)))>=3)");
        migrationBuilder.Sql("ALTER TABLE dbo.Causas ADD CONSTRAINT CK_Causas_Datos CHECK (Categoria IN (N'Entorno',N'Organización',N'Ergonomía',N'Equipamiento',N'Procedimiento') AND LEN(LTRIM(RTRIM(Descripcion)))>=10)");
        migrationBuilder.Sql("ALTER TABLE dbo.Observaciones ADD CONSTRAINT CK_Observaciones_Texto CHECK (LEN(LTRIM(RTRIM(Texto)))>=5)");
        migrationBuilder.Sql("ALTER TABLE dbo.Evidencias ADD CONSTRAINT CK_Evidencias_Archivo CHECK (DATALENGTH(Contenido) BETWEEN 8 AND 5242880 AND Tipo IN ('application/pdf','image/png','image/jpeg'))");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW dbo.v_CasosOperativos; DROP VIEW dbo.v_ResumenEstablecimiento; DROP VIEW dbo.v_SeguimientoMedidas;");
        foreach (var pair in new[] { ("Casos", "CK_Casos_RelatoMaximo"), ("Casos", "CK_Casos_Fecha"), ("Trabajadores", "CK_Trabajadores_Textos"), ("Establecimientos", "CK_Establecimientos_Textos"), ("Causas", "CK_Causas_Datos"), ("Observaciones", "CK_Observaciones_Texto"), ("Evidencias", "CK_Evidencias_Archivo") }) migrationBuilder.Sql($"ALTER TABLE dbo.{pair.Item1} DROP CONSTRAINT {pair.Item2}");
    }
}
