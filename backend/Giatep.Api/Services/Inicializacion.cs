using Giatep.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Giatep.Api.Services;
public static class Inicializacion
{
    public static void CargarEntorno(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()) return;
        var path = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../.env"));
        if (!File.Exists(path)) return;
        var values = new Dictionary<string, string?>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
            var pair = line.Split('=', 2);
            if (pair.Length != 2) throw new InvalidOperationException("Entrada inválida en .env.");
            var key = pair[0].Replace("__", ":");
            if (builder.Configuration[key] is null) values[key] = pair[1];
        }
        builder.Configuration.AddInMemoryCollection(values);
    }
    public static void Sembrar(GiatepDbContext db, IConfiguration config)
    {
        if (db.Establecimientos.Any()) { AsegurarCuentasIniciales(db, config); InvestigacionService.ImportarAntecedentesPrevios(db); if (config.GetValue<bool>("Seed:DatosDemostracion")) DatosDemostracion.Cargar(db); return; }
        var centros = new[] { "Centro de Salud Norte", "Centro de Salud Sur", "Centro de Salud Rural" }
            .Select((nombre, i) => new Establecimiento { Id = Guid.NewGuid(), Nombre = nombre, Direccion = $"Dirección sintética {i + 1}" }).ToArray();
        db.Establecimientos.AddRange(centros);
        var hasher = new PasswordHasher<Usuario>();
        var usuarios = new[] { ("admin", "Administrador", (Guid?)null, "Admin"), ("prevencion", "Prevencion", (Guid?)null, "Prevencion"), ("jefatura", "Jefatura", (Guid?)centros[0].Id, "Jefatura"), ("jefatura.sur", "Jefatura", (Guid?)centros[1].Id, "JefaturaSur") }
            .Select(item => {
                var password = config[$"Seed:{item.Item4}Password"] ?? throw new InvalidOperationException("Faltan las contraseñas de cuentas iniciales en .env. Consulta .env.example.");
                if (password.Length < 12) throw new InvalidOperationException("La contraseña inicial debe tener al menos 12 caracteres.");
                var user = new Usuario { Id = Guid.NewGuid(), Cuenta = item.Item1, Nombre = $"Cuenta demostración {item.Item1}", Rol = item.Item2, EstablecimientoId = item.Item3 };
                user.PasswordHash = hasher.HashPassword(user, password);
                return user;
            }).ToArray();
        db.Usuarios.AddRange(usuarios);
        var trabajadores = Enumerable.Range(1, 60).Select(i => new Trabajador { Id = Guid.NewGuid(), Codigo = $"SINT-{i:0000}", Nombre = $"Persona sintética {i:000}", Cargo = new[] { "Administración", "Enfermería", "Mantención", "Apoyo clínico" }[i % 4], EstablecimientoId = centros[i % 3].Id }).ToArray();
        db.Trabajadores.AddRange(trabajadores);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Pacific SA Standard Time").DateTime);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var random = new Random(2026);
        for (var i = 0; i < 500; i++)
        {
            var worker = trabajadores[i % trabajadores.Length];
            var centro = centros.Single(e => e.Id == worker.EstablecimientoId);
            var mes = start.AddMonths(i % 12);
            var fecha = mes.AddDays(random.Next(0, Math.Min(DateTime.DaysInMonth(mes.Year, mes.Month), mes.Month == today.Month && mes.Year == today.Year ? today.Day : 28)));
            var state = (EstadoCaso)(i % 4);
            var caso = new CasoRegistro { Id = Guid.NewGuid(), Codigo = $"DEMO-{i + 1:0000}", Tipo = i % 5 == 0 ? TipoCaso.EnfermedadProfesional : TipoCaso.Accidente, EstablecimientoId = centro.Id, Establecimiento = centro.Nombre, TrabajadorId = worker.Id, Fecha = fecha, Relato = $"Caso sintético {i + 1}: " + new[] { "caída al desplazarse por un pasillo durante tareas de apoyo.", "molestias relacionadas con tareas repetitivas en puesto de trabajo.", "contacto con objeto durante traslado de materiales.", "sobreesfuerzo durante organización de insumos." }[i % 4], Estado = state, CreadoEn = new DateTimeOffset(fecha.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero), CerradoEn = state == EstadoCaso.Cerrado ? new DateTimeOffset(fecha.AddDays(3).ToDateTime(new TimeOnly(16, 0)), TimeSpan.Zero) : null };
            if (caso.CerradoEn > DateTimeOffset.UtcNow) caso.CerradoEn = DateTimeOffset.UtcNow;
            db.Casos.Add(caso);
            if (state != EstadoCaso.Borrador)
            {
                db.Causas.Add(new() { Id = Guid.NewGuid(), CasoId = caso.Id, Categoria = new[] { "Entorno", "Organización", "Ergonomía" }[i % 3], Descripcion = "Condición sintética identificada durante investigación de demostración." });
                var responsable = usuarios.FirstOrDefault(u => u.EstablecimientoId == centro.Id);
                db.Medidas.Add(new() { Id = Guid.NewGuid(), CasoId = caso.Id, Descripcion = "Revisar condiciones y aplicar procedimiento preventivo documentado.", Responsable = responsable?.Nombre ?? usuarios[1].Nombre, UsuarioResponsableId = responsable?.Id ?? usuarios[1].Id, Plazo = fecha.AddDays(14 + i % 30), Completada = state == EstadoCaso.Cerrado || i % 7 == 0 });
            }
        }
        // Asociar registros iniciales existentes sin borrar su contenido.
        foreach (var caso in db.Casos.Where(c => c.EstablecimientoId == null)) { caso.EstablecimientoId = centros[0].Id; caso.TrabajadorId = trabajadores[0].Id; }
        db.SaveChanges();
        InvestigacionService.ImportarAntecedentesPrevios(db);
        if (config.GetValue<bool>("Seed:DatosDemostracion")) DatosDemostracion.Cargar(db);
    }

    private static void AsegurarCuentasIniciales(GiatepDbContext db, IConfiguration config)
    {
        var centros = db.Establecimientos.OrderBy(e => e.Nombre).Take(2).ToArray();
        var cuentas = new List<(string Cuenta, string Rol, Guid? Centro, string Clave)>
        {
            ("admin", "Administrador", null, "Admin"),
            ("prevencion", "Prevencion", null, "Prevencion")
        };
        if (centros.Length > 0) cuentas.Add(("jefatura", "Jefatura", centros[0].Id, "Jefatura"));
        if (centros.Length > 1) cuentas.Add(("jefatura.sur", "Jefatura", centros[1].Id, "JefaturaSur"));

        var faltantes = cuentas.Where(c => !db.Usuarios.Any(u => u.Cuenta == c.Cuenta)).ToArray();
        if (faltantes.Length == 0) return;
        var hasher = new PasswordHasher<Usuario>();
        foreach (var cuenta in faltantes)
        {
            var password = config[$"Seed:{cuenta.Clave}Password"] ?? throw new InvalidOperationException("Faltan contraseñas de cuentas iniciales en backend/.env. Consulta backend/.env.example.");
            if (password.Length < 12) throw new InvalidOperationException("La contraseña inicial debe tener al menos 12 caracteres.");
            var usuario = new Usuario { Id = Guid.NewGuid(), Cuenta = cuenta.Cuenta, Nombre = $"Cuenta demostración {cuenta.Cuenta}", Rol = cuenta.Rol, EstablecimientoId = cuenta.Centro };
            usuario.PasswordHash = hasher.HashPassword(usuario, password);
            db.Usuarios.Add(usuario);
        }
        db.SaveChanges();
    }
}
