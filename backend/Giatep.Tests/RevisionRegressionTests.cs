using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;
namespace Giatep.Tests;

public class RevisionRegressionTests
{
    private static GiatepDbContext Context(string name) => new(new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(name).Options);

    [Fact]
    public void CompletarUnaMedidaNoRevierteElCumplimientoConcurrente()
    {
        var name = Guid.NewGuid().ToString();
        using var db = Context(name);
        var caso = new CasoRegistro { Id = Guid.NewGuid(), Estado = EstadoCaso.EnSeguimiento };
        var a = new MedidaRegistro { Id = Guid.NewGuid(), CasoId = caso.Id };
        var b = new MedidaRegistro { Id = Guid.NewGuid(), CasoId = caso.Id };
        db.Casos.Add(caso); db.Medidas.AddRange(a, b); db.SaveChanges(); db.ChangeTracker.Clear();
        var repo = new CasoSqlRepository(db);
        var interceptado = new LecturaConcurrente(repo, () => {
            using var concurrente = Context(name);
            new CasoSqlRepository(concurrente).CompletarMedida(caso.Id, a.Id);
        });
        new CasoService(interceptado).CompletarMedida(caso.Id, b.Id);
        using var verificacion = Context(name);
        Assert.All(verificacion.Medidas.ToArray(), m => Assert.True(m.Completada));
    }

    [Fact]
    public void ImportacionRecuperaTrabajadorYNoSobrescribeAntecedentesExistentes()
    {
        using var db = Context(Guid.NewGuid().ToString());
        var trabajador = new Trabajador { Id = Guid.NewGuid(), Nombre = "Persona sintética asociada" };
        var caso = new CasoRegistro { Id = Guid.NewGuid(), TrabajadorId = trabajador.Id, Estado = EstadoCaso.EnInvestigacion };
        db.Trabajadores.Add(trabajador); db.Casos.Add(caso); db.SaveChanges();
        InvestigacionService.ImportarAntecedentesPrevios(db);
        var inv = db.Investigaciones.Single();
        var nuevo = JsonDocument.Parse(inv.Antecedentes).RootElement;
        Assert.Equal(trabajador.Nombre, nuevo.GetProperty("trabajador").GetString());
        Assert.True(nuevo.GetProperty("trabajadorReferenciaActual").GetBoolean());
        inv.Antecedentes = "{\"trabajador\":\"Registro histórico\",\"relatoInicial\":\"Antecedente original conservado\"}"; db.SaveChanges();
        InvestigacionService.ImportarAntecedentesPrevios(db);
        var reparado = JsonDocument.Parse(inv.Antecedentes).RootElement;
        Assert.Equal(trabajador.Nombre, reparado.GetProperty("trabajador").GetString());
        Assert.Equal("Antecedente original conservado", reparado.GetProperty("relatoInicial").GetString());
        var snapshot = inv.Antecedentes;
        trabajador.Nombre = "Nombre modificado después"; db.SaveChanges();
        InvestigacionService.ImportarAntecedentesPrevios(db);
        Assert.Equal(snapshot, inv.Antecedentes);
    }

    private sealed class LecturaConcurrente(ICasoRepository inner, Action intervenir) : ICasoRepository
    {
        private bool pendiente = true;
        public IReadOnlyList<Caso> Listar() => inner.Listar();
        public Caso? Buscar(Guid id) { var anterior = inner.Buscar(id); if (pendiente) { pendiente = false; intervenir(); } return anterior; }
        public void Guardar(Caso caso) => inner.Guardar(caso);
        public void CompletarMedida(Guid casoId, Guid medidaId) => inner.CompletarMedida(casoId, medidaId);
    }
}
