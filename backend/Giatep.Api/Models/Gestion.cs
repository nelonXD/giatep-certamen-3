namespace Giatep.Api.Models;
public sealed class Establecimiento
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public bool Activo { get; set; } = true;
}
public sealed class Trabajador
{
    public string? FechaIngreso { get; set; }
    public int? Antiguedad { get; set; }
    public string? AntiguedadUnidad { get; set; }
    public string? TipoContrato { get; set; }
    public string? TipoIngreso { get; set; }
    public string? CategoriaOcupacional { get; set; }
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string? Run { get; set; }
    public string? Direccion { get; set; }
    public string? Comuna { get; set; }
    public string? Telefono { get; set; }
    public string? Sexo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Nacionalidad { get; set; }
    public string? PuebloOriginario { get; set; }
    public string? OtroPueblo { get; set; }

    public Guid EstablecimientoId { get; set; }
    public bool Activo { get; set; } = true;
}
public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Cuenta { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = "";
    public Guid? EstablecimientoId { get; set; }
    public bool Activo { get; set; } = true;
    public int Fallos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }
}
public sealed class Causa
{
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public string Categoria { get; set; } = "";
    public string Descripcion { get; set; } = "";
}
public sealed class Observacion
{
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Autor { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateTimeOffset Fecha { get; set; }
}
public sealed class Evidencia
{
    public Guid? InvestigacionId { get; set; }
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public byte[] Contenido { get; set; } = [];
    public Guid UsuarioId { get; set; }
    public DateTimeOffset Fecha { get; set; }
}
public sealed class Auditoria
{
    public long Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Actor { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string RegistroId { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Cambios { get; set; } = "";
    public DateTimeOffset Fecha { get; set; }
}
