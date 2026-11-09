using System.Globalization;
using Giatep.Api.Models;
using Giatep.Api.ValidacionesDTO;
namespace Giatep.Api.Services;

public static class DenunciaAlmacen
{
    private static readonly string[] Campos = Denuncias.Grupos.SelectMany(g => g.Fields).Select(c => c.Key).ToArray();
    public static string NormalizarRut(string rut) => rut.Replace(".", "").Replace("-", "").Trim().ToUpperInvariant();
    private static (object? Owner, string Property) Campo(CasoRegistro caso, string key) => key switch {
            "trabajadorNombre" => (caso.Trabajador, "Nombre"),
            "trabajadorRun" => (caso.Trabajador, "Run"),
            "trabajadorDireccion" => (caso.Trabajador, "Direccion"),
            "trabajadorComuna" => (caso.Trabajador, "Comuna"),
            "trabajadorTelefono" => (caso.Trabajador, "Telefono"),
            "sexo" => (caso.Trabajador, "Sexo"),
            "fechaNacimiento" => (caso.Trabajador, "FechaNacimiento"),
            "nacionalidad" => (caso.Trabajador, "Nacionalidad"),
            "profesion" => (caso.Trabajador, "Cargo"),
            "fechaIngreso" => (caso.Trabajador, "FechaIngreso"),
            "antiguedad" => (caso.Trabajador, "Antiguedad"),
            "antiguedadUnidad" => (caso.Trabajador, "AntiguedadUnidad"),
            "tipoContrato" => (caso.Trabajador, "TipoContrato"),
            "tipoIngreso" => (caso.Trabajador, "TipoIngreso"),
            "categoriaOcupacional" => (caso.Trabajador, "CategoriaOcupacional"),
            "puebloOriginario" => (caso.Trabajador, "PuebloOriginario"),
            "otroPueblo" => (caso.Trabajador, "OtroPueblo"),
            "empleadorNombre" => (caso.Empleador, "Nombre"),
            "empleadorRut" => (caso.Empleador, "Rut"),
            "empleadorDireccion" => (caso.Empleador, "Direccion"),
            "empleadorComuna" => (caso.Empleador, "Comuna"),
            "empleadorTelefono" => (caso.Empleador, "Telefono"),
            "actividadEconomica" => (caso.Empleador, "ActividadEconomica"),
            "propiedad" => (caso.Empleador, "Propiedad"),
            "tipoEmpresa" => (caso.Empleador, "TipoEmpresa"),
            "actividadPrincipal" => (caso.Empleador, "ActividadPrincipal"),
            "horaAccidente" => (caso.Diat, "HoraAccidente"),
            "horaIngreso" => (caso.Diat, "HoraIngreso"),
            "horaSalida" => (caso.Diat, "HoraSalida"),
            "accidenteDireccion" => (caso.Diat, "AccidenteDireccion"),
            "accidenteComuna" => (caso.Diat, "AccidenteComuna"),
            "actividadAccidente" => (caso.Diat, "ActividadAccidente"),
            "lugarAccidente" => (caso.Diat, "LugarAccidente"),
            "trabajoHabitual" => (caso.Diat, "TrabajoHabitual"),
            "realizabaHabitual" => (caso.Diat, "RealizabaHabitual"),
            "clasificacionAccidente" => (caso.Diat, "ClasificacionAccidente"),
            "tipoAccidente" => (caso.Diat, "TipoAccidente"),
            "tipoTrayecto" => (caso.Diat, "TipoTrayecto"),
            "medioPrueba" => (caso.Diat, "MedioPrueba"),
            "detallePrueba" => (caso.Diat, "DetallePrueba"),
            "duracionSintomas" => (caso.Diep, "DuracionSintomas"),
            "duracionSintomasUnidad" => (caso.Diep, "DuracionSintomasUnidad"),
            "molestiasPrevias" => (caso.Diep, "MolestiasPrevias"),
            "parteCuerpo" => (caso.Diep, "ParteCuerpo"),
            "actividadSintomas" => (caso.Diep, "ActividadSintomas"),
            "puestoSintomas" => (caso.Diep, "PuestoSintomas"),
            "companerosSintomas" => (caso.Diep, "CompanerosSintomas"),
            "agentes" => (caso.Diep, "Agentes"),
            "tiempoExposicion" => (caso.Diep, "TiempoExposicion"),
            "tiempoExposicionUnidad" => (caso.Diep, "TiempoExposicionUnidad"),
            _ => (caso, char.ToUpperInvariant(key[0]) + key[1..])
        };
    public static DenunciaDTO? Obtener(CasoRegistro caso)
    {
        if (caso.Diat is null && caso.Diep is null) return Denuncias.Leer(caso.DenunciaJson);
        var formulario = caso.Tipo == TipoCaso.Accidente ? "DIAT" : "DIEP";
        var datos = new Dictionary<string, string>();
        foreach (var key in Denuncias.Grupos.Where(g => g.Types.Contains(formulario)).SelectMany(g => g.Fields).Select(f => f.Key)) {
            var (owner, property) = Campo(caso, key);
            var value = owner?.GetType().GetProperty(property)!.GetValue(owner);
            if (value is not null) datos[key] = value switch { DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), TimeOnly time => time.ToString("HH:mm", CultureInfo.InvariantCulture), _ => Convert.ToString(value, CultureInfo.InvariantCulture)! };
        }
        return new(formulario, datos);
    }
    public static bool Equivale(DenunciaDTO? a, DenunciaDTO? b) => a?.Formulario == b?.Formulario && Campos.All(k => (a?.Datos.GetValueOrDefault(k) ?? "") == (b?.Datos.GetValueOrDefault(k) ?? ""));
    public static void Guardar(GiatepDbContext db, CasoRegistro caso, DenunciaDTO dto)
    {
        var d = dto.Datos;
        string Texto(string key) => d.GetValueOrDefault(key, "");
        string? Opcional(string key) => string.IsNullOrWhiteSpace(Texto(key)) ? null : Texto(key);
        var run = NormalizarRut(Texto("trabajadorRun"));
        var registrado = db.Trabajadores.SingleOrDefault(t => t.Run == run);
        var trabajador = caso.TrabajadorId.HasValue ? db.Trabajadores.Find(caso.TrabajadorId.Value) : registrado;
        if (caso.TrabajadorId.HasValue && trabajador is null) throw new ReglaNegocioException("El trabajador indicado no existe.");
        if (registrado is not null && trabajador is not null && registrado.Id != trabajador.Id) throw new ReglaNegocioException("El RUN ya pertenece a otro trabajador.");
        if (trabajador is not null && (!trabajador.Activo || trabajador.EstablecimientoId != caso.EstablecimientoId)) throw new ReglaNegocioException("El trabajador debe estar activo y pertenecer al establecimiento del caso.");
        if (trabajador?.Run is not null && trabajador.Run != run) throw new ReglaNegocioException("El RUN identifica a otra persona. Busca al trabajador por su RUN.");
        if (trabajador is null) {
            trabajador = new Trabajador { Id = Guid.NewGuid(), Codigo = "TRAB-" + run, Run = run, EstablecimientoId = caso.EstablecimientoId ?? throw new ReglaNegocioException("Selecciona un establecimiento."), Cargo = "No informado" };
            db.Trabajadores.Add(trabajador);
        }
        caso.TrabajadorId = trabajador.Id;
        var rut = NormalizarRut(Texto("empleadorRut"));
        var empleador = db.Empleadores.SingleOrDefault(e => e.Rut == rut);
        if (empleador is null) { empleador = new Empleador { Id = Guid.NewGuid(), Rut = rut }; db.Empleadores.Add(empleador); }
        empleador.Nombre = Texto("empleadorNombre"); empleador.Direccion = Texto("empleadorDireccion"); empleador.Comuna = Texto("empleadorComuna"); empleador.Telefono = Opcional("empleadorTelefono"); empleador.ActividadEconomica = Opcional("actividadEconomica"); empleador.Propiedad = Opcional("propiedad"); empleador.TipoEmpresa = Opcional("tipoEmpresa"); empleador.ActividadPrincipal = Opcional("actividadPrincipal");
        trabajador.Run = run; trabajador.Nombre = Texto("trabajadorNombre");
        if (Opcional("profesion") is { } cargo) trabajador.Cargo = cargo;
        trabajador.Direccion = Opcional("trabajadorDireccion"); trabajador.Comuna = Opcional("trabajadorComuna"); trabajador.Telefono = Opcional("trabajadorTelefono"); trabajador.Sexo = Opcional("sexo"); trabajador.FechaNacimiento = DateOnly.TryParse(Texto("fechaNacimiento"), out var nacimiento) ? nacimiento : null; trabajador.Nacionalidad = Opcional("nacionalidad"); trabajador.PuebloOriginario = Opcional("puebloOriginario"); trabajador.OtroPueblo = Opcional("otroPueblo");
        caso.EmpleadorId = empleador.Id;
        caso.Trabajador = trabajador; caso.Empleador = empleador;
        if (dto.Formulario == "DIAT") {
            if (caso.Diep is not null) { db.Dieps.Remove(caso.Diep); caso.Diep = null; }
            if (caso.Diat is null) { caso.Diat = new Diat { Id = Guid.NewGuid(), CasoId = caso.Id }; db.Diats.Add(caso.Diat); }
        } else {
            if (caso.Diat is not null) { db.Diats.Remove(caso.Diat); caso.Diat = null; }
            if (caso.Diep is null) { caso.Diep = new Diep { Id = Guid.NewGuid(), CasoId = caso.Id }; db.Dieps.Add(caso.Diep); }
        }
        foreach (var key in Campos) {
            var (owner, name) = Campo(caso, key);
            if (owner is null) continue;
            var propiedad = owner.GetType().GetProperty(name)!;
            var valor = Opcional(key);
            if ((owner is Trabajador && name is ("Nombre" or "Run" or "Cargo")) || owner is Empleador) continue;
            object? convertido = valor is null ? null : propiedad.PropertyType == typeof(DateOnly?) ? DateOnly.ParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture) : propiedad.PropertyType == typeof(TimeOnly?) ? TimeOnly.ParseExact(valor, "HH:mm", CultureInfo.InvariantCulture) : propiedad.PropertyType == typeof(int?) ? int.Parse(valor, CultureInfo.InvariantCulture) : valor;
            propiedad.SetValue(owner, convertido);
        }
        caso.DenunciaJson = null;
        // SaveChanges del repositorio confirma juntos caso, trabajador, empleador y denuncia.
    }
}
