using Giatep.Api.Models;
using Giatep.Api.Services;

namespace Giatep.Api.Endpoints;

public record DocumentoEvidenciaDTO(string Titulo, string Texto, DistribucionOcr? Distribucion = null, RecorteOcr[]? Recortes = null);
public record DocumentoTemporalDTO(string Titulo, string Texto, string NombreImagen);
public static class EvidenciaOcrEndpoints
{
    private static Evidencia Fuente(Guid id, Guid evidenciaId, InvestigacionService investigaciones, GiatepDbContext db)
    {
        investigaciones.Abierta(id);
        var row = db.Evidencias.SingleOrDefault(e => e.Id == evidenciaId && e.InvestigacionId == id)
            ?? throw new KeyNotFoundException("La imagen no pertenece a esta investigación.");
        if (row.Tipo is not ("image/png" or "image/jpeg")) throw new ReglaNegocioException("Selecciona una evidencia PNG o JPG para reconocer su texto.");
        return row;
    }
    public static void MapEvidenciaOcr(this WebApplication app)
    {
        var temporal = app.MapGroup("/api/v1/investigaciones/{id:guid}/ocr").RequireAuthorization("Prevencion").WithTags("OCR temporal");
        temporal.MapPost("/escanear", async (Guid id, HttpRequest request, InvestigacionService investigaciones, IOcrLocalService ocr, CancellationToken token) => {
            investigaciones.Abierta(id);
            if (!request.HasFormContentType) throw new System.ComponentModel.DataAnnotations.ValidationException("Selecciona una imagen PNG o JPG.");
            var form = await request.ReadFormAsync(token);
            var file = form.Files.GetFile("archivo");
            if (file is null || file.Length < 8 || file.Length > 1_000_000) throw new System.ComponentModel.DataAnnotations.ValidationException("Selecciona una imagen de hasta 1 MB.");
            using var buffer = new MemoryStream(); await file.CopyToAsync(buffer, token); var bytes = buffer.ToArray();
            var png = bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
            var jpg = bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
            if (!png && !jpg) throw new System.ComponentModel.DataAnnotations.ValidationException("El archivo debe ser una imagen PNG o JPG válida.");
            try { return Results.Ok(await ocr.Leer(bytes, token)); }
            catch (OcrNoDisponibleException ex) { return Results.Problem(statusCode: 503, title: ex.Message); }
        }).RequireRateLimiting("ocr");
        temporal.MapPost("/documento-con-original", async (Guid id, HttpRequest request, InvestigacionService investigaciones, GiatepDbContext db, AccesoService acceso, CancellationToken token) => {
            var inv = investigaciones.Abierta(id);
            if (!request.HasFormContentType) throw new System.ComponentModel.DataAnnotations.ValidationException("Adjunta la imagen original.");
            var form = await request.ReadFormAsync(token);
            var titulo = form["titulo"].ToString(); var texto = form["texto"].ToString();
            DistribucionOcr? distribucion = null;
            var layoutJson = form["distribucion"].ToString();
            if (!string.IsNullOrWhiteSpace(layoutJson)) {
                if (layoutJson.Length > 200000) throw new System.ComponentModel.DataAnnotations.ValidationException("Distribución demasiado grande.");
                try { distribucion = System.Text.Json.JsonSerializer.Deserialize<DistribucionOcr>(layoutJson, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)); }
                catch (System.Text.Json.JsonException) { throw new System.ComponentModel.DataAnnotations.ValidationException("Distribución inválida."); }
                if (distribucion is not null) DocumentoEvidencia.ValidarDistribucion(distribucion);
            }
            CasoEndpoints.Texto(titulo, 3, 120, "Título del documento");
            CasoEndpoints.Texto(texto, 1, 30000, "Texto revisado");
            var file = form.Files.GetFile("archivo");
            if (file is null || file.Length < 8 || file.Length > 1_000_000) throw new System.ComponentModel.DataAnnotations.ValidationException("Selecciona una imagen de hasta 1 MB.");
            using var buffer = new MemoryStream(); await file.CopyToAsync(buffer, token); var bytes = buffer.ToArray();
            var png = bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
            var jpg = bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
            if (!png && !jpg) throw new System.ComponentModel.DataAnnotations.ValidationException("El archivo debe ser PNG o JPG.");
            var nombre = Path.GetFileName(file.FileName);
            CasoEndpoints.Texto(nombre, 1, 180, "Nombre de la imagen");
            RecorteOcr[]? recortes = null;
            var cropJson = form["recortes"].ToString();
            if (!string.IsNullOrWhiteSpace(cropJson)) {
                if (cropJson.Length > 10000) throw new System.ComponentModel.DataAnnotations.ValidationException("Demasiados recortes.");
                try { recortes = System.Text.Json.JsonSerializer.Deserialize<RecorteOcr[]>(cropJson, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)); }
                catch (System.Text.Json.JsonException) { throw new System.ComponentModel.DataAnnotations.ValidationException("Recortes inválidos."); }
            }
            var fecha = DateTimeOffset.UtcNow;
            var original = new Evidencia { Id = Guid.NewGuid(), CasoId = inv.CasoId, InvestigacionId = id, Nombre = nombre, Tipo = png ? "image/png" : "image/jpeg", Contenido = bytes, UsuarioId = acceso.UsuarioId, Fecha = fecha };
            var documento = new Evidencia { Id = Guid.NewGuid(), CasoId = inv.CasoId, InvestigacionId = id, Tipo = DocumentoEvidencia.Mime, UsuarioId = acceso.UsuarioId, Fecha = fecha };
            documento.Nombre = $"Transcripcion-{documento.Id:N}.docx";
            documento.Contenido = DocumentoEvidencia.Crear(titulo.Trim(), texto.Trim(), nombre, original.Id, acceso.Actor.Identity?.Name ?? "", distribucion, recortes, bytes);
            db.Evidencias.AddRange(original, documento); inv.ActualizadaEn = fecha;
            db.SaveChanges();
            acceso.Auditar("Evidencia", documento.Id.ToString(), "DocumentoOCR", $"Transcripción revisada vinculada al original {original.Id}.");
            return Results.Created($"/api/v1/casos/{inv.CasoId}/evidencias/{documento.Id}", new { documento.Id, documento.Nombre, OriginalId = original.Id });
        });
        temporal.MapPost("/documento", (Guid id, DocumentoTemporalDTO dto, InvestigacionService investigaciones, GiatepDbContext db, AccesoService acceso) => {
            var inv = investigaciones.Abierta(id);
            CasoEndpoints.Texto(dto.Titulo, 3, 120, "Título del documento");
            CasoEndpoints.Texto(dto.Texto, 1, 30000, "Texto revisado");
            CasoEndpoints.Texto(dto.NombreImagen, 1, 180, "Nombre de la imagen");
            var documentoId = Guid.NewGuid();
            var documento = new Evidencia { Id = documentoId, CasoId = inv.CasoId, InvestigacionId = id, Nombre = $"Transcripcion-{documentoId:N}.docx", Tipo = DocumentoEvidencia.Mime,
                Contenido = DocumentoEvidencia.Crear(dto.Titulo.Trim(), dto.Texto.Trim(), Path.GetFileName(dto.NombreImagen), null, acceso.Actor.Identity?.Name ?? ""), UsuarioId = acceso.UsuarioId, Fecha = DateTimeOffset.UtcNow };
            db.Evidencias.Add(documento); inv.ActualizadaEn = documento.Fecha; db.SaveChanges();
            return Results.Created($"/api/v1/casos/{inv.CasoId}/evidencias/{documento.Id}", new { documento.Id, documento.Nombre });
        });
        var group = app.MapGroup("/api/v1/investigaciones/{id:guid}/evidencias/{evidenciaId:guid}")
            .RequireAuthorization("Prevencion").WithTags("OCR de evidencias");
        group.MapPost("/escanear", async (Guid id, Guid evidenciaId, InvestigacionService investigaciones,
            GiatepDbContext db, IOcrLocalService ocr, AccesoService acceso, CancellationToken token) =>
        {
            var fuente = Fuente(id, evidenciaId, investigaciones, db);
            try
            {
                var lectura = await ocr.Leer(db.Archivos.Leer(fuente), token);
                acceso.Auditar("Evidencia", fuente.Id.ToString(), "LecturaOCR", "Reconocimiento OCR; proveedor configurado en el servidor.");
                return Results.Ok(lectura);
            }
            catch (OcrNoDisponibleException ex) { return Results.Problem(statusCode: 503, title: ex.Message); }
        }).RequireRateLimiting("ocr");
        group.MapPost("/documento", (Guid id, Guid evidenciaId, DocumentoEvidenciaDTO dto,
            InvestigacionService investigaciones, GiatepDbContext db, AccesoService acceso) =>
        {
            var fuente = Fuente(id, evidenciaId, investigaciones, db);
            CasoEndpoints.Texto(dto.Titulo, 3, 120, "Título del documento");
            CasoEndpoints.Texto(dto.Texto, 1, 30000, "Texto revisado");
            var documentoId = Guid.NewGuid();
            var documento = new Evidencia
            {
                Id = documentoId, CasoId = fuente.CasoId, InvestigacionId = id,
                Nombre = $"Transcripcion-{documentoId:N}.docx", Tipo = DocumentoEvidencia.Mime,
                Contenido = DocumentoEvidencia.Crear(dto.Titulo.Trim(), dto.Texto.Trim(), fuente.Nombre, fuente.Id, acceso.Actor.Identity?.Name ?? "", dto.Distribucion, dto.Recortes, dto.Recortes is { Length: > 0 } ? db.Archivos.Leer(fuente) : null),
                UsuarioId = acceso.UsuarioId, Fecha = DateTimeOffset.UtcNow
            };
            db.Evidencias.Add(documento);
            db.Investigaciones.Single(i => i.Id == id).ActualizadaEn = documento.Fecha;
            db.SaveChanges();
            acceso.Auditar("Evidencia", documento.Id.ToString(), "DocumentoOCR", $"Documento revisado a partir de evidencia {fuente.Id}.");
            return Results.Created($"/api/v1/casos/{fuente.CasoId}/evidencias/{documento.Id}", new { documento.Id, documento.Nombre });
        });
    }
}
