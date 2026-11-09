using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Giatep.Api.Models;

public sealed class CasoRegistro
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public TipoCaso Tipo { get; set; }
    public string Establecimiento { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public string Relato { get; set; } = "";
    // Archivo de compatibilidad de denuncias anteriores a la normalización.
    public string? DenunciaJson { get; set; }
    public Guid? EmpleadorId { get; set; }
    public DateOnly? FechaEmision { get; set; }
    public string? CodigoOrganismo { get; set; }
    public string? Folio { get; set; }
    public string? DenuncianteNombre { get; set; }
    public string? DenuncianteRun { get; set; }
    public string? DenuncianteTelefono { get; set; }
    public string? DenuncianteClasificacion { get; set; }
    public string? FirmaNombre { get; set; }
    public Diat? Diat { get; set; }
    public Diep? Diep { get; set; }
    public Trabajador? Trabajador { get; set; }
    public Empleador? Empleador { get; set; }
    public EstadoCaso Estado { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public List<MedidaRegistro> Medidas { get; set; } = [];
    public Guid? EstablecimientoId { get; set; }
    public Guid? TrabajadorId { get; set; }
    public DateTimeOffset? CerradoEn { get; set; }
}

public sealed class MedidaRegistro
{
    public bool Activa { get; set; } = true;
    public Guid? PropuestaId { get; set; }
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public string Descripcion { get; set; } = "";
    public string Responsable { get; set; } = "";
    public DateOnly Plazo { get; set; }
    public bool Completada { get; set; }
    public Guid? UsuarioResponsableId { get; set; }
    public DateTimeOffset? ImplementadaEn { get; set; }
    public string? ComentarioCumplimiento { get; set; }
    public DateTimeOffset? RevisadaEn { get; set; }
    public Guid? RevisorId { get; set; }
    public string? Revision { get; set; }
}

public sealed class EvidenciaMedida : Giatep.Api.Services.IArchivoEvidencia
{
    public Guid Id { get; set; }
    public Guid MedidaId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Ruta { get; set; } = "";
    public long Tamano { get; set; }
    public string Sha256 { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public byte[] Contenido { get; set; } = [];
}

public sealed class GiatepDbContext(DbContextOptions<GiatepDbContext> options, IHttpContextAccessor? accessor = null, AlmacenEvidencias? almacen = null) : DbContext(options)
{
    public AlmacenEvidencias Archivos { get; } = almacen ?? new AlmacenEvidencias(Path.Combine(Path.GetTempPath(), $"giatep-pruebas-{Environment.ProcessId}"));
    public DbSet<Empleador> Empleadores => Set<Empleador>();
    public DbSet<Diat> Diats => Set<Diat>();
    public DbSet<Diep> Dieps => Set<Diep>();
    public DbSet<CasoRegistro> Casos => Set<CasoRegistro>();
    public DbSet<MedidaRegistro> Medidas => Set<MedidaRegistro>();
    public DbSet<EvidenciaMedida> EvidenciasMedidas => Set<EvidenciaMedida>();
    public DbSet<Establecimiento> Establecimientos => Set<Establecimiento>();
    public DbSet<Trabajador> Trabajadores => Set<Trabajador>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Causa> Causas => Set<Causa>();
    public DbSet<Observacion> Observaciones => Set<Observacion>();
    public DbSet<Evidencia> Evidencias => Set<Evidencia>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Investigacion> Investigaciones => Set<Investigacion>();
    public DbSet<Entrevista> Entrevistas => Set<Entrevista>();
    public DbSet<Hecho> Hechos => Set<Hecho>();
    public DbSet<RelacionCausal> RelacionesCausales => Set<RelacionCausal>();
    public DbSet<MedidaInvestigacion> MedidasInvestigacion => Set<MedidaInvestigacion>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ChangeTracker.DetectChanges();
        var nuevos = ChangeTracker.Entries().Where(e => e.State == EntityState.Added && e.Entity is IArchivoEvidencia).Select(e => (IArchivoEvidencia)e.Entity).ToArray();
        var creados = new List<IArchivoEvidencia>();
        try {
            foreach (var archivo in nuevos) {
                Archivos.Guardar(archivo, archivo is Evidencia ? "investigaciones" : "planes");
                creados.Add(archivo);
            }

        var actor = accessor?.HttpContext?.User;
        var logs = ChangeTracker.Entries().Where(e => e.Entity is not Auditoria && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new Auditoria {
                UsuarioId = Guid.TryParse(actor?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null,
                Actor = actor?.Identity?.Name ?? "Inicialización", Entidad = e.Metadata.ClrType.Name,
                RegistroId = e.Property("Id").CurrentValue?.ToString() ?? "", Accion = e.State.ToString(), Fecha = DateTimeOffset.UtcNow,
                Cambios = JsonSerializer.Serialize(e.Properties.Where(p => p.Metadata.Name is not ("PasswordHash" or "Contenido") && (e.State != EntityState.Modified || p.IsModified))
                    .Select(p => new { campo = p.Metadata.Name, antes = e.State == EntityState.Added ? null : p.OriginalValue, despues = e.State == EntityState.Deleted ? null : p.CurrentValue }))
            }).ToArray();
        Auditorias.AddRange(logs);
        var resultado = base.SaveChanges(acceptAllChangesOnSuccess);
        foreach (var archivo in nuevos) archivo.Contenido = [];
        return resultado;
        } catch {
            foreach (var archivo in creados) { Archivos.Eliminar(archivo.Ruta); archivo.Ruta = ""; }
            foreach (var log in ChangeTracker.Entries<Auditoria>().Where(e => e.State == EntityState.Added).ToArray()) log.State = EntityState.Detached;
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var evidenciaMedida = model.Entity<EvidenciaMedida>();
        evidenciaMedida.ToTable("EvidenciasMedidas", t => t.HasCheckConstraint("CK_EvidenciasMedidas_Metadatos", "[Tamano] BETWEEN 8 AND 5242880 AND LEN([Ruta]) > 0 AND LEN([Sha256]) = 64"));
        evidenciaMedida.HasKey(e => e.Id);
        evidenciaMedida.Property(e => e.Nombre).HasMaxLength(180);
        evidenciaMedida.Property(e => e.Tipo).HasMaxLength(80);
        evidenciaMedida.HasOne<MedidaRegistro>().WithMany().HasForeignKey(e => e.MedidaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<MedidaRegistro>().Property(m => m.ComentarioCumplimiento).HasMaxLength(2000);
        model.Entity<MedidaRegistro>().Property(m => m.Revision).HasMaxLength(2000);
        var empleadores = model.Entity<Empleador>();
        empleadores.ToTable("Empleadores"); empleadores.HasKey(e => e.Id);
        empleadores.Property(e => e.Rut).HasMaxLength(9).IsRequired(); empleadores.HasIndex(e => e.Rut).IsUnique();
        empleadores.Property(e => e.Nombre).HasMaxLength(2000).IsRequired(); empleadores.Property(e => e.Direccion).HasMaxLength(2000).IsRequired(); empleadores.Property(e => e.Comuna).HasMaxLength(2000).IsRequired();
        foreach (var nombre in new[] { "Telefono", "ActividadEconomica", "Propiedad", "TipoEmpresa", "ActividadPrincipal" }) empleadores.Property<string>(nombre).HasMaxLength(2000);
        var diat = model.Entity<Diat>();
        diat.ToTable("DIAT"); diat.HasKey(d => d.Id);
        diat.HasIndex(d => d.CasoId).IsUnique();
        foreach (var propiedad in typeof(Diat).GetProperties().Where(p => p.PropertyType == typeof(string))) diat.Property<string>(propiedad.Name).HasMaxLength(2000);
        var diep = model.Entity<Diep>();
        diep.ToTable("DIEP"); diep.HasKey(d => d.Id);
        diep.HasIndex(d => d.CasoId).IsUnique();
        foreach (var propiedad in typeof(Diep).GetProperties().Where(p => p.PropertyType == typeof(string))) diep.Property<string>(propiedad.Name).HasMaxLength(2000);
        model.Entity<Trabajador>().Property(t => t.Run).HasMaxLength(9);
        model.Entity<Trabajador>().HasIndex(t => t.Run).IsUnique().HasFilter("[Run] IS NOT NULL");
        foreach (var nombre in new[] { "Direccion", "Comuna", "Telefono", "Sexo", "Nacionalidad", "PuebloOriginario", "OtroPueblo" }) model.Entity<Trabajador>().Property<string>(nombre).HasMaxLength(2000);
        foreach (var nombre in new[] { "FechaIngreso", "AntiguedadUnidad", "TipoContrato", "TipoIngreso", "CategoriaOcupacional" }) model.Entity<Trabajador>().Property<string>(nombre).HasMaxLength(2000);
        var casos = model.Entity<CasoRegistro>();
        casos.ToTable("Casos", table =>
        {
            table.HasCheckConstraint("CK_Casos_Tipo", "[Tipo] IN (0, 1)");
            table.HasCheckConstraint("CK_Casos_Estado", "[Estado] IN (0, 1, 2, 3)");
            table.HasCheckConstraint("CK_Casos_Establecimiento", "LEN(LTRIM(RTRIM([Establecimiento]))) >= 3");
            table.HasCheckConstraint("CK_Casos_Relato", "LEN(LTRIM(RTRIM([Relato]))) >= 20");
        });
        casos.HasKey(c => c.Id);
        casos.HasOne(c => c.Diat).WithOne().HasForeignKey<Diat>(d => d.CasoId).OnDelete(DeleteBehavior.Restrict);
        casos.HasOne(c => c.Diep).WithOne().HasForeignKey<Diep>(d => d.CasoId).OnDelete(DeleteBehavior.Restrict);
        foreach (var nombre in new[] { "CodigoOrganismo", "Folio", "DenuncianteNombre", "DenuncianteRun", "DenuncianteTelefono", "DenuncianteClasificacion", "FirmaNombre" }) casos.Property<string>(nombre).HasMaxLength(2000);
        casos.HasOne(c => c.Empleador).WithMany().HasForeignKey(c => c.EmpleadorId).OnDelete(DeleteBehavior.Restrict);
        casos.Property(c => c.Codigo).HasMaxLength(32).IsRequired();
        casos.HasIndex(c => c.Codigo).IsUnique();
        casos.Property(c => c.Establecimiento).HasMaxLength(120).IsRequired();
        casos.Property(c => c.Relato).HasMaxLength(5000).IsRequired();
        casos.HasIndex(c => new { c.Estado, c.Fecha });
        casos.HasMany(c => c.Medidas).WithOne().HasForeignKey(m => m.CasoId).OnDelete(DeleteBehavior.Cascade);
        casos.HasOne<Establecimiento>().WithMany().HasForeignKey(c => c.EstablecimientoId).OnDelete(DeleteBehavior.Restrict);
        casos.HasOne(c => c.Trabajador).WithMany().HasForeignKey(c => c.TrabajadorId).OnDelete(DeleteBehavior.Restrict);

        var medidas = model.Entity<MedidaRegistro>();
        medidas.ToTable("Medidas", table =>
        {
            table.HasCheckConstraint("CK_Medidas_Descripcion", "LEN(LTRIM(RTRIM([Descripcion]))) >= 10");
            table.HasCheckConstraint("CK_Medidas_Responsable", "LEN(LTRIM(RTRIM([Responsable]))) >= 3");
        });
        medidas.HasKey(m => m.Id);
        medidas.Property(m => m.Activa).HasDefaultValue(true);
        medidas.Property(m => m.Descripcion).HasMaxLength(1000).IsRequired();
        medidas.Property(m => m.Responsable).HasMaxLength(120).IsRequired();
        medidas.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioResponsableId).OnDelete(DeleteBehavior.Restrict);

        model.Entity<Establecimiento>().ToTable("Establecimientos").HasKey(e => e.Id);
        model.Entity<Establecimiento>().Property(e => e.Nombre).HasMaxLength(120).IsRequired();
        model.Entity<Establecimiento>().HasIndex(e => e.Nombre).IsUnique();
        model.Entity<Establecimiento>().Property(e => e.Direccion).HasMaxLength(200).IsRequired();
        model.Entity<Trabajador>().ToTable("Trabajadores").HasKey(e => e.Id);
        model.Entity<Trabajador>().Property(e => e.Codigo).HasMaxLength(30).IsRequired();
        model.Entity<Trabajador>().HasIndex(e => e.Codigo).IsUnique();
        model.Entity<Trabajador>().Property(e => e.Nombre).HasMaxLength(120).IsRequired();
        model.Entity<Trabajador>().Property(e => e.Cargo).HasMaxLength(100).IsRequired();
        model.Entity<Trabajador>().HasOne<Establecimiento>().WithMany().HasForeignKey(e => e.EstablecimientoId).OnDelete(DeleteBehavior.Restrict);
        foreach (var tipoArchivo in new[] { typeof(Evidencia), typeof(EvidenciaMedida) }) {
            model.Entity(tipoArchivo).Property<string>("Ruta").HasMaxLength(300).IsRequired();
            model.Entity(tipoArchivo).Property<string>("Sha256").HasMaxLength(64).IsRequired();
            model.Entity(tipoArchivo).HasIndex("Ruta").IsUnique();
        }
        var usuarios = model.Entity<Usuario>();
        usuarios.ToTable("Usuarios", t => t.HasCheckConstraint("CK_Usuarios_Rol", "[Rol] IN ('Administrador','Prevencion','Jefatura') AND ([Rol] <> 'Jefatura' OR [EstablecimientoId] IS NOT NULL)"));
        usuarios.HasKey(e => e.Id);
        usuarios.Property(e => e.Cuenta).HasMaxLength(50).IsRequired();
        usuarios.HasIndex(e => e.Cuenta).IsUnique();
        usuarios.Property(e => e.Nombre).HasMaxLength(120).IsRequired();
        usuarios.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        usuarios.Property(e => e.Rol).HasMaxLength(20).IsRequired();
        usuarios.HasOne<Establecimiento>().WithMany().HasForeignKey(e => e.EstablecimientoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Causa>().ToTable("Causas").HasKey(e => e.Id);
        model.Entity<Causa>().Property(e => e.Categoria).HasMaxLength(80).IsRequired();
        model.Entity<Causa>().Property(e => e.Descripcion).HasMaxLength(1000).IsRequired();
        model.Entity<Causa>().HasOne<CasoRegistro>().WithMany().HasForeignKey(e => e.CasoId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Observacion>().ToTable("Observaciones").HasKey(e => e.Id);
        model.Entity<Observacion>().Property(e => e.Texto).HasMaxLength(2000).IsRequired();
        model.Entity<Observacion>().Property(e => e.Autor).HasMaxLength(120).IsRequired();
        model.Entity<Observacion>().HasOne<CasoRegistro>().WithMany().HasForeignKey(e => e.CasoId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Observacion>().HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Evidencia>().ToTable("Evidencias", t => t.HasCheckConstraint("CK_Evidencias_Metadatos", "[Tamano] BETWEEN 8 AND 5242880 AND LEN([Ruta]) > 0 AND LEN([Sha256]) = 64")).HasKey(e => e.Id);
        model.Entity<Evidencia>().Property(e => e.Nombre).HasMaxLength(180).IsRequired();
        model.Entity<Evidencia>().Property(e => e.Tipo).HasMaxLength(80).IsRequired();
        model.Entity<Evidencia>().HasOne<CasoRegistro>().WithMany().HasForeignKey(e => e.CasoId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Evidencia>().HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Auditoria>().ToTable("Auditorias").HasKey(e => e.Id);
        model.Entity<Auditoria>().Property(e => e.Actor).HasMaxLength(120).IsRequired();
        model.Entity<Auditoria>().Property(e => e.Entidad).HasMaxLength(80).IsRequired();
        model.Entity<Auditoria>().Property(e => e.RegistroId).HasMaxLength(80).IsRequired();
        model.Entity<Auditoria>().Property(e => e.Accion).HasMaxLength(40).IsRequired();
        model.Entity<Auditoria>().HasIndex(e => e.Fecha);
        // No FK de auditoría al usuario: conservar la trazabilidad aunque se desactive una cuenta.
        var investigaciones = model.Entity<Investigacion>();
        investigaciones.ToTable("Investigaciones", t => t.HasCheckConstraint("CK_Investigaciones_Estado", "[Estado] IN ('EnCurso','Cerrada','Historica')"));
        investigaciones.HasKey(i => i.Id); investigaciones.HasIndex(i => i.CasoId).IsUnique();
        investigaciones.Property(i => i.Estado).HasMaxLength(20).IsRequired();
        investigaciones.Property(i => i.Antecedentes).IsRequired(); investigaciones.Property(i => i.RelatoFinal).HasMaxLength(10000).IsRequired();
        investigaciones.Property(i => i.Version).IsRowVersion();
        investigaciones.HasOne<CasoRegistro>().WithMany().HasForeignKey(i => i.CasoId).OnDelete(DeleteBehavior.Restrict);
        var entrevistas = model.Entity<Entrevista>(); entrevistas.ToTable("Entrevistas"); entrevistas.HasKey(e => e.Id);
        entrevistas.Property(e => e.Entrevistado).HasMaxLength(120).IsRequired(); entrevistas.Property(e => e.Calidad).HasMaxLength(30).IsRequired(); entrevistas.Property(e => e.Declaracion).HasMaxLength(5000).IsRequired();
        entrevistas.HasOne<Investigacion>().WithMany().HasForeignKey(e => e.InvestigacionId).OnDelete(DeleteBehavior.Restrict);
        var hechos = model.Entity<Hecho>(); hechos.ToTable("Hechos"); hechos.HasKey(h => h.Id); hechos.Property(h => h.Descripcion).HasMaxLength(1000).IsRequired();
        hechos.HasIndex(h => h.InvestigacionId).HasFilter("[EsFinal] = 1").IsUnique();
        hechos.HasOne<Investigacion>().WithMany().HasForeignKey(h => h.InvestigacionId).OnDelete(DeleteBehavior.Restrict);
        var relaciones = model.Entity<RelacionCausal>(); relaciones.ToTable("RelacionesCausales", t => t.HasCheckConstraint("CK_Relaciones_NoAutoRelacion", "[AntecedenteId] <> [ConsecuenciaId]")); relaciones.HasKey(r => r.Id);
        relaciones.HasIndex(r => new { r.AntecedenteId, r.ConsecuenciaId }).IsUnique();
        relaciones.HasOne<Investigacion>().WithMany().HasForeignKey(r => r.InvestigacionId).OnDelete(DeleteBehavior.Restrict);
        relaciones.HasOne<Hecho>().WithMany().HasForeignKey(r => r.AntecedenteId).OnDelete(DeleteBehavior.Restrict);
        relaciones.HasOne<Hecho>().WithMany().HasForeignKey(r => r.ConsecuenciaId).OnDelete(DeleteBehavior.Restrict);
        var propuestas = model.Entity<MedidaInvestigacion>(); propuestas.ToTable("MedidasInvestigacion"); propuestas.HasKey(m => m.Id);
        propuestas.Property(m => m.Descripcion).HasMaxLength(1000).IsRequired(); propuestas.Property(m => m.Responsable).HasMaxLength(120).IsRequired();
        propuestas.HasOne<Investigacion>().WithMany().HasForeignKey(m => m.InvestigacionId).OnDelete(DeleteBehavior.Restrict);
        propuestas.HasOne<Hecho>().WithMany().HasForeignKey(m => m.HechoId).OnDelete(DeleteBehavior.Restrict);
        propuestas.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioResponsableId).OnDelete(DeleteBehavior.Restrict);
        medidas.HasOne<MedidaInvestigacion>().WithMany().HasForeignKey(m => m.PropuestaId).OnDelete(DeleteBehavior.Restrict);
        medidas.HasIndex(m => m.PropuestaId).IsUnique().HasFilter("[PropuestaId] IS NOT NULL");
        model.Entity<Evidencia>().HasOne<Investigacion>().WithMany().HasForeignKey(e => e.InvestigacionId).OnDelete(DeleteBehavior.Restrict);
    }
}
