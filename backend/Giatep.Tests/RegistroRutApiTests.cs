using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Giatep.Tests;
public class RegistroRutApiTests(GiatepFactory factory) : IClassFixture<GiatepFactory>
{
    private async Task<HttpClient> Cliente(string cuenta = "prevencion") {
        var c = factory.CreateClient(); var login = await c.PostAsJsonAsync("/api/v1/auth/login", new { cuenta, password = GiatepFactory.Password }); login.EnsureSuccessStatusCode();
        var sesion = await login.Content.ReadFromJsonAsync<JsonElement>(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString()); return c;
    }
    [Fact]
    public async Task SugierePorPrefijoConLimiteYPermisos()
    {
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<GiatepDbContext>(); var centro = db.Establecimientos.First();
            for (var i = 1; i <= 12; i++) {
                db.Trabajadores.Add(new Trabajador { Id = Guid.NewGuid(), Codigo = "SUG-" + i, Nombre = "Persona sugerencia " + i, Cargo = "Prueba", Run = DatosDemostracion.RutDePrueba(62000000 + i), EstablecimientoId = centro.Id });
                db.Empleadores.Add(new Empleador { Id = Guid.NewGuid(), Rut = DatosDemostracion.RutDePrueba(96000000 + i), Nombre = "Empresa sugerencia " + i });
            }
            db.SaveChanges();
        }
        using var c = await Cliente();
        var trabajadores = await c.GetFromJsonAsync<JsonElement>("/api/v1/trabajadores/sugerencias?rut=62"); Assert.Equal(8, trabajadores.GetArrayLength());
        Assert.All(trabajadores.EnumerateArray(), t => Assert.StartsWith("62", t.GetProperty("rut").GetString()));
        var empleadores = await c.GetFromJsonAsync<JsonElement>("/api/v1/empleadores/sugerencias?rut=96.000"); Assert.Equal(8, empleadores.GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/trabajadores/sugerencias?rut=6")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/empleadores/sugerencias?rut=%25")).StatusCode);
        using var jefe = await Cliente("jefatura"); Assert.Equal(HttpStatusCode.Forbidden, (await jefe.GetAsync("/api/v1/trabajadores/sugerencias?rut=62")).StatusCode);
    }
    [Fact]
    public async Task RegistraDesdeCasoYReutilizaPorRutSinIdentificadoresNiSelectores()
    {
        using var c = await Cliente(); var opciones = await c.GetFromJsonAsync<JsonElement>("/api/v1/opciones"); var centro = opciones.GetProperty("establecimientos")[0];
        var run = DatosDemostracion.RutDePrueba(61000001); var rut = DatosDemostracion.RutDePrueba(95000001);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/trabajadores/por-rut/{run}")).StatusCode);
        var denuncia = DenunciaTests.Datos("DIAT"); denuncia.Datos["trabajadorRun"] = run; denuncia.Datos["empleadorRut"] = rut; denuncia.Datos["trabajadorNombre"] = "Persona nueva desde el caso";
        var dto = new { establecimiento = centro.GetProperty("nombre").GetString(), establecimientoId = centro.GetProperty("id").GetString(), tipo = "Accidente", fecha = Consultas.Hoy().ToString("yyyy-MM-dd"), relato = "Caso sintético registrado sin seleccionar un trabajador previo.", denuncia };
        var response = await c.PostAsJsonAsync("/api/v1/casos", dto); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var primero = await response.Content.ReadFromJsonAsync<JsonElement>(); var workerId = primero.GetProperty("trabajadorId").GetString(); Assert.False(string.IsNullOrEmpty(workerId)); Assert.False(string.IsNullOrEmpty(primero.GetProperty("empleadorId").GetString()));
        var encontrado = await c.GetFromJsonAsync<JsonElement>($"/api/v1/trabajadores/por-rut/{run[..^1]}-{run[^1]}"); Assert.Equal(workerId, encontrado.GetProperty("id").GetString()); Assert.Equal("Persona nueva desde el caso", encontrado.GetProperty("datos").GetProperty("trabajadorNombre").GetString());
        (await c.GetAsync($"/api/v1/empleadores/por-rut/{rut}")).EnsureSuccessStatusCode();
        denuncia.Datos["trabajadorNombre"] = "Nombre revisado en el segundo caso";
        var segundo = await c.PostAsJsonAsync("/api/v1/casos", dto); segundo.EnsureSuccessStatusCode(); Assert.Equal(workerId, (await segundo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("trabajadorId").GetString());
        // Ambos casos referencian el mismo maestro; la investigación conserva su instantánea propia.
        var original = await c.GetFromJsonAsync<JsonElement>($"/api/v1/casos/{primero.GetProperty("id").GetString()}"); Assert.Equal("Nombre revisado en el segundo caso", original.GetProperty("denuncia").GetProperty("datos").GetProperty("trabajadorNombre").GetString());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GiatepDbContext>(); Assert.Equal(1, db.Trabajadores.Count(t => t.Run == run)); Assert.Equal(1, db.Empleadores.Count(e => e.Rut == rut));
    }
    [Fact]
    public async Task NoRegistraMaestrosCuandoElCasoEsInvalidoYProtegeLaBusqueda()
    {
        using var c = await Cliente(); var opciones = await c.GetFromJsonAsync<JsonElement>("/api/v1/opciones"); var centro = opciones.GetProperty("establecimientos")[0];
        var run = DatosDemostracion.RutDePrueba(61000002); var rut = DatosDemostracion.RutDePrueba(95000002); var d = DenunciaTests.Datos("DIEP"); d.Datos["trabajadorRun"] = run; d.Datos["empleadorRut"] = rut;
        var response = await c.PostAsJsonAsync("/api/v1/casos", new { establecimiento = centro.GetProperty("nombre").GetString(), establecimientoId = centro.GetProperty("id").GetString(), tipo = "EnfermedadProfesional", fecha = Consultas.Hoy().ToString("yyyy-MM-dd"), relato = "corto", denuncia = d }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/trabajadores/por-rut/{run}")).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/empleadores/por-rut/{rut}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/trabajadores/por-rut/12345678-9")).StatusCode);
        using var jefe = await Cliente("jefatura"); Assert.Equal(HttpStatusCode.Forbidden, (await jefe.GetAsync($"/api/v1/trabajadores/por-rut/{run}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/v1/empleadores/por-rut/{rut}")).StatusCode);
    }
}
