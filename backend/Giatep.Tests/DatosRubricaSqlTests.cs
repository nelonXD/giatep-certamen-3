using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Giatep.Tests;
public sealed class SqlDatosFactAttribute : FactAttribute
{
    public SqlDatosFactAttribute() { if (Environment.GetEnvironmentVariable("GIATEP_SQL_DATOS") != "1") Skip = "Validación de datos locales: habilitar GIATEP_SQL_DATOS=1 y GIATEP_ROOT."; }
}
public class DatosRubricaSqlTests
{
    [SqlDatosFact]
    public void VerificaDatosRubricaCargadosEnSqlServer()
    {
        var root = Environment.GetEnvironmentVariable("GIATEP_ROOT")!;
        var linea = File.ReadLines(Path.Combine(root, ".env")).Single(l => l.StartsWith("ConnectionStrings__Giatep="));
        var conexion = new SqlConnectionStringBuilder(linea.Split('=', 2)[1]); Assert.Equal("GiatepDB", conexion.InitialCatalog);
        using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseSqlServer(conexion.ConnectionString).Options);
        var casos = db.Casos.AsNoTracking().Where(c => c.Codigo.StartsWith(DatosDemostracion.Prefijo)).ToArray();
        Assert.Equal(600, casos.Length); Assert.Equal(12, casos.Select(c => c.Fecha.ToString("yyyy-MM")).Distinct().Count());
        var ids = casos.Select(c => c.Id).ToArray(); var empleadores = casos.Select(c => c.EmpleadorId).Distinct().ToArray();
        Assert.Equal(6, empleadores.Length); Assert.Equal(600, casos.Select(c => c.TrabajadorId).Distinct().Count());
        Assert.Equal(600, db.Trabajadores.Count(t => t.Codigo.StartsWith("RUB26-T"))); Assert.Equal(600, db.Diats.Count(d => ids.Contains(d.CasoId)) + db.Dieps.Count(d => ids.Contains(d.CasoId)));
        Assert.All(casos, c => { Assert.NotNull(c.EmpleadorId); Assert.NotNull(c.TrabajadorId); Assert.Null(c.DenunciaJson); });
        var investigaciones = db.Investigaciones.AsNoTracking().Where(i => ids.Contains(i.CasoId)).ToArray(); var invIds = investigaciones.Select(i => i.Id).ToArray();
        Assert.Equal(casos.Count(c => c.Estado != EstadoCaso.Borrador), investigaciones.Length);
        Assert.Equal(investigaciones.Length * 4, db.Hechos.Count(h => invIds.Contains(h.InvestigacionId)));
        Assert.Equal(investigaciones.Length * 3, db.RelacionesCausales.Count(r => invIds.Contains(r.InvestigacionId)));
        Assert.Equal(investigaciones.Length * 2, db.MedidasInvestigacion.Count(m => invIds.Contains(m.InvestigacionId)));
        Assert.Equal(investigaciones.Length, db.Evidencias.Count(e => ids.Contains(e.CasoId)));
        var medidas = db.Medidas.AsNoTracking().Where(m => ids.Contains(m.CasoId) && m.Activa).ToArray();
        var fechasPlan = investigaciones.Where(i => i.PlanIniciadoEn.HasValue).ToDictionary(i => i.CasoId, i => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.PlanIniciadoEn!.Value, "Pacific SA Standard Time").DateTime));
        Assert.All(medidas, m => Assert.True(m.Plazo >= fechasPlan[m.CasoId], "El plazo de una acción no puede preceder al inicio de su plan."));
        Assert.Contains(medidas, m => m.Completada); Assert.Contains(medidas, m => !m.Completada && m.Plazo < Consultas.Hoy()); Assert.Contains(medidas, m => !m.Completada && m.Plazo >= Consultas.Hoy());
        Assert.All(casos.Where(c => c.Estado == EstadoCaso.Cerrado), c => Assert.All(medidas.Where(m => m.CasoId == c.Id), m => Assert.True(m.Completada)));
        var resumen = new {
            verificadoEn = DateTimeOffset.UtcNow, origen = "Datos sintéticos RUB26, leídos desde SQL Server", casos = casos.Length,
            meses = casos.Select(c => c.Fecha.ToString("yyyy-MM")).Distinct().Count(), desde = casos.Min(c => c.Fecha), hasta = casos.Max(c => c.Fecha),
            tipos = casos.GroupBy(c => c.Tipo.ToString()).ToDictionary(g => g.Key, g => g.Count()), estados = casos.GroupBy(c => c.Estado.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            trabajadores = 600, empleadores = db.Empleadores.Where(e => empleadores.Contains(e.Id)).Select(e => e.Nombre).ToArray(), denuncias = 600,
            investigaciones = investigaciones.Length, investigacionesEnCurso = investigaciones.Count(i => i.Estado == "EnCurso"), investigacionesCerradasSinPlan = investigaciones.Count(i => i.Estado == "Cerrada" && i.PlanIniciadoEn == null), planes = investigaciones.Count(i => i.PlanIniciadoEn != null),
            entrevistas = db.Entrevistas.Count(e => invIds.Contains(e.InvestigacionId)), evidencias = investigaciones.Length, hechos = investigaciones.Length * 4, relacionesCausales = investigaciones.Length * 3, propuestas = investigaciones.Length * 2,
            medidasActivas = medidas.Length, cumplidas = medidas.Count(m => m.Completada), vencidas = medidas.Count(m => !m.Completada && m.Plazo < Consultas.Hoy()), pendientesEnPlazo = medidas.Count(m => !m.Completada && m.Plazo >= Consultas.Hoy()),
            registrosAnterioresConservados = db.Casos.Count(c => !c.Codigo.StartsWith(DatosDemostracion.Prefijo)), resultado = "Requisito de datos: al menos 500 casos, 12 meses, datos sintéticos relacionados y flujo coherente verificados"
        };
        Directory.CreateDirectory(Path.Combine(root, "evidencias"));
        File.WriteAllText(Path.Combine(root, "evidencias", "datos-rubrica.json"), JsonSerializer.Serialize(resumen, new JsonSerializerOptions { WriteIndented = true }));
    }
}
