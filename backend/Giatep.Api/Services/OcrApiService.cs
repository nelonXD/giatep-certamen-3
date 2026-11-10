using System.Net.Http.Json;
using System.Text.Json;

namespace Giatep.Api.Services;

// The existing OCR contract also allows the local engines to remain available.
public sealed class OcrApiService(HttpClient http, IConfiguration config) : IOcrLocalService
{
    public async Task<LecturaOcr> Leer(byte[] imagen, CancellationToken token)
    {
        if (imagen.Length < 8 || imagen.Length > 5 * 1024 * 1024 ||
            !(imagen.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ||
              (imagen[0] == 255 && imagen[1] == 216 && imagen[2] == 255)))
            throw new ReglaNegocioException("Selecciona una imagen PNG o JPG de hasta 5 MB.");
        var key = config["Ocr:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new OcrNoDisponibleException("Configura Ocr__ApiKey en el servidor para activar Google Cloud Vision.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://vision.googleapis.com/v1/images:annotate");
            request.Headers.Add("x-goog-api-key", key);
            request.Content = JsonContent.Create(new { requests = new[] { new {
                image = new { content = Convert.ToBase64String(imagen) },
                features = new[] { new { type = "DOCUMENT_TEXT_DETECTION" } },
                imageContext = new { languageHints = new[] { "es" } }
            } } });
            using var response = await http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new OcrNoDisponibleException("El servicio de reconocimiento no está disponible. Revisa la configuración y la cuota o intenta más tarde.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (!json.RootElement.TryGetProperty("responses", out var responses) || responses.ValueKind != JsonValueKind.Array || responses.GetArrayLength() != 1)
                throw new JsonException();
            var result = responses[0];
            if (result.TryGetProperty("error", out _))
                throw new OcrNoDisponibleException("El proveedor no pudo procesar la imagen. Intenta nuevamente más tarde.");
            if (!result.TryGetProperty("fullTextAnnotation", out var annotation))
                throw new ReglaNegocioException("No se encontró texto legible. Prueba con una imagen más nítida.");
            var texto = annotation.GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(texto)) throw new ReglaNegocioException("No se encontró texto legible. Prueba con una imagen más nítida.");
            if (texto.Length > 30000) throw new ReglaNegocioException("El texto supera los 30000 caracteres. Divide el documento en imágenes más pequeñas.");
            var scores = new List<double>();
            if (annotation.TryGetProperty("pages", out var pages))
                foreach (var page in pages.EnumerateArray())
                    if (page.TryGetProperty("confidence", out var score) && score.TryGetDouble(out var value) && double.IsFinite(value)) scores.Add(Math.Clamp(value, 0, 1));
            return new LecturaOcr(texto, scores.Count == 0 ? 0 : scores.Average() * 100);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new OcrNoDisponibleException("La lectura superó el tiempo disponible. Intenta nuevamente."); }
        catch (HttpRequestException)
        { throw new OcrNoDisponibleException("No se pudo conectar con el servicio de reconocimiento. Intenta nuevamente."); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        { throw new OcrNoDisponibleException("El servicio devolvió una respuesta inválida. Intenta nuevamente."); }
    }
}
