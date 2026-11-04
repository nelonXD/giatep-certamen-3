using Giatep.Api.Models;
namespace Giatep.Api.Repositories;
public interface ICasoRepository
{
    IReadOnlyList<Caso> Listar();
    Caso? Buscar(Guid id);
    void Guardar(Caso caso);
    void CompletarMedida(Guid casoId, Guid medidaId);
}
