import { CheckCircle2, AlertCircle, Clock3, FileText, ArrowUpRight, CheckSquare, Square, Building2, Calendar } from 'lucide-react';
import type { Caso } from '../../types/caso.types';

const fechaCorta = (f: string) => {
  if (!f) return '—';
  try {
    return new Intl.DateTimeFormat('es-CL', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${f}T12:00:00Z`));
  } catch {
    return f;
  }
};

interface ComplexTableProps {
  casos: Caso[];
  loading?: boolean;
}

export function HorizonComplexTable({ casos, loading }: ComplexTableProps) {
  const items = casos.slice(0, 6);

  return (
    <section className="dash-horizon-card dash-horizon-table" aria-labelledby="horizon-table-title">
      <header className="dash-horizon-card-header">
        <div>
          <span className="dash-category-tag">PANEL OPERATIVO</span>
          <h2 id="horizon-table-title">Expedientes Recientes y Estado SUSESO</h2>
          <p className="dash-horizon-subtitle">Trazabilidad de los últimos incidentes ingresados a la red asistencial.</p>
        </div>
        <a className="dash-horizon-action-link" href="#/casos">
          Ver todos los casos
          <ArrowUpRight size={14} aria-hidden="true" />
        </a>
      </header>

      <div className="dash-horizon-table-wrap">
        <table className="dash-horizon-table-elem">
          <caption className="sr-only">Lista de expedientes recientes con establecimiento, tipo, fecha y estado</caption>
          <thead>
            <tr>
              <th scope="col">EXPEDIENTE</th>
              <th scope="col">ESTABLECIMIENTO</th>
              <th scope="col">TIPO</th>
              <th scope="col">FECHA EVENTO</th>
              <th scope="col">ESTADO</th>
              <th scope="col">RESOLUCIÓN</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="dash-horizon-empty-cell">Cargando expedientes…</td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={6} className="dash-horizon-empty-cell">No se encontraron expedientes en el período.</td>
              </tr>
            ) : (
              items.map((c) => {
                const totalMedidas = c.medidas?.length || 0;
                const cumplidas = c.medidas?.filter((m) => m.completada).length || 0;
                const pct = totalMedidas ? Math.round((cumplidas / totalMedidas) * 100) : c.estado === 'Cerrado' ? 100 : 0;

                return (
                  <tr key={c.id}>
                    <td>
                      <a href="#/casos" className="dash-horizon-code-badge" title={`Ver detalle de ${c.codigo}`}>
                        {c.codigo}
                      </a>
                    </td>
                    <td>
                      <span className="dash-horizon-center-name" title={c.establecimiento}>
                        <Building2 size={13} className="dash-horizon-subicon" aria-hidden="true" />
                        {c.establecimiento}
                      </span>
                    </td>
                    <td>
                      <span className={`dash-horizon-type-chip ${c.tipo === 'Accidente' ? 'accidente' : 'diep'}`}>
                        {c.tipo === 'Accidente' ? 'Accidente' : 'DIEP'}
                      </span>
                    </td>
                    <td>
                      <span className="dash-horizon-date">
                        <Calendar size={13} className="dash-horizon-subicon" aria-hidden="true" />
                        {fechaCorta(c.fecha)}
                      </span>
                    </td>
                    <td>
                      <span className={`dash-horizon-status-pill ${c.estado}`}>
                        {c.estado === 'Cerrado' && <CheckCircle2 size={13} aria-hidden="true" />}
                        {c.estado === 'EnSeguimiento' && <Clock3 size={13} aria-hidden="true" />}
                        {c.estado === 'EnInvestigacion' && <AlertCircle size={13} aria-hidden="true" />}
                        {c.estado === 'Borrador' && <FileText size={13} aria-hidden="true" />}
                        {c.estado === 'EnSeguimiento' ? 'En seguimiento' : c.estado === 'EnInvestigacion' ? 'En investigación' : c.estado}
                      </span>
                    </td>
                    <td>
                      <div className="dash-horizon-progress-cell">
                        <div className="dash-horizon-bar-track">
                          <div
                            className={`dash-horizon-bar-fill ${pct === 100 ? 'done' : ''}`}
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                        <span className="dash-horizon-pct">{totalMedidas > 0 ? `${cumplidas}/${totalMedidas}` : `${pct}%`}</span>
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

interface TaskCardProps {
  casos: Caso[];
  pendientesVencidas?: number;
}

export function HorizonTaskCard({ casos, pendientesVencidas = 0 }: TaskCardProps) {
  // Extraer medidas reales de los casos
  const medidasReales = casos.flatMap((c) =>
    (c.medidas || []).map((m) => ({
      id: m.id,
      descripcion: m.descripcion,
      responsable: m.responsable || 'Comité Paritario',
      plazo: m.plazo,
      completada: m.completada,
      centro: c.establecimiento,
    }))
  );

  // Tareas predeterminadas de control normativo SUSESO si aún no hay muchas registradas
  const checkpointsSuseso = [
    {
      id: 'chk-1',
      descripcion: 'Verificación de medidas correctivas del Comité Paritario (CPHS)',
      responsable: 'Experto en Prevención',
      plazo: '2026-11-15',
      completada: pendientesVencidas === 0,
      centro: 'Red Asistencial',
    },
    {
      id: 'chk-2',
      descripcion: 'Revisión y validación de expedientes de investigación en curso',
      responsable: 'Jefatura de Salud Ocupacional',
      plazo: '2026-11-20',
      completada: false,
      centro: 'Dirección de Salud',
    },
    {
      id: 'chk-3',
      descripcion: 'Actualización de catastro de riesgos y dotación expuesta SUSESO',
      responsable: 'Administrador SST',
      plazo: '2026-11-30',
      completada: true,
      centro: 'Nivel Central',
    },
  ];

  const tareas = [...medidasReales, ...checkpointsSuseso].slice(0, 4);

  return (
    <section className="dash-horizon-card dash-horizon-tasks" aria-labelledby="horizon-tasks-title">
      <header className="dash-horizon-card-header">
        <div>
          <span className="dash-category-tag">COMPROMISOS SST</span>
          <h2 id="horizon-tasks-title">Medidas y Tareas Prioritarias</h2>
          <p className="dash-horizon-subtitle">Plazos y mitigaciones del plan de acción Circular SUSESO 3336.</p>
        </div>
        <a className="dash-horizon-action-link" href="#/planes">
          Ver planes
          <ArrowUpRight size={14} aria-hidden="true" />
        </a>
      </header>

      <ul className="dash-horizon-task-list">
        {tareas.map((t) => {
          const hoy = new Date().toISOString().slice(0, 10);
          const vencida = !t.completada && t.plazo < hoy;

          return (
            <li key={t.id} className={`dash-horizon-task-item ${t.completada ? 'completed' : ''} ${vencida ? 'overdue' : ''}`}>
              <div className="dash-horizon-task-check" aria-hidden="true">
                {t.completada ? (
                  <CheckSquare size={18} className="dash-horizon-check-icon checked" />
                ) : (
                  <Square size={18} className="dash-horizon-check-icon" />
                )}
              </div>
              <div className="dash-horizon-task-content">
                <span className="dash-horizon-task-desc">{t.descripcion}</span>
                <div className="dash-horizon-task-meta">
                  <span>{t.responsable}</span>
                  <span className="dash-horizon-bullet">•</span>
                  <span>{t.centro}</span>
                  <span className="dash-horizon-bullet">•</span>
                  <span className="dash-horizon-due">Plazo: {fechaCorta(t.plazo)}</span>
                </div>
              </div>
              <div className="dash-horizon-task-badge-wrap">
                <span className={`dash-horizon-task-badge ${t.completada ? 'completed' : vencida ? 'overdue' : 'pending'}`}>
                  {t.completada ? 'Cumplida' : vencida ? 'Vencida' : 'En plazo'}
                </span>
              </div>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
