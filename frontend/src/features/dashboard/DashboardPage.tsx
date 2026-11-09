import { useEffect, useState } from 'react';
import { RefreshCw, ArrowUpRight, CalendarRange, ClipboardList, CircleCheck, Clock3, ListChecks, TriangleAlert, SlidersHorizontal, Building2, X, RotateCcw } from 'lucide-react';
import { useLoad } from '../../hooks/useLoad';
import { useAuth } from '../../context/AuthContext';
import { Field, Select, Loading, Message, today } from '../../components/common/UI';
import type { Opciones } from '../../components/common/UI';
import { EvolucionMensual, ComposicionEventos, FlujoCasos, CasosCentros } from './DashboardCharts';
import type { Serie } from './DashboardCharts';
import './dashboard.css';

interface Indicador { nombre: string; valor: number; unidad: string; formula: string; pregunta: string }
interface Dashboard { indicadores: Indicador[]; meses: Serie; tipos: Serie; establecimientos: Serie; estados: Serie; interpretacion: string; actualizado: string }
type Filtros = { desde: string; hasta: string; centro: string };
type Dimension = 'mes' | 'tipoCaso' | 'estado' | 'centroId';
type Selecciones = Partial<Record<Dimension, string>>;
function periodo(meses: number): Filtros {
  const hasta = today(), [year, month] = hasta.split('-').map(Number);
  const inicio = new Date(Date.UTC(year, month - meses, 1)).toISOString().slice(0, 10);
  return { desde: inicio, hasta, centro: '' };
}
function mesesEntre(desde: string, hasta: string) {
  const [yearInicio, mesInicio] = desde.split('-').map(Number), [yearFin, mesFin] = hasta.split('-').map(Number);
  const cursor = new Date(Date.UTC(yearInicio, mesInicio - 1, 1)), fin = new Date(Date.UTC(yearFin, mesFin - 1, 1));
  const meses: string[] = [];
  while (cursor <= fin && meses.length < 1200) { meses.push(cursor.toISOString().slice(0, 7)); cursor.setUTCMonth(cursor.getUTCMonth() + 1); }
  return meses;
}
const numero = (n: number) => new Intl.NumberFormat('es-CL', { maximumFractionDigits: 1 }).format(n);
const fechaCorta = (f: string) => new Intl.DateTimeFormat('es-CL', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${f}T12:00:00Z`));
const principales = [
  { nombre: 'Casos registrados', icono: ClipboardList, clase: 'total', detalle: 'Eventos dentro del período seleccionado' },
  { nombre: 'Casos abiertos', icono: Clock3, clase: 'abiertos', detalle: 'Expedientes que aún requieren gestión' },
  { nombre: 'Medidas cumplidas', icono: ListChecks, clase: 'cumplidas', detalle: 'Acciones verificadas como completadas' },
  { nombre: 'Medidas vencidas', icono: TriangleAlert, clase: 'vencidas', detalle: 'Acciones pendientes fuera de plazo' }
];

export function DashboardPage() {
  const user = useAuth();
  const [filtros, setFiltros] = useState<Filtros>(() => periodo(12));
  const [borrador, setBorrador] = useState<Filtros>(filtros), [rango, setRango] = useState<number | null>(12);
  const [selecciones, setSelecciones] = useState<Selecciones>({});
  const options = useLoad<Opciones>('/opciones');
  const periodoSeleccionado = selecciones.mes ? (() => {
    const [year, month] = selecciones.mes!.split('-').map(Number);
    const inicioMes = `${selecciones.mes}-01`, finMes = new Date(Date.UTC(year, month, 0)).toISOString().slice(0, 10);
    return { desde: inicioMes < filtros.desde ? filtros.desde : inicioMes, hasta: finMes > filtros.hasta ? filtros.hasta : finMes };
  })() : { desde: filtros.desde, hasta: filtros.hasta };
  const establecimientoId = selecciones.centroId || filtros.centro;
  const mesesDisponibles = mesesEntre(filtros.desde, filtros.hasta);
  const query = new URLSearchParams({ desde: periodoSeleccionado.desde, hasta: periodoSeleccionado.hasta, ...(establecimientoId ? { establecimientoId } : {}), ...(selecciones.estado ? { estado: selecciones.estado } : {}), ...(selecciones.tipoCaso ? { tipoCaso: selecciones.tipoCaso } : {}) });
  const { data, error, loading, refresh } = useLoad<Dashboard>(`/dashboard?${query}`);
  useEffect(() => {
    const timer = window.setInterval(refresh, 60000);
    window.addEventListener('focus', refresh);
    return () => { window.clearInterval(timer); window.removeEventListener('focus', refresh); };
  }, [refresh]);
  const indicador = (nombre: string) => data?.indicadores.find(k => k.nombre === nombre);
  const total = indicador('Casos registrados')?.valor ?? 0;
  const pendientes = indicador('Medidas vencidas')?.valor ?? 0;
  const cambiado = JSON.stringify(borrador) !== JSON.stringify(filtros);
  const centroNombre = options.data?.establecimientos.find(c => c.id === establecimientoId)?.nombre || (user.rol === 'Jefatura' ? 'Mi establecimiento' : 'Todos los establecimientos');
  const enlaceReporte = (extras: Record<string, string> = {}) => {
    const parametros = new URLSearchParams(query); parametros.set('reporte', 'operativo');
    Object.entries(extras).forEach(([key, value]) => parametros.set(key, value));
    return `#/reportes?${parametros}`;
  };
  const seleccionar = (dimension: Dimension, valor: string) => setSelecciones(actuales => {
    const siguientes = { ...actuales };
    if (siguientes[dimension] === valor) delete siguientes[dimension]; else siguientes[dimension] = valor;
    return siguientes;
  });
  const limpiarSelecciones = () => setSelecciones({});
  const nombreEstado = (estado: string) => ({ Borrador: 'Borrador', EnInvestigacion: 'En investigación', EnSeguimiento: 'En seguimiento', Cerrado: 'Cerrado', Abierto: 'Abiertos' } as Record<string, string>)[estado] || estado;
  function rapido(meses: number) {
    const siguiente = { ...periodo(meses), centro: borrador.centro };
    setBorrador(siguiente); setFiltros(siguiente); setRango(meses); limpiarSelecciones();
  }
  return <div className="dash-workspace" aria-busy={loading}>
    <header className="dash-heading"><div><p className="eyebrow">PANORAMA PREVENTIVO</p><h1>Comprende. Prioriza. Actúa.</h1><p>Selecciona un dato en los gráficos para cruzar y actualizar el panel completo.</p></div>
      <div className="dash-heading-actions"><a className="secondary" href="#/reportes">Ver reportes<ArrowUpRight size={16} aria-hidden="true"/></a><button className="dash-refresh" disabled={loading} onClick={refresh}><RefreshCw size={16} aria-hidden="true"/>Actualizar</button></div>
    </header>
    <form className="dash-filters" aria-label="Filtros del dashboard" onSubmit={e => {
      e.preventDefault(); setFiltros(borrador); setRango(null); limpiarSelecciones();
    }}>
      <div className="dash-filter-top"><span><SlidersHorizontal size={17} aria-hidden="true"/>Período de análisis</span><div className="dash-presets" role="group" aria-label="Períodos rápidos">
        {[{ meses: 1, texto: 'Este mes' }, { meses: 3, texto: '3 meses' }, { meses: 12, texto: '12 meses' }].map(p => <button type="button" key={p.meses} aria-pressed={rango === p.meses && !cambiado} onClick={() => rapido(p.meses)}>{p.texto}</button>)}
      </div></div>
      <div className="dash-filter-fields"><Field label="Desde" type="date" required value={borrador.desde} max={borrador.hasta || today()} onChange={e => setBorrador({ ...borrador, desde: e.target.value })}/>
        <Field label="Hasta" type="date" required value={borrador.hasta} min={borrador.desde} max={today()} onChange={e => setBorrador({ ...borrador, hasta: e.target.value })}/>
        <Select label="Establecimiento" value={borrador.centro} onChange={e => setBorrador({ ...borrador, centro: e.target.value })}><option value="">{user.rol === 'Jefatura' ? 'Mi establecimiento' : 'Todos los establecimientos'}</option>{options.data?.establecimientos.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</Select>
        <button className="primary" type="submit" disabled={!cambiado}>Aplicar filtros</button>
      </div>
      {cambiado && <p className="dash-filter-pending" role="status">Tienes cambios de filtros por aplicar.</p>}
    </form>
    <div className="dash-context"><span><CalendarRange size={15} aria-hidden="true"/>{fechaCorta(filtros.desde)} — {fechaCorta(filtros.hasta)}</span><span><Building2 size={15} aria-hidden="true"/>{centroNombre}</span>
      <span className="dash-updated" role="status">{loading ? 'Actualizando datos…' : data ? `Actualizado ${new Intl.DateTimeFormat('es-CL', { timeStyle: 'short', timeZone: 'America/Santiago' }).format(new Date(data.actualizado))}` : 'Datos no disponibles'}</span>
    </div>
    {Object.values(selecciones).some(Boolean) && <div className="dash-active-filters" aria-label="Filtros aplicados desde los gráficos" aria-live="polite"><span className="dash-active-label">Explorando</span>{selecciones.mes && <button type="button" onClick={() => seleccionar('mes', selecciones.mes!)}>Mes: {new Intl.DateTimeFormat('es-CL', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${selecciones.mes}-01T12:00:00Z`))}<X size={14}/></button>}{selecciones.tipoCaso && <button type="button" onClick={() => seleccionar('tipoCaso', selecciones.tipoCaso!)}>Tipo: {selecciones.tipoCaso === 'Accidente' ? 'Accidente' : 'Enfermedad profesional'}<X size={14}/></button>}{selecciones.estado && <button type="button" onClick={() => seleccionar('estado', selecciones.estado!)}>Estado: {nombreEstado(selecciones.estado)}<X size={14}/></button>}{selecciones.centroId && <button type="button" onClick={() => seleccionar('centroId', selecciones.centroId!)}>Centro: {options.data?.establecimientos.find(c => c.id === selecciones.centroId)?.nombre || 'Seleccionado'}<X size={14}/></button>}<button type="button" className="dash-clear-filters" onClick={limpiarSelecciones}><RotateCcw size={14}/>Limpiar selecciones</button></div>}
    <Message error={error || options.error}/>
    {loading && !data ? <div className="dash-loading"><Loading/></div> : error ? <section className="dash-empty"><TriangleAlert size={30} aria-hidden="true"/><h2>No pudimos cargar el panorama</h2><p>Los filtros se conservaron. Puedes volver a intentarlo.</p><button className="secondary" onClick={refresh}>Volver a intentar</button></section> : data && <>
      <section className="dash-kpis" aria-label="Indicadores de gestión">{principales.map(p => {
        const k = indicador(p.nombre); if (!k) return null;
        const drilldown: Record<string, string> = p.clase === 'abiertos' ? { estado: 'Abierto' } : p.clase === 'cumplidas' ? { reporte: 'parametrizable', estadoMedida: 'Completada' } : p.clase === 'vencidas' ? { reporte: 'parametrizable', estadoMedida: 'Vencida' } : {};
        return <article key={p.nombre} className={`dash-kpi dash-kpi-${p.clase}${p.clase === 'vencidas' && k.valor > 0 ? ' attention' : ''}`}>
          <div className="dash-kpi-label"><span>{k.nombre}</span><p.icono size={19} aria-hidden="true"/></div>
          <div className="dash-kpi-value"><strong>{!total && k.unidad === '%' ? '—' : numero(k.valor)}</strong><span>{k.unidad}</span></div>
          <p>{!total && k.unidad === '%' ? 'Sin casos para calcular el indicador' : p.detalle}</p>
          {total > 0 && k.unidad === '%' && <div className="dash-kpi-progress" aria-hidden="true"><span style={{ width: `${Math.max(0, Math.min(100, k.valor))}%` }}/></div>}
          <details><summary>Cómo se calcula</summary><p>{k.pregunta}</p><p>{k.formula}</p></details>
          <a className="dash-kpi-link" href={enlaceReporte(drilldown)} aria-label={`Ver detalle: ${p.nombre}`}>Ver detalle <ArrowUpRight size={14} aria-hidden="true"/></a>
        </article>;
      })}</section>
      {!total ? <section className="dash-empty"><ClipboardList size={32} aria-hidden="true"/><h2>No hay casos en este período</h2><p>Amplía las fechas o selecciona otro establecimiento para consultar su información.</p><button className="secondary" onClick={() => rapido(12)}>Consultar últimos 12 meses</button></section> : <>
        <div className="dash-primary-charts"><EvolucionMensual key={query.toString()} data={data.meses} mesesDisponibles={mesesDisponibles} seleccionado={selecciones.mes} onSelect={mes => seleccionar('mes', mes)}/><ComposicionEventos data={data.tipos} seleccionado={selecciones.tipoCaso} onSelect={tipoCaso => seleccionar('tipoCaso', tipoCaso)}/></div>
        <div className="dash-secondary-charts"><FlujoCasos data={data.estados} seleccionado={selecciones.estado} onSelect={estado => seleccionar('estado', estado)}/><CasosCentros data={data.establecimientos} idCentro={nombre => options.data?.establecimientos.find(c => c.nombre === nombre)?.id} seleccionado={selecciones.centroId} onSelect={centroId => seleccionar('centroId', centroId)}/></div>
        <section className="dash-closure" aria-label="Indicadores de cierre">{[{ nombre: 'Casos cerrados', icono: CircleCheck, detalle: 'Proporción de expedientes finalizados' }, { nombre: 'Tiempo de cierre', icono: Clock3, detalle: 'Promedio desde el evento hasta su cierre' }].map(p => {
          const k = indicador(p.nombre); if (!k) return null;
          const sinCierres = p.nombre === 'Tiempo de cierre' && !data.estados.some(e => e.etiqueta === 'Cerrado' && e.valor > 0);
          return <div className="dash-closure-item" key={p.nombre}><p.icono size={22} aria-hidden="true"/><div><span>{p.nombre}</span><strong>{sinCierres ? '—' : numero(k.valor)}<small>{k.unidad}</small></strong><p>{sinCierres ? 'Aún no hay casos cerrados en el período' : p.detalle}</p><details><summary>Ver fórmula</summary><p>{k.formula}</p></details></div></div>;
        })}</section>
        <aside className={`dash-priority${pendientes > 0 ? ' has-overdue' : ''}`}><span className="dash-priority-icon">{pendientes > 0 ? <TriangleAlert size={24} aria-hidden="true"/> : <ListChecks size={24} aria-hidden="true"/>}</span><div><p className="dash-overline">PARA TU PRÓXIMA REVISIÓN</p><h2>{pendientes > 0 ? `${numero(pendientes)} % de las medidas están fuera de plazo` : 'Revisa la distribución de tus casos'}</h2><p>{data.interpretacion}</p></div><a className="secondary" href={user.rol === 'Administrador' ? '#/reportes' : '#/planes'}>{user.rol === 'Administrador' ? 'Explorar reportes' : 'Revisar planes'}<ArrowUpRight size={16} aria-hidden="true"/></a></aside>
      </>}
      <p className="dash-footnote">Actualización automática cada minuto. Los filtros corresponden a la fecha del evento. Las medidas pertenecen a los casos del período; un porcentaje sin denominador se registra como 0 en la fuente.</p>
    </>}
  </div>;
}
