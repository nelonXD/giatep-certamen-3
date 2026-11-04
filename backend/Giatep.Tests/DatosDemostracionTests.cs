using Giatep.Api.Models;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Giatep.Tests;

public class DatosDemostracionTests
{
    [Fact]
    public void CreaSeiscientosCasosCompletosEnDoceMesesSinDuplicarNiBorrarDatos()
    {
        using var db = new GiatepDbContext(new DbContextOptionsBuilder<GiatepDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var centros = Enumerable.Range(0, 3).Select(i => new Establecimiento { Id = Guid.NewGuid(), Nombre = "Centro demo " + i }).ToArray(); db.Establecimientos.AddRange(centros);
        db.Usuarios.Add(new Usuario { Id = Guid.NewGuid(), Nombre = "Prevención de prueba", Cuenta = "prevencion", Rol = "Prevencion" });
        db.Usuarios.Add(new Usuario { Id = Guid.NewGuid(), Nombre = "Jefatura de prueba", Cuenta = "jefatura", Rol = "Jefatura", EstablecimientoId = centros[0].Id });
        var previo = new CasoRegistro { Id = Guid.NewGuid(), Codigo = "PREVIO-001", Establecimiento = centros[0].Nombre, EstablecimientoId = centros[0].Id, Relato = "Registro sintético previo que no debe eliminarse.", Fecha = Consultas.Hoy() }; db.Casos.Add(previo); db.SaveChanges();
        DatosDemostracion.Cargar(db);
        var casos = db.Casos.Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Where(c => c.Codigo.StartsWith(DatosDemostracion.Prefijo)).ToArray();
        Assert.Equal(600, casos.Length); Assert.Equal(12, casos.Select(c => c.Fecha.ToString("yyyy-MM")).Distinct().Count());
        Assert.Equal(420, casos.Count(c => c.Tipo == TipoCaso.Accidente)); Assert.Equal(180, casos.Count(c => c.Tipo == TipoCaso.EnfermedadProfesional));
        Assert.Equal(6, db.Empleadores.Count()); Assert.Equal(600, db.Trabajadores.Count()); Assert.Equal(420, db.Diats.Count()); Assert.Equal(180, db.Dieps.Count()); Assert.Equal(600, casos.Select(c => c.TrabajadorId).Distinct().Count()); Assert.Equal(4, casos.Select(c => c.Estado).Distinct().Count());
        foreach (var caso in casos) {
            Assert.NotNull(caso.EmpleadorId); Assert.NotNull(caso.TrabajadorId); Assert.Null(caso.DenunciaJson);
            Assert.NotNull(Denuncias.Validar(DenunciaAlmacen.Obtener(caso), caso.Tipo, caso.Fecha));
            Assert.True(caso.Fecha <= Consultas.Hoy()); Assert.True(caso.CreadoEn <= DateTimeOffset.UtcNow);
            if (caso.Estado == EstadoCaso.Borrador) { Assert.False(db.Investigaciones.Any(i => i.CasoId == caso.Id)); continue; }
            var inv = db.Investigaciones.Single(i => i.CasoId == caso.Id);
            Assert.True(inv.IniciadaEn >= caso.CreadoEn); Assert.True(inv.IniciadaEn <= DateTimeOffset.UtcNow);
            Assert.True(db.Entrevistas.Any(e => e.InvestigacionId == inv.Id)); Assert.True(db.Evidencias.Any(e => e.InvestigacionId == inv.Id));
            Assert.Equal(4, db.Hechos.Count(h => h.InvestigacionId == inv.Id)); Assert.Equal(3, db.RelacionesCausales.Count(r => r.InvestigacionId == inv.Id)); Assert.Equal(2, db.MedidasInvestigacion.Count(m => m.InvestigacionId == inv.Id));
            if (inv.Estado == "Cerrada") { Assert.True(inv.CerradaEn >= inv.IniciadaEn); Assert.True(inv.CerradaEn <= DateTimeOffset.UtcNow); Assert.True(inv.RelatoFinal.Length >= 20); }
            if (caso.Estado is EstadoCaso.EnSeguimiento or EstadoCaso.Cerrado) { Assert.True(inv.PlanIniciadoEn >= inv.CerradaEn); Assert.True(inv.PlanIniciadoEn <= DateTimeOffset.UtcNow); Assert.Equal(2, db.Medidas.Count(m => m.CasoId == caso.Id && m.Activa)); var fechaPlan = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(inv.PlanIniciadoEn!.Value, "Pacific SA Standard Time").DateTime); Assert.All(db.Medidas.Where(m => m.CasoId == caso.Id).ToArray(), m => Assert.True(m.Plazo >= fechaPlan)); }
            else Assert.False(db.Medidas.Any(m => m.CasoId == caso.Id));
            if (caso.Estado == EstadoCaso.Cerrado) { Assert.True(caso.CerradoEn >= inv.PlanIniciadoEn); Assert.True(caso.CerradoEn <= DateTimeOffset.UtcNow); Assert.All(db.Medidas.Where(m => m.CasoId == caso.Id).ToArray(), m => Assert.True(m.Completada)); }
        }
        var conteos = new[] { db.Casos.Count(), db.Investigaciones.Count(), db.Auditorias.Count() };
        DatosDemostracion.Cargar(db);
        Assert.Equal(conteos, new[] { db.Casos.Count(), db.Investigaciones.Count(), db.Auditorias.Count() }); Assert.True(db.Casos.Any(c => c.Id == previo.Id));
        Assert.All(db.Trabajadores.ToArray(), t => Assert.True(Denuncias.RutValido(t.Run!)));
        Assert.All(db.Empleadores.ToArray(), e => Assert.True(Denuncias.RutValido(e.Rut)));
    }
}
