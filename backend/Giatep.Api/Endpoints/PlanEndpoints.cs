using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Endpoints;

public record RevisionMedidaDTO(bool Aprobada, string Observacion);
public static class PlanEndpoints
{
    private static MedidaRegistro Medida(Guid casoId, Guid id, AccesoService acceso, GiatepDbContext db)
    {
        var caso = acceso.Exigir(casoId);
        if (caso.Estado != EstadoCaso.EnSeguimiento) throw new ReglaNegocioException("El plan debe estar en seguimiento para registrar cambios.");
        return db.Medidas.SingleOrDefault(m => m.Id == id && m.CasoId == casoId && m.Activa) ?? throw new KeyNotFoundException("Medida no encontrada.");
    }
    public static void MapPlanes(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/planes").RequireAuthorization("Operacion").WithTags("Planes de acción");
        group.MapGet("/", (AccesoService acceso, GiatepDbContext db, string? buscar, string? estado, int pagina = 1, bool soloMios = false) => {
            if (pagina < 1 || pagina > 100000 || buscar?.Length > 150 || estado is not (null or "" or "PorIniciar" or "EnSeguimiento" or "Cerrado")) throw new ValidationException("Filtros inválidos.");
            var usuarioId = acceso.UsuarioId;
            var query = from c in acceso.Casos.AsNoTracking()
                        join i in db.Investigaciones.AsNoTracking() on c.Id equals i.CasoId
                        where !soloMios || db.Medidas.Any(m => m.CasoId == c.Id && m.Activa && m.UsuarioResponsableId == usuarioId)
                        where i.Estado == "Cerrada" || i.Estado == "Historica"
                        where string.IsNullOrEmpty(buscar) || c.Codigo.Contains(buscar) || c.Establecimiento.Contains(buscar)
                        where string.IsNullOrEmpty(estado) || (estado == "PorIniciar" && c.Estado == EstadoCaso.EnInvestigacion) || (estado == "EnSeguimiento" && c.Estado == EstadoCaso.EnSeguimiento) || (estado == "Cerrado" && c.Estado == EstadoCaso.Cerrado)
                        select new { casoId = c.Id, c.Codigo, c.Establecimiento, c.Estado, investigacionId = i.Id, i.PlanIniciadoEn,
                            totalMedidas = db.Medidas.Count(m => m.CasoId == c.Id && m.Activa),
                            verificadas = db.Medidas.Count(m => m.CasoId == c.Id && m.Activa && m.Completada),
                            porRevisar = db.Medidas.Count(m => m.CasoId == c.Id && m.Activa && !m.Completada && m.ImplementadaEn != null),
                            vencidas = db.Medidas.Count(m => m.CasoId == c.Id && m.Activa && !m.Completada && m.Plazo < Consultas.Hoy()) };
            return Results.Ok(new { total = query.Count(), datos = query.OrderByDescending(c => c.PlanIniciadoEn).ThenBy(c => c.Codigo).Skip((pagina - 1) * 15).Take(15).ToArray() });
        });
        group.MapGet("/{casoId:guid}", (Guid casoId, AccesoService acceso, GiatepDbContext db) => {
            var caso = acceso.Exigir(casoId);
            var inv = db.Investigaciones.AsNoTracking().Where(i => i.CasoId == casoId).Select(i => new { i.Id, i.Estado, i.PlanIniciadoEn }).SingleOrDefault() ?? throw new KeyNotFoundException("El caso todavía no tiene investigación.");
            var medidas = db.Medidas.AsNoTracking().Where(m => m.CasoId == casoId && m.Activa).OrderBy(m => m.Plazo).ThenBy(m => m.Id)
                .Select(m => new { m.Id, m.Descripcion, m.Responsable, m.UsuarioResponsableId, m.Plazo, m.Completada, m.ImplementadaEn, m.ComentarioCumplimiento, m.RevisadaEn, m.Revision,
                    revisor = db.Usuarios.Where(u => u.Id == m.RevisorId).Select(u => u.Nombre).FirstOrDefault(),
                    hecho = db.MedidasInvestigacion.Where(p => p.Id == m.PropuestaId).Select(p => db.Hechos.Where(h => h.Id == p.HechoId).Select(h => h.Descripcion).FirstOrDefault()).FirstOrDefault() }).ToArray();
            var ids = medidas.Select(m => m.Id).ToArray();
            var evidencias = db.EvidenciasMedidas.AsNoTracking().Where(e => ids.Contains(e.MedidaId)).Select(e => new { e.Id, e.MedidaId, e.Nombre, e.Fecha, tamano = e.Tamano }).ToArray();
            return Results.Ok(new { caso.Id, caso.Codigo, caso.Establecimiento, caso.Estado, caso.CerradoEn, investigacion = inv, medidas, evidencias });
        });
        group.MapPost("/{casoId:guid}/medidas/{id:guid}/entrega", async (Guid casoId, Guid id, HttpRequest request, AccesoService acceso, GiatepDbContext db) => {
            var medida = Medida(casoId, id, acceso, db);
            if (acceso.EsJefatura && medida.UsuarioResponsableId != acceso.UsuarioId) return Results.Forbid();
            if (medida.Completada || medida.ImplementadaEn.HasValue) throw new ReglaNegocioException("La medida ya fue enviada o verificada.");
            if (!request.HasFormContentType) throw new ValidationException("Adjunta evidencia de cumplimiento.");
            var form = await request.ReadFormAsync();
            var comentario = form["comentario"].ToString(); CasoEndpoints.Texto(comentario, 10, 2000, "Detalle de implementación");
            var file = form.Files.GetFile("archivo");
            if (file is null || file.Length < 8 || file.Length > 5 * 1024 * 1024) throw new ValidationException("Adjunta un PDF, PNG o JPG de hasta 5 MB.");
            using var buffer = new MemoryStream(); await file.CopyToAsync(buffer); var bytes = buffer.ToArray();
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var tipo = ext switch { ".pdf" when bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) => "application/pdf", ".png" when bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) => "image/png", ".jpg" or ".jpeg" when bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 => "image/jpeg", _ => throw new ValidationException("El contenido no corresponde a PDF, PNG o JPG.") };
            var nombre = Path.GetFileName(file.FileName); CasoEndpoints.Texto(nombre, 1, 180, "Archivo");
            var evidencia = new EvidenciaMedida { Id = Guid.NewGuid(), MedidaId = id, UsuarioId = acceso.UsuarioId, Fecha = DateTimeOffset.UtcNow, Nombre = nombre, Tipo = tipo, Contenido = bytes };
            db.EvidenciasMedidas.Add(evidencia); medida.ImplementadaEn = evidencia.Fecha; medida.ComentarioCumplimiento = comentario.Trim(); db.SaveChanges();
            return Results.Ok(new { evidencia.Id });
        }).DisableAntiforgery();
        group.MapPut("/{casoId:guid}/medidas/{id:guid}/revision", (Guid casoId, Guid id, RevisionMedidaDTO dto, AccesoService acceso, GiatepDbContext db) => {
            var medida = Medida(casoId, id, acceso, db);
            if (medida.Completada || !medida.ImplementadaEn.HasValue) throw new ReglaNegocioException("Primero debe entregarse la implementación con su evidencia.");
            CasoEndpoints.Texto(dto.Observacion, 10, 2000, "Observación de revisión");
            if (!db.EvidenciasMedidas.Any(e => e.MedidaId == id && e.Fecha >= medida.ImplementadaEn)) throw new ReglaNegocioException("Falta la evidencia de esta entrega.");
            medida.Completada = dto.Aprobada; medida.RevisadaEn = DateTimeOffset.UtcNow; medida.RevisorId = acceso.UsuarioId; medida.Revision = dto.Observacion.Trim();
            if (!dto.Aprobada) medida.ImplementadaEn = null;
            db.SaveChanges(); return Results.NoContent();
        }).RequireAuthorization("Prevencion");
        group.MapGet("/{casoId:guid}/evidencias/{id:guid}", (Guid casoId, Guid id, AccesoService acceso, GiatepDbContext db) => {
            acceso.Exigir(casoId);
            var evidencia = db.EvidenciasMedidas.SingleOrDefault(e => e.Id == id && db.Medidas.Any(m => m.Id == e.MedidaId && m.CasoId == casoId && m.Activa)) ?? throw new KeyNotFoundException("Evidencia no encontrada.");
            acceso.Auditar("EvidenciaMedida", id.ToString(), "Descarga", "Descarga autorizada del plan de acción.");
            return Results.File(db.Archivos.Leer(evidencia), evidencia.Tipo, evidencia.Nombre);
        });
    }
}
