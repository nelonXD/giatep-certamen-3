using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Giatep.Tests;
public class GiatepFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = "giatep-tests-" + Guid.NewGuid();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Giatep", "Server=unused;Database=test;Trusted_Connection=True");
        builder.UseSetting("Jwt:Key", "CLAVE_FICTICIA_SOLO_PRUEBAS_2026_MINIMO_32_BYTES");
        builder.ConfigureServices(services => {
            services.RemoveAll<AlmacenEvidencias>();
            services.AddSingleton(new AlmacenEvidencias(Path.Combine(Path.GetTempPath(), databaseName)));
            services.RemoveAll<DbContextOptions<GiatepDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GiatepDbContext>>();
            services.AddDbContext<GiatepDbContext>(o => o.UseInMemoryDatabase(databaseName));
        });
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GiatepDbContext>();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Seed:AdminPassword"] = Password, ["Seed:PrevencionPassword"] = Password, ["Seed:JefaturaPassword"] = Password, ["Seed:JefaturaSurPassword"] = Password }).Build();
        Inicializacion.Sembrar(db, config); return host;
    }
    public const string Password = "Solo-Pruebas-2026!";
}
public class ApiTests(GiatepFactory factory) : IClassFixture<GiatepFactory>
{
    private async Task<HttpClient> Login(string cuenta)
    {
        var client = factory.CreateClient(); var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { cuenta, password = GiatepFactory.Password }); response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString()); return client;
    }
    [Fact] public async Task SesionDuraOchoHoras()
    {
        using var client = factory.CreateClient();
        var inicio = DateTime.UtcNow;
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { cuenta = "prevencion", password = GiatepFactory.Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("token").GetString());
        Assert.InRange(token.ValidTo, inicio.AddHours(8).AddSeconds(-2), DateTime.UtcNow.AddHours(8));
    }
    [Fact] public async Task RequiereLogin() => Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/v1/casos")).StatusCode);
    [Fact] public async Task CredencialesIncorrectasNoIngresan() => Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { cuenta = "prevencion", password = "incorrecta" })).StatusCode);
    [Fact] public async Task PrevencionNoAdministra() { using var c = await Login("prevencion"); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/usuarios")).StatusCode); }
    [Fact] public async Task AdminNoInvestiga() { using var c = await Login("admin"); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/casos")).StatusCode); }
    [Fact] public async Task JefaturaNoCreaCasos() { using var c = await Login("jefatura"); Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/v1/casos", new { establecimiento = "Ficticio", tipo = "Accidente", fecha = "2026-01-01", relato = "Relato suficientemente largo para pruebas" })).StatusCode); }
    [Fact] public async Task JefaturaNoVeOtroCentro()
    {
        using var prev = await Login("prevencion"); using var jefe = await Login("jefatura");
        var todos = await prev.GetFromJsonAsync<JsonElement>("/api/v1/casos?tamano=100"); var me = await jefe.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var otro = todos.GetProperty("datos").EnumerateArray().First(x => x.GetProperty("establecimientoId").GetString() != me.GetProperty("establecimientoId").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await jefe.GetAsync($"/api/v1/casos/{otro.GetProperty("id").GetString()}")).StatusCode);
        var scoped = await jefe.GetFromJsonAsync<JsonElement>("/api/v1/casos?tamano=100"); Assert.All(scoped.GetProperty("datos").EnumerateArray(), x => Assert.Equal(me.GetProperty("establecimientoId").GetString(), x.GetProperty("establecimientoId").GetString()));
    }
    [Fact] public async Task DashboardTieneSieteIndicadoresYDatos()
    {
        using var c = await Login("prevencion"); var d = await c.GetFromJsonAsync<JsonElement>("/api/v1/dashboard");
        var indicadores = d.GetProperty("indicadores");
        Assert.Equal(7, indicadores.GetArrayLength());
        Assert.Contains(indicadores.EnumerateArray(), k => k.GetProperty("nombre").GetString() == "Medidas vencidas");
        Assert.True(d.GetProperty("meses").GetArrayLength() >= 12); Assert.True(indicadores[0].GetProperty("valor").GetDouble() >= 500);
    }
    [Fact] public async Task RangoInvalidoRechazado() { using var c = await Login("prevencion"); Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/dashboard?desde=2026-12-01&hasta=2026-01-01")).StatusCode); }
    [Fact] public async Task PaginacionInvalidaRechazada() { using var c = await Login("prevencion"); Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/casos?pagina=0")).StatusCode); }
    [Theory] [InlineData("operativo")] [InlineData("gerencial")] [InlineData("parametrizable")]
    public async Task ReportesExportanTresFormatos(string tipo)
    {
        using var c = await Login("prevencion");
        foreach (var formato in new[] { "xlsx", "pdf", "csv" }) {
            var response = await c.GetAsync($"/api/v1/reportes/{tipo}/exportar/{formato}?desde=2020-01-01&hasta=2030-01-01"); response.EnsureSuccessStatusCode(); var bytes = await response.Content.ReadAsByteArrayAsync(); Assert.True(bytes.Length > 100);
            if (formato == "xlsx") Assert.Equal((byte)'P', bytes[0]);
            if (formato == "pdf") Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes[..8]));
            if (formato == "csv") Assert.Equal(new byte[] {239,187,191}, bytes[..3]);
        }
    }
    [Fact] public async Task ExportacionAuditada() { using var c = await Login("prevencion"); (await c.GetAsync("/api/v1/reportes/gerencial/exportar/csv")).EnsureSuccessStatusCode(); using var scope = factory.Services.CreateScope(); Assert.True(scope.ServiceProvider.GetRequiredService<GiatepDbContext>().Auditorias.Any(a => a.Accion == "Exportacion")); }
    [Fact] public async Task InvestigacionSeparadaConCierreYPlanUnaSolaVez()
    {
        using var c = await Login("prevencion"); var options = await c.GetFromJsonAsync<JsonElement>("/api/v1/opciones"); var centro = options.GetProperty("establecimientos")[0]; var trabajador = options.GetProperty("trabajadores").EnumerateArray().First(t => t.GetProperty("establecimientoId").GetString() == centro.GetProperty("id").GetString()); var responsable = options.GetProperty("responsables").EnumerateArray().First(u => u.GetProperty("rol").GetString() == "Prevencion");
        var created = await c.PostAsJsonAsync("/api/v1/casos", new { establecimiento = centro.GetProperty("nombre").GetString(), establecimientoId = centro.GetProperty("id").GetString(), trabajadorId = trabajador.GetProperty("id").GetString(), tipo = "Accidente", fecha = "2026-01-01", relato = "Relato inicial sintético para investigar el accidente de trabajo." }); created.EnsureSuccessStatusCode(); var caso = await created.Content.ReadFromJsonAsync<JsonElement>(); var path = $"/api/v1/casos/{caso.GetProperty("id").GetString()}";
        var inicio = await c.PostAsync(path + "/investigaciones", null); inicio.EnsureSuccessStatusCode(); var ficha = await inicio.Content.ReadFromJsonAsync<JsonElement>(); var inv = $"/api/v1/investigaciones/{ficha.GetProperty("id").GetString()}";
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync(path + "/investigaciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(path + "/estado", new { estado = "EnSeguimiento" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync(inv + "/plan", null)).StatusCode);
        Assert.False((await c.PostAsync(inv + "/cerrar", null)).IsSuccessStatusCode);
        var a = await (await c.PostAsJsonAsync(inv + "/hechos", new { descripcion = "Materiales obstruían el pasillo de circulación", esFinal = false })).Content.ReadFromJsonAsync<JsonElement>();
        var b = await (await c.PostAsJsonAsync(inv + "/hechos", new { descripcion = "La persona tropezó al transitar por el pasillo", esFinal = false })).Content.ReadFromJsonAsync<JsonElement>();
        var final = await (await c.PostAsJsonAsync(inv + "/hechos", new { descripcion = "La persona cayó durante su desplazamiento", esFinal = true })).Content.ReadFromJsonAsync<JsonElement>();
        string Id(JsonElement e) => e.GetProperty("id").GetString()!;
        (await c.PostAsJsonAsync(inv + "/relaciones", new { antecedenteId = Id(a), consecuenciaId = Id(b) })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(inv + "/relaciones", new { antecedenteId = Id(b), consecuenciaId = Id(a) })).StatusCode);
        (await c.PostAsJsonAsync(inv + "/relaciones", new { antecedenteId = Id(b), consecuenciaId = Id(final) })).EnsureSuccessStatusCode();
        (await c.PutAsJsonAsync(inv + "/relato", new { texto = "Las entrevistas y la evidencia permiten reconstruir una caída causada por una obstrucción del pasillo." })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync(inv + "/medidas", new { hechoId = Id(a), descripcion = "Despejar el pasillo y mantener inspecciones preventivas", usuarioResponsableId = responsable.GetProperty("id").GetString(), plazo = "2026-12-01" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync(inv + "/cerrar", null)).StatusCode);
        (await c.PostAsJsonAsync(inv + "/entrevistas", new { entrevistado = "Persona sintética de prueba", calidad = "Testigo", fecha = "2026-01-02", declaracion = "Se observó que el pasillo tenía materiales que impedían transitar normalmente." })).EnsureSuccessStatusCode();
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jF5kAAAAASUVORK5CYII=")), "archivo", "prueba.png");
        (await c.PostAsync(path + "/evidencias", form)).EnsureSuccessStatusCode();
        (await c.PostAsync(inv + "/cerrar", null)).EnsureSuccessStatusCode();
        var antes = await c.GetFromJsonAsync<JsonElement>(path); Assert.Equal("EnInvestigacion", antes.GetProperty("estado").GetString()); Assert.Empty(antes.GetProperty("medidas").EnumerateArray());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(inv + "/hechos", new { descripcion = "Hecho añadido después del cierre", esFinal = false })).StatusCode);
        (await c.PostAsync(inv + "/plan", null)).EnsureSuccessStatusCode(); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync(inv + "/plan", null)).StatusCode);
        var detail = await c.GetFromJsonAsync<JsonElement>(path); Assert.Single(detail.GetProperty("medidas").EnumerateArray()); var medida = detail.GetProperty("medidas")[0].GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(path + "/estado", new { estado = "Cerrado" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsync(path + $"/medidas/{medida}/cumplimiento", null)).StatusCode);
        var planPath = $"/api/v1/planes/{caso.GetProperty("id").GetString()}/medidas/{medida}";
        using var entrega = new MultipartFormDataContent();
        entrega.Add(new StringContent("Se despejó el pasillo y se verificó la circulación segura."), "comentario");
        entrega.Add(new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jF5kAAAAASUVORK5CYII=")), "archivo", "cumplimiento.png");
        (await c.PostAsync(planPath + "/entrega", entrega)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(path + "/estado", new { estado = "Cerrado" })).StatusCode);
        (await c.PutAsJsonAsync(planPath + "/revision", new { aprobada = true, observacion = "Evidencia revisada y cumplimiento verificado." })).EnsureSuccessStatusCode(); (await c.PutAsJsonAsync(path + "/estado", new { estado = "Cerrado" })).EnsureSuccessStatusCode();
        using var jefe = await Login("jefatura.sur"); var me = await jefe.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        if (me.GetProperty("establecimientoId").GetString() != centro.GetProperty("id").GetString()) Assert.Equal(HttpStatusCode.NotFound, (await jefe.GetAsync(inv)).StatusCode);
    }
    [Fact] public async Task AdjuntoFalsoRechazado()
    {
        using var c = await Login("prevencion"); var list = await c.GetFromJsonAsync<JsonElement>("/api/v1/casos?estado=Borrador"); var id = list.GetProperty("datos")[0].GetProperty("id").GetString(); (await c.PostAsync($"/api/v1/casos/{id}/investigaciones", null)).EnsureSuccessStatusCode(); using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent("archivo falso de prueba"u8.ToArray()), "archivo", "falso.pdf"); Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync($"/api/v1/casos/{id}/evidencias", form)).StatusCode);
    }
}
