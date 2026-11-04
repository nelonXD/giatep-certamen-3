namespace Giatep.Api.Models;

public enum TipoCaso { Accidente, EnfermedadProfesional }
public enum EstadoCaso { Borrador, EnInvestigacion, EnSeguimiento, Cerrado }
public record Medida(Guid Id, string Descripcion, string Responsable, DateOnly Plazo, bool Completada, Guid? UsuarioResponsableId = null);
public record Caso(Guid Id, string Codigo, TipoCaso Tipo, string Establecimiento,
    DateOnly Fecha, string Relato, EstadoCaso Estado, DateTimeOffset CreadoEn,
    IReadOnlyList<Medida> Medidas, Guid? EstablecimientoId = null, Guid? TrabajadorId = null, DateTimeOffset? CerradoEn = null, Giatep.Api.ValidacionesDTO.DenunciaDTO? Denuncia = null, Guid? EmpleadorId = null);
