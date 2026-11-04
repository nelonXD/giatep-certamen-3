using Giatep.Api.Models;
using Giatep.Api.Repositories;
using Giatep.Api.Services;
using Giatep.Api.ValidacionesDTO;
using System.ComponentModel.DataAnnotations;
using Xunit;
namespace Giatep.Tests;

public class CasoServiceTests
{
    private readonly CasoService service = new(new CasoMemoriaRepository());
    private Caso Crear() => service.Crear(new("Centro sintético Norte", TipoCaso.Accidente,
        DateOnly.FromDateTime(DateTime.Today), "Relato ficticio de un accidente durante una actividad de prueba."));
    [Fact] public void NoPermiteSaltarDeBorradorACerrado()
    {
        var caso = Crear();
        Assert.Throws<ReglaNegocioException>(() => service.CambiarEstado(caso.Id, EstadoCaso.Cerrado));
        Assert.Equal(EstadoCaso.Borrador, service.Obtener(caso.Id).Estado);
    }
    [Fact] public void NoPermiteSeguimientoSinMedidas()
    {
        var caso = Crear(); service.CambiarEstado(caso.Id, EstadoCaso.EnInvestigacion);
        Assert.Throws<ReglaNegocioException>(() => service.CambiarEstado(caso.Id, EstadoCaso.EnSeguimiento));
    }
    [Fact] public void ExigeCompletarTodasLasMedidasAntesDelCierre()
    {
        var caso = Crear(); service.CambiarEstado(caso.Id, EstadoCaso.EnInvestigacion);
        service.AgregarMedida(caso.Id, new("Instalar señalización de prueba", "Responsable ficticio", caso.Fecha));
        var seguimiento = service.AgregarMedida(caso.Id, new("Revisar procedimiento de prueba", "Responsable ficticio", caso.Fecha));
        service.CambiarEstado(caso.Id, EstadoCaso.EnSeguimiento);
        service.CompletarMedida(caso.Id, seguimiento.Medidas[0].Id);
        Assert.Throws<ReglaNegocioException>(() => service.CambiarEstado(caso.Id, EstadoCaso.Cerrado));
        service.CompletarMedida(caso.Id, seguimiento.Medidas[1].Id);
        Assert.Equal(EstadoCaso.Cerrado, service.CambiarEstado(caso.Id, EstadoCaso.Cerrado).Estado);
        Assert.Throws<ReglaNegocioException>(() => service.AgregarMedida(caso.Id, new("Medida fuera de plazo", "Responsable ficticio", caso.Fecha)));
    }
    [Fact] public void RechazaFechaFutura()
    {
        Assert.Throws<ValidationException>(() => service.Crear(new("Centro de prueba", TipoCaso.Accidente,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Relato suficientemente extenso para validar la fecha.")));
    }
    [Fact] public void RechazaMedidaAjenaAlCaso()
    {
        var caso = Crear(); service.CambiarEstado(caso.Id, EstadoCaso.EnInvestigacion);
        service.AgregarMedida(caso.Id, new("Medida de control de prueba", "Responsable ficticio", caso.Fecha));
        service.CambiarEstado(caso.Id, EstadoCaso.EnSeguimiento);
        Assert.Throws<KeyNotFoundException>(() => service.CompletarMedida(caso.Id, Guid.NewGuid()));
    }
}
