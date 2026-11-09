using System.Security.Cryptography;

namespace Giatep.Api.Services;

public interface IArchivoEvidencia
{
    Guid Id { get; set; }
    string Ruta { get; set; }
    long Tamano { get; set; }
    string Sha256 { get; set; }
    byte[] Contenido { get; set; }
}

public sealed class AlmacenEvidencias(string raiz)
{
    public string Raiz { get; } = Path.GetFullPath(raiz);
    public string Resolver(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || Path.IsPathRooted(ruta)) throw new InvalidOperationException("Ruta de evidencia inválida.");
        var completa = Path.GetFullPath(Path.Combine(Raiz, ruta.Replace('/', Path.DirectorySeparatorChar)));
        if (!completa.StartsWith(Raiz.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("Ruta fuera del almacenamiento privado.");
        return completa;
    }
    public void Guardar(IArchivoEvidencia archivo, string carpeta)
    {
        if (archivo.Contenido.Length is < 8 or > 5242880) throw new System.ComponentModel.DataAnnotations.ValidationException("El archivo debe tener entre 8 bytes y 5 MB.");
        archivo.Ruta = $"{carpeta}/{archivo.Id:N}.bin";
        var destino = Resolver(archivo.Ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        // CreateNew impide sobrescribir evidencias existentes.
        using (var stream = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
            try { stream.Write(archivo.Contenido); stream.Flush(true); }
            catch { stream.Dispose(); File.Delete(destino); throw; }
        }
        archivo.Tamano = archivo.Contenido.LongLength;
        archivo.Sha256 = Convert.ToHexString(SHA256.HashData(archivo.Contenido));
    }
    public byte[] Leer(IArchivoEvidencia archivo)
    {
        var ruta = Resolver(archivo.Ruta);
        if (!File.Exists(ruta)) throw new KeyNotFoundException("El archivo de evidencia no está disponible.");
        var bytes = File.ReadAllBytes(ruta);
        if (bytes.LongLength != archivo.Tamano || Convert.ToHexString(SHA256.HashData(bytes)) != archivo.Sha256) throw new InvalidOperationException("La evidencia no coincide con su registro de integridad.");
        return bytes;
    }
    public void Eliminar(string ruta) => File.Delete(Resolver(ruta));
}
