using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Giatep.Api.Services;

public record LineaOcr(string Texto, double X, double Y, double Ancho, double Alto);
public record DistribucionOcr(double Ancho, double Alto, LineaOcr[] Lineas);
public record LecturaOcr(string Texto, double Confianza, DistribucionOcr? Distribucion = null);
public interface IOcrLocalService
{
    Task<LecturaOcr> Leer(byte[] imagen, CancellationToken token);
}

public sealed class OcrLocalService(IWebHostEnvironment environment, IConfiguration config) : IOcrLocalService
{
    private static readonly SemaphoreSlim Turno = new(1, 1);
    public async Task<LecturaOcr> Leer(byte[] imagen, CancellationToken token)
    {
        if (!await Turno.WaitAsync(0, token)) throw new ReglaNegocioException("Hay otra imagen en proceso. Intenta nuevamente en un momento.");
        var archivo = Path.Combine(Path.GetTempPath(), $"giatep-ocr-{Guid.NewGuid():N}.img");
        try
        {
            var paddle = string.Equals(config["Ocr:Engine"], "Paddle", StringComparison.OrdinalIgnoreCase);
            var directorio = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../ocr"));
            var script = Path.GetFullPath(config["Ocr:Script"] ?? Path.Combine(directorio, paddle ? "scan_paddle.py" : "scan.mjs"));
            var python = config["Ocr:PythonPath"] ?? Path.Combine(directorio, ".venv-paddle", OperatingSystem.IsWindows() ? "Scripts/python.exe" : "bin/python");
            if (!File.Exists(script) || (paddle ? !File.Exists(python) : !Directory.Exists(Path.Combine(Path.GetDirectoryName(script)!, "node_modules"))))
                throw new OcrNoDisponibleException("El motor OCR local no está instalado. Revisa backend/ocr/README.md.");
            await File.WriteAllBytesAsync(archivo, imagen, token);
            var start = new ProcessStartInfo(paddle ? python : config["Ocr:NodePath"] ?? "node")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(script)!
            };
            if (paddle) {
                start.Environment["PYTHONIOENCODING"] = "utf-8";
                foreach (var name in new[] { "OMP_NUM_THREADS", "OPENBLAS_NUM_THREADS", "MKL_NUM_THREADS" }) start.Environment[name] = "1";
                if (config["Ocr:ModelsPath"] is { } modelos) start.Environment["GIATEP_PADDLE_MODELS"] = modelos;
            } else start.ArgumentList.Add("--max-old-space-size=512");
            start.ArgumentList.Add(script);
            start.ArgumentList.Add(archivo);
            using var process = new Process { StartInfo = start };
            try { process.Start(); }
            catch (System.ComponentModel.Win32Exception) { throw new OcrNoDisponibleException("No se encontró el ejecutable del motor OCR local."); }
            if (paddle) {
                try {
                    process.PriorityClass = ProcessPriorityClass.BelowNormal;
                    if (OperatingSystem.IsWindows()) {
                        var permitidos = process.ProcessorAffinity.ToInt64();
                        process.ProcessorAffinity = (IntPtr)(permitidos & -permitidos);
                    }
                } catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException) { /* Los límites de hilos del motor siguen activos. */ }
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errors = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                var json = await output;
                await errors;
                if (process.ExitCode != 0) throw new ReglaNegocioException("No se pudo leer la imagen. Usa un PNG o JPG nítido, de hasta 25 megapíxeles.");
                var result = JsonSerializer.Deserialize<LecturaOcr>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (result is null || string.IsNullOrWhiteSpace(result.Texto)) throw new ReglaNegocioException("No se encontró texto legible. Prueba con una imagen más nítida.");
                return result;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new ReglaNegocioException("La lectura superó el tiempo disponible. Reduce el tamaño de la imagen e intenta nuevamente.");
            }
            finally
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                // Observe redirected streams even when processing was cancelled.
                try { await Task.WhenAll(output, errors); } catch (OperationCanceledException) { }
            }
        }
        finally
        {
            try { File.Delete(archivo); } finally { Turno.Release(); }
        }
    }
}

public sealed class OcrNoDisponibleException(string message) : Exception(message);
