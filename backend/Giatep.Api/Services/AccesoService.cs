using System.Security.Claims;
using Giatep.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Services;
public sealed class AccesoService(GiatepDbContext db, IHttpContextAccessor accessor)
{
    public ClaimsPrincipal Actor => accessor.HttpContext!.User;
    public Guid UsuarioId => Guid.Parse(Actor.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public bool EsJefatura => Actor.IsInRole("Jefatura");
    public Guid? EstablecimientoId => Guid.TryParse(Actor.FindFirstValue("establecimiento"), out var id) ? id : null;
    public IQueryable<CasoRegistro> Casos => EsJefatura ? db.Casos.Where(c => c.EstablecimientoId == EstablecimientoId) : db.Casos;
    public CasoRegistro Exigir(Guid id) => Casos.AsNoTracking().SingleOrDefault(c => c.Id == id) ?? throw new KeyNotFoundException("No se encontró el caso en tu ámbito de acceso.");
    public void Auditar(string entidad, string registro, string accion, string cambios)
    {
        db.Auditorias.Add(new() { UsuarioId = UsuarioId, Actor = Actor.Identity?.Name ?? "", Entidad = entidad, RegistroId = registro, Accion = accion, Cambios = cambios, Fecha = DateTimeOffset.UtcNow });
        db.SaveChanges();
    }
}
