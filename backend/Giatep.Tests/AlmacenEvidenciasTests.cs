using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Giatep.Tests;
public class AlmacenEvidenciasTests
{
    [Fact] public void GuardaVerificaIntegridadYNoPermiteSalirDeLaCarpeta()
    {
        var root = Path.Combine(Path.GetTempPath(), "giatep-storage-" + Guid.NewGuid());
        try {
            var almacen = new AlmacenEvidencias(root);
            Assert.Throws<InvalidOperationException>(() => almacen.Resolver("../afuera.txt"));
            Assert.Throws<InvalidOperationException>(() => almacen.Resolver(Path.GetFullPath("afuera.txt")));
            var evidencia = new Evidencia { Id = Guid.NewGuid(), Contenido = "evidencia de prueba"u8.ToArray() };
            almacen.Guardar(evidencia, "investigaciones");
            Assert.Equal(evidencia.Contenido, almacen.Leer(evidencia));
            Assert.Equal(64, evidencia.Sha256.Length);
            Assert.Throws<IOException>(() => almacen.Guardar(evidencia, "investigaciones"));
            Assert.Equal(evidencia.Contenido, almacen.Leer(evidencia));
            File.WriteAllBytes(almacen.Resolver(evidencia.Ruta), "archivo modificado"u8.ToArray());
            Assert.Throws<InvalidOperationException>(() => almacen.Leer(evidencia));
        } finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact] public void ErrorDeBaseRetiraArchivoNuevoYPermiteReintentar()
    {
        var root = Path.Combine(Path.GetTempPath(), "giatep-storage-" + Guid.NewGuid());
        try {
            var options = new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var db = new ContextoFallido(options, new AlmacenEvidencias(root));
            var evidencia = new Evidencia { Id = Guid.NewGuid(), Contenido = "evidencia de prueba"u8.ToArray() };
            db.Evidencias.Add(evidencia);
            Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
            Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
            Assert.NotEmpty(evidencia.Contenido);
        } finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    // El interceptor falla después de escribir el archivo y antes del commit de EF.
    private sealed class Fallo : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> SavingChanges(Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData e, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result) => throw new InvalidOperationException("Fallo de prueba");
    }
    private sealed class ContextoFallido : IDisposable
    {
        private readonly GiatepDbContext db;
        public ContextoFallido(DbContextOptions<GiatepDbContext> options, AlmacenEvidencias almacen) {
            var builder = new DbContextOptionsBuilder<GiatepDbContext>(options).AddInterceptors(new Fallo());
            db = new GiatepDbContext(builder.Options, almacen: almacen);
        }
        public DbSet<Evidencia> Evidencias => db.Evidencias;
        public int SaveChanges() => db.SaveChanges();
        public void Dispose() => db.Dispose();
    }
}
