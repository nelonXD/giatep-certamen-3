using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.ValidacionesDTO;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Services;
public record EntrevistaDTO(string Entrevistado, string Calidad, DateOnly Fecha, string Declaracion);
public record HechoDTO(string Descripcion, bool EsFinal);
public record RelacionDTO(Guid AntecedenteId, Guid ConsecuenciaId);
public record PropuestaDTO(Guid HechoId, string Descripcion, Guid UsuarioResponsableId, DateOnly Plazo);
public sealed class InvestigacionService(GiatepDbContext db, AccesoService acceso)
{
    private static void Texto(string? text, int min, int max, string campo) { if (text is null || text.Trim().Length < min || text.Length > max) throw new ValidationException($"{campo}: entre {min} y {max} caracteres."); }
    public Investigacion Obtener(Guid id) { var row = db.Investigaciones.SingleOrDefault(i => i.Id == id) ?? throw new KeyNotFoundException("La investigación no existe."); acceso.Exigir(row.CasoId); return row; }
    public Investigacion Abierta(Guid id) { var row = Obtener(id); if (row.Estado != "EnCurso") throw new ReglaNegocioException("La investigación está cerrada y no puede modificarse."); return row; }
    private void Guardar(Investigacion row) { row.ActualizadaEn = DateTimeOffset.UtcNow; db.SaveChanges(); }
    private static string Snapshot(CasoRegistro caso, string? trabajador, bool trabajadorReferenciaActual = false) => JsonSerializer.Serialize(new { denuncia = DenunciaAlmacen.Obtener(caso), trabajadorReferenciaActual, codigo = caso.Codigo, tipo = caso.Tipo.ToString(), establecimiento = caso.Establecimiento, establecimientoId = caso.EstablecimientoId, trabajador = trabajador ?? "Registro histórico", fecha = caso.Fecha, relatoInicial = caso.Relato }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    public Investigacion Iniciar(Guid casoId)
    {
        acceso.Exigir(casoId); var caso = db.Casos.Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Single(c => c.Id == casoId);
        if (caso.Estado != EstadoCaso.Borrador || db.Investigaciones.Any(i => i.CasoId == casoId)) throw new ReglaNegocioException("El caso ya tiene una investigación o no está disponible para iniciarla.");
        var row = new Investigacion { Id = Guid.NewGuid(), CasoId = casoId, Antecedentes = Snapshot(caso, db.Trabajadores.Where(t => t.Id == caso.TrabajadorId).Select(t => t.Nombre).FirstOrDefault()), IniciadaEn = DateTimeOffset.UtcNow };
        caso.Estado = EstadoCaso.EnInvestigacion; db.Investigaciones.Add(row);
        foreach (var evidencia in db.Evidencias.Where(e => e.CasoId == casoId && e.InvestigacionId == null)) evidencia.InvestigacionId = row.Id;
        Guardar(row); return row;
    }
    public object Detalle(Guid id)
    {
        var row = Obtener(id);
        return new { row.Id, row.CasoId, row.Estado, row.RelatoFinal, row.IniciadaEn, row.CerradaEn, row.PlanIniciadoEn, row.EsHistorica,
            antecedentes = JsonSerializer.Deserialize<JsonElement>(row.Antecedentes),
            entrevistas = db.Entrevistas.Where(e => e.InvestigacionId == id).OrderBy(e => e.Fecha).ToArray(),
            hechos = db.Hechos.Where(h => h.InvestigacionId == id).OrderBy(h => h.EsFinal).ThenBy(h => h.Id).ToArray(),
            relaciones = db.RelacionesCausales.Where(r => r.InvestigacionId == id).ToArray(),
            medidas = db.MedidasInvestigacion.Where(m => m.InvestigacionId == id).ToArray(),
            evidencias = db.Evidencias.Where(e => e.InvestigacionId == id).Select(e => new { e.Id, e.Nombre, e.Tipo, e.Fecha, tamano = e.Tamano }).ToArray(),
            causasPrevias = db.Causas.Where(c => c.CasoId == row.CasoId).Select(c => new { c.Categoria, c.Descripcion }).ToArray(),
            historial = db.Auditorias.Where(a => a.RegistroId == id.ToString()).OrderByDescending(a => a.Fecha).Take(100).Select(a => new { a.Actor, a.Accion, a.Fecha }).ToArray()
        };
    }
    public void Relato(Guid id, string texto) { var row = Abierta(id); Texto(texto, 20, 10000, "Relato final"); row.RelatoFinal = texto.Trim(); Guardar(row); }
    public Entrevista Entrevistar(Guid id, EntrevistaDTO dto, Guid? entrevistaId = null)
    {
        var row = Abierta(id); Texto(dto.Entrevistado, 3, 120, "Persona entrevistada"); Texto(dto.Declaracion, 20, 5000, "Declaración");
        if (!new[] { "Trabajador", "Testigo", "Jefatura", "Otro" }.Contains(dto.Calidad) || dto.Fecha == default || dto.Fecha > Consultas.Hoy() || dto.Fecha < db.Casos.Single(c => c.Id == row.CasoId).Fecha) throw new ValidationException("Calidad o fecha de entrevista inválidas; debe estar entre el evento y hoy.");
        var entrevista = entrevistaId.HasValue ? db.Entrevistas.SingleOrDefault(e => e.Id == entrevistaId && e.InvestigacionId == id) ?? throw new KeyNotFoundException() : new Entrevista { Id = Guid.NewGuid(), InvestigacionId = id };
        entrevista.Entrevistado = dto.Entrevistado.Trim(); entrevista.Calidad = dto.Calidad; entrevista.Fecha = dto.Fecha; entrevista.Declaracion = dto.Declaracion.Trim();
        if (!entrevistaId.HasValue) db.Entrevistas.Add(entrevista); Guardar(row); return entrevista;
    }
    public Hecho Hecho(Guid id, HechoDTO dto, Guid? hechoId = null)
    {
        var row = Abierta(id); Texto(dto.Descripcion, 10, 1000, "Hecho");
        if (dto.EsFinal && db.Hechos.Any(h => h.InvestigacionId == id && h.EsFinal && h.Id != hechoId)) throw new ReglaNegocioException("Ya existe un hecho final. Solo puede haber uno.");
        if (dto.EsFinal && hechoId.HasValue && (db.RelacionesCausales.Any(r => r.AntecedenteId == hechoId) || db.MedidasInvestigacion.Any(m => m.HechoId == hechoId))) throw new ReglaNegocioException("El hecho tiene relaciones como antecedente o medidas asociadas. No puede convertirse en hecho final.");
        var hecho = hechoId.HasValue ? db.Hechos.SingleOrDefault(h => h.Id == hechoId && h.InvestigacionId == id) ?? throw new KeyNotFoundException() : new Hecho { Id = Guid.NewGuid(), InvestigacionId = id };
        hecho.Descripcion = dto.Descripcion.Trim(); hecho.EsFinal = dto.EsFinal;
        if (!hechoId.HasValue) db.Hechos.Add(hecho); Guardar(row); return hecho;
    }
    public RelacionCausal Relacion(Guid id, RelacionDTO dto)
    {
        var row = Abierta(id); var hechos = db.Hechos.Where(h => h.InvestigacionId == id).ToArray();
        if (dto.AntecedenteId == dto.ConsecuenciaId || !hechos.Any(h => h.Id == dto.AntecedenteId) || !hechos.Any(h => h.Id == dto.ConsecuenciaId)) throw new ValidationException("Selecciona dos hechos diferentes de esta investigación.");
        if (hechos.Single(h => h.Id == dto.AntecedenteId).EsFinal) throw new ReglaNegocioException("El hecho final no puede actuar como antecedente.");
        var edges = db.RelacionesCausales.Where(r => r.InvestigacionId == id).ToList();
        if (edges.Any(r => r.AntecedenteId == dto.AntecedenteId && r.ConsecuenciaId == dto.ConsecuenciaId)) throw new ReglaNegocioException("La relación ya existe.");
        if (Alcanza(dto.ConsecuenciaId, dto.AntecedenteId, edges)) throw new ReglaNegocioException("La relación crearía un ciclo. El árbol debe avanzar hacia el hecho final.");
        var edge = new RelacionCausal { Id = Guid.NewGuid(), InvestigacionId = id, AntecedenteId = dto.AntecedenteId, ConsecuenciaId = dto.ConsecuenciaId }; db.RelacionesCausales.Add(edge); Guardar(row); return edge;
    }
    public void QuitarRelacion(Guid id, Guid edgeId) { var row = Abierta(id); var edge = db.RelacionesCausales.SingleOrDefault(r => r.Id == edgeId && r.InvestigacionId == id) ?? throw new KeyNotFoundException(); db.RelacionesCausales.Remove(edge); Guardar(row); }
    public MedidaInvestigacion Propuesta(Guid id, Guid? propuestaId, PropuestaDTO dto)
    {
        var row = Abierta(id); var caso = db.Casos.Single(c => c.Id == row.CasoId); Texto(dto.Descripcion, 10, 1000, "Medida");
        if (!db.Hechos.Any(h => h.Id == dto.HechoId && h.InvestigacionId == id && !h.EsFinal)) throw new ValidationException("La medida debe vincularse a un hecho antecedente de esta investigación.");
        var responsable = db.Usuarios.SingleOrDefault(u => u.Id == dto.UsuarioResponsableId && u.Activo && (u.Rol == "Prevencion" || u.Rol == "Jefatura" && u.EstablecimientoId == caso.EstablecimientoId)) ?? throw new ValidationException("Responsable no autorizado para el establecimiento.");
        if (dto.Plazo == default || dto.Plazo < caso.Fecha) throw new ValidationException("El plazo no puede preceder al evento.");
        var medida = propuestaId.HasValue ? db.MedidasInvestigacion.SingleOrDefault(m => m.Id == propuestaId && m.InvestigacionId == id) ?? throw new KeyNotFoundException() : new MedidaInvestigacion { Id = Guid.NewGuid(), InvestigacionId = id };
        medida.HechoId = dto.HechoId; medida.Descripcion = dto.Descripcion.Trim(); medida.UsuarioResponsableId = responsable.Id; medida.Responsable = responsable.Nombre; medida.Plazo = dto.Plazo;
        if (!propuestaId.HasValue) db.MedidasInvestigacion.Add(medida); Guardar(row); return medida;
    }
    private static bool Alcanza(Guid start, Guid target, IEnumerable<RelacionCausal> edges)
    {
        var visitados = new HashSet<Guid>(); var pendientes = new Stack<Guid>(); pendientes.Push(start);
        while (pendientes.TryPop(out var actual)) { if (actual == target) return true; if (!visitados.Add(actual)) continue; foreach (var edge in edges.Where(e => e.AntecedenteId == actual)) pendientes.Push(edge.ConsecuenciaId); } return false;
    }
    public void Cerrar(Guid id)
    {
        var row = Abierta(id); Texto(row.RelatoFinal, 20, 10000, "Relato final");
        var hechos = db.Hechos.Where(h => h.InvestigacionId == id).ToArray(); var edges = db.RelacionesCausales.Where(r => r.InvestigacionId == id).ToArray(); var final = hechos.SingleOrDefault(h => h.EsFinal);
        if (!db.Entrevistas.Any(e => e.InvestigacionId == id) || !db.Evidencias.Any(e => e.InvestigacionId == id)) throw new ReglaNegocioException("Completa al menos una entrevista y una evidencia antes de cerrar.");
        if (hechos.Length < 2 || final is null || hechos.Any(h => h.Id != final.Id && !Alcanza(h.Id, final.Id, edges))) throw new ReglaNegocioException("Registra un hecho final y conecta todos los hechos antecedentes hacia él en el árbol.");
        var medidas = db.MedidasInvestigacion.Where(m => m.InvestigacionId == id).ToArray();
        if (medidas.Length == 0 || medidas.Any(m => !m.HechoId.HasValue || !hechos.Any(h => h.Id == m.HechoId && !h.EsFinal))) throw new ReglaNegocioException("Todas las medidas deben estar vinculadas a hechos antecedentes; registra al menos una.");
        var centro = db.Casos.Single(c => c.Id == row.CasoId).EstablecimientoId;
        foreach (var medida in medidas) if (!db.Usuarios.Any(u => u.Id == medida.UsuarioResponsableId && u.Activo && (u.Rol == "Prevencion" || u.Rol == "Jefatura" && u.EstablecimientoId == centro))) throw new ReglaNegocioException("Una medida tiene responsable inactivo o fuera del establecimiento. Actualiza sus datos.");
        row.Estado = "Cerrada"; row.CerradaEn = DateTimeOffset.UtcNow; Guardar(row);
    }
    public void IniciarPlan(Guid id)
    {
        var row = Obtener(id); var caso = db.Casos.Single(c => c.Id == row.CasoId);
        if (row.Estado != "Cerrada" || row.PlanIniciadoEn.HasValue || caso.Estado != EstadoCaso.EnInvestigacion) throw new ReglaNegocioException("El plan solo se inicia una vez, después de cerrar la investigación.");
        foreach (var propuesta in db.MedidasInvestigacion.Where(m => m.InvestigacionId == id).ToArray()) {
            if (!db.Usuarios.Any(u => u.Id == propuesta.UsuarioResponsableId && u.Activo && (u.Rol == "Prevencion" || u.Rol == "Jefatura" && u.EstablecimientoId == caso.EstablecimientoId))) throw new ReglaNegocioException("Un responsable dejó de estar autorizado. Revisa la cuenta antes de iniciar el plan.");
            var medida = db.Medidas.SingleOrDefault(m => m.PropuestaId == propuesta.Id) ?? new MedidaRegistro { Id = Guid.NewGuid(), CasoId = caso.Id, PropuestaId = propuesta.Id };
            if (db.Entry(medida).State == EntityState.Detached) db.Medidas.Add(medida);
            medida.Activa = true; medida.Descripcion = propuesta.Descripcion; medida.UsuarioResponsableId = propuesta.UsuarioResponsableId; medida.Responsable = propuesta.Responsable; medida.Plazo = propuesta.Plazo; medida.Completada = false;
        }
        caso.Estado = EstadoCaso.EnSeguimiento; row.PlanIniciadoEn = DateTimeOffset.UtcNow; Guardar(row);
    }
    public static void ImportarAntecedentesPrevios(GiatepDbContext db)
    {
        var trabajadores = db.Trabajadores.ToDictionary(t => t.Id, t => t.Nombre);
        var reparados = false;
        var incompletos = (from inv in db.Investigaciones
                           join caso in db.Casos on inv.CasoId equals caso.Id
                           where inv.EsHistorica && inv.Antecedentes.Contains("Registro histórico")
                           select new { Investigacion = inv, caso.TrabajadorId }).ToArray();
        foreach (var registro in incompletos) {
            var inv = registro.Investigacion;
            var snapshot = System.Text.Json.Nodes.JsonNode.Parse(inv.Antecedentes)!;
            if (snapshot["trabajador"]?.GetValue<string>() == "Registro histórico" && registro.TrabajadorId.HasValue && trabajadores.TryGetValue(registro.TrabajadorId.Value, out var nombre)) {
                snapshot["trabajador"] = nombre; snapshot["trabajadorReferenciaActual"] = true;
                inv.Antecedentes = snapshot.ToJsonString(); reparados = true;
            }
        }
        var idsPendientes = db.Casos.Where(c => c.Estado != EstadoCaso.Borrador && !db.Investigaciones.Any(i => i.CasoId == c.Id)).Select(c => c.Id).ToArray();
        var pendientes = idsPendientes.Length == 0 ? [] : db.Casos.Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Include(c => c.Medidas).AsSplitQuery().Where(c => idsPendientes.Contains(c.Id)).ToArray();
        foreach (var caso in pendientes) {
            var historica = caso.Estado != EstadoCaso.EnInvestigacion;
            var inv = new Investigacion { Id = Guid.NewGuid(), CasoId = caso.Id, Estado = historica ? "Historica" : "EnCurso", EsHistorica = true, Antecedentes = Snapshot(caso, caso.TrabajadorId.HasValue ? trabajadores.GetValueOrDefault(caso.TrabajadorId.Value) : null, true), IniciadaEn = caso.CreadoEn, ActualizadaEn = DateTimeOffset.UtcNow };
            db.Investigaciones.Add(inv);
            foreach (var evidencia in db.Evidencias.Where(e => e.CasoId == caso.Id && e.InvestigacionId == null)) evidencia.InvestigacionId = inv.Id;
            if (!historica) foreach (var medida in caso.Medidas.Where(m => m.Activa && m.UsuarioResponsableId.HasValue)) {
                var propuesta = new MedidaInvestigacion { Id = Guid.NewGuid(), InvestigacionId = inv.Id, Descripcion = medida.Descripcion, UsuarioResponsableId = medida.UsuarioResponsableId!.Value, Responsable = medida.Responsable, Plazo = medida.Plazo };
                db.MedidasInvestigacion.Add(propuesta); medida.Activa = false; medida.PropuestaId = propuesta.Id;
            }
        }
        // En expedientes históricos no consta la fecha de inicio del plan: no inferirla de la creación del caso.
        var fechasInferidas = db.Investigaciones.Where(i => i.Estado == "Historica" && i.PlanIniciadoEn != null).ToArray();
        foreach (var inv in fechasInferidas) inv.PlanIniciadoEn = null;
        if (pendientes.Length > 0 || fechasInferidas.Length > 0 || reparados) db.SaveChanges();
    }
}
