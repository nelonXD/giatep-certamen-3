using Giatep.Api.Models;
using Giatep.Api.ValidacionesDTO;
using Giatep.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Giatep.Api.Repositories;

public sealed class CasoSqlRepository(GiatepDbContext db) : ICasoRepository
{
    public void CompletarMedida(Guid casoId, Guid medidaId)
    {
        var medida = db.Medidas.SingleOrDefault(m => m.CasoId == casoId && m.Id == medidaId && m.Activa)
            ?? throw new KeyNotFoundException("La medida no existe.");
        // Actualizar solo esta medida evita reponer valores antiguos de otras acciones.
        medida.Completada = true;
        db.SaveChanges();
    }

    private static Caso Convertir(CasoRegistro row) => new(row.Id, row.Codigo, row.Tipo,
        row.Establecimiento, row.Fecha, row.Relato, row.Estado, row.CreadoEn,
        row.Medidas.OrderBy(m => m.Plazo).ThenBy(m => m.Id)
            .Select(m => new Medida(m.Id, m.Descripcion, m.Responsable, m.Plazo, m.Completada, m.UsuarioResponsableId)).ToArray(), row.EstablecimientoId, row.TrabajadorId, row.CerradoEn, DenunciaAlmacen.Obtener(row), row.EmpleadorId);

    public IReadOnlyList<Caso> Listar() => db.Casos.AsNoTracking().Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Include(c => c.Medidas.Where(m => m.Activa))
        .OrderByDescending(c => c.CreadoEn).ToList().Select(Convertir).ToArray();

    public Caso? Buscar(Guid id)
    {
        var row = db.Casos.AsNoTracking().Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Include(c => c.Medidas.Where(m => m.Activa)).SingleOrDefault(c => c.Id == id);
        return row is null ? null : Convertir(row);
    }

    public void Guardar(Caso caso)
    {
        var row = db.Casos.Include(c => c.Diat).Include(c => c.Diep).Include(c => c.Trabajador).Include(c => c.Empleador).Include(c => c.Medidas).SingleOrDefault(c => c.Id == caso.Id);
        if (row is null)
        {
            row = new CasoRegistro { Id = caso.Id };
            db.Casos.Add(row);
        }
        var cambiaDenuncia = !DenunciaAlmacen.Equivale(DenunciaAlmacen.Obtener(row), caso.Denuncia) || row.TrabajadorId != caso.TrabajadorId;
        row.Codigo = caso.Codigo;
        row.Tipo = caso.Tipo;
        row.Establecimiento = caso.Establecimiento;
        row.Fecha = caso.Fecha;
        row.Relato = caso.Relato;

        row.Estado = caso.Estado;
        row.CreadoEn = caso.CreadoEn;
        row.EstablecimientoId = caso.EstablecimientoId;
        row.TrabajadorId = caso.TrabajadorId;
        row.CerradoEn = caso.CerradoEn;
        if (caso.Denuncia is not null && (cambiaDenuncia || (row.Diat is null && row.Diep is null))) DenunciaAlmacen.Guardar(db, row, caso.Denuncia);
        foreach (var medida in caso.Medidas)
        {
            var existing = row.Medidas.SingleOrDefault(m => m.Id == medida.Id);
            if (existing is null)
            {
                existing = new MedidaRegistro { Id = medida.Id, CasoId = caso.Id };
                // Marcar explícitamente Added: los identificadores se asignan en el dominio.
                db.Medidas.Add(existing);
                row.Medidas.Add(existing);
            }
            existing.Descripcion = medida.Descripcion;
            existing.Responsable = medida.Responsable;
            existing.Plazo = medida.Plazo;
            existing.Completada = medida.Completada;
            existing.UsuarioResponsableId = medida.UsuarioResponsableId;
        }
        db.SaveChanges();
    }
}
