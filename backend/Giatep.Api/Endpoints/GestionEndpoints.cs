using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.AspNetCore.Identity;
namespace Giatep.Api.Endpoints;
public record CentroDTO(string Nombre, string Direccion, bool Activo);
public record TrabajadorDTO(string Codigo, string Nombre, string Cargo, Guid EstablecimientoId, bool Activo);
public record UsuarioDTO(string Cuenta, string Nombre, string Rol, Guid? EstablecimientoId, bool Activo, string? Password);
public static class GestionEndpoints
{
    public static void MapGestion(this WebApplication app)
    {
        app.MapGet("/api/v1/opciones", (GiatepDbContext db, AccesoService acceso) => Results.Ok(new {
            empleadores = db.Empleadores.Where(e => !acceso.EsJefatura || db.Casos.Any(c => c.EmpleadorId == e.Id && c.EstablecimientoId == acceso.EstablecimientoId)).ToArray().OrderBy(e => e.Nombre).ToArray(),
            establecimientos = db.Establecimientos.Where(e => e.Activo && (!acceso.EsJefatura || e.Id == acceso.EstablecimientoId)).ToArray().OrderBy(e => e.Nombre).ToArray(),
            trabajadores = db.Trabajadores.Where(t => t.Activo && (!acceso.EsJefatura || t.EstablecimientoId == acceso.EstablecimientoId)).Select(t => new { t.Id, t.Nombre, t.Codigo, t.Cargo, t.EstablecimientoId, t.Run, t.Direccion, t.Comuna, t.Telefono, t.Sexo, t.FechaNacimiento, t.Nacionalidad, t.PuebloOriginario, t.OtroPueblo }).ToArray().OrderBy(t => t.Nombre).ToArray(),
            responsables = db.Usuarios.Where(u => u.Activo && (u.Rol == "Prevencion" || u.Rol == "Jefatura") && (!acceso.EsJefatura || u.EstablecimientoId == acceso.EstablecimientoId || u.Rol == "Prevencion")).Select(u => new { u.Id, u.Nombre, u.Rol, u.EstablecimientoId }).ToArray(),
            categorias = new[] { "Entorno", "Organización", "Ergonomía", "Equipamiento", "Procedimiento" }
        })).RequireAuthorization().WithTags("Catálogos");
        var admin = app.MapGroup("/api/v1/admin").RequireAuthorization("Administrador").WithTags("Administración");
        admin.MapGet("/establecimientos", (GiatepDbContext db) => db.Establecimientos.OrderBy(e => e.Nombre).ToArray());
        admin.MapPost("/establecimientos", (CentroDTO dto, GiatepDbContext db) => GuardarCentro(null, dto, db));
        admin.MapPut("/establecimientos/{id:guid}", (Guid id, CentroDTO dto, GiatepDbContext db) => GuardarCentro(id, dto, db));
        admin.MapGet("/trabajadores", (GiatepDbContext db, string? buscar, int pagina = 1) => {
            if (pagina < 1 || pagina > 100000) throw new ValidationException("Página inválida.");
            var q = db.Trabajadores.Where(t => string.IsNullOrEmpty(buscar) || t.Nombre.Contains(buscar) || t.Codigo.Contains(buscar));
            return Results.Ok(new { total = q.Count(), datos = q.OrderBy(t => t.Nombre).Skip((pagina - 1) * 15).Take(15).ToArray() });
        });
        admin.MapPost("/trabajadores", (TrabajadorDTO dto, GiatepDbContext db) => GuardarTrabajador(null, dto, db));
        admin.MapPut("/trabajadores/{id:guid}", (Guid id, TrabajadorDTO dto, GiatepDbContext db) => GuardarTrabajador(id, dto, db));
        admin.MapGet("/usuarios", (GiatepDbContext db) => db.Usuarios.OrderBy(u => u.Cuenta).Select(u => new { u.Id, u.Cuenta, u.Nombre, u.Rol, u.EstablecimientoId, u.Activo }).ToArray());
        admin.MapPost("/usuarios", (UsuarioDTO dto, GiatepDbContext db, AccesoService acceso) => GuardarUsuario(null, dto, db, acceso));
        admin.MapPut("/usuarios/{id:guid}", (Guid id, UsuarioDTO dto, GiatepDbContext db, AccesoService acceso) => GuardarUsuario(id, dto, db, acceso));
        admin.MapGet("/auditoria", (GiatepDbContext db, int pagina = 1) => {
            if (pagina < 1 || pagina > 100000) throw new ValidationException("Página inválida.");
            return Results.Ok(new { total = db.Auditorias.Count(), datos = db.Auditorias.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).Skip((pagina - 1) * 20).Take(20).ToArray() });
        });
    }
    private static IResult GuardarCentro(Guid? id, CentroDTO dto, GiatepDbContext db)
    {
        CasoEndpoints.Texto(dto.Nombre, 3, 120, "Nombre"); CasoEndpoints.Texto(dto.Direccion, 3, 200, "Dirección");
        if (db.Establecimientos.Any(e => e.Nombre == dto.Nombre.Trim() && e.Id != id)) throw new ReglaNegocioException("Ya existe un establecimiento con ese nombre.");
        var row = id.HasValue ? db.Establecimientos.Find(id) ?? throw new KeyNotFoundException() : new Establecimiento { Id = Guid.NewGuid() };
        if (!dto.Activo && db.Usuarios.Any(u => u.EstablecimientoId == row.Id && u.Activo)) throw new ReglaNegocioException("Desactiva o reasigna primero las cuentas del establecimiento.");
        row.Nombre = dto.Nombre.Trim(); row.Direccion = dto.Direccion.Trim(); row.Activo = dto.Activo;
        if (!id.HasValue) db.Establecimientos.Add(row); db.SaveChanges(); return id.HasValue ? Results.Ok(row) : Results.Created($"/api/v1/admin/establecimientos/{row.Id}", row);
    }
    private static IResult GuardarTrabajador(Guid? id, TrabajadorDTO dto, GiatepDbContext db)
    {
        CasoEndpoints.Texto(dto.Codigo, 3, 30, "Código"); CasoEndpoints.Texto(dto.Nombre, 3, 120, "Nombre"); CasoEndpoints.Texto(dto.Cargo, 3, 100, "Cargo");
        if (!db.Establecimientos.Any(e => e.Id == dto.EstablecimientoId && e.Activo)) throw new ValidationException("Establecimiento no activo.");
        if (db.Trabajadores.Any(t => t.Codigo == dto.Codigo.Trim() && t.Id != id)) throw new ReglaNegocioException("El código ya existe.");
        var row = id.HasValue ? db.Trabajadores.Find(id) ?? throw new KeyNotFoundException() : new Trabajador { Id = Guid.NewGuid() };
        row.Codigo = dto.Codigo.Trim(); row.Nombre = dto.Nombre.Trim(); row.Cargo = dto.Cargo.Trim(); row.EstablecimientoId = dto.EstablecimientoId; row.Activo = dto.Activo;
        if (!id.HasValue) db.Trabajadores.Add(row); db.SaveChanges(); return id.HasValue ? Results.Ok(row) : Results.Created($"/api/v1/admin/trabajadores/{row.Id}", row);
    }
    private static IResult GuardarUsuario(Guid? id, UsuarioDTO dto, GiatepDbContext db, AccesoService acceso)
    {
        CasoEndpoints.Texto(dto.Cuenta, 3, 50, "Cuenta"); CasoEndpoints.Texto(dto.Nombre, 3, 120, "Nombre");
        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Cuenta, "^[a-zA-Z0-9._-]+$")) throw new ValidationException("Cuenta: usa letras, números, punto o guion.");
        if (!new[] { "Administrador", "Prevencion", "Jefatura" }.Contains(dto.Rol)) throw new ValidationException("Rol inválido.");
        if (dto.Rol == "Jefatura" && !db.Establecimientos.Any(e => e.Id == dto.EstablecimientoId && e.Activo)) throw new ValidationException("Jefatura necesita un establecimiento activo.");
        if (id == acceso.UsuarioId && (!dto.Activo || dto.Rol != "Administrador")) throw new ReglaNegocioException("No puedes desactivar tu propia cuenta ni retirarte el rol administrador.");
        if (db.Usuarios.Any(u => u.Cuenta == dto.Cuenta.ToLower().Trim() && u.Id != id)) throw new ReglaNegocioException("La cuenta ya existe.");
        var row = id.HasValue ? db.Usuarios.Find(id) ?? throw new KeyNotFoundException() : new Usuario { Id = Guid.NewGuid() };
        if ((db.Medidas.Any(m => m.UsuarioResponsableId == row.Id && !m.Completada) || db.MedidasInvestigacion.Any(m => m.UsuarioResponsableId == row.Id && db.Investigaciones.Any(i => i.Id == m.InvestigacionId && i.PlanIniciadoEn == null))) && (!dto.Activo || row.EstablecimientoId != (dto.Rol == "Jefatura" ? dto.EstablecimientoId : null) || row.Rol != dto.Rol)) throw new ReglaNegocioException("La cuenta tiene medidas pendientes; complétalas antes de desactivarla o cambiar su ámbito.");
        if (!id.HasValue || !string.IsNullOrEmpty(dto.Password)) {
            if (dto.Password is null || dto.Password.Length < 12 || dto.Password.Length > 200) throw new ValidationException("La contraseña debe tener entre 12 y 200 caracteres.");
            row.PasswordHash = new PasswordHasher<Usuario>().HashPassword(row, dto.Password); row.Fallos = 0; row.BloqueadoHasta = null;
        }
        row.Cuenta = dto.Cuenta.Trim().ToLower(); row.Nombre = dto.Nombre.Trim(); row.Rol = dto.Rol; row.EstablecimientoId = dto.Rol == "Jefatura" ? dto.EstablecimientoId : null; row.Activo = dto.Activo;
        if (!id.HasValue) db.Usuarios.Add(row); db.SaveChanges(); return id.HasValue ? Results.Ok(AuthEndpoints.Publico(row)) : Results.Created($"/api/v1/admin/usuarios/{row.Id}", AuthEndpoints.Publico(row));
    }
}
