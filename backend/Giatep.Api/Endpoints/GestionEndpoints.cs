using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using Microsoft.AspNetCore.Identity;
namespace Giatep.Api.Endpoints;
public record CentroDTO(string Nombre, string Direccion, bool Activo);
public record TrabajadorDTO(string Codigo, string Nombre, string Cargo, Guid EstablecimientoId, bool Activo,
    string? Run = null, string? Direccion = null, string? Comuna = null, string? Telefono = null, string? Sexo = null,
    string? FechaNacimiento = null, string? Nacionalidad = null, string? FechaIngreso = null, string? TipoContrato = null,
    string? TipoIngreso = null, string? CategoriaOcupacional = null, string? PuebloOriginario = null, string? OtroPueblo = null);
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
        admin.MapGet("/trabajadores", (GiatepDbContext db, string? buscar, string? estado, string? orden, int pagina = 1) => {
            if (pagina < 1 || pagina > 100000) throw new ValidationException("Página inválida.");
            var q = db.Trabajadores.AsQueryable();
            if (!string.IsNullOrWhiteSpace(buscar)) {
                var texto = buscar.Trim().ToLowerInvariant();
                if (texto.Length > 150) throw new ValidationException("La búsqueda supera el largo permitido.");
                var run = texto.Replace(".", "").Replace("-", "");
                q = q.Where(t => t.Nombre.ToLower().Contains(texto) || t.Codigo.ToLower().Contains(texto) || (t.Run != null && t.Run.Contains(run)));
            }
            if (estado == "activos") q = q.Where(t => t.Activo);
            else if (estado == "inactivos") q = q.Where(t => !t.Activo);
            var ordenado = orden == "desc" ? q.OrderByDescending(t => t.Nombre).ThenBy(t => t.Id) : q.OrderBy(t => t.Nombre).ThenBy(t => t.Id);
            return Results.Ok(new { total = q.Count(), datos = ordenado.Skip((pagina - 1) * 15).Take(15).ToArray() });
        });
        admin.MapPost("/trabajadores", (TrabajadorDTO dto, GiatepDbContext db) => GuardarTrabajador(null, dto, db));
        admin.MapPut("/trabajadores/{id:guid}", (Guid id, TrabajadorDTO dto, GiatepDbContext db) => GuardarTrabajador(id, dto, db));
        admin.MapGet("/usuarios", (GiatepDbContext db) => db.Usuarios.OrderBy(u => u.Cuenta).Select(u => new { u.Id, u.Cuenta, u.Nombre, u.Rol, u.EstablecimientoId, u.Activo }).ToArray());
        admin.MapPost("/usuarios", (UsuarioDTO dto, GiatepDbContext db, AccesoService acceso) => GuardarUsuario(null, dto, db, acceso));
        admin.MapPut("/usuarios/{id:guid}", (Guid id, UsuarioDTO dto, GiatepDbContext db, AccesoService acceso) => GuardarUsuario(id, dto, db, acceso));
        admin.MapGet("/auditoria", (GiatepDbContext db, string? buscar, string? desde, string? hasta, string? orden, int pagina = 1) => {
            if (pagina < 1 || pagina > 100000) throw new ValidationException("Página inválida.");
            var q = db.Auditorias.AsQueryable();
            if (!string.IsNullOrWhiteSpace(buscar)) { var texto = buscar.Trim().ToLowerInvariant(); if (texto.Length > 150) throw new ValidationException("La búsqueda supera el largo permitido."); q = q.Where(a => a.Actor.ToLower().Contains(texto) || a.Entidad.ToLower().Contains(texto) || a.Accion.ToLower().Contains(texto)); }
            var zona = TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time");
            if (!string.IsNullOrEmpty(desde)) { if (!DateOnly.TryParseExact(desde, "yyyy-MM-dd", out var d)) throw new ValidationException("Fecha inicial inválida."); var limite = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), zona)); q = q.Where(a => a.Fecha >= limite); }
            if (!string.IsNullOrEmpty(hasta)) { if (!DateOnly.TryParseExact(hasta, "yyyy-MM-dd", out var d) || d == DateOnly.MaxValue) throw new ValidationException("Fecha final inválida."); var limite = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(d.AddDays(1).ToDateTime(TimeOnly.MinValue), zona)); q = q.Where(a => a.Fecha < limite); }
            var ordenado = orden == "asc" ? q.OrderBy(a => a.Fecha).ThenBy(a => a.Id) : q.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id);
            return Results.Ok(new { total = q.Count(), datos = ordenado.Skip((pagina - 1) * 20).Take(20).ToArray() });
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
        if (dto.Run is not null) {
            var run = dto.Run.Trim();
            if (run.Length > 0 && !Denuncias.RutValido(run)) throw new ValidationException("Revisa el RUN y su dígito verificador.");
            var normalizado = run.Length > 0 ? DenunciaAlmacen.NormalizarRut(run) : null;
            if (normalizado is not null && db.Trabajadores.Any(t => t.Run == normalizado && t.Id != row.Id)) throw new ReglaNegocioException("El RUN ya está registrado.");
            row.Run = normalizado;
        }
        static string? Dato(string? value, string? previo, int max, string campo) { if (value is null) return previo; var texto = value.Trim(); if (texto.Length > max) throw new ValidationException($"{campo} supera el largo permitido."); return texto.Length == 0 ? null : texto; }
        row.Direccion = Dato(dto.Direccion, row.Direccion, 200, "Dirección"); row.Comuna = Dato(dto.Comuna, row.Comuna, 100, "Comuna"); row.Telefono = Dato(dto.Telefono, row.Telefono, 30, "Teléfono");
        row.Sexo = Dato(dto.Sexo, row.Sexo, 30, "Sexo"); if (row.Sexo is not null && row.Sexo is not ("Hombre" or "Mujer")) throw new ValidationException("Sexo inválido.");
        if (dto.FechaNacimiento is not null) { if (dto.FechaNacimiento.Length == 0) row.FechaNacimiento = null; else if (DateOnly.TryParseExact(dto.FechaNacimiento, "yyyy-MM-dd", out var fecha) && fecha <= Consultas.Hoy()) row.FechaNacimiento = fecha; else throw new ValidationException("Fecha de nacimiento inválida."); }
        row.Nacionalidad = Dato(dto.Nacionalidad, row.Nacionalidad, 100, "Nacionalidad");
        row.FechaIngreso = Dato(dto.FechaIngreso, row.FechaIngreso, 7, "Fecha de ingreso");
        if (row.FechaIngreso is not null && (!DateOnly.TryParseExact(row.FechaIngreso + "-01", "yyyy-MM-dd", out var ingreso) || ingreso > Consultas.Hoy())) throw new ValidationException("Fecha de ingreso inválida.");
        row.TipoContrato = Dato(dto.TipoContrato, row.TipoContrato, 50, "Tipo de contrato"); row.TipoIngreso = Dato(dto.TipoIngreso, row.TipoIngreso, 50, "Tipo de ingreso"); row.CategoriaOcupacional = Dato(dto.CategoriaOcupacional, row.CategoriaOcupacional, 80, "Categoría ocupacional");
        row.PuebloOriginario = Dato(dto.PuebloOriginario, row.PuebloOriginario, 80, "Pueblo originario"); row.OtroPueblo = Dato(dto.OtroPueblo, row.OtroPueblo, 100, "Otro pueblo");
        static void Opcion(string? recibido, string? valor, string campo, params string[] opciones) { if (recibido is not null && valor is not null && !opciones.Contains(valor)) throw new ValidationException($"{campo} inválido."); }
        Opcion(dto.TipoContrato, row.TipoContrato, "Tipo de contrato", "Indefinido", "Plazo fijo", "Por obra o faena", "Temporada");
        Opcion(dto.TipoIngreso, row.TipoIngreso, "Tipo de ingreso", "Remuneración fija", "Remuneración variable", "Honorarios");
        Opcion(dto.CategoriaOcupacional, row.CategoriaOcupacional, "Categoría ocupacional", "Empleador", "Trabajador dependiente", "Trabajador independiente", "Familiar no remunerado", "Trabajador voluntario");
        Opcion(dto.PuebloOriginario, row.PuebloOriginario, "Pueblo originario", "Alacalufe", "Atacameño", "Aimara", "Colla", "Diaguita", "Mapuche", "Quechua", "Rapanui", "Yamana (Yagán)", "Otro", "Ninguno");
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
