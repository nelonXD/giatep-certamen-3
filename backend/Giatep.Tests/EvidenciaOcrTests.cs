using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Giatep.Tests;

public sealed class PaddleLocalFactAttribute : FactAttribute
{
    public PaddleLocalFactAttribute() { if (Environment.GetEnvironmentVariable("GIATEP_TEST_PADDLE") != "1") Skip = "Requiere Paddle local y GIATEP_TEST_PADDLE=1."; }
}
public sealed class OcrFactory : GiatepFactory
{
    public sealed class OcrPrueba : IOcrLocalService
    {
        public Task<LecturaOcr> Leer(byte[] imagen, CancellationToken token) => Task.FromResult(new LecturaOcr("Texto reconocido de prueba", 92));
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => { services.RemoveAll<IOcrLocalService>(); services.AddScoped<IOcrLocalService, OcrPrueba>(); });
    }
}

public class EvidenciaOcrTests(OcrFactory factory) : IClassFixture<OcrFactory>
{
    [PaddleLocalFact]
    public async Task PaddleLocalReconoceConRecursosLimitados()
    {
        var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Ocr:Engine"] = "Paddle" }).Build();
        var service = new OcrLocalService(environment, config);
        var image = await File.ReadAllBytesAsync(Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../ocr/test-fixtures/documento.png")));
        var lectura = await service.Leer(image, CancellationToken.None);
        Assert.Contains("pasillo", lectura.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("08-11-2026", lectura.Texto);
    }
    [Fact]
    public async Task OcrTemporalNoGuardaImagenYSoloPersisteWordRevisado()
    {
        using var client = await Login("prevencion"); var p = Preparar();
        int antes; using (var scope = factory.Services.CreateScope()) antes = scope.ServiceProvider.GetRequiredService<GiatepDbContext>().Evidencias.Count();
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(new byte[] {137,80,78,71,13,10,26,10}), "archivo", "Temporal.png");
        var scan = await client.PostAsync($"/api/v1/investigaciones/{p.Inv}/ocr/escanear", form); scan.EnsureSuccessStatusCode();
        using (var scope = factory.Services.CreateScope()) Assert.Equal(antes, scope.ServiceProvider.GetRequiredService<GiatepDbContext>().Evidencias.Count());
        var save = await client.PostAsJsonAsync($"/api/v1/investigaciones/{p.Inv}/ocr/documento", new { titulo = "Documento revisado", texto = "Texto transcrito y revisado.", nombreImagen = "Temporal.png" });
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        var saved = await save.Content.ReadFromJsonAsync<JsonElement>();
        using var finalScope = factory.Services.CreateScope(); var db = finalScope.ServiceProvider.GetRequiredService<GiatepDbContext>();
        Assert.Equal(antes + 1, db.Evidencias.Count()); Assert.Equal(DocumentoEvidencia.Mime, db.Evidencias.Single(e => e.Id == saved.GetProperty("id").GetGuid()).Tipo);
        Assert.False(db.Evidencias.Any(e => e.Nombre == "Temporal.png"));
    }
    [Fact]
    public async Task ServicioEjecutaElModeloRealLocalSinProveedorExterno()
    {
        var environment = factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var imagePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../ocr/test-fixtures/documento.png"));
        var service = new OcrLocalService(environment, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var result = await service.Leer(await File.ReadAllBytesAsync(imagePath), CancellationToken.None);
        Assert.Contains("pasillo estaba obstruido con cajas", result.Texto);
        Assert.True(result.Confianza > 60);
    }
    private async Task<HttpClient> Login(string cuenta)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { cuenta, password = GiatepFactory.Password });
        response.EnsureSuccessStatusCode();
        var sesion = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());
        return client;
    }
    private (Guid Inv, Guid Imagen, Guid Caso) Preparar(string tipo = "image/png", string estado = "EnCurso")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GiatepDbContext>();
        var caso = db.Casos.First(c => db.Investigaciones.Any(i => i.CasoId == c.Id));
        var inv = db.Investigaciones.Single(i => i.CasoId == caso.Id);
        inv.Estado = estado;
        var imagen = new Evidencia { Id = Guid.NewGuid(), CasoId = caso.Id, InvestigacionId = inv.Id,
            Nombre = "Fuente.png", Tipo = tipo, Contenido = [137, 80, 78, 71, 13, 10, 26, 10],
            UsuarioId = db.Usuarios.First(u => u.Cuenta == "prevencion").Id, Fecha = DateTimeOffset.UtcNow };
        db.Evidencias.Add(imagen); db.SaveChanges();
        return (inv.Id, imagen.Id, caso.Id);
    }
    [Fact]
    public async Task EscaneaRevisaGuardaYDescargaDocumentoConFuenteYAprobacion()
    {
        using var client = await Login("prevencion");
        var p = Preparar(); var url = $"/api/v1/investigaciones/{p.Inv}/evidencias/{p.Imagen}";
        var scan = await client.PostAsync(url + "/escanear", null); scan.EnsureSuccessStatusCode();
        var lectura = await scan.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Texto reconocido de prueba", lectura.GetProperty("texto").GetString());
        var save = await client.PostAsJsonAsync(url + "/documento", new { titulo = "Evidencia revisada", texto = "Texto corregido: señalización & pasillo <libre>." });
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        var doc = await save.Content.ReadFromJsonAsync<JsonElement>();
        var download = await client.GetAsync($"/api/v1/casos/{p.Caso}/evidencias/{doc.GetProperty("id").GetGuid()}");
        download.EnsureSuccessStatusCode();
        Assert.Equal(DocumentoEvidencia.Mime, download.Content.Headers.ContentType!.MediaType);
        using var zip = new ZipArchive(new MemoryStream(await download.Content.ReadAsByteArrayAsync()));
        using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        var xml = XDocument.Parse(await reader.ReadToEndAsync());
        Assert.Contains("señalización & pasillo <libre>", xml.Root!.Value);
        Assert.Contains(p.Imagen.ToString(), xml.Root!.Value);
        Assert.Contains("Revisado y guardado por", xml.Root!.Value);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GiatepDbContext>();
        Assert.True(db.Evidencias.Any(e => e.Id == p.Imagen));
        Assert.True(db.Auditorias.Any(a => a.Accion == "DocumentoOCR" && a.RegistroId == doc.GetProperty("id").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/documento", new { titulo = "Título", texto = " " })).StatusCode);
    }
    [Fact]
    public async Task ProtegeRolesFuentesYCierre()
    {
        var p = Preparar(); var url = $"/api/v1/investigaciones/{p.Inv}/evidencias/{p.Imagen}";
        using var anonimo = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync(url + "/escanear", null)).StatusCode);
        foreach (var cuenta in new[] { "jefatura", "admin" })
        {
            using var usuario = await Login(cuenta);
            Assert.Equal(HttpStatusCode.Forbidden, (await usuario.PostAsync(url + "/escanear", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await usuario.PostAsJsonAsync(url + "/documento", new { titulo = "Título", texto = "Texto" })).StatusCode);
        }
        using var prev = await Login("prevencion");
        Assert.Equal(HttpStatusCode.NotFound, (await prev.PostAsync($"/api/v1/investigaciones/{p.Inv}/evidencias/{Guid.NewGuid()}/escanear", null)).StatusCode);
        var pdf = Preparar("application/pdf");
        Assert.Equal(HttpStatusCode.Conflict, (await prev.PostAsync($"/api/v1/investigaciones/{pdf.Inv}/evidencias/{pdf.Imagen}/escanear", null)).StatusCode);
        Preparar(estado: "Cerrada");
        Assert.Equal(HttpStatusCode.Conflict, (await prev.PostAsync(url + "/escanear", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await prev.PostAsJsonAsync(url + "/documento", new { titulo = "Título", texto = "Texto" })).StatusCode);
    }
}
