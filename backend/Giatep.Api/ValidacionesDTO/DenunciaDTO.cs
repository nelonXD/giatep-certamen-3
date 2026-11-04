using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Giatep.Api.Models;
namespace Giatep.Api.ValidacionesDTO;

public record DenunciaDTO(string Formulario, Dictionary<string, string> Datos);
public record CondicionDenuncia(string Key, string[] Values);
public record CampoDenuncia(string Key, string Label, string Type, bool Required, string[]? Options, CondicionDenuncia? When);
public record GrupoDenuncia(string Id, string Title, string[] Types, CampoDenuncia[] Fields);
public static class Denuncias
{
    public static readonly GrupoDenuncia[] Grupos = Cargar();
    private static GrupoDenuncia[] Cargar()
    {
        using var stream = typeof(Denuncias).Assembly.GetManifestResourceStream("Giatep.Api.denuncias.json")!;
        return JsonSerializer.Deserialize<GrupoDenuncia[]>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    public static DenunciaDTO? Leer(string? json) => json is null ? null : JsonSerializer.Deserialize<DenunciaDTO>(json);
    public static string? Serializar(DenunciaDTO? dto) => dto is null ? null : JsonSerializer.Serialize(dto);
    public static DenunciaDTO? Validar(DenunciaDTO? dto, TipoCaso tipo, DateOnly fecha)
    {
        // Compatibilidad con los expedientes y clientes anteriores a los formularios.
        if (dto is null) return null;
        var esperado = tipo == TipoCaso.Accidente ? "DIAT" : "DIEP";
        if (dto.Formulario != esperado || dto.Datos is null || dto.Datos.Count > 80) throw new ValidationException("El formulario no corresponde al tipo de caso.");
        var campos = Grupos.Where(g => g.Types.Contains(esperado)).SelectMany(g => g.Fields).ToArray();
        if (dto.Datos.Keys.Any(k => !campos.Any(c => c.Key == k))) throw new ValidationException("La denuncia contiene campos que no corresponden a su formulario.");
        if (dto.Datos.Any(p => p.Value is null || p.Value.Length > 2000) || dto.Datos.Sum(p => p.Value?.Length ?? 0) > 40000) throw new ValidationException("Hay datos demasiado extensos en la denuncia.");
        if (dto.Datos.GetValueOrDefault("trabajadorNombre", "").Length > 120 || dto.Datos.GetValueOrDefault("profesion", "").Length > 100) throw new ValidationException("Nombre del trabajador: máximo 120 caracteres; profesión: máximo 100.");
        var datos = dto.Datos.ToDictionary(p => p.Key, p => p.Value.Trim());
        if (!string.IsNullOrWhiteSpace(datos.GetValueOrDefault("fechaIngreso"))) {
            if (!Regex.IsMatch(datos["fechaIngreso"], @"^\d{4}-\d{2}$") || !DateOnly.TryParseExact(datos["fechaIngreso"] + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ingreso) || ingreso > fecha)
                throw new ValidationException("El mes de ingreso debe ser válido y no posterior al evento.");
            var meses = (fecha.Year - ingreso.Year) * 12 + fecha.Month - ingreso.Month;
            datos["antiguedad"] = meses.ToString(CultureInfo.InvariantCulture);
            datos["antiguedadUnidad"] = "Meses";
        }
        foreach (var campo in campos)
        {
            if (campo.When is not null && !campo.When.Values.Contains(datos.GetValueOrDefault(campo.When.Key, ""))) { datos.Remove(campo.Key); continue; }
            var valor = datos.GetValueOrDefault(campo.Key, "");
            if (campo.Required && string.IsNullOrWhiteSpace(valor)) throw new ValidationException($"Completa {campo.Label}.");
            if (valor.Length == 0) continue;
            if (campo.Options is not null && !campo.Options.Contains(valor)) throw new ValidationException($"{campo.Label}: opción inválida.");
            if (campo.Type == "rut" && !RutValido(valor)) throw new ValidationException($"{campo.Label}: RUT/RUN inválido; revisa su dígito verificador.");
            if (campo.Type == "number" && (!int.TryParse(valor, out var numero) || numero < 0 || numero > 1000000)) throw new ValidationException($"{campo.Label}: ingresa un número entero no negativo.");
            if (campo.Type == "time" && !TimeOnly.TryParseExact(valor, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new ValidationException($"{campo.Label}: hora inválida.");
            if (campo.Type == "date") {
                if (!DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia) || dia > Services.Consultas.Hoy()) throw new ValidationException($"{campo.Label}: fecha inválida o futura.");
                if (campo.Key == "fechaNacimiento" && (dia > fecha || fecha.Year - dia.Year > 120)) throw new ValidationException("La fecha de nacimiento no corresponde a la fecha del evento.");
                if (campo.Key == "fechaEmision" && dia < fecha) throw new ValidationException("La fecha de emisión no puede ser anterior al evento.");
            }
        }
        if (datos.TryGetValue("antiguedad", out var antiguedad) && antiguedad.Length > 0 && string.IsNullOrWhiteSpace(datos.GetValueOrDefault("antiguedadUnidad"))) throw new ValidationException("Selecciona la unidad de antigüedad.");
        if (datos.GetValueOrDefault("puebloOriginario") == "Otro" && string.IsNullOrWhiteSpace(datos.GetValueOrDefault("otroPueblo"))) throw new ValidationException("Indica el otro pueblo originario.");
        return new(esperado, datos);
    }
    public static bool RutValido(string valor)
    {
        var limpio = valor.Replace(".", "").Replace("-", "").ToUpperInvariant();
        if (!Regex.IsMatch(limpio, "^[0-9]{7,8}[0-9K]$")) return false;
        var suma = 0; var multiplicador = 2;
        for (var i = limpio.Length - 2; i >= 0; i--) { suma += (limpio[i] - '0') * multiplicador; multiplicador = multiplicador == 7 ? 2 : multiplicador + 1; }
        var digito = 11 - suma % 11;
        return limpio[^1].ToString() == (digito == 11 ? "0" : digito == 10 ? "K" : digito.ToString());
    }
}
