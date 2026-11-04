namespace Giatep.Api.Models;
public sealed class Investigacion
{
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public string Estado { get; set; } = "EnCurso";
    public string Antecedentes { get; set; } = "";
    public string RelatoFinal { get; set; } = "";
    public DateTimeOffset IniciadaEn { get; set; }
    public DateTimeOffset ActualizadaEn { get; set; }
    public DateTimeOffset? CerradaEn { get; set; }
    public DateTimeOffset? PlanIniciadoEn { get; set; }
    public bool EsHistorica { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class Entrevista
{
    public Guid Id { get; set; }
    public Guid InvestigacionId { get; set; }
    public string Entrevistado { get; set; } = "";
    public string Calidad { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public string Declaracion { get; set; } = "";
}
public sealed class Hecho
{
    public Guid Id { get; set; }
    public Guid InvestigacionId { get; set; }
    public string Descripcion { get; set; } = "";
    public bool EsFinal { get; set; }
}
public sealed class RelacionCausal
{
    public Guid Id { get; set; }
    public Guid InvestigacionId { get; set; }
    public Guid AntecedenteId { get; set; }
    public Guid ConsecuenciaId { get; set; }
}
public sealed class MedidaInvestigacion
{
    public Guid Id { get; set; }
    public Guid InvestigacionId { get; set; }
    public Guid? HechoId { get; set; }
    public string Descripcion { get; set; } = "";
    public Guid UsuarioResponsableId { get; set; }
    public string Responsable { get; set; } = "";
    public DateOnly Plazo { get; set; }
}
