using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Giatep.Api.ValidacionesDTO;
namespace Giatep.Api.Services;

public sealed class CasoService(ICasoRepository repository)
{
    private static void Validar(object dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);
    }
    public Caso Crear(CasoDTO dto)
    {
        Validar(dto);
        if (!Enum.IsDefined(dto.Tipo)) throw new ValidationException("El tipo de caso no es válido.");
        if (dto.Fecha == default || dto.Fecha > Consultas.Hoy())
            throw new ValidationException("La fecha debe existir y no puede ser futura.");
        var denuncia = Denuncias.Validar(dto.Denuncia, dto.Tipo, dto.Fecha);
        var id = Guid.NewGuid();
        var caso = new Caso(id, $"GIA-{DateTime.UtcNow:yyyy}-{id.ToString("N")[..8].ToUpperInvariant()}",
            dto.Tipo, dto.Establecimiento.Trim(), dto.Fecha, dto.Relato.Trim(), EstadoCaso.Borrador,
            DateTimeOffset.UtcNow, [], dto.EstablecimientoId, dto.TrabajadorId, Denuncia: denuncia);
        repository.Guardar(caso);
        return repository.Buscar(id) ?? caso;
    }
    public Caso Obtener(Guid id) => repository.Buscar(id) ?? throw new KeyNotFoundException("El caso no existe.");
    public Caso CambiarEstado(Guid id, EstadoCaso destino)
    {
        var caso = Obtener(id);
        var permitido = (caso.Estado, destino) switch
        {
            (EstadoCaso.Borrador, EstadoCaso.EnInvestigacion) => true,
            (EstadoCaso.EnInvestigacion, EstadoCaso.EnSeguimiento) => caso.Medidas.Count > 0,
            (EstadoCaso.EnSeguimiento, EstadoCaso.Cerrado) => caso.Medidas.Count > 0 && caso.Medidas.All(m => m.Completada),
            _ => false
        };
        if (!permitido) throw new ReglaNegocioException("Transición no permitida. Para iniciar seguimiento debe haber medidas; para cerrar deben estar completadas.");
        var actualizado = caso with { Estado = destino, CerradoEn = destino == EstadoCaso.Cerrado ? DateTimeOffset.UtcNow : null };
        repository.Guardar(actualizado);
        return actualizado;
    }
    public Caso AgregarMedida(Guid id, MedidaDTO dto)
    {
        Validar(dto);
        var caso = Obtener(id);
        if (caso.Estado != EstadoCaso.EnInvestigacion) throw new ReglaNegocioException("Las medidas se definen durante la investigación.");
        if (dto.Plazo == default || dto.Plazo < caso.Fecha) throw new ValidationException("El plazo no puede ser anterior al caso.");
        var actualizado = caso with { Medidas = [.. caso.Medidas, new(Guid.NewGuid(), dto.Descripcion.Trim(), dto.Responsable.Trim(), dto.Plazo, false, dto.UsuarioResponsableId)] };
        repository.Guardar(actualizado);
        return actualizado;
    }
    public Caso CompletarMedida(Guid id, Guid medidaId)
    {
        var caso = Obtener(id);
        if (caso.Estado != EstadoCaso.EnSeguimiento) throw new ReglaNegocioException("El caso debe estar en seguimiento.");
        if (!caso.Medidas.Any(m => m.Id == medidaId)) throw new KeyNotFoundException("La medida no existe.");
        repository.CompletarMedida(id, medidaId);
        return Obtener(id);
    }
}
