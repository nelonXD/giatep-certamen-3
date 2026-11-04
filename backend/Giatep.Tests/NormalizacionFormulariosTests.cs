using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Giatep.Tests;

public class NormalizacionFormulariosTests
{
    [Theory]
    [InlineData("DIAT", TipoCaso.Accidente)]
    [InlineData("DIEP", TipoCaso.EnfermedadProfesional)]
    public void PersisteCadaCampoEnSuEntidadYLoRecuperaSinPerderElFormulario(string formulario, TipoCaso tipo)
    {
        using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var centro = new Establecimiento { Id = Guid.NewGuid(), Nombre = "Centro de prueba" };
        db.Establecimientos.Add(centro); db.SaveChanges();
        var datos = DenunciaTests.Datos(formulario);
        datos.Datos["fechaIngreso"] = Consultas.Hoy().AddMonths(-18).ToString("yyyy-MM");
        datos.Datos["denuncianteNombre"] = "Declarante de prueba";
        if (formulario == "DIAT") datos.Datos["horaAccidente"] = "09:30";
        else datos.Datos["duracionSintomas"] = "25";
        var servicio = new CasoService(new CasoSqlRepository(db));
        var creado = servicio.Crear(new(centro.Nombre, tipo, Consultas.Hoy(), "Descripción sintética para comprobar la normalización.", centro.Id, null, datos));
        db.ChangeTracker.Clear();
        var recuperado = servicio.Obtener(creado.Id);
        var caso = db.Casos.Single(); var trabajador = db.Trabajadores.Single();
        Assert.Equal("Declarante de prueba", caso.DenuncianteNombre);
        Assert.Equal(18, trabajador.Antiguedad);
        Assert.Equal(datos.Datos["empleadorNombre"], db.Empleadores.Single().Nombre);
        Assert.Equal(formulario, recuperado.Denuncia!.Formulario);
        Assert.Equal(trabajador.Nombre, recuperado.Denuncia.Datos["trabajadorNombre"]);
        Assert.Equal("18", recuperado.Denuncia.Datos["antiguedad"]);
        if (formulario == "DIAT") {
            Assert.Equal(new TimeOnly(9, 30), db.Diats.Single().HoraAccidente);
            Assert.Empty(db.Dieps); Assert.DoesNotContain("duracionSintomas", recuperado.Denuncia.Datos.Keys);
        } else {
            Assert.Equal(25, db.Dieps.Single().DuracionSintomas);
            Assert.Empty(db.Diats); Assert.DoesNotContain("horaAccidente", recuperado.Denuncia.Datos.Keys);
        }
        Assert.Null(db.Model.FindEntityType(typeof(CasoRegistro))!.FindProperty("TrabajadorNombre"));
        Assert.Null(db.Model.FindEntityType(typeof(Diat))!.FindProperty("FechaEmision"));
        Assert.Null(db.Model.FindEntityType(typeof(Diep))!.FindProperty("TrabajadorNombre"));
    }

    [Fact]
    public void CambiarTipoSustituyeSoloElDetalleYConservaElCaso()
    {
        using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var centro = new Establecimiento { Id = Guid.NewGuid(), Nombre = "Centro sintético" };
        db.Establecimientos.Add(centro); db.SaveChanges();
        var repo = new CasoSqlRepository(db); var servicio = new CasoService(repo);
        var caso = servicio.Crear(new(centro.Nombre, TipoCaso.Accidente, Consultas.Hoy(), "Relato sintético para comprobar el cambio de detalle.", centro.Id, null, DenunciaTests.Datos("DIAT")));
        repo.Guardar(caso with { Tipo = TipoCaso.EnfermedadProfesional, Denuncia = DenunciaTests.Datos("DIEP") });
        db.ChangeTracker.Clear();
        Assert.Single(db.Casos); Assert.Single(db.Trabajadores); Assert.Single(db.Empleadores);
        Assert.Empty(db.Diats); Assert.Equal(caso.Id, db.Dieps.Single().CasoId);
        Assert.Equal("DIEP", repo.Buscar(caso.Id)!.Denuncia!.Formulario);
    }
}
