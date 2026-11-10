using System.Net;
using System.Text;
using Giatep.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Giatep.Tests;

public class OcrApiTests
{
    private static readonly byte[] Imagen = [137,80,78,71,13,10,26,10];
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
    private static OcrApiService Service(HttpMessageHandler handler, string? key = "clave-simulada") => new(new HttpClient(handler),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Ocr:ApiKey"] = key }).Build());
    [Fact]
    public async Task EnviaDocumentoEnEspanolYObtieneTextoYConfianza()
    {
        var handler = new Handler(async (request, token) => {
            Assert.Equal("https://vision.googleapis.com/v1/images:annotate", request.RequestUri!.ToString());
            Assert.Equal("clave-simulada", request.Headers.GetValues("x-goog-api-key").Single());
            var body = await request.Content!.ReadAsStringAsync(token);
            Assert.Contains("DOCUMENT_TEXT_DETECTION", body);
            Assert.Contains(Convert.ToBase64String(Imagen), body);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"responses":[{"fullTextAnnotation":{"text":"Texto de prueba","pages":[{"confidence":0.92}]}}]}""", Encoding.UTF8,"application/json") };
        });
        var result = await Service(handler).Leer(Imagen, CancellationToken.None);
        Assert.Equal("Texto de prueba", result.Texto);
        Assert.Equal(92, result.Confianza, 5);
    }
    [Theory]
    [InlineData(429, "{}")]
    [InlineData(403, "detalle sensible")]
    [InlineData(200, "no-json")]
    [InlineData(200, "{\"responses\":[{\"error\":{\"message\":\"secreto\"}}]}")]
    public async Task ErroresExternosNoExponenDetalles(int code, string body)
    {
        var service = Service(new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)code) { Content = new StringContent(body) })));
        var error = await Assert.ThrowsAsync<OcrNoDisponibleException>(() => service.Leer(Imagen, CancellationToken.None));
        Assert.DoesNotContain("secreto", error.Message);
        Assert.DoesNotContain("detalle sensible", error.Message);
    }
    [Fact]
    public async Task SinClaveOConImagenInvalidaNoEnviaSolicitudes()
    {
        var handler = new Handler((_, _) => throw new Exception("No debe llamarse"));
        await Assert.ThrowsAsync<OcrNoDisponibleException>(() => Service(handler, null).Leer(Imagen, CancellationToken.None));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Service(handler).Leer([1,2], CancellationToken.None));
    }
    [Fact]
    public async Task TimeoutYCancelacionDelUsuarioSeDistinguen()
    {
        var service = Service(new Handler((_, _) => throw new TaskCanceledException()));
        await Assert.ThrowsAsync<OcrNoDisponibleException>(() => service.Leer(Imagen, CancellationToken.None));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.Leer(Imagen, cancel.Token));
    }
    [Fact]
    public async Task RespuestaSinTextoEsComprensible()
    {
        var service = Service(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"responses\":[{}]}") })));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => service.Leer(Imagen, CancellationToken.None));
    }
}
