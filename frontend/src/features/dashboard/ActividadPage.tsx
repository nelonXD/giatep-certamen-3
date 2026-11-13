import { Activity, ClipboardList, AlertCircle, CheckCircle2, ArrowUpRight } from 'lucide-react';
import { useLoad } from '../../hooks/useLoad';
import { useAuth } from '../../context/AuthContext';
import { Loading, Message } from '../../components/common/UI';
import type { Caso, Pagina } from '../../types/caso.types';
import { HorizonComplexTable, HorizonTaskCard } from './HorizonCards';
import './dashboard.css';

interface Indicador { nombre: string; valor: number; formula: string; cantidad?: number | null; denominador?: number | null }
interface Dashboard { indicadores: Indicador[]; actualizado: string }
const numero = (n: number) => new Intl.NumberFormat('es-CL').format(n);

export function ActividadPage() {
  const user = useAuth();
  const { data: dashData, loading: dashLoading, error: dashError } = useLoad<Dashboard>('/dashboard');
  const { data: casosData, loading: casosLoading, error: casosError } = useLoad<Pagina<Caso>>('/casos?tamano=8&orden=Fecha&descendente=true');
  const casos = casosData?.datos || [];
  const indicador = (nombre: string) => dashData?.indicadores.find(k => k.nombre === nombre);
  const abiertos = indicador('Casos abiertos')?.valor ?? casos.filter(c => c.estado !== 'Cerrado').length;
  const vencidas = indicador('Medidas vencidas');
  const cumplidas = indicador('Medidas cumplidas');
  const porcentaje = (k?: Indicador) => k?.denominador ? `${numero(k.cantidad ?? 0)} · ${numero(k.valor)}% de ${numero(k.denominador)}` : `${numero(k?.cantidad ?? 0)} medidas`;
  const enlaceMedidas = user.rol === 'Administrador' ? '#/reportes?reporte=parametrizable' : '#/planes';

  return <div className="dash-workspace" aria-busy={casosLoading || dashLoading}>
    <header className="dash-heading"><div>
      <div className="dash-badge-row"><span className="dash-badge-institutional"><Activity size={14} aria-hidden="true"/>GIATEP · Actividad</span></div>
      <h1>Actividad reciente</h1><p>Casos nuevos y estado actual de las medidas.</p>
    </div><div className="dash-heading-actions">
      {user.rol !== 'Administrador' && <a className="secondary" href="#/casos/nuevo">Registrar caso <ArrowUpRight size={16} aria-hidden="true"/></a>}
      <a className="secondary" href="#/dashboard">Ir al dashboard <ArrowUpRight size={16} aria-hidden="true"/></a>
    </div></header>
    <Message error={casosError || dashError}/>
    <section className="dash-actividad-summary-grid" aria-label="Resumen de actividad">
      <article className="dash-actividad-card"><div className="dash-kpi-icon-wrap"><ClipboardList size={22} aria-hidden="true"/></div><div><span className="dash-actividad-label">Casos abiertos</span><strong className="dash-actividad-val">{numero(abiertos)}</strong><p className="dash-actividad-desc">Estado actual · casos visibles</p></div></article>
      <article className="dash-actividad-card"><div className="dash-kpi-icon-wrap overdue"><AlertCircle size={22} aria-hidden="true"/></div><div><span className="dash-actividad-label">Medidas vencidas</span><strong className="dash-actividad-val">{numero(vencidas?.cantidad ?? 0)}</strong><p className="dash-actividad-desc">{vencidas ? porcentaje(vencidas) : 'Cargando indicador'}</p><a href={enlaceMedidas}>Ver medidas <ArrowUpRight size={13} aria-hidden="true"/></a></div></article>
      <article className="dash-actividad-card"><div className="dash-kpi-icon-wrap"><CheckCircle2 size={22} aria-hidden="true"/></div><div><span className="dash-actividad-label">Medidas cumplidas</span><strong className="dash-actividad-val">{numero(cumplidas?.cantidad ?? 0)}</strong><p className="dash-actividad-desc">{cumplidas ? porcentaje(cumplidas) : 'Cargando indicador'}</p><a href={enlaceMedidas}>Ver medidas <ArrowUpRight size={13} aria-hidden="true"/></a></div></article>
    </section>
    {casosLoading ? <div className="dash-loading"><Loading/></div> : <div className="dash-horizon-grid"><HorizonComplexTable casos={casos}/><HorizonTaskCard href={enlaceMedidas}/></div>}
  </div>;
}
