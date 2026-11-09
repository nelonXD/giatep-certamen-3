using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Endpoints;
public static class InvestigacionEndpoints
{
    public static void MapInvestigaciones(this WebApplication app)
    {
        app.MapPost("/api/v1/casos/{casoId:guid}/investigaciones", (Guid casoId, InvestigacionService service) => { var row = service.Iniciar(casoId); return Results.Created($"/api/v1/investigaciones/{row.Id}", new { row.Id, row.CasoId }); }).RequireAuthorization("Prevencion").WithTags("Investigaciones");
        var group = app.MapGroup("/api/v1/investigaciones").RequireAuthorization("Operacion").WithTags("Investigaciones");
        group.MapGet("/", (AccesoService acceso, GiatepDbContext db, Guid? casoId, string? estado, string? buscar, int pagina = 1, bool soloMias = false) => {
            if (pagina < 1 || pagina > 100000 || buscar?.Length > 150) throw new System.ComponentModel.DataAnnotations.ValidationException("Página o búsqueda inválidas.");
            var usuarioId = acceso.UsuarioId;
            var registrosPropios = db.Auditorias.Where(a => a.Entidad == nameof(Investigacion) && a.Accion == "Added" && a.UsuarioId == usuarioId).Select(a => a.RegistroId);
            var query = from i in db.Investigaciones.AsNoTracking() join c in acceso.Casos.AsNoTracking() on i.CasoId equals c.Id
                where !soloMias || registrosPropios.Contains(i.Id.ToString())
                where (!casoId.HasValue || i.CasoId == casoId) && (string.IsNullOrEmpty(estado) || i.Estado == estado) && (string.IsNullOrEmpty(buscar) || c.Codigo.Contains(buscar) || c.Establecimiento.Contains(buscar))
                select new { i.Id, i.CasoId, c.Codigo, c.Establecimiento, i.Estado, i.EsHistorica, i.IniciadaEn, i.CerradaEn, i.PlanIniciadoEn };
            return Results.Ok(new { total = query.Count(), datos = query.OrderByDescending(i => i.IniciadaEn).ThenBy(i => i.Id).Skip((pagina - 1) * 15).Take(15).ToArray() });
        });
        group.MapGet("/{id:guid}", (Guid id, InvestigacionService service) => Results.Ok(service.Detalle(id)));
        group.MapPut("/{id:guid}/relato", (Guid id, TextoDTO dto, InvestigacionService service) => { service.Relato(id, dto.Texto); return Results.NoContent(); }).RequireAuthorization("Prevencion");
        group.MapPost("/{id:guid}/entrevistas", (Guid id, EntrevistaDTO dto, InvestigacionService service) => { var row = service.Entrevistar(id, dto); return Results.Created($"/api/v1/investigaciones/{id}", row); }).RequireAuthorization("Prevencion");
        group.MapPut("/{id:guid}/entrevistas/{entrevistaId:guid}", (Guid id, Guid entrevistaId, EntrevistaDTO dto, InvestigacionService service) => Results.Ok(service.Entrevistar(id, dto, entrevistaId))).RequireAuthorization("Prevencion");
        group.MapPost("/{id:guid}/hechos", (Guid id, HechoDTO dto, InvestigacionService service) => { var row = service.Hecho(id, dto); return Results.Created($"/api/v1/investigaciones/{id}", row); }).RequireAuthorization("Prevencion");
        group.MapPut("/{id:guid}/hechos/{hechoId:guid}", (Guid id, Guid hechoId, HechoDTO dto, InvestigacionService service) => Results.Ok(service.Hecho(id, dto, hechoId))).RequireAuthorization("Prevencion");
        group.MapPost("/{id:guid}/relaciones", (Guid id, RelacionDTO dto, InvestigacionService service) => Results.Ok(service.Relacion(id, dto))).RequireAuthorization("Prevencion");
        group.MapDelete("/{id:guid}/relaciones/{relacionId:guid}", (Guid id, Guid relacionId, InvestigacionService service) => { service.QuitarRelacion(id, relacionId); return Results.NoContent(); }).RequireAuthorization("Prevencion");
        group.MapPost("/{id:guid}/medidas", (Guid id, PropuestaDTO dto, InvestigacionService service) => { var row = service.Propuesta(id, null, dto); return Results.Created($"/api/v1/investigaciones/{id}", row); }).RequireAuthorization("Prevencion");
        group.MapPut("/{id:guid}/medidas/{medidaId:guid}", (Guid id, Guid medidaId, PropuestaDTO dto, InvestigacionService service) => Results.Ok(service.Propuesta(id, medidaId, dto))).RequireAuthorization("Prevencion");
        group.MapPost("/{id:guid}/cerrar", (Guid id, InvestigacionService service) => { service.Cerrar(id); return Results.NoContent(); }).RequireAuthorization("Prevencion");
        group.MapPost("/{id:guid}/plan", (Guid id, InvestigacionService service) => { service.IniciarPlan(id); return Results.NoContent(); }).RequireAuthorization("Prevencion");
    }
}
