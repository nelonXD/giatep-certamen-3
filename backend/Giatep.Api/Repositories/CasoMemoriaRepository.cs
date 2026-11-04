using System.Collections.Concurrent;
using Giatep.Api.Models;
namespace Giatep.Api.Repositories;

// Adaptador temporal de desarrollo. Se sustituirá por EF Core y SQL Server.
public sealed class CasoMemoriaRepository : ICasoRepository
{
    private readonly ConcurrentDictionary<Guid, Caso> casos = new();
    public IReadOnlyList<Caso> Listar() => casos.Values.OrderByDescending(c => c.CreadoEn).ToArray();
    public Caso? Buscar(Guid id) => casos.GetValueOrDefault(id);
    public void Guardar(Caso caso) => casos[caso.Id] = caso;
    public void CompletarMedida(Guid casoId, Guid medidaId)
    {
        while (true)
        {
            var caso = Buscar(casoId) ?? throw new KeyNotFoundException("El caso no existe.");
            if (!caso.Medidas.Any(m => m.Id == medidaId)) throw new KeyNotFoundException("La medida no existe.");
            var actualizado = caso with { Medidas = caso.Medidas.Select(m => m.Id == medidaId ? m with { Completada = true } : m).ToArray() };
            if (casos.TryUpdate(casoId, actualizado, caso)) return;
        }
    }
}
