using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
namespace Giatep.Api.ValidacionesDTO;

public record CasoDTO(
    [Required, StringLength(120, MinimumLength = 3)] string Establecimiento,
    TipoCaso Tipo, DateOnly Fecha,
    [Required, StringLength(5000, MinimumLength = 20)] string Relato,
    Guid? EstablecimientoId = null, Guid? TrabajadorId = null, DenunciaDTO? Denuncia = null);
public record CambiarEstadoDTO(EstadoCaso Estado);
public record MedidaDTO(
    [Required, StringLength(1000, MinimumLength = 10)] string Descripcion,
    [Required, StringLength(120, MinimumLength = 3)] string Responsable, DateOnly Plazo, Guid? UsuarioResponsableId = null);
