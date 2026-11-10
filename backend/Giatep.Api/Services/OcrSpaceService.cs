using System.Net.Http.Headers;
using System.Text.Json;

namespace Giatep.Api.Services;

public sealed class OcrSpaceService(HttpClient http, IConfiguration config) : IOcrLocalService
{
    private static (double Ancho, double Alto) Dimensiones(byte[] image)
    {
        if (image[0] == 137 && image.Length >= 24)
            return (System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(16,4)), System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(20,4)));
        if (image[0] == 255) {
            var i = 2;
            while (i + 4 < image.Length) {
                if (image[i++] != 255) continue;
                var marker = image[i++];
                if (marker is 0xD9 or 0xDA) break;
                if (marker is 0xFF or 0x00 or 0xD8 || marker is >= 0xD0 and <= 0xD7) continue;
                var length = (image[i] << 8) + image[i+1];
                if (length < 2 || i + length > image.Length) break;
                if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF && length >= 7)
                    return ((image[i+5] << 8) + image[i+6], (image[i+3] << 8) + image[i+4]);
                i += length;
            }
        }
        return (0,0);
    }
    public async Task<LecturaOcr> Leer(byte[] imagen, CancellationToken token)
    {
        if (imagen.Length < 8 || imagen.Length > 1_000_000 ||
            !(imagen.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ||
              (imagen[0] == 255 && imagen[1] == 216 && imagen[2] == 255)))
            throw new ReglaNegocioException("Selecciona una imagen PNG o JPG de hasta 1 MB.");
        var key = config["Ocr:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) throw new OcrNoDisponibleException("Configura Ocr__ApiKey en el servidor para activar OCR.space.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ocr.space/parse/image");
            request.Headers.Add("apikey", key);
            using var form = new MultipartFormDataContent();
            var png = imagen[0] == 137;
            var file = new ByteArrayContent(imagen);
            file.Headers.ContentType = new MediaTypeHeaderValue(png ? "image/png" : "image/jpeg");
            form.Add(file, "file", png ? "documento.png" : "documento.jpg");
            form.Add(new StringContent("2"), "OCREngine");
            form.Add(new StringContent("auto"), "language");
            form.Add(new StringContent("true"), "detectOrientation");
            form.Add(new StringContent("true"), "isOverlayRequired");
            request.Content = form;
            using var response = await http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new OcrNoDisponibleException("OCR.space no está disponible. Revisa la clave y la cuota o intenta más tarde.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            var root = json.RootElement;
            if (root.GetProperty("IsErroredOnProcessing").GetBoolean())
                throw new OcrNoDisponibleException("OCR.space no pudo procesar la imagen. Revisa la cuota o prueba con otra imagen.");
            // Partial processing is not accepted as a complete transcription.
            if (root.GetProperty("OCRExitCode").GetInt32() != 1)
                throw new OcrNoDisponibleException("El reconocimiento no se completó. Prueba con una imagen más nítida.");
            var results = root.GetProperty("ParsedResults");
            var parts = new List<string>();
            var lineas = new List<LineaOcr>();
            foreach (var result in results.EnumerateArray()) {
                if (result.GetProperty("FileParseExitCode").GetInt32() != 1) throw new JsonException();
                parts.Add(result.GetProperty("ParsedText").GetString() ?? "");
                if (result.TryGetProperty("TextOverlay", out var overlay) && overlay.ValueKind == JsonValueKind.Object && overlay.TryGetProperty("Lines", out var lines))
                    foreach (var line in lines.EnumerateArray()) {
                        var words = line.GetProperty("Words").EnumerateArray().ToArray();
                        if (words.Length == 0) continue;
                        var x = words.Min(v => v.GetProperty("Left").GetDouble());
                        var y = words.Min(v => v.GetProperty("Top").GetDouble());
                        var right = words.Max(v => v.GetProperty("Left").GetDouble() + v.GetProperty("Width").GetDouble());
                        var bottom = words.Max(v => v.GetProperty("Top").GetDouble() + v.GetProperty("Height").GetDouble());
                        lineas.Add(new LineaOcr(string.Join(" ", words.Select(v => v.GetProperty("WordText").GetString())), x, y, right-x, bottom-y));
                    }
            }
            var text = string.Join("\n", parts).Trim();
            if (string.IsNullOrWhiteSpace(text)) throw new ReglaNegocioException("No se encontró texto legible. Prueba con una imagen más nítida.");
            if (text.Length > 30000) throw new ReglaNegocioException("El texto supera los 30000 caracteres. Divide el documento en imágenes más pequeñas.");
            // OCR.space does not provide a confidence percentage in this response.
            DistribucionOcr? distribucion = null;
            if (lineas.Count > 0) {
                var ordered = lineas.OrderBy(l => l.Y).ThenBy(l => l.X).ToArray();
                // The bounding canvas preserves relative positions; photographed margins remain approximate.
                var size = Dimensiones(imagen);
                var width = Math.Max(size.Ancho, ordered.Max(l => l.X + l.Ancho) + 1);
                var height = Math.Max(size.Alto, ordered.Max(l => l.Y + l.Alto) + 1);
                distribucion = new DistribucionOcr(width, height, ordered);
                text = string.Join("\n", ordered.Select(l => l.Texto));
            }
            return new LecturaOcr(text, 0, distribucion);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new OcrNoDisponibleException("La lectura superó el tiempo disponible. Intenta nuevamente."); }
        catch (HttpRequestException)
        { throw new OcrNoDisponibleException("No se pudo conectar con OCR.space. Intenta nuevamente."); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        { throw new OcrNoDisponibleException("OCR.space devolvió una respuesta inválida. Intenta nuevamente."); }
    }
}
