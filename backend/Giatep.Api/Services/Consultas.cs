using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
namespace Giatep.Api.Services;
public static class Consultas
{
    public static DateOnly Hoy() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Pacific SA Standard Time").DateTime);
    public static IQueryable<CasoRegistro> Filtrar(IQueryable<CasoRegistro> query, string? buscar, string? estado, string? tipo, Guid? establecimientoId, DateOnly? desde, DateOnly? hasta)
    {
        if (desde > hasta) throw new ValidationException("La fecha inicial supera la final.");
        if (!string.IsNullOrWhiteSpace(buscar)) { if (buscar.Length > 150) throw new ValidationException("Búsqueda demasiado extensa."); query = query.Where(c => c.Codigo.Contains(buscar) || c.Establecimiento.Contains(buscar) || c.Relato.Contains(buscar)); }
        if (!string.IsNullOrEmpty(estado)) { if (!Enum.TryParse<EstadoCaso>(estado, out var value) || !Enum.IsDefined(value)) throw new ValidationException("Estado inválido."); query = query.Where(c => c.Estado == value); }
        if (!string.IsNullOrEmpty(tipo)) { if (!Enum.TryParse<TipoCaso>(tipo, out var value) || !Enum.IsDefined(value)) throw new ValidationException("Tipo inválido."); query = query.Where(c => c.Tipo == value); }
        if (establecimientoId.HasValue) query = query.Where(c => c.EstablecimientoId == establecimientoId);
        if (desde.HasValue) query = query.Where(c => c.Fecha >= desde);
        if (hasta.HasValue) query = query.Where(c => c.Fecha <= hasta);
        return query;
    }
}
