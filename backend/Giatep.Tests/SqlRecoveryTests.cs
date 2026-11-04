using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Xunit;
using System.Text.Json;
namespace Giatep.Tests;
public sealed class SqlLocalFactAttribute : FactAttribute
{
    public SqlLocalFactAttribute() { if (Environment.GetEnvironmentVariable("GIATEP_SQL_RECOVERY") != "1") Skip = "Respaldo/restauración SQL local: habilitar GIATEP_SQL_RECOVERY=1 y GIATEP_ROOT."; }
}
public class SqlRecoveryTests
{
    [SqlLocalFact]
    public void BackupYRestauracionConVerificacionDeConteos()
    {
        var root = Environment.GetEnvironmentVariable("GIATEP_ROOT") ?? throw new InvalidOperationException("Falta GIATEP_ROOT.");
        var line = File.ReadLines(Path.Combine(root, ".env")).Single(l => l.StartsWith("ConnectionStrings__Giatep="));
        var connection = new SqlConnectionStringBuilder(line.Split('=', 2)[1]);
        Assert.Equal("GiatepDB", connection.InitialCatalog);
        connection.InitialCatalog = "master";
        using var sql = new SqlConnection(connection.ConnectionString); sql.Open();
        object? Scalar(string text) { using var cmd = new SqlCommand(text, sql) { CommandTimeout = 120 }; return cmd.ExecuteScalar(); }
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dbName = "GiatepRestoreValidation_" + suffix;
        var backupDir = Convert.ToString(Scalar("SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))")) ?? throw new InvalidOperationException("No hay ruta de respaldos.");
        var dataDir = Convert.ToString(Scalar("SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath'))"))!;
        var logDir = Convert.ToString(Scalar("SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultLogPath'))"))!;
        var backup = Path.Combine(backupDir, $"GiatepDB_validacion_{suffix}.bak");
        var before = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM GiatepDB.dbo.Casos"));
        try {
            using (var cmd = new SqlCommand("BACKUP DATABASE [GiatepDB] TO DISK=@file WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM;", sql) { CommandTimeout = 120 }) { cmd.Parameters.AddWithValue("@file", backup); cmd.ExecuteNonQuery(); }
            using var list = new SqlCommand("RESTORE FILELISTONLY FROM DISK=@file", sql); list.Parameters.AddWithValue("@file", backup);
            string logicalData = "", logicalLog = "";
            using (var reader = list.ExecuteReader()) { while (reader.Read()) { if (reader["Type"].ToString() == "D") logicalData = reader["LogicalName"].ToString()!; else logicalLog = reader["LogicalName"].ToString()!; } }
            using (var restore = new SqlCommand($"RESTORE DATABASE [{dbName}] FROM DISK=@backup WITH MOVE @dataName TO @dataPath, MOVE @logName TO @logPath, CHECKSUM;", sql) { CommandTimeout = 120 }) {
                restore.Parameters.AddWithValue("@backup", backup); restore.Parameters.AddWithValue("@dataName", logicalData); restore.Parameters.AddWithValue("@dataPath", Path.Combine(dataDir, dbName + ".mdf")); restore.Parameters.AddWithValue("@logName", logicalLog); restore.Parameters.AddWithValue("@logPath", Path.Combine(logDir, dbName + ".ldf")); restore.ExecuteNonQuery();
            }
            var restored = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM [{dbName}].dbo.Casos")); Assert.Equal(before, restored);
            var measures = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM [{dbName}].dbo.Medidas"));
            if (Environment.GetEnvironmentVariable("GIATEP_VALIDAR_NORMALIZACION") == "1") {
                var copia = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = dbName };
                using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseSqlServer(copia.ConnectionString).Options);
                var anteriores = db.Casos.AsNoTracking().Where(c => c.DenunciaJson != null).Select(c => new { c.Id, c.DenunciaJson }).ToArray();
                db.Database.Migrate();
                Assert.Equal(before, db.Casos.Count());
                Assert.Equal(anteriores.Length, db.Diats.Count() + db.Dieps.Count());
                foreach (var anterior in anteriores) {
                    var caso = db.Casos.AsNoTracking().Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Single(c => c.Id == anterior.Id);
                    Assert.True(caso.Diat is not null || caso.Diep is not null); Assert.NotNull(caso.EmpleadorId);
                    Assert.True(DenunciaAlmacen.Equivale(Giatep.Api.ValidacionesDTO.Denuncias.Leer(anterior.DenunciaJson), DenunciaAlmacen.Obtener(caso)), "La denuncia trasladada debe conservar todos sus campos.");
                }
            }

            Directory.CreateDirectory(Path.Combine(root, "evidencias"));

            File.WriteAllText(Path.Combine(root, "evidencias", "restauracion.json"), JsonSerializer.Serialize(new { fecha = DateTimeOffset.UtcNow, origen = "GiatepDB", copia = dbName, casosOrigen = before, casosRestaurados = restored, medidasRestauradas = measures, backup, checksum = "Correcto", resultado = "Respaldo y restauración probados sin reemplazar la base original" }, new JsonSerializerOptions { WriteIndented = true }));
        } finally {
            // Únicamente elimina la copia de validación cuyo nombre generó esta prueba.
            if (!dbName.StartsWith("GiatepRestoreValidation_") || suffix.Length != 8) throw new InvalidOperationException("Nombre de copia no seguro.");
            Scalar($"IF DB_ID(N'{dbName}') IS NOT NULL BEGIN ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]; END");
        }
    }
}
