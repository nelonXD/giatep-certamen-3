using Giatep.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Services;
public sealed class AnalisisService(AccesoService acceso, GiatepDbContext db)
{
    private Dictionary<Guid, string>? nombres;
    private string NombreCentro(Guid? id, string historico) => id.HasValue ? (nombres ??= db.Establecimientos.AsNoTracking().ToDictionary(e => e.Id, e => e.Nombre)).GetValueOrDefault(id.Value, historico) : historico;
    public async Task<CasoRegistro[]> Datos(DateOnly? desde, DateOnly? hasta, Guid? establecimientoId, string? estado = null, string? tipo = null)
    {
        var consulta = Consultas.Filtrar(acceso.Casos.AsNoTracking(), null, estado, tipo, establecimientoId, desde, hasta);
        var datos = (await consulta.Select(c => new CasoRegistro { Id = c.Id, Codigo = c.Codigo, Tipo = c.Tipo, Establecimiento = c.Establecimiento, EstablecimientoId = c.EstablecimientoId, Fecha = c.Fecha, Estado = c.Estado, CerradoEn = c.CerradoEn }).ToArrayAsync()).OrderBy(c => c.Fecha).ToArray();
        // Consultas separadas evitan multiplicar y ordenar filas anchas al cargar las medidas.
        var medidas = await db.Medidas.AsNoTracking().Where(m => m.Activa && consulta.Any(c => c.Id == m.CasoId)).ToArrayAsync();
        var porCaso = medidas.ToLookup(m => m.CasoId);
        foreach (var caso in datos) caso.Medidas = porCaso[caso.Id].ToList();
        nombres ??= await db.Establecimientos.AsNoTracking().ToDictionaryAsync(e => e.Id, e => e.Nombre);
        return datos;
    }
    public async Task<object> Dashboard(DateOnly? desde, DateOnly? hasta, Guid? establecimientoId, string? estado = null, string? tipoCaso = null)
    {
        var datos = await Datos(desde, hasta, establecimientoId, estado, tipoCaso); var medidas = datos.SelectMany(c => c.Medidas).ToArray(); var hoy = Consultas.Hoy();
        var cerrados = datos.Where(c => c.Estado == EstadoCaso.Cerrado).ToArray();
        var dias = cerrados.Where(c => c.CerradoEn.HasValue).Select(c => Math.Max(0, (c.CerradoEn!.Value.Date - c.Fecha.ToDateTime(TimeOnly.MinValue)).TotalDays)).ToArray();
        var centros = datos.GroupBy(c => c.EstablecimientoId).OrderByDescending(g => g.Count()).Select(g => new { etiqueta = NombreCentro(g.Key, g.First().Establecimiento), valor = g.Count() }).ToArray();
        var mensual = datos.GroupBy(c => c.Fecha.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Count());
        var series = new List<object>();
        if (datos.Length > 0) {
            var inicio = desde ?? datos.Min(c => c.Fecha); var fin = hasta ?? datos.Max(c => c.Fecha);
            var primero = new DateOnly(inicio.Year, inicio.Month, 1); var count = (fin.Year - inicio.Year) * 12 + fin.Month - inicio.Month + 1;
            if (count > 1200) throw new System.ComponentModel.DataAnnotations.ValidationException("El dashboard admite como máximo cien años de meses.");
            for (var i = 0; i < count; i++) { var mes = primero.AddMonths(i).ToString("yyyy-MM"); series.Add(new { etiqueta = mes, valor = mensual.GetValueOrDefault(mes) }); }
        }
        return new {
            indicadores = new[] {
                new { nombre = "Casos registrados", valor = (double)datos.Length, unidad = "casos", formula = "Conteo de casos filtrados", pregunta = "¿Cuántos eventos se registraron?" },
                new { nombre = "Casos abiertos", valor = (double)(datos.Length - cerrados.Length), unidad = "casos", formula = "Casos cuyo estado no es Cerrado", pregunta = "¿Cuántos casos requieren seguimiento?" },
                new { nombre = "Casos cerrados", valor = datos.Length == 0 ? 0 : Math.Round(cerrados.Length * 100.0 / datos.Length, 1), unidad = "%", formula = "Cerrados / total de casos × 100", pregunta = "¿Qué proporción del trabajo está terminada?" },
                new { nombre = "Tiempo de cierre", valor = dias.Length == 0 ? 0 : Math.Round(dias.Average(), 1), unidad = "días", formula = "Promedio de (fecha de cierre − fecha del evento), solo cerrados con fecha", pregunta = "¿Cuánto tarda la gestión completa?" },
                new { nombre = "Medidas cumplidas", valor = medidas.Length == 0 ? 0 : Math.Round(medidas.Count(m => m.Completada) * 100.0 / medidas.Length, 1), unidad = "%", formula = "Medidas completadas / total de medidas × 100", pregunta = "¿Se ejecutan las acciones acordadas?" },
                new { nombre = "Medidas vencidas", valor = medidas.Length == 0 ? 0 : Math.Round(medidas.Count(m => !m.Completada && m.Plazo < hoy) * 100.0 / medidas.Length, 1), unidad = "%", formula = "Medidas pendientes con plazo anterior a hoy / total de medidas × 100", pregunta = "¿Qué proporción necesita atención por atraso?" }
            },
            meses = series,
            tipos = datos.GroupBy(c => c.Tipo).Select(g => new { etiqueta = g.Key == TipoCaso.Accidente ? "Accidentes" : "Enfermedades", valor = g.Count() }),
            establecimientos = centros,
            estados = datos.GroupBy(c => c.Estado).Select(g => new { etiqueta = Estado(g.Key), valor = g.Count() }),
            interpretacion = datos.Length == 0 ? "No hay casos en el período seleccionado; amplía los filtros antes de interpretar indicadores." : $"{centros[0].etiqueta} concentra {centros[0].valor} de {datos.Length} casos. Prioriza la revisión de sus medidas vencidas y compara períodos equivalentes antes de concluir que existe mayor riesgo: los conteos no consideran la dotación expuesta.",
            actualizado = DateTimeOffset.UtcNow
        };
    }
    public static string Estado(EstadoCaso e) => e switch { EstadoCaso.EnInvestigacion => "En investigación", EstadoCaso.EnSeguimiento => "En seguimiento", _ => e.ToString() };
    public async Task<Reporte> Construir(string tipo, DateOnly? desde, DateOnly? hasta, Guid? establecimientoId, string? estado = null, string? tipoCaso = null, string? estadoMedida = null)
    {
        if (!new[] { "operativo", "gerencial", "parametrizable" }.Contains(tipo)) throw new System.ComponentModel.DataAnnotations.ValidationException("Reporte desconocido.");
        if (tipo == "parametrizable" && (!desde.HasValue || !hasta.HasValue)) throw new System.ComponentModel.DataAnnotations.ValidationException("El reporte de medidas requiere fecha inicial y final del caso.");
        if (!string.IsNullOrEmpty(estadoMedida) && (tipo != "parametrizable" || !new[] { "Completada", "Vencida", "Pendiente" }.Contains(estadoMedida))) throw new System.ComponentModel.DataAnnotations.ValidationException("Estado de medida inválido.");
        var datos = await Datos(desde, hasta, establecimientoId, estado, tipoCaso);
        var filtros = $"Fechas de los casos: {desde?.ToString("yyyy-MM-dd") ?? "inicio"} a {hasta?.ToString("yyyy-MM-dd") ?? "hoy"}. Establecimiento: {(establecimientoId.HasValue ? datos.FirstOrDefault()?.Establecimiento ?? "seleccionado (sin datos)" : "todos los autorizados")}.";
        if (!string.IsNullOrEmpty(estado)) filtros += $" Estado del caso: {estado}.";
        if (!string.IsNullOrEmpty(tipoCaso)) filtros += $" Tipo de evento: {(tipoCaso == nameof(TipoCaso.Accidente) ? "Accidente" : "Enfermedad profesional")}.";
        if (!string.IsNullOrEmpty(estadoMedida)) filtros += $" Estado de medida: {estadoMedida}.";
        var titulo = tipo switch { "operativo" => "Detalle operativo de casos", "gerencial" => "Resumen gerencial por establecimiento", _ => "Seguimiento de medidas por período" };
        if (tipo == "gerencial") {
            var filas = datos.GroupBy(c => c.EstablecimientoId).Select(g => new object?[] { NombreCentro(g.Key, g.First().Establecimiento), g.Count(), g.Count(c => c.Estado == EstadoCaso.Cerrado), g.Count(c => c.Estado != EstadoCaso.Cerrado), g.SelectMany(c => c.Medidas).Count(m => !m.Completada && m.Plazo < Consultas.Hoy()) }).OrderBy(f => f[0]).ToList();
            filas.Add(["TOTAL", datos.Length, datos.Count(c => c.Estado == EstadoCaso.Cerrado), datos.Count(c => c.Estado != EstadoCaso.Cerrado), datos.SelectMany(c => c.Medidas).Count(m => !m.Completada && m.Plazo < Consultas.Hoy())]);
            return new(titulo, filtros, ["Establecimiento", "Casos", "Cerrados", "Abiertos", "Medidas vencidas"], filas, datos.Length, true);
        }
        if (tipo == "parametrizable") {
            var filasMedidas = datos.SelectMany(c => c.Medidas.Select(m => new object?[] { c.Codigo, c.Fecha, c.Establecimiento, m.Descripcion, m.Responsable, m.Plazo, m.Completada ? "Completada" : m.Plazo < Consultas.Hoy() ? "Vencida" : "Pendiente" }))
                .Where(f => string.IsNullOrEmpty(estadoMedida) || string.Equals(f[6]?.ToString(), estadoMedida, StringComparison.Ordinal)).ToList();
            return new(titulo, filtros, ["Caso", "Fecha del caso", "Establecimiento", "Medida", "Responsable", "Plazo", "Estado"], filasMedidas, filasMedidas.Select(f => f[0]?.ToString()).Distinct().Count(), false);
        }
        return new(titulo, filtros, ["Caso", "Fecha", "Establecimiento", "Tipo", "Estado", "Medidas"], datos.Select(c => new object?[] { c.Codigo, c.Fecha, c.Establecimiento, c.Tipo == TipoCaso.Accidente ? "Accidente" : "Enfermedad profesional", Estado(c.Estado), c.Medidas.Count }).ToList(), datos.Length, false);
    }
}
public record Reporte(string Titulo, string Filtros, string[] Columnas, List<object?[]> Filas, int TotalCasos, bool Grafico);
