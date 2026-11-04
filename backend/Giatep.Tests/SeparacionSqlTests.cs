using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Giatep.Tests;

public sealed class SeparacionSqlFactAttribute : FactAttribute
{
    public SeparacionSqlFactAttribute() { if (Environment.GetEnvironmentVariable("GIATEP_SQL_SEPARACION") != "1") Skip = "Ensayo de separación en copia SQL: habilitar GIATEP_SQL_SEPARACION y GIATEP_ROOT."; }
}
public class SeparacionSqlTests
{
    [SeparacionSqlFact]
    public void RespaldaEnsayaEnCopiaYAplicaSoloTrasVerificarLaSeparacion()
    {
        var root = Environment.GetEnvironmentVariable("GIATEP_ROOT")!;
        var linea = File.ReadLines(Path.Combine(root, ".env")).Single(l => l.StartsWith("ConnectionStrings__Giatep="));
        var original = new SqlConnectionStringBuilder(linea.Split('=', 2)[1]);
        Assert.Equal("GiatepDB", original.InitialCatalog);
        var master = new SqlConnectionStringBuilder(original.ConnectionString) { InitialCatalog = "master" };
        using var sql = new SqlConnection(master.ConnectionString); sql.Open();
        object? Scalar(string statement) { using var command = new SqlCommand(statement, sql) { CommandTimeout = 180 }; return command.ExecuteScalar(); }
        var suffix = Guid.NewGuid().ToString("N");
        var copy = "GiatepSeparacion_" + suffix;
        var backupDir = (string)Scalar("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")!;
        var dataDir = (string)Scalar("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))")!;
        var logDir = (string)Scalar("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'))")!;
        var backup = Path.Combine(backupDir, "Giatep_antes_separacion_" + suffix + ".bak");
        using (var cmd = new SqlCommand("BACKUP DATABASE [GiatepDB] TO DISK=@file WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM;", sql) { CommandTimeout = 180 }) { cmd.Parameters.AddWithValue("@file", backup); cmd.ExecuteNonQuery(); }
        string logicalData = "", logicalLog = "";
        using (var list = new SqlCommand("RESTORE FILELISTONLY FROM DISK=@file", sql)) {
            list.Parameters.AddWithValue("@file", backup);
            using var reader = list.ExecuteReader();
            while (reader.Read()) { if (reader["Type"].ToString() == "D") logicalData = reader["LogicalName"].ToString()!; else logicalLog = reader["LogicalName"].ToString()!; }
        }
        try {
            using (var restore = new SqlCommand($"RESTORE DATABASE [{copy}] FROM DISK=@backup WITH MOVE @dataName TO @dataPath, MOVE @logName TO @logPath, CHECKSUM;", sql) { CommandTimeout = 180 }) {
                restore.Parameters.AddWithValue("@backup", backup); restore.Parameters.AddWithValue("@dataName", logicalData); restore.Parameters.AddWithValue("@dataPath", Path.Combine(dataDir, copy + ".mdf")); restore.Parameters.AddWithValue("@logName", logicalLog); restore.Parameters.AddWithValue("@logPath", Path.Combine(logDir, copy + ".ldf")); restore.ExecuteNonQuery();
            }
            var copyConnection = new SqlConnectionStringBuilder(original.ConnectionString) { InitialCatalog = copy };
            using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseSqlServer(copyConnection.ConnectionString).Options);
            var count = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM [{copy}].dbo.Casos"));
            var diat = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM [{copy}].dbo.DenunciasCasos WHERE Formulario='DIAT'"));
            var diep = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM [{copy}].dbo.DenunciasCasos WHERE Formulario='DIEP'"));
            db.Database.Migrate();
            Verificar(db, count, diat, diep);
            if (Environment.GetEnvironmentVariable("GIATEP_APLICAR_SEPARACION") == "1") {
                using var real = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseSqlServer(original.ConnectionString).Options);
                // Prevent migration if another process changed the source after the backup.
                Assert.Equal(count, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM GiatepDB.dbo.Casos")));
                Assert.Equal(diat + diep, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM GiatepDB.dbo.DenunciasCasos")));
                real.Database.Migrate(); Verificar(real, count, diat, diep);
            }
            Directory.CreateDirectory(Path.Combine(root, "evidencias"));
            File.WriteAllText(Path.Combine(root, "evidencias", "separacion-diat-diep.json"), JsonSerializer.Serialize(new { fecha = DateTimeOffset.UtcNow, casos = count, diat, diep, backup, ensayoEnCopia = true, aplicada = Environment.GetEnvironmentVariable("GIATEP_APLICAR_SEPARACION") == "1", archivoHistorico = "Auditorias con accion ArchivoNormalizacion", resultado = "Conteos, lectura del formulario, relaciones y archivo de datos anteriores verificados" }, new JsonSerializerOptions { WriteIndented = true }));
        } finally {
            if (!copy.StartsWith("GiatepSeparacion_") || suffix.Length != 32) throw new InvalidOperationException("Nombre de copia inválido.");
            Scalar($"IF DB_ID(N'{copy}') IS NOT NULL BEGIN ALTER DATABASE [{copy}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{copy}]; END");
        }
    }
    private static void Verificar(GiatepDbContext db, int count, int diat, int diep)
    {
        Assert.Equal(count, db.Casos.Count()); Assert.Equal(diat, db.Diats.Count()); Assert.Equal(diep, db.Dieps.Count());
        Assert.Equal(diat + diep, db.Auditorias.Count(a => a.Accion == "ArchivoNormalizacion"));
        var repo = new CasoSqlRepository(db);
        foreach (var id in db.Diats.Select(d => d.CasoId).Take(4).ToArray()) Assert.Equal("DIAT", repo.Buscar(id)!.Denuncia!.Formulario);
        foreach (var id in db.Dieps.Select(d => d.CasoId).Take(4).ToArray()) Assert.Equal("DIEP", repo.Buscar(id)!.Denuncia!.Formulario);
        Assert.False(db.Diats.Any(d => db.Dieps.Any(e => e.CasoId == d.CasoId)));
        Assert.False(db.Diats.Any(d => !db.Casos.Any(c => c.Id == d.CasoId && c.Tipo == TipoCaso.Accidente)));
        Assert.False(db.Dieps.Any(d => !db.Casos.Any(c => c.Id == d.CasoId && c.Tipo == TipoCaso.EnfermedadProfesional)));
        var table = db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name='DenunciasCasos'").Single(); Assert.Equal(0, table);
    }
}
