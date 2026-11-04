using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Giatep.Tests;

public class DatosCasoRelacionadosTests
{
    [Fact]
    public void GuardaDatosEnSusTablasSinDuplicarPersonasNiTiposDeFormulario()
    {
        using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var centro = new Establecimiento { Id = Guid.NewGuid(), Nombre = "Centro sintético" };
        var trabajador = new Trabajador { Id = Guid.NewGuid(), Nombre = "Persona de prueba", Cargo = "Cargo de prueba", EstablecimientoId = centro.Id };
        db.Establecimientos.Add(centro); db.Trabajadores.Add(trabajador); db.SaveChanges();
        var service = new CasoService(new CasoSqlRepository(db));
        var primera = DenunciaTests.Datos("DIAT"); primera.Datos["trabajadorNombre"] = "Nombre declarado original"; primera.Datos["empleadorNombre"] = "Empleador original";
        var caso = service.Crear(new(centro.Nombre, TipoCaso.Accidente, Consultas.Hoy(), "Relato sintético para comprobar tablas relacionadas.", centro.Id, trabajador.Id, primera));
        Assert.NotNull(caso.EmpleadorId); Assert.Single(db.Empleadores); Assert.Single(db.Diats);
        Assert.Null(db.Casos.Single().DenunciaJson); Assert.Equal("123456785", db.Trabajadores.Single().Run);
        Assert.Equal("Nombre declarado original", db.Trabajadores.Single().Nombre);
        var segunda = DenunciaTests.Datos("DIEP"); segunda.Datos["empleadorRut"] = "12345678-5"; segunda.Datos["trabajadorNombre"] = "Nombre actual del trabajador"; segunda.Datos["empleadorNombre"] = "Razón social actualizada";
        var otro = service.Crear(new(centro.Nombre, TipoCaso.EnfermedadProfesional, Consultas.Hoy(), "Síntomas sintéticos para el segundo caso del mismo trabajador.", centro.Id, trabajador.Id, segunda));
        Assert.Single(db.Empleadores); Assert.Single(db.Trabajadores); Assert.Single(db.Diats); Assert.Single(db.Dieps);
        Assert.Equal(caso.EmpleadorId, otro.EmpleadorId);
        var original = service.Obtener(caso.Id);
        Assert.Equal("Nombre actual del trabajador", original.Denuncia!.Datos["trabajadorNombre"]);
        Assert.Equal("Razón social actualizada", original.Denuncia.Datos["empleadorNombre"]);
        Assert.Equal("Nombre actual del trabajador", db.Trabajadores.Single().Nombre);
        // Guardar un cambio operativo del caso anterior no debe restaurar los maestros antiguos.
        new CasoSqlRepository(db).Guardar(original with { Estado = EstadoCaso.EnInvestigacion });
        Assert.Equal("Nombre actual del trabajador", db.Trabajadores.Single().Nombre);
        Assert.Equal("Razón social actualizada", db.Empleadores.Single().Nombre);
    }
    [Fact]
    public void NoPermiteAsignarElMismoRunAOtroTrabajador()
    {
        using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var t = new Trabajador { Id = Guid.NewGuid(), Run = "123456785" };
        var otro = new Trabajador { Id = Guid.NewGuid() }; db.Trabajadores.AddRange(t, otro); db.SaveChanges();
        var caso = new CasoRegistro { Id = Guid.NewGuid(), TrabajadorId = otro.Id };
        Assert.Throws<ReglaNegocioException>(() => DenunciaAlmacen.Guardar(db, caso, DenunciaTests.Datos("DIAT")));
        Assert.Empty(db.Empleadores); Assert.Empty(db.Diats); Assert.Empty(db.Dieps);
    }
}
