using Giatep.Api.Models;
namespace Giatep.Api.ValidacionesDTO;
// Contratos públicos para OpenAPI. Coinciden con los campos de las proyecciones de lectura.
public record UsuarioPublicoDTO(Guid Id, string Cuenta, string Nombre, string Rol, Guid? EstablecimientoId, bool Activo);
public record SesionDTO(string Token, UsuarioPublicoDTO Usuario, DateTime Expira);
public record CasoListadoDTO(Guid Id, string Codigo, TipoCaso Tipo, string Establecimiento, Guid? EstablecimientoId, Guid? TrabajadorId, DateOnly Fecha, string Relato, EstadoCaso Estado, DateTimeOffset CreadoEn, DateTimeOffset? CerradoEn, Guid? InvestigacionId);
public record PaginaCasosDTO(CasoListadoDTO[] Datos, int Total, int Pagina, int Tamano);
public record IndicadorDTO(string Nombre, double Valor, string Unidad, string Formula, string Pregunta, int? Cantidad = null, int? Denominador = null, int? Muestra = null);
public record PuntoDTO(string Etiqueta, int Valor);
public record DashboardDTO(IndicadorDTO[] Indicadores, PuntoDTO[] Meses, PuntoDTO[] Tipos, PuntoDTO[] Establecimientos, PuntoDTO[] Estados, string Interpretacion, DateTimeOffset Actualizado);
public record ReportePaginaDTO(string Titulo, string Filtros, string[] Columnas, object?[][] Filas, int Total, int TotalCasos, int Pagina, int Tamano, PuntoDTO[]? Grafico);
