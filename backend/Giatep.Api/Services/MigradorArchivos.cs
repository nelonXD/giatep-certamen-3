using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Giatep.Api.Models;

namespace Giatep.Api.Services;

public static class MigradorArchivos
{
    // La conexión permanece abierta: la migración exige esta verificación en la misma sesión SQL.
    public static async Task Migrar(GiatepDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try {
            var conexion = db.Database.GetDbConnection();
            foreach (var (tabla, carpeta) in new[] { ("Evidencias", "investigaciones"), ("EvidenciasMedidas", "planes") }) {
                using var existe = conexion.CreateCommand();
                existe.CommandText = $"SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.{tabla}') AND name='Contenido'";
                if (Convert.ToInt32(await existe.ExecuteScalarAsync()) == 0) continue;
                using var command = conexion.CreateCommand(); command.CommandText = $"SELECT [Id],[Contenido] FROM [dbo].[{tabla}]";
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) {
                    var id = reader.GetGuid(0); var bytes = (byte[])reader.GetValue(1);
                    var ruta = $"{carpeta}/{id:N}.bin"; var absoluta = db.Archivos.Resolver(ruta);
                    if (!File.Exists(absoluta)) {
                        var archivo = new Evidencia { Id = id, Contenido = bytes };
                        db.Archivos.Guardar(archivo, carpeta);
                    }
                    var recuperado = await File.ReadAllBytesAsync(absoluta);
                    if (bytes.LongLength != recuperado.LongLength || !SHA256.HashData(bytes).SequenceEqual(SHA256.HashData(recuperado)))
                        throw new InvalidOperationException("Una evidencia exportada no coincide. No se modificó el contenido de SQL.");
                }
            }
            using (var verificado = conexion.CreateCommand()) {
                verificado.CommandText = "EXEC sys.sp_set_session_context @key=N'GIATEP_ARCHIVOS_VERIFICADOS', @value=1";
                await verificado.ExecuteNonQueryAsync();
            }
            await db.Database.MigrateAsync();
        } finally { await db.Database.CloseConnectionAsync(); }
    }
}
