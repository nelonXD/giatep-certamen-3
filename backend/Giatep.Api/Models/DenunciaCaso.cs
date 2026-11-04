namespace Giatep.Api.Models;

public sealed class Empleador
{
    public Guid Id { get; set; }
    public string Rut { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Comuna { get; set; } = "";
    public string? Telefono { get; set; }
    public string? ActividadEconomica { get; set; }
    public int? TrabajadoresHombres { get; set; }
    public int? TrabajadorasMujeres { get; set; }
    public string? Propiedad { get; set; }
    public string? TipoEmpresa { get; set; }
    public string? ActividadPrincipal { get; set; }
}

public sealed class Diat
{
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public TimeOnly? HoraAccidente { get; set; }
    public TimeOnly? HoraIngreso { get; set; }
    public TimeOnly? HoraSalida { get; set; }
    public string? AccidenteDireccion { get; set; }
    public string? AccidenteComuna { get; set; }
    public string? ActividadAccidente { get; set; }
    public string? LugarAccidente { get; set; }
    public string? TrabajoHabitual { get; set; }
    public string? RealizabaHabitual { get; set; }
    public string? ClasificacionAccidente { get; set; }
    public string? TipoAccidente { get; set; }
    public string? TipoTrayecto { get; set; }
    public string? MedioPrueba { get; set; }
    public string? DetallePrueba { get; set; }
}

public sealed class Diep
{
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public int? DuracionSintomas { get; set; }
    public string? DuracionSintomasUnidad { get; set; }
    public string? MolestiasPrevias { get; set; }
    public string? ParteCuerpo { get; set; }
    public string? ActividadSintomas { get; set; }
    public string? PuestoSintomas { get; set; }
    public string? CompanerosSintomas { get; set; }
    public string? Agentes { get; set; }
    public int? TiempoExposicion { get; set; }
    public string? TiempoExposicionUnidad { get; set; }
}
