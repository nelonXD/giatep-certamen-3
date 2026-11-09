using System.ComponentModel.DataAnnotations;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Endpoints;
public record RegistroRutDTO(Guid Id, Guid? EstablecimientoId, bool Activo, Dictionary<string, string> Datos);
public record SugerenciaRutDTO(string Rut, string Nombre, bool Activo);
public static class RegistroRutEndpoints
{
    private static string Rut(string rut) { if (!Denuncias.RutValido(rut.Trim())) throw new ValidationException("Revisa el RUT/RUN y su dígito verificador."); return DenunciaAlmacen.NormalizarRut(rut); }
    private static string Prefijo(string rut) {
        var value = DenunciaAlmacen.NormalizarRut(rut);
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{2,8}[\dK]?$") || value.Length > 9) throw new ValidationException("Escribe entre 2 y 9 caracteres del RUT/RUN.");
        return value;
    }
    public static void MapRegistrosRut(this WebApplication app)
    {
        app.MapGet("/api/v1/trabajadores/sugerencias", (string rut, GiatepDbContext db) => {
            var prefijo = Prefijo(rut);
            return db.Trabajadores.AsNoTracking().Where(t => t.Run != null && t.Run.StartsWith(prefijo)).OrderBy(t => t.Run).Take(8).Select(t => new SugerenciaRutDTO(t.Run!, t.Nombre, t.Activo)).ToArray();
        }).RequireAuthorization("Prevencion").WithTags("Registro por RUT");
        app.MapGet("/api/v1/empleadores/sugerencias", (string rut, GiatepDbContext db) => {
            var prefijo = Prefijo(rut);
            return db.Empleadores.AsNoTracking().Where(e => e.Rut.StartsWith(prefijo)).OrderBy(e => e.Rut).Take(8).Select(e => new SugerenciaRutDTO(e.Rut, e.Nombre, true)).ToArray();
        }).RequireAuthorization("Prevencion").WithTags("Registro por RUT");

        app.MapGet("/api/v1/trabajadores/por-rut/{rut}", (string rut, GiatepDbContext db) => {
            var run = Rut(rut); var t = db.Trabajadores.AsNoTracking().SingleOrDefault(t => t.Run == run);
            if (t is null) throw new KeyNotFoundException("Trabajador no registrado.");
            return (IResult)Results.Ok(new RegistroRutDTO(t.Id, t.EstablecimientoId, t.Activo, new() {
                ["trabajadorRun"] = t.Run!, ["trabajadorNombre"] = t.Nombre, ["profesion"] = t.Cargo,
                ["fechaIngreso"] = t.FechaIngreso ?? "", ["antiguedad"] = t.Antiguedad?.ToString() ?? "", ["antiguedadUnidad"] = t.AntiguedadUnidad ?? "", ["tipoContrato"] = t.TipoContrato ?? "", ["tipoIngreso"] = t.TipoIngreso ?? "", ["categoriaOcupacional"] = t.CategoriaOcupacional ?? "",
                ["trabajadorDireccion"] = t.Direccion ?? "", ["trabajadorComuna"] = t.Comuna ?? "", ["trabajadorTelefono"] = t.Telefono ?? "", ["sexo"] = t.Sexo ?? "", ["fechaNacimiento"] = t.FechaNacimiento?.ToString("yyyy-MM-dd") ?? "", ["nacionalidad"] = t.Nacionalidad ?? "", ["puebloOriginario"] = t.PuebloOriginario ?? "", ["otroPueblo"] = t.OtroPueblo ?? ""
            }));
        }).Produces<RegistroRutDTO>().RequireAuthorization("Prevencion").WithTags("Registro por RUT");
        app.MapGet("/api/v1/empleadores/por-rut/{rut}", (string rut, GiatepDbContext db) => {
            var id = Rut(rut); var e = db.Empleadores.AsNoTracking().SingleOrDefault(e => e.Rut == id);
            if (e is null) throw new KeyNotFoundException("Empleador no registrado.");
            return (IResult)Results.Ok(new RegistroRutDTO(e.Id, null, true, new() {
                ["empleadorRut"] = e.Rut, ["empleadorNombre"] = e.Nombre, ["empleadorDireccion"] = e.Direccion, ["empleadorComuna"] = e.Comuna, ["empleadorTelefono"] = e.Telefono ?? "", ["actividadEconomica"] = e.ActividadEconomica ?? "", ["propiedad"] = e.Propiedad ?? "", ["tipoEmpresa"] = e.TipoEmpresa ?? "", ["actividadPrincipal"] = e.ActividadPrincipal ?? ""
            }));
        }).Produces<RegistroRutDTO>().RequireAuthorization("Prevencion").WithTags("Registro por RUT");
    }
}
