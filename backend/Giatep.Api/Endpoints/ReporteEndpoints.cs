using Giatep.Api.Services;
using System.ComponentModel.DataAnnotations;
using Giatep.Api.ValidacionesDTO;
namespace Giatep.Api.Endpoints;
public static class ReporteEndpoints
{
    public static void MapReportes(this WebApplication app)
    {
        app.MapGet("/api/v1/dashboard", async (AnalisisService analisis, DateOnly? desde, DateOnly? hasta, Guid? establecimientoId) => Results.Ok(await analisis.Dashboard(desde, hasta, establecimientoId))).Produces<DashboardDTO>().RequireAuthorization().WithTags("Análisis");
        var group = app.MapGroup("/api/v1/reportes").RequireAuthorization().WithTags("Reportes");
        group.MapGet("/{tipo}", async (string tipo, AnalisisService analisis, DateOnly? desde, DateOnly? hasta, Guid? establecimientoId, int pagina = 1, int tamano = 20) => {
            if (pagina < 1 || pagina > 100000 || tamano < 1 || tamano > 100) throw new ValidationException("Paginación inválida.");
            var r = await analisis.Construir(tipo, desde, hasta, establecimientoId);
            return Results.Ok(new { r.Titulo, r.Filtros, r.Columnas, filas = r.Filas.Skip((pagina - 1) * tamano).Take(tamano), total = r.Filas.Count, r.TotalCasos, pagina, tamano, grafico = r.Grafico ? r.Filas.Where(f => f[0]?.ToString() != "TOTAL").Select(f => new { etiqueta = f[0], valor = f[1] }) : null });
        }).Produces<ReportePaginaDTO>();
        group.MapGet("/{tipo}/exportar/{formato}", async (string tipo, string formato, AnalisisService analisis, AccesoService acceso, IEnumerable<IExportador> exporters, DateOnly? desde, DateOnly? hasta, Guid? establecimientoId, CancellationToken token) => {
            var exporter = exporters.SingleOrDefault(e => e.Formato == formato) ?? throw new ValidationException("Usa xlsx, pdf o csv.");
            var report = await analisis.Construir(tipo, desde, hasta, establecimientoId);
            if (report.Filas.Count > 20000) throw new ValidationException("Reduce el rango de fechas: el límite es 20.000 filas.");
            var bytes = await Task.Run(() => exporter.Exportar(report), token);
            acceso.Auditar("Reporte", tipo, "Exportacion", System.Text.Json.JsonSerializer.Serialize(new { formato, report.Filtros, filas = report.Filas.Count }));
            return Results.File(bytes, exporter.Mime, $"giatep-{tipo}-{Consultas.Hoy():yyyy-MM-dd}.{formato}");
        }).RequireRateLimiting("exportacion");
    }
}
