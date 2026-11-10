using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Endpoints;
public record TextoDTO(string Texto);
public record CausaDTO(string Categoria, string Descripcion);
public static class CasoEndpoints
{
    public static void Texto(string? value, int min, int max, string label)
    {
        if (value is null || value.Trim().Length < min || value.Length > max) throw new ValidationException($"{label}: debe tener entre {min} y {max} caracteres.");
    }
    private static void ValidarTrabajador(CasoDTO dto, Guid centroId, GiatepDbContext db)
    {
        if (dto.TrabajadorId.HasValue && !db.Trabajadores.Any(t => t.Id == dto.TrabajadorId && t.Activo && t.EstablecimientoId == centroId)) throw new ValidationException("El trabajador indicado no está activo en el establecimiento.");
        if (dto.Denuncia is null && !dto.TrabajadorId.HasValue) throw new ValidationException("Completa los datos del trabajador en la denuncia.");
        if (dto.Denuncia?.Datos is { } datos && datos.TryGetValue("trabajadorRun", out var run)) {
            var normalizado = DenunciaAlmacen.NormalizarRut(run ?? "");
            var existente = db.Trabajadores.SingleOrDefault(t => t.Run == normalizado);
            if (existente is not null && (!existente.Activo || existente.EstablecimientoId != centroId)) throw new ValidationException("El trabajador registrado debe estar activo y corresponder al establecimiento seleccionado.");
        }
    }
    public static void MapCasos(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/v1/casos").WithTags("Casos").RequireAuthorization("Operacion");
        grupo.MapGet("/", (AccesoService acceso, GiatepDbContext db, string? buscar, string? estado, string? tipo, Guid? establecimientoId, DateOnly? desde, DateOnly? hasta, string? orden, int pagina = 1, int tamano = 10, bool soloMios = false) =>
        {
            if (pagina < 1 || pagina > 100000 || tamano < 1 || tamano > 100) throw new ValidationException("Paginación inválida.");
            var consulta = Consultas.Filtrar(acceso.Casos.AsNoTracking(), buscar, estado, tipo, establecimientoId, desde, hasta);
            if (soloMios) {
                var usuarioId = acceso.UsuarioId;
                var registros = db.Auditorias.Where(a => a.Entidad == nameof(CasoRegistro) && a.Accion == "Added" && a.UsuarioId == usuarioId).Select(a => a.RegistroId);
                consulta = consulta.Where(c => registros.Contains(c.Id.ToString()));
            }
            consulta = orden switch { "fechaAsc" => consulta.OrderBy(c => c.Fecha).ThenBy(c => c.Id), "codigo" => consulta.OrderBy(c => c.Codigo), _ => consulta.OrderByDescending(c => c.Fecha).ThenBy(c => c.Id) };
            return Results.Ok(new { total = consulta.Count(), datos = consulta.Skip((pagina - 1) * tamano).Take(tamano).Select(c => new { c.Id, c.Codigo, c.Tipo, c.Establecimiento, c.EstablecimientoId, c.TrabajadorId, c.Fecha, c.Relato, c.Estado, c.CreadoEn, c.CerradoEn, investigacionId = db.Investigaciones.Where(i => i.CasoId == c.Id).Select(i => (Guid?)i.Id).FirstOrDefault() }).ToArray(), pagina, tamano });
        }).Produces<PaginaCasosDTO>();
        grupo.MapGet("/{id:guid}", (Guid id, AccesoService acceso, CasoService service) => { acceso.Exigir(id); return Results.Ok(service.Obtener(id)); }).Produces<Caso>();
        grupo.MapPost("/", (CasoDTO dto, GiatepDbContext db, CasoService service) =>
        {
            var centro = db.Establecimientos.SingleOrDefault(e => e.Id == dto.EstablecimientoId && e.Activo) ?? throw new ValidationException("Selecciona un establecimiento activo.");
            ValidarTrabajador(dto, centro.Id, db);
            Texto(dto.Relato, 20, 5000, "Relato"); var caso = service.Crear(dto with { Establecimiento = centro.Nombre });
            return Results.Created($"/api/v1/casos/{caso.Id}", caso);
        }).RequireAuthorization("Prevencion").Produces<Caso>(201);
        grupo.MapPut("/{id:guid}", (Guid id, CasoDTO dto, AccesoService acceso, ICasoRepository repo, GiatepDbContext db) =>
        {
            acceso.Exigir(id); var caso = repo.Buscar(id)!;
            if (caso.Estado != EstadoCaso.Borrador || db.Investigaciones.Any(i => i.CasoId == id)) throw new ReglaNegocioException("Solo se editan los antecedentes antes de iniciar la investigación.");
            Texto(dto.Relato, 20, 5000, "Relato");
            if (dto.Fecha == default || dto.Fecha > Consultas.Hoy() || !Enum.IsDefined(dto.Tipo)) throw new ValidationException("Fecha o tipo inválidos.");
            var centro = db.Establecimientos.SingleOrDefault(e => e.Id == dto.EstablecimientoId && e.Activo) ?? throw new ValidationException("Establecimiento inválido.");
            ValidarTrabajador(dto, centro.Id, db);
            if (caso.Medidas.Any(m => m.Plazo < dto.Fecha)) throw new ValidationException("La fecha supera el plazo de una medida.");
            if (caso.Medidas.Count > 0 && caso.EstablecimientoId != centro.Id) throw new ReglaNegocioException("No se cambia de establecimiento con medidas asignadas.");
            if (caso.Denuncia is not null && dto.Denuncia is null) throw new ValidationException("Conserva los datos de la denuncia al editar el caso.");
            var actualizado = caso with { Establecimiento = centro.Nombre, EstablecimientoId = centro.Id, TrabajadorId = dto.TrabajadorId, Tipo = dto.Tipo, Fecha = dto.Fecha, Relato = dto.Relato.Trim(), Denuncia = Denuncias.Validar(dto.Denuncia, dto.Tipo, dto.Fecha) };
            repo.Guardar(actualizado); return Results.Ok(repo.Buscar(id));
        }).RequireAuthorization("Prevencion");
        grupo.MapPut("/{id:guid}/estado", (Guid id, CambiarEstadoDTO dto, AccesoService acceso, GiatepDbContext db, CasoService service) =>
        {
            acceso.Exigir(id);
            if (dto.Estado != EstadoCaso.Cerrado) throw new ReglaNegocioException("Inicia y cierra la investigación desde su módulo; después inicia su plan de acción.");
            return Results.Ok(service.CambiarEstado(id, dto.Estado));
        }).RequireAuthorization("Prevencion");
        grupo.MapPost("/{id:guid}/medidas", (Guid id, AccesoService acceso) => { acceso.Exigir(id); return Results.Problem(statusCode: 409, title: "Las medidas se definen dentro de la investigación y pasan al plan después de su cierre."); }).RequireAuthorization("Prevencion");
        grupo.MapPut("/{id:guid}/medidas/{medidaId:guid}/cumplimiento", (Guid id, Guid medidaId, AccesoService acceso, GiatepDbContext db, CasoService service) =>
        {
            acceso.Exigir(id); var medida = db.Medidas.SingleOrDefault(m => m.CasoId == id && m.Id == medidaId && m.Activa) ?? throw new KeyNotFoundException("La medida no existe.");
            if (acceso.EsJefatura && medida.UsuarioResponsableId != acceso.UsuarioId) return Results.Forbid();
            return Results.Problem(statusCode: 409, title: "Entrega la evidencia desde Planes de acción y solicita su verificación a Prevención.");
        }).RequireAuthorization("Prevencion");
        grupo.MapGet("/{id:guid}/observaciones", (Guid id, AccesoService acceso, GiatepDbContext db) =>
        {
            acceso.Exigir(id);
            var observaciones = db.Observaciones.Where(o => o.CasoId == id).ToArray().OrderByDescending(o => o.Fecha).ToArray();
            return Results.Ok(new { observaciones });
        });
        grupo.MapGet("/{id:guid}/investigacion", (Guid id, AccesoService acceso, GiatepDbContext db) =>
        {
            acceso.Exigir(id);
            return Results.Ok(new { causas = db.Causas.Where(c => c.CasoId == id).ToArray(), observaciones = db.Observaciones.Where(o => o.CasoId == id).OrderByDescending(o => o.Fecha).ToArray(), evidencias = db.Evidencias.Where(e => e.CasoId == id).Select(e => new { e.Id, e.Nombre, e.Tipo, e.Fecha, tamano = e.Tamano }).ToArray(), historial = db.Auditorias.Where(a => a.RegistroId == id.ToString()).OrderByDescending(a => a.Fecha).Take(100).Select(a => new { a.Id, a.Actor, a.Accion, a.Fecha, a.Cambios }).ToArray() });
        });
        grupo.MapPost("/{id:guid}/causas", (Guid id, AccesoService acceso) => { acceso.Exigir(id); return Results.Problem(statusCode: 409, title: "Registra hechos y relaciones en el árbol de la investigación."); }).RequireAuthorization("Prevencion");
        grupo.MapPost("/{id:guid}/observaciones", (Guid id, TextoDTO dto, AccesoService acceso, GiatepDbContext db) =>
        {
            var caso = acceso.Exigir(id); if (caso.Estado == EstadoCaso.Cerrado) throw new ReglaNegocioException("El expediente cerrado es de solo lectura.");
            Texto(dto.Texto, 5, 2000, "Observación"); var obs = new Observacion { Id = Guid.NewGuid(), CasoId = id, UsuarioId = acceso.UsuarioId, Autor = acceso.Actor.Identity!.Name!, Texto = dto.Texto.Trim(), Fecha = DateTimeOffset.UtcNow }; db.Observaciones.Add(obs); db.SaveChanges(); return Results.Created($"/api/v1/casos/{id}/investigacion", obs);
        }).RequireAuthorization("Prevencion");
        grupo.MapPost("/{id:guid}/evidencias", async (Guid id, HttpRequest request, AccesoService acceso, GiatepDbContext db, InvestigacionService investigaciones) =>
        {
            var caso = acceso.Exigir(id); var inv = db.Investigaciones.SingleOrDefault(i => i.CasoId == id) ?? throw new ReglaNegocioException("Inicia la investigación antes de adjuntar evidencias."); investigaciones.Abierta(inv.Id);
            if (!request.HasFormContentType) throw new ValidationException("Adjunta un archivo JPG, PNG o PDF.");
            var form = await request.ReadFormAsync(); var file = form.Files.GetFile("archivo");
            if (file is null || file.Length < 8 || file.Length > 5 * 1024 * 1024) throw new ValidationException("Archivo vacío o superior a 5 MB.");
            using var buffer = new MemoryStream(); await file.CopyToAsync(buffer); var bytes = buffer.ToArray(); var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mime = ext switch { ".pdf" when bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) => "application/pdf", ".png" when bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) => "image/png", ".jpg" or ".jpeg" when bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 => "image/jpeg", _ => throw new ValidationException("El contenido no corresponde a PDF, PNG o JPG.") };
            var nombre = Path.GetFileName(file.FileName); Texto(nombre, 1, 180, "Archivo"); var evidencia = new Evidencia { Id = Guid.NewGuid(), CasoId = id, InvestigacionId = inv.Id, Nombre = nombre, Tipo = mime, Contenido = bytes, UsuarioId = acceso.UsuarioId, Fecha = DateTimeOffset.UtcNow }; db.Evidencias.Add(evidencia); inv.ActualizadaEn = DateTimeOffset.UtcNow; db.SaveChanges(); return Results.Created($"/api/v1/casos/{id}/evidencias/{evidencia.Id}", new { evidencia.Id, evidencia.Nombre });
        }).RequireAuthorization("Prevencion").DisableAntiforgery();
        grupo.MapGet("/{id:guid}/evidencias/{evidenciaId:guid}", (Guid id, Guid evidenciaId, AccesoService acceso, GiatepDbContext db) =>
        {
            acceso.Exigir(id); var evidencia = db.Evidencias.SingleOrDefault(e => e.Id == evidenciaId && e.CasoId == id) ?? throw new KeyNotFoundException("Evidencia no encontrada."); acceso.Auditar("Evidencia", evidencia.Id.ToString(), "Descarga", "Descarga autorizada del expediente."); return Results.File(db.Archivos.Leer(evidencia), evidencia.Tipo, evidencia.Nombre);
        });
    }
}
