using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using Xunit;
namespace Giatep.Tests;

public class DenunciaTests
{
    [Theory] [InlineData("DIAT", TipoCaso.Accidente)] [InlineData("DIEP", TipoCaso.EnfermedadProfesional)]
    public void CalculaAntiguedadDesdeMesIngreso(string formulario, TipoCaso tipo)
    {
        var dto = Datos(formulario);
        var fecha = Consultas.Hoy();
        dto.Datos["fechaIngreso"] = fecha.AddMonths(-26).ToString("yyyy-MM");
        dto.Datos["antiguedad"] = "999";
        dto.Datos["antiguedadUnidad"] = "Años";
        var resultado = Denuncias.Validar(dto, tipo, fecha)!;
        Assert.Equal("26", resultado.Datos["antiguedad"]);
        Assert.Equal("Meses", resultado.Datos["antiguedadUnidad"]);
        dto.Datos["fechaIngreso"] = fecha.ToString("yyyy-MM");
        Assert.Equal("0", Denuncias.Validar(dto, tipo, fecha)!.Datos["antiguedad"]);
        dto.Datos["fechaIngreso"] = fecha.AddMonths(1).ToString("yyyy-MM");
        Assert.Throws<ValidationException>(() => Denuncias.Validar(dto, tipo, fecha));
        dto.Datos["fechaIngreso"] = "2020-13";
        Assert.Throws<ValidationException>(() => Denuncias.Validar(dto, tipo, fecha));
    }
    public static DenunciaDTO Datos(string formulario)
    {
        var datos = new Dictionary<string, string>();
        foreach (var campo in Denuncias.Grupos.Where(g => g.Types.Contains(formulario)).SelectMany(g => g.Fields).Where(c => c.Required && c.When is null))
            datos[campo.Key] = campo.Type switch { "rut" => "12.345.678-5", "date" => campo.Key == "fechaNacimiento" ? "1990-01-01" : Consultas.Hoy().ToString("yyyy-MM-dd"), "time" => "09:30", "number" => "10", "select" => campo.Key == "tipoAccidente" ? "Trabajo" : campo.Options![0], _ => "Antecedente sintético del formulario" };
        return new(formulario, datos);
    }
    [Theory] [InlineData("DIAT", TipoCaso.Accidente)] [InlineData("DIEP", TipoCaso.EnfermedadProfesional)]
    public void ValidaDatosCorrespondientesAlTipo(string formulario, TipoCaso tipo) => Assert.Equal(formulario, Denuncias.Validar(Datos(formulario), tipo, Consultas.Hoy())!.Formulario);
    [Fact] public void RechazaFormularioDeOtroTipo() => Assert.Throws<ValidationException>(() => Denuncias.Validar(Datos("DIAT"), TipoCaso.EnfermedadProfesional, Consultas.Hoy()));
    [Fact] public void RechazaRunInvalido() { var dto = Datos("DIEP"); dto.Datos["trabajadorRun"] = "12.345.678-9"; Assert.Throws<ValidationException>(() => Denuncias.Validar(dto, TipoCaso.EnfermedadProfesional, Consultas.Hoy())); }
    [Fact] public void TrayectoExigeDatosDePrueba() {
        var dto = Datos("DIAT"); dto.Datos["tipoAccidente"] = "Trayecto";
        Assert.Throws<ValidationException>(() => Denuncias.Validar(dto, TipoCaso.Accidente, Consultas.Hoy()));
        dto.Datos["tipoTrayecto"] = "Domicilio - Trabajo"; dto.Datos["medioPrueba"] = "Testigos"; dto.Datos["detallePrueba"] = "Declaración de una persona testigo sintética";
        Assert.NotNull(Denuncias.Validar(dto, TipoCaso.Accidente, Consultas.Hoy()));
        dto.Datos["tipoAccidente"] = "Trabajo";
        Assert.False(Denuncias.Validar(dto, TipoCaso.Accidente, Consultas.Hoy())!.Datos.ContainsKey("detallePrueba"));
    }
    [Fact] public void RechazaNacimientoPosteriorAlEvento() { var dto = Datos("DIEP"); dto.Datos["fechaNacimiento"] = Consultas.Hoy().ToString("yyyy-MM-dd"); Assert.Throws<ValidationException>(() => Denuncias.Validar(dto, TipoCaso.EnfermedadProfesional, Consultas.Hoy().AddDays(-10))); }
    [Fact] public void RechazaCamposDeAccidenteEnEnfermedad() { var dto = Datos("DIEP"); dto.Datos["horaAccidente"] = "09:00"; Assert.Throws<ValidationException>(() => Denuncias.Validar(dto, TipoCaso.EnfermedadProfesional, Consultas.Hoy())); }
}
public class DenunciaApiTests(GiatepFactory factory) : IClassFixture<GiatepFactory>
{
    [Theory] [InlineData("DIAT", "Accidente")] [InlineData("DIEP", "EnfermedadProfesional")]
    public async Task ConservaDenunciaAlEditarEIniciarInvestigacion(string formulario, string tipo)
    {
        using var c = factory.CreateClient();
        var login = await c.PostAsJsonAsync("/api/v1/auth/login", new { cuenta = "prevencion", password = GiatepFactory.Password });
        var sesion = await login.Content.ReadFromJsonAsync<JsonElement>(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());
        var opciones = await c.GetFromJsonAsync<JsonElement>("/api/v1/opciones"); var centro = opciones.GetProperty("establecimientos")[0]; var trabajador = opciones.GetProperty("trabajadores").EnumerateArray().First(t => t.GetProperty("establecimientoId").GetString() == centro.GetProperty("id").GetString());
        var dto = new { establecimiento = centro.GetProperty("nombre").GetString(), establecimientoId = centro.GetProperty("id").GetString(), trabajadorId = trabajador.GetProperty("id").GetString(), tipo, fecha = Consultas.Hoy().ToString("yyyy-MM-dd"), relato = "Relato sintético suficientemente extenso para la denuncia.", denuncia = DenunciaTests.Datos(formulario) };
        var creado = await c.PostAsJsonAsync("/api/v1/casos", dto); Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var caso = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = caso.GetProperty("id").GetString();
        dto.denuncia.Datos["denuncianteNombre"] = "Denunciante actualizado";
        (await c.PutAsJsonAsync($"/api/v1/casos/{id}", dto)).EnsureSuccessStatusCode();
        var leido = await c.GetFromJsonAsync<JsonElement>($"/api/v1/casos/{id}"); Assert.Equal("Denunciante actualizado", leido.GetProperty("denuncia").GetProperty("datos").GetProperty("denuncianteNombre").GetString());
        var inv = await c.PostAsync($"/api/v1/casos/{id}/investigaciones", null); inv.EnsureSuccessStatusCode(); var invId = (await inv.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var detalle = await c.GetFromJsonAsync<JsonElement>($"/api/v1/investigaciones/{invId}"); Assert.Equal(formulario, detalle.GetProperty("antecedentes").GetProperty("denuncia").GetProperty("formulario").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v1/casos/{id}", dto)).StatusCode);
        var lista = await c.GetFromJsonAsync<JsonElement>($"/api/v1/casos?buscar={caso.GetProperty("codigo").GetString()}"); Assert.Equal(invId, lista.GetProperty("datos")[0].GetProperty("investigacionId").GetString());
    }
}
