import { CheckCircle2, AlertCircle, Clock3, FileText, ArrowUpRight, Building2, Calendar } from 'lucide-react';
import type { Caso } from '../../types/caso.types';

const fechaCorta = (f: string) => f
  ? new Intl.DateTimeFormat('es-CL', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${f}T12:00:00Z`))
  : '—';

export function HorizonComplexTable({ casos, loading }: { casos: Caso[]; loading?: boolean }) {
  const items = casos.slice(0, 6);
  return <section className="dash-horizon-card dash-horizon-table" aria-labelledby="horizon-table-title">
    <header className="dash-horizon-card-header">
      <div><span className="dash-category-tag">ACTIVIDAD</span><h2 id="horizon-table-title">Casos recientes</h2><p className="dash-horizon-subtitle">Últimos casos registrados.</p></div>
      <a className="dash-horizon-action-link" href="#/casos">Ver todos los casos <ArrowUpRight size={14} aria-hidden="true"/></a>
    </header>
    <div className="dash-horizon-table-wrap"><table className="dash-horizon-table-elem">
      <caption className="sr-only">Casos recientes con establecimiento, tipo, fecha y estado</caption>
      <thead><tr><th scope="col">Caso</th><th scope="col">Establecimiento</th><th scope="col">Tipo</th><th scope="col">Fecha</th><th scope="col">Estado</th></tr></thead>
      <tbody>{loading ? <tr><td colSpan={5} className="dash-horizon-empty-cell">Cargando casos…</td></tr> : !items.length ? <tr><td colSpan={5} className="dash-horizon-empty-cell">No hay casos recientes.</td></tr> : items.map(c => (<tr key={c.id}>
        <td><a href="#/casos" className="dash-horizon-code-badge" title={`Ver detalle de ${c.codigo}`}>{c.codigo}</a></td>
        <td><span className="dash-horizon-center-name" title={c.establecimiento}><Building2 size={13} className="dash-horizon-subicon" aria-hidden="true"/>{c.establecimiento}</span></td>
        <td><span className={`dash-horizon-type-chip ${c.tipo === 'Accidente' ? 'accidente' : 'diep'}`}>{c.tipo === 'Accidente' ? 'Accidente' : 'DIEP'}</span></td>
        <td><span className="dash-horizon-date"><Calendar size={13} className="dash-horizon-subicon" aria-hidden="true"/>{fechaCorta(c.fecha)}</span></td>
        <td><span className={`dash-horizon-status-pill ${c.estado}`}>
          {c.estado === 'Cerrado' && <CheckCircle2 size={13} aria-hidden="true"/>}{c.estado === 'EnSeguimiento' && <Clock3 size={13} aria-hidden="true"/>}{c.estado === 'EnInvestigacion' && <AlertCircle size={13} aria-hidden="true"/>}{c.estado === 'Borrador' && <FileText size={13} aria-hidden="true"/>}
          {c.estado === 'EnSeguimiento' ? 'En seguimiento' : c.estado === 'EnInvestigacion' ? 'En investigación' : c.estado}
        </span></td>
      </tr>))}</tbody>
    </table></div>
  </section>;
}

export function HorizonTaskCard({ href }: { href: string }) {
  return <section className="dash-horizon-card dash-horizon-tasks" aria-labelledby="horizon-tasks-title">
    <header className="dash-horizon-card-header"><div><span className="dash-category-tag">MEDIDAS</span><h2 id="horizon-tasks-title">Seguimiento de medidas</h2><p className="dash-horizon-subtitle">Consulta estado, responsables y plazos registrados.</p></div></header>
    <div className="dash-measure-link-card"><p>Revisa las medidas activas asociadas a los casos.</p><a className="dash-horizon-action-link" href={href}>Ver medidas <ArrowUpRight size={14} aria-hidden="true"/></a></div>
  </section>;
}
