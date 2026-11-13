using Giatep.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Services;
public sealed class AnalisisService(AccesoService acceso, GiatepDbContext db)
{
    private static readonly TimeZoneInfo ZonaLocal = TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time");
    private static DateTimeOffset InicioDiaLocalUtc(DateOnly fecha) => new(TimeZoneInfo.ConvertTimeToUtc(fecha.ToDateTime(TimeOnly.MinValue), ZonaLocal));
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
        var inicioCierre = desde.HasValue ? InicioDiaLocalUtc(desde.Value) : (DateTimeOffset?)null;
        var finCierre = hasta.HasValue ? InicioDiaLocalUtc(hasta.Value.AddDays(1)) : (DateTimeOffset?)null;
        var consultaCierres = Consultas.Filtrar(acceso.Casos.AsNoTracking(), null, estado, tipoCaso, establecimientoId, null, null)
            .Where(c => c.Estado == EstadoCaso.Cerrado && c.CerradoEn.HasValue);
        if (inicioCierre.HasValue) consultaCierres = consultaCierres.Where(c => c.CerradoEn >= inicioCierre.Value);
        if (finCierre.HasValue) consultaCierres = consultaCierres.Where(c => c.CerradoEn < finCierre.Value);
        var cierresPeriodo = await consultaCierres.CountAsync();
        var abiertos = datos.Length - cerrados.Length;
        var cumplidas = medidas.Count(m => m.Completada);
        var vencidas = medidas.Count(m => !m.Completada && m.Plazo < hoy);
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
                new { nombre = "Casos registrados", valor = (double)datos.Length, unidad = "casos", formula = "Conteo de casos filtrados por fecha del evento", pregunta = "¿Cuántos eventos se registraron?", cantidad = (int?)datos.Length, denominador = (int?)null, muestra = (int?)null },
                new { nombre = "Casos abiertos", valor = (double)abiertos, unidad = "casos", formula = "Casos del período cuyo estado actual no es Cerrado", pregunta = "¿Cuántos casos requieren seguimiento?", cantidad = (int?)abiertos, denominador = (int?)datos.Length, muestra = (int?)null },
                new { nombre = "Casos cerrados", valor = datos.Length == 0 ? 0 : Math.Round(cerrados.Length * 100.0 / datos.Length, 1), unidad = "%", formula = "Casos del período actualmente cerrados / total de casos del período × 100", pregunta = "¿Qué proporción de los casos del período está actualmente cerrada?", cantidad = (int?)cerrados.Length, denominador = (int?)datos.Length, muestra = (int?)null },
                new { nombre = "Cierres del período", valor = (double)cierresPeriodo, unidad = "casos", formula = "Casos con estado Cerrado y fecha de cierre dentro del período seleccionado", pregunta = "¿Cuántos casos se cerraron durante el período?", cantidad = (int?)cierresPeriodo, denominador = (int?)null, muestra = (int?)null },
                new { nombre = "Tiempo de cierre", valor = dias.Length == 0 ? 0 : Math.Round(dias.Average(), 1), unidad = "días", formula = "Promedio de (fecha de cierre − fecha del evento), solo casos cerrados con fecha de cierre", pregunta = "¿Cuánto tarda la gestión completa?", cantidad = (int?)null, denominador = (int?)null, muestra = (int?)dias.Length },
                new { nombre = "Medidas cumplidas", valor = medidas.Length == 0 ? 0 : Math.Round(cumplidas * 100.0 / medidas.Length, 1), unidad = "%", formula = "Medidas completadas / medidas activas asociadas a los casos filtrados × 100", pregunta = "¿Cuántas medidas están completadas?", cantidad = (int?)cumplidas, denominador = (int?)medidas.Length, muestra = (int?)null },
                new { nombre = "Medidas vencidas", valor = medidas.Length == 0 ? 0 : Math.Round(vencidas * 100.0 / medidas.Length, 1), unidad = "%", formula = "Medidas activas pendientes con plazo anterior a hoy / medidas activas asociadas a los casos filtrados × 100", pregunta = "¿Cuántas medidas requieren atención por atraso?", cantidad = (int?)vencidas, denominador = (int?)medidas.Length, muestra = (int?)null }
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
        if (!new[] { "operativo", "gerencial", "parametrizable", "cierres" }.Contains(tipo)) throw new System.ComponentModel.DataAnnotations.ValidationException("Reporte desconocido.");
        if (tipo == "parametrizable" && (!desde.HasValue || !hasta.HasValue)) throw new System.ComponentModel.DataAnnotations.ValidationException("El reporte de medidas requiere fecha inicial y final del caso.");
        if (tipo == "cierres" && desde > hasta) throw new System.ComponentModel.DataAnnotations.ValidationException("La fecha inicial supera la final.");
        if (!string.IsNullOrEmpty(estadoMedida) && (tipo != "parametrizable" || !new[] { "Completada", "Vencida", "Pendiente" }.Contains(estadoMedida))) throw new System.ComponentModel.DataAnnotations.ValidationException("Estado de medida inválido.");
        var datos = await Datos(desde, hasta, establecimientoId, estado, tipoCaso);
        var filtros = $"Fechas de los casos: {desde?.ToString("yyyy-MM-dd") ?? "inicio"} a {hasta?.ToString("yyyy-MM-dd") ?? "hoy"}. Establecimiento: {(establecimientoId.HasValue ? datos.FirstOrDefault()?.Establecimiento ?? "seleccionado (sin datos)" : "todos los autorizados")}.";
        if (!string.IsNullOrEmpty(estado)) filtros += $" Estado del caso: {estado}.";
        if (!string.IsNullOrEmpty(tipoCaso)) filtros += $" Tipo de evento: {(tipoCaso == nameof(TipoCaso.Accidente) ? "Accidente" : "Enfermedad profesional")}.";
        if (!string.IsNullOrEmpty(estadoMedida)) filtros += $" Estado de medida: {estadoMedida}.";
        if (tipo == "cierres") {
            var consultaCierres = Consultas.Filtrar(acceso.Casos.AsNoTracking(), null, estado, tipoCaso, establecimientoId, null, null)
                .Where(c => c.Estado == EstadoCaso.Cerrado && c.CerradoEn.HasValue);
            if (desde.HasValue) consultaCierres = consultaCierres.Where(c => c.CerradoEn >= InicioDiaLocalUtc(desde.Value));
            if (hasta.HasValue) consultaCierres = consultaCierres.Where(c => c.CerradoEn < InicioDiaLocalUtc(hasta.Value.AddDays(1)));
            var cierres = await consultaCierres.OrderByDescending(c => c.CerradoEn)
                .Select(c => new { c.Codigo, c.Fecha, c.CerradoEn, c.Establecimiento, c.Tipo }).ToArrayAsync();
            var filas = cierres.Select(c => new object?[] { c.Codigo, c.Fecha, c.CerradoEn, c.Establecimiento, c.Tipo == TipoCaso.Accidente ? "Accidente" : "Enfermedad profesional" }).ToList();
            return new("Casos cerrados durante el período", filtros.Replace("Fechas de los casos:", "Fechas de cierre:"), ["Caso", "Fecha del evento", "Fecha de cierre", "Establecimiento", "Tipo"], filas, cierres.Length, false);
        }        var titulo = tipo switch { "operativo" => "Detalle operativo de casos", "gerencial" => "Resumen gerencial por establecimiento", _ => "Seguimiento de medidas por período" };
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
