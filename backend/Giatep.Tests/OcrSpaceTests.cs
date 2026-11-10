using System.Net;
using Giatep.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Giatep.Tests;
public class OcrSpaceTests
{
    private static readonly byte[] Image = [137,80,78,71,13,10,26,10];
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
    private static OcrSpaceService Service(HttpMessageHandler handler, string? key = "prueba") => new(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Ocr:ApiKey"] = key }).Build());
    [Fact]
    public async Task EnviaMultipartYExtraeTextoSinInventarConfianza() {
        var service = Service(new Handler(async (req, token) => {
            Assert.Equal("https://api.ocr.space/parse/image", req.RequestUri!.ToString());
            Assert.Equal("prueba", req.Headers.GetValues("apikey").Single());
            Assert.IsType<MultipartFormDataContent>(req.Content);
            var body = await req.Content!.ReadAsStringAsync(token);
            Assert.Contains("documento.png",body); Assert.Contains("auto",body);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"IsErroredOnProcessing":false,"OCRExitCode":1,"ParsedResults":[{"FileParseExitCode":1,"ParsedText":"Texto de prueba","TextOverlay":{"Lines":[{"Words":[{"WordText":"Texto de prueba","Left":10,"Top":20,"Width":100,"Height":15}]}]}}]}""") };
        }));
        var result = await service.Leer(Image,CancellationToken.None);
        Assert.Equal("Texto de prueba",result.Texto); Assert.Equal(0,result.Confianza); Assert.NotNull(result.Distribucion); Assert.Equal(10,result.Distribucion.Lineas[0].X);
    }
    [Theory]
    [InlineData(403,"secreto")]
    [InlineData(429,"cuota")]
    [InlineData(200,"no-json")]
    [InlineData(200,"{\"IsErroredOnProcessing\":true}")]
    [InlineData(200,"{\"IsErroredOnProcessing\":false,\"OCRExitCode\":2}")]
    public async Task RechazaErroresYResultadosParciales(int code,string body) {
        var service = Service(new Handler((_,_) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)code) { Content = new StringContent(body) })));
        var error = await Assert.ThrowsAsync<OcrNoDisponibleException>(() => service.Leer(Image,CancellationToken.None));
        Assert.DoesNotContain("secreto",error.Message);
    }
    [Fact]
    public async Task ValidaAntesDeEnviar() {
        var handler = new Handler((_,_) => throw new Exception("No debe llamar"));
        await Assert.ThrowsAsync<OcrNoDisponibleException>(() => Service(handler,null).Leer(Image,CancellationToken.None));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Service(handler).Leer(new byte[1_000_001],CancellationToken.None));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => Service(handler).Leer([1,2],CancellationToken.None));
    }
    [Fact]
    public async Task ControlaTimeoutYRespetaCancelacion() {
        var service = Service(new Handler((_,_) => throw new TaskCanceledException()));
        await Assert.ThrowsAsync<OcrNoDisponibleException>(() => service.Leer(Image,CancellationToken.None));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.Leer(Image,cancel.Token));
    }
}
